using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantCellManager
{
    private readonly PlantGrowth plant;
    private readonly GameObject seedCellPrefab;
    private readonly GameObject stemCellPrefab;
    private readonly GameObject leafCellPrefab;
    private readonly GameObject berryCellPrefab;
    private readonly float cellSpacing;

    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private List<GameObject> activeCellGameObjects = new List<GameObject>();
    public GameObject RootCellInstance { get; set; }
    public List<LeafData> LeafDataList { get; } = new List<LeafData>();

    private bool? offsetRightForPattern1 = null;

    public PlantCellManager(PlantGrowth plant, GameObject seedPrefab, GameObject stemPrefab, GameObject leafPrefab, GameObject berryPrefab, float spacing)
    {
        this.plant = plant;
        seedCellPrefab = seedPrefab;
        stemCellPrefab = stemPrefab;
        leafCellPrefab = leafPrefab;
        berryCellPrefab = berryPrefab;
        cellSpacing = spacing;
    }

    public void ReportCellDestroyed(Vector2Int coord)
    {
        if (cells.ContainsKey(coord))
        {
            PlantCellType cellType = cells[coord];

            if (cellType == PlantCellType.Leaf)
            {
                for (int i = 0; i < LeafDataList.Count; i++)
                {
                    if (LeafDataList[i].GridCoord == coord)
                    {
                        // Mark the original data as inactive instead of removing
                        LeafData updatedData = LeafDataList[i];
                        updatedData.IsActive = false;
                        LeafDataList[i] = updatedData;
                        break;
                    }
                }
            }

            cells.Remove(coord);
            // Remove the visual GameObject from tracking
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));

            // Update visuals if enabled
            if (plant.IsOutlineEnabled() && plant.VisualManager.OutlineController != null)
            {
                plant.VisualManager.OutlineController.OnPlantCellRemoved(coord);
            }
        }
    }

    public void ClearAllVisuals()
    {
        List<GameObject> cellsToClear = new List<GameObject>(activeCellGameObjects);
        foreach (GameObject cellGO in cellsToClear)
        {
            if (cellGO != null)
            {
                Object.Destroy(cellGO);
            }
        }
        activeCellGameObjects.Clear();
        cells.Clear();
        RootCellInstance = null;
    }

    public GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null)
    {
        if (cells.ContainsKey(coords))
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Trying to spawn {cellType} at occupied coord {coords}.");
            return null;
        }

        GameObject prefab = null;
        switch (cellType)
        {
            case PlantCellType.Seed: prefab = seedCellPrefab; break;
            case PlantCellType.Stem: prefab = stemCellPrefab; break;
            case PlantCellType.Leaf: prefab = leafCellPrefab; break;
            case PlantCellType.Fruit: prefab = berryCellPrefab; break;
        }

        if (prefab == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] Prefab for PlantCellType.{cellType} is null!");
            return null;
        }

        Vector2 worldPos = (Vector2)plant.transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, plant.transform);
        instance.name = $"{plant.gameObject.name}_{cellType}_{coords.x}_{coords.y}";

        // --- CORE FIX ---
        // Plant parts (leaves, stems, etc.) should not have their own GridEntity.
        // Their logical position is ALWAYS that of the root plant. By destroying their
        // personal GridEntity, we prevent them from polluting the GridPositionManager
        // with incorrect, offset positions.
        if (cellType != PlantCellType.Seed)
        {
            GridEntity partGridEntity = instance.GetComponent<GridEntity>();
            if (partGridEntity != null)
            {
                // Destroy the component immediately to prevent it from ever registering.
                Object.Destroy(partGridEntity);
            }
        }
        // --- END FIX ---

        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = plant;
        cellComp.GridCoord = coords; // This is now for visual/local logic only
        cellComp.CellType = cellType;

        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);

        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
        sorter.SetUseParentYCoordinate(cellType != PlantCellType.Seed);

        // Apply scents to fruits if needed
        if (cellType == PlantCellType.Fruit)
        {
            plant.ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }

        // Handle visual systems like shadows and outlines
        plant.VisualManager.RegisterShadowForCell(instance, cellType.ToString());
        if (plant.IsOutlineEnabled() && plant.VisualManager.OutlineController != null)
        {
            plant.VisualManager.OutlineController.OnPlantCellAdded(coords, instance);
        }

        return instance;
    }


    public List<Vector2Int> CalculateLeafPositions(Vector2Int stemPos, int stageCounter, int leafPattern)
    {
        List<Vector2Int> leafPositions = new List<Vector2Int>();
        Vector2Int leftBase = stemPos + Vector2Int.left;
        Vector2Int rightBase = stemPos + Vector2Int.right;

        switch (leafPattern)
        {
            case 0: // Symmetrical
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;
            case 1: // Offset
                if (offsetRightForPattern1 == null) offsetRightForPattern1 = Random.value < 0.5f;
                if (offsetRightForPattern1.Value) {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else {
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;
            case 2: // Alternating
                switch (stageCounter % 4)
                {
                    case 0:
                    case 2:
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase);
                        break;
                    case 1:
                        leafPositions.Add(leftBase + Vector2Int.up);
                        leafPositions.Add(rightBase);
                        break;
                    case 3:
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase + Vector2Int.up);
                        break;
                }
                break;
            case 3: // Spiral
                int spiralDir = (stageCounter % 2 == 0) ? 1 : -1;
                if (spiralDir > 0) {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else {
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;
            case 4: // Dense
                leafPositions.Add(leftBase);
                leafPositions.Add(leftBase + Vector2Int.up);
                leafPositions.Add(rightBase);
                leafPositions.Add(rightBase + Vector2Int.up);
                break;
            default: // Fallback to symmetrical
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;
        }
        return leafPositions;
    }

    public bool TryRegrowLeaf()
    {
        // Find the first inactive leaf in our data list
        int missingLeafIndex = -1;
        for (int i = 0; i < LeafDataList.Count; i++)
        {
            if (!LeafDataList[i].IsActive)
            {
                missingLeafIndex = i;
                break;
            }
        }
        if (missingLeafIndex == -1) return false; // No missing leaves to regrow

        Vector2Int leafCoord = LeafDataList[missingLeafIndex].GridCoord;
        // Check if a cell (of any type) already exists at the target location
        if (cells.ContainsKey(leafCoord)) return false;

        // Spawn a new leaf visual at the original coordinate
        GameObject newLeaf = SpawnCellVisual(PlantCellType.Leaf, leafCoord);
        if (newLeaf != null)
        {
            // Update our data to show the leaf is now active
            LeafDataList[missingLeafIndex] = new LeafData(leafCoord, true);
            return true;
        }
        return false;
    }


    public bool DoesCellExistAt(Vector2Int coord) => cells.ContainsKey(coord);

    public GameObject GetCellGameObjectAt(Vector2Int coord)
    {
        return activeCellGameObjects.FirstOrDefault(go => go != null && go.GetComponent<PlantCell>()?.GridCoord == coord);
    }

    public Dictionary<Vector2Int, PlantCellType> GetCells() => cells;
}
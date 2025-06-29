using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantCellManager
{
    readonly PlantGrowth plant;
    readonly GameObject seedCellPrefab;
    readonly GameObject stemCellPrefab;
    readonly GameObject leafCellPrefab;
    readonly GameObject berryCellPrefab;
    readonly float cellSpacing;

    readonly Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    readonly List<GameObject> activeCellGameObjects = new List<GameObject>();

    public List<LeafData> LeafDataList { get; } = new List<LeafData>();
    public GameObject RootCellInstance { get; set; }

    bool? offsetRightForPattern1 = null;

    public PlantCellManager(PlantGrowth plant, GameObject seedPrefab, GameObject stemPrefab, GameObject leafPrefab, GameObject berryPrefab, float spacing)
    {
        this.plant = plant;
        seedCellPrefab = seedPrefab;
        stemCellPrefab = stemPrefab;
        leafCellPrefab = leafPrefab;
        berryCellPrefab = berryPrefab;
        cellSpacing = spacing;
    }

    public GameObject CreateSeedCell(Vector2Int coords)
    {
        return SpawnCellVisual(PlantCellType.Seed, coords);
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
                        LeafData updatedData = LeafDataList[i];
                        updatedData.IsActive = false;
                        LeafDataList[i] = updatedData;
                        break;
                    }
                }
            }

            cells.Remove(coord);
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));

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

        if (cellType != PlantCellType.Seed)
        {
            GridEntity partGridEntity = instance.GetComponent<GridEntity>();
            if (partGridEntity != null)
            {
                Object.Destroy(partGridEntity);
            }
        }

        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = plant;
        cellComp.GridCoord = coords;
        cellComp.CellType = cellType;

        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);

        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();

        if (cellType == PlantCellType.Leaf)
        {
            LeafDataList.Add(new LeafData(coords, true));
        }

        plant.VisualManager.RegisterShadowForCell(instance, cellType.ToString());

        // FIXED: Use the new method instead of the broken property
        if (plant.IsOutlineEnabled() && plant.VisualManager.OutlineController != null)
        {
            plant.VisualManager.RegisterOutlineForCell(instance, cellType.ToString());
        }

        if (accumulatedScentRadiusBonus != null && accumulatedScentStrengthBonus != null)
        {
            plant.ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }

        return instance;
    }

    public GameObject CreateStemSegment(int stemIndex, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null)
    {
        Vector2Int stemCoord = new Vector2Int(0, stemIndex + 1);
        return SpawnCellVisual(PlantCellType.Stem, stemCoord, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
    }

    public List<GameObject> CreateLeavesForStemSegment(int stemIndex, int leafGap, int leafPattern, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null)
    {
        List<GameObject> newLeaves = new List<GameObject>();

        if (leafGap == 0 || (stemIndex % (leafGap + 1)) == 0)
        {
            Vector2Int stemCoord = new Vector2Int(0, stemIndex + 1);
            List<Vector2Int> leafPositions = CalculateLeafPositions(stemCoord, leafPattern, stemIndex);

            foreach (Vector2Int leafCoord in leafPositions)
            {
                GameObject leafInstance = SpawnCellVisual(PlantCellType.Leaf, leafCoord, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                if (leafInstance != null)
                {
                    newLeaves.Add(leafInstance);
                }
            }
        }

        return newLeaves;
    }

    public List<Vector2Int> CalculateLeafPositions(Vector2Int stemPos, int leafPattern, int stageCounter)
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
                if (offsetRightForPattern1.Value)
                {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                }
                else
                {
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
                if (spiralDir > 0)
                {
                    leafPositions.Add(rightBase);
                }
                else
                {
                    leafPositions.Add(leftBase);
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
        if (cells.ContainsKey(leafCoord)) return false;

        GameObject newLeaf = SpawnCellVisual(PlantCellType.Leaf, leafCoord);
        if (newLeaf != null)
        {
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

    public bool HasCellAt(Vector2Int coord)
    {
        return cells.ContainsKey(coord);
    }

    public PlantCellType? GetCellTypeAt(Vector2Int coord)
    {
        if (cells.TryGetValue(coord, out PlantCellType cellType))
        {
            return cellType;
        }
        return null;
    }

    public int GetActiveLeafCount()
    {
        int count = 0;
        foreach (var leafData in LeafDataList)
        {
            if (leafData.IsActive)
            {
                count++;
            }
        }
        return count;
    }

    public List<GameObject> ActiveCellGameObjects => activeCellGameObjects;
    public Dictionary<Vector2Int, PlantCellType> Cells => cells;
    
    public int GetBerryCount()
    {
        int count = 0;
        foreach (var kvp in cells)
        {
            if (kvp.Value == PlantCellType.Fruit)
            {
                count++;
            }
        }
        return count;
    }

    public List<Vector2Int> GetBerryPositions()
    {
        List<Vector2Int> berryPositions = new List<Vector2Int>();
        foreach (var kvp in cells)
        {
            if (kvp.Value == PlantCellType.Fruit)
            {
                berryPositions.Add(kvp.Key);
            }
        }
        return berryPositions;
    }
}
// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantCellManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

/// <summary>
/// A manager class, owned by a PlantGrowth instance, responsible for creating and tracking
/// the visual GameObjects that make up a plant.
/// </summary>
public class PlantCellManager
{
    private readonly PlantGrowth plant;
    private readonly GameObject seedCellPrefab;
    private readonly GameObject stemCellPrefab;
    private readonly GameObject leafCellPrefab;
    private readonly GameObject berryCellPrefab;
    private readonly float cellSpacing;

    private readonly Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private readonly List<GameObject> activeCellGameObjects = new List<GameObject>();

    public List<LeafData> LeafDataList { get; } = new List<LeafData>();
    public GameObject RootCellInstance { get; set; }

    public PlantCellManager(PlantGrowth plant, GameObject seedPrefab, GameObject stemPrefab, GameObject leafPrefab, GameObject berryPrefab, float spacing)
    {
        this.plant = plant;
        this.seedCellPrefab = seedPrefab;
        this.stemCellPrefab = stemPrefab;
        this.leafCellPrefab = leafPrefab;
        this.berryCellPrefab = berryPrefab;
        this.cellSpacing = spacing;
    }

    public void ReportCellDestroyed(Vector2Int coord)
    {
        if (cells.TryGetValue(coord, out PlantCellType cellType))
        {
            if (cellType == PlantCellType.Leaf)
            {
                // Mark the leaf data as inactive for photosynthesis calculations
                for (int i = 0; i < LeafDataList.Count; i++)
                {
                    if (LeafDataList[i].GridCoord == coord)
                    {
                        LeafDataList[i] = new LeafData(coord, false);
                        break;
                    }
                }
            }

            cells.Remove(coord);
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));

            plant.VisualManager.OutlineController?.OnPlantCellRemoved(coord);
        }
    }

    public void ClearAllVisuals()
    {
        // Use a copy to avoid modification during iteration
        foreach (GameObject cellGO in new List<GameObject>(activeCellGameObjects))
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

    public GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
        if (cells.ContainsKey(coords))
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Trying to spawn {cellType} at already occupied coordinate {coords}.");
            return GetCellGameObjectAt(coords);
        }

        GameObject prefab = GetPrefabForType(cellType);
        if (prefab == null) return null;

        Vector2 worldPos = (Vector2)plant.transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, plant.transform);
        instance.name = $"{plant.gameObject.name}_{cellType}_{coords.x}_{coords.y}";
        
        // Remove GridEntity from parts to avoid registering them with the GridPositionManager
        if (cellType != PlantCellType.Seed)
        {
            if (instance.TryGetComponent<GridEntity>(out var partGridEntity))
            {
                Object.Destroy(partGridEntity);
            }
        }
        
        var cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = plant;
        cellComp.GridCoord = coords;
        cellComp.CellType = cellType;
        
        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);
        
        if (cellType == PlantCellType.Leaf)
        {
            LeafDataList.Add(new LeafData(coords, true));
        }

        // Register for visual systems
        plant.VisualManager.RegisterShadowForCell(instance, cellType.ToString());
        plant.VisualManager.RegisterOutlineForCell(instance, cellType.ToString());

        return instance;
    }

    private GameObject GetPrefabForType(PlantCellType cellType)
    {
        switch (cellType)
        {
            case PlantCellType.Seed: return seedCellPrefab;
            case PlantCellType.Stem: return stemCellPrefab;
            case PlantCellType.Leaf: return leafCellPrefab;
            case PlantCellType.Fruit: return berryCellPrefab;
            default:
                Debug.LogError($"[{plant.gameObject.name}] No prefab assigned for PlantCellType.{cellType}!");
                return null;
        }
    }

    public bool HasCellAt(Vector2Int coord) => cells.ContainsKey(coord);
    public GameObject GetCellGameObjectAt(Vector2Int coord) => activeCellGameObjects.FirstOrDefault(go => go != null && go.GetComponent<PlantCell>()?.GridCoord == coord);
    public int GetActiveLeafCount() => LeafDataList.Count(leaf => leaf.IsActive);
}
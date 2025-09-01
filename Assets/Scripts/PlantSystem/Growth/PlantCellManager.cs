using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes;
using WegoSystem;

public class PlantCellManager
{
    private readonly PlantGrowth plant;
    private readonly GameObject seedCellPrefab;
    private readonly GameObject stemCellPrefab;
    private readonly GameObject leafCellPrefab;
    private readonly GameObject berryCellPrefab;
    private readonly FoodType _leafFoodType;

    public readonly Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private readonly List<GameObject> activeCellGameObjects = new List<GameObject>();

    public List<LeafData> LeafDataList { get; } = new List<LeafData>();
    public GameObject RootCellInstance { get; set; }

    public PlantCellManager(PlantGrowth plant, GameObject seedPrefab, GameObject stemPrefab, GameObject leafPrefab, GameObject berryPrefab, FoodType leafFoodType)
    {
        this.plant = plant;
        this.seedCellPrefab = seedPrefab;
        this.stemCellPrefab = stemPrefab;
        this.leafCellPrefab = leafPrefab;
        this.berryCellPrefab = berryPrefab;
        this._leafFoodType = leafFoodType;
    }

    public GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
        if (cells.ContainsKey(coords))
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Cell already exists at {coords}. Skipping spawn.");
            return GetCellGameObjectAt(coords);
        }

        GameObject prefab = GetPrefabForType(cellType);
        if (prefab == null) return null;

        float spacing = plant.GetCellWorldSpacing();
        Vector3 cellLocalPos = new Vector3(coords.x * spacing, coords.y * spacing, 0);
        Vector3 cellWorldPos = plant.transform.position + cellLocalPos;

        // --- THIS IS THE FIX ---
        // Instantiate the object at its final, correct world position.
        GameObject instance = Object.Instantiate(prefab, cellWorldPos, Quaternion.identity, plant.transform);
        
        // ... (Code to set up PlantCell component remains the same) ...
        PlantCell cellComponent = instance.GetComponent<PlantCell>();
        if (cellComponent == null)
        {
            cellComponent = instance.AddComponent<PlantCell>();
        }
        cellComponent.ParentPlantGrowth = plant;
        cellComponent.GridCoord = coords;
        cellComponent.CellType = cellType;
        
        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);

        if (cellType == PlantCellType.Seed)
        {
            RootCellInstance = instance;
        }
        else if (cellType == PlantCellType.Leaf)
        {
            LeafDataList.Add(new LeafData(coords, true));

            var foodItem = instance.GetComponent<FoodItem>();
            if (foodItem != null)
            {
                if (_leafFoodType == null)
                {
                    Debug.LogError($"[PlantCellManager] Cannot assign FoodType to new leaf on '{plant.name}' because the 'Leaf Food Type' field is not set!", plant);
                }
                
                // Get the grid position that this world position corresponds to
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(cellWorldPos);
                
                // Manually initialize the food item, telling it its type and grid position.
                // This bypasses the problematic Start() method in FoodItem.
                foodItem.InitializeAsPlantPart(_leafFoodType, gridPos);
            }
            else
            {
                Debug.LogWarning($"[{plant.gameObject.name}] Leaf prefab is missing FoodItem component.", plant);
            }
        }

        plant.VisualManager.RegisterShadowForCell(instance, cellType.ToString());
        plant.VisualManager.RegisterOutlineForCell(instance, cellType.ToString());

        return instance;
    }

    // --- Unchanged methods below ---

    public void ReportCellDestroyed(Vector2Int coord)
    {
        if (cells.TryGetValue(coord, out PlantCellType cellType))
        {
            GameObject cellObj = GetCellGameObjectAt(coord);

            if (cellObj != null)
            {
                plant.VisualManager.UnregisterShadowForCell(cellObj);
                plant.VisualManager.OutlineController?.OnPlantCellRemoved(coord);
                activeCellGameObjects.Remove(cellObj);
                Object.Destroy(cellObj);
            }

            if (cellType == PlantCellType.Leaf)
            {
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
        }
    }

    public void ClearAllVisuals()
    {
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
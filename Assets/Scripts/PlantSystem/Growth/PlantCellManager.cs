using UnityEngine;
using Abracodabra.Genes;
using WegoSystem;
using System.Collections.Generic;
using System.Linq;

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

    public GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
        if (cells.ContainsKey(coords))
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Trying to spawn {cellType} at already occupied coordinate {coords}.");
            return GetCellGameObjectAt(coords);
        }

        GameObject prefab = GetPrefabForType(cellType);
        if (prefab == null) return null;

        // Calculate the position in the plant's local grid
// Each cell should be exactly 1 world unit (6 pixels at 6 PPU)
        float worldUnitsPerCell = plant.cellSpacingInPixels / 6f; // Convert pixels to world units at base PPU
        Vector2 localOffset = (Vector2)coords * worldUnitsPerCell;
        Vector2 worldPos = (Vector2)plant.transform.position + localOffset;

// Ensure pixel-perfect positioning
        if (ResolutionManager.HasInstance && ResolutionManager.Instance.CurrentPPU > 0) {
            float pixelSize = 1f / ResolutionManager.Instance.CurrentPPU;
            // Round to nearest pixel to ensure crisp rendering
            worldPos.x = Mathf.Round(worldPos.x / pixelSize) * pixelSize;
            worldPos.y = Mathf.Round(worldPos.y / pixelSize) * pixelSize;
        }
        
        GameObject instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, plant.transform);
        instance.name = $"{plant.gameObject.name}_{cellType}_{coords.x}_{coords.y}";

        // Child parts of a plant should not have their own GridEntity
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
            instance.tag = "FruitSpawn";

            if (_leafFoodType != null)
            {
                var foodItem = instance.AddComponent<FoodItem>();
                foodItem.foodType = _leafFoodType;
            }
            else
            {
                Debug.LogWarning($"[{plant.gameObject.name}] Cannot add FoodItem to leaf. The 'Leaf Food Type' is not assigned in the PlantGrowth inspector.", plant);
            }
        }

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
// FILE: Assets/Scripts/Tiles/Data/PlantPlacementManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Added for LINQ

public class PlantPlacementManager : MonoBehaviour
{
    public static PlantPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform plantParent; 
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private PlantGrowthModifierManager growthModifierManager;
    [SerializeField] private NodeExecutor nodeExecutor;

    [Header("Planting Settings")]
    [Tooltip("Maximum radius from cell center for random seed placement (in units)")]
    [SerializeField] private float spawnRadius = 0.25f;
    [Tooltip("Increment for position randomization (in pixels, for pixel-perfect placement)")]
    [SerializeField] private float spawnRadiusIncrement = 4f;

    [Header("Tile Restrictions")]
    [Tooltip("List of tiles that cannot be planted on")]
    [SerializeField] private List<TileDefinition> invalidPlantingTiles = new List<TileDefinition>();

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    private HashSet<TileDefinition> invalidTilesSet = new HashSet<TileDefinition>();
    private Dictionary<Vector3Int, GameObject> plantsByGridPosition = new Dictionary<Vector3Int, GameObject>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RebuildInvalidTilesSet();
    }

    private void Start()
    {
        if (plantParent == null && EcosystemManager.Instance != null) plantParent = EcosystemManager.Instance.plantParent;
        if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance;
        if (growthModifierManager == null) growthModifierManager = PlantGrowthModifierManager.Instance;
        if (nodeExecutor == null)
        {
            nodeExecutor = FindAnyObjectByType<NodeExecutor>(); 
            if (nodeExecutor == null) Debug.LogError("[PlantPlacementManager] NodeExecutor instance not found!");
        }
    }
    
    private void RebuildInvalidTilesSet()
    {
        invalidTilesSet.Clear();
        foreach (var tile in invalidPlantingTiles) if (tile != null) invalidTilesSet.Add(tile);
        if (showDebugMessages) Debug.Log($"PlantPlacementManager: Invalid tiles set with {invalidTilesSet.Count} entries");
    }
    
    private void OnValidate() { RebuildInvalidTilesSet(); }

    public bool IsPositionOccupied(Vector3Int gridPosition)
    {
        if (plantsByGridPosition.TryGetValue(gridPosition, out GameObject plant)) {
            if (plant == null) { plantsByGridPosition.Remove(gridPosition); return false; }
            return true;
        }
        return false;
    }
    
    public bool IsTileValidForPlanting(TileDefinition tileDef)
    {
        if (tileDef == null) return false;
        return !invalidTilesSet.Contains(tileDef);
    }

    public void CleanupDestroyedPlants()
    {
        List<Vector3Int> keysToRemove = plantsByGridPosition.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove) plantsByGridPosition.Remove(key);
        if (showDebugMessages && keysToRemove.Count > 0) Debug.Log($"PPM: Removed {keysToRemove.Count} destroyed plant refs.");
    }

    // This method is for planting from the SEED SLOT (Node Editor)
    public bool TryPlantSeed(Vector3Int gridPosition, Vector3 worldPosition)
    {
        CleanupDestroyedPlants();
        if (IsPositionOccupied(gridPosition)) {
            if (showDebugMessages) Debug.Log($"Cannot plant (from seed slot): Position {gridPosition} occupied.");
            return false;
        }
        
        TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
        if (!IsTileValidForPlanting(tileDef)) {
            if (showDebugMessages) Debug.Log($"Cannot plant (from seed slot): Tile {tileDef?.displayName ?? "Unknown"} invalid.");
            return false;
        }

        if (nodeExecutor == null) {
            Debug.LogError("Cannot plant (from seed slot): NodeExecutor reference is missing in PlantPlacementManager.");
            return false;
        }
        
        Vector3 plantingPosition = GetRandomizedPlantingPosition(worldPosition);
        GameObject plantObj = nodeExecutor.SpawnPlantFromSeedInSlot(plantingPosition, plantParent);

        if (plantObj != null) {
            plantsByGridPosition[gridPosition] = plantObj;
            PlantGrowth plantGrowth = plantObj.GetComponent<PlantGrowth>();
            if (growthModifierManager != null && plantGrowth != null) {
                growthModifierManager.RegisterPlantTile(plantGrowth, tileDef);
                if (showDebugMessages) Debug.Log($"Plant (from seed slot) registered with tile: {tileDef?.displayName ?? "Unknown"}");
            }
            return true;
        }
        if (showDebugMessages) Debug.Log($"Failed to plant seed (from seed slot) at {gridPosition} (NodeExecutor returned null).");
        return false;
    }
    
    public bool TryPlantSeedFromInventory(InventoryBarItem seedItem,
        Vector3Int      gridPosition,
        Vector3         worldPosition)
    {
        if (seedItem == null || !seedItem.IsSeed())                     return false;
        if (IsPositionOccupied(gridPosition))                           return false;

        TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
        if (!IsTileValidForPlanting(tileDef))                           return false;
        if (nodeExecutor == null)                                       return false;

        Vector3  spawnPos  = GetRandomizedPlantingPosition(worldPosition);
        GameObject plantGO = nodeExecutor.SpawnPlantFromInventorySeed(
            seedItem.NodeData, spawnPos, plantParent);

        if (plantGO == null)                                            return false;

        plantsByGridPosition[gridPosition] = plantGO;

        // Register growth modifiers
        PlantGrowth pg = plantGO.GetComponent<PlantGrowth>();
        if (pg != null && growthModifierManager != null)
            growthModifierManager.RegisterPlantTile(pg, tileDef);

        // Optional planting animation
        GardenerController g = FindAnyObjectByType<GardenerController>();
        if (g != null) g.Plant();

        return true;
    }


    private Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition)
    {
        if (spawnRadius < 0.01f) return centerPosition;
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        float randomDistance = Random.Range(0.05f, spawnRadius);
        float offsetX = direction.x * randomDistance;
        float offsetY = direction.y * randomDistance;
        if (spawnRadiusIncrement > 0.001f) {
            offsetX = Mathf.Round(offsetX / spawnRadiusIncrement) * spawnRadiusIncrement;
            offsetY = Mathf.Round(offsetY / spawnRadiusIncrement) * spawnRadiusIncrement;
        }
        return centerPosition + new Vector3(offsetX, offsetY, 0f);
    }
}
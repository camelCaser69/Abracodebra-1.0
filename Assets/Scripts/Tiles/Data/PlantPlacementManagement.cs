// FILE: Assets/Scripts/Tiles/Data/PlantPlacementManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Added for LINQ

public class PlantPlacementManager : MonoBehaviour
{
    public static PlantPlacementManager Instance { get; private set; }

    [Header("References")]
    // REMOVED: [SerializeField] private GameObject plantPrefab; // NodeExecutor now handles this
    // REMOVED: [SerializeField] private NodeEditorGridController nodeEditorGrid; // NodeExecutor accesses this
    [SerializeField] private Transform plantParent; // Still needed for parenting spawned plants
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private PlantGrowthModifierManager growthModifierManager;
    [SerializeField] private NodeExecutor nodeExecutor; // <<< NEW: Reference to NodeExecutor

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
        // <<< NEW: Ensure NodeExecutor is found >>>
        if (nodeExecutor == null)
        {
            nodeExecutor = FindAnyObjectByType<NodeExecutor>(); // Or however you manage its instance
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

    public bool TryPlantSeed(Vector3Int gridPosition, Vector3 worldPosition)
    {
        CleanupDestroyedPlants();
        if (IsPositionOccupied(gridPosition)) {
            if (showDebugMessages) Debug.Log($"Cannot plant: Position {gridPosition} occupied.");
            return false;
        }
        
        TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
        if (!IsTileValidForPlanting(tileDef)) {
            if (showDebugMessages) Debug.Log($"Cannot plant: Tile {tileDef?.displayName ?? "Unknown"} invalid.");
            return false;
        }

        // <<< MODIFIED: Use NodeExecutor to spawn plant from the seed in slot >>>
        if (nodeExecutor == null) {
            Debug.LogError("Cannot plant: NodeExecutor reference is missing in PlantPlacementManager.");
            return false;
        }
        
        // NodeExecutor now fetches the seed and its graph from NodeEditorGridController.seedSlotCellReference
        // It needs the planting position and parent.
        Vector3 plantingPosition = GetRandomizedPlantingPosition(worldPosition);
        GameObject plantObj = nodeExecutor.SpawnPlantFromSeedInSlot(plantingPosition, plantParent);
        // <<< END MODIFIED >>>

        if (plantObj != null) {
            plantsByGridPosition[gridPosition] = plantObj;
            PlantGrowth plantGrowth = plantObj.GetComponent<PlantGrowth>();
            if (growthModifierManager != null && plantGrowth != null) {
                growthModifierManager.RegisterPlantTile(plantGrowth, tileDef);
                if (showDebugMessages) Debug.Log($"Plant registered with tile: {tileDef?.displayName ?? "Unknown"}");
            }
            return true;
        }
        if (showDebugMessages) Debug.Log($"Failed to plant seed at {gridPosition} (NodeExecutor returned null).");
        return false;
    }

    // SpawnPlant method is now effectively inside NodeExecutor.SpawnPlantFromSeedInSlot
    // CloneNodeGraph and CloneEffectsList are also in NodeExecutor

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
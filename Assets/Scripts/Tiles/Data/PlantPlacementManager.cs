// FILE: Assets/Scripts/Tiles/Data/PlantPlacementManager.cs (FIXED)
using System.Collections.Generic;
using UnityEngine;

public class PlantPlacementManager : MonoBehaviour
{
    public static PlantPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private Transform plantParent;
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private PlantGrowthModifierManager growthModifierManager;

    [Header("Planting Settings")]
    [Tooltip("Maximum radius from cell center for random seed placement (in units)")]
    [SerializeField] private float spawnRadius = 0.25f;
    
    [Tooltip("Increment for position randomization (in pixels, for pixel-perfect placement)")]
    [SerializeField] private float spawnRadiusIncrement = 4f;

    [Header("Tile Restrictions")]
    [Tooltip("List of tiles that cannot be planted on")]
    [SerializeField] private List<TileDefinition> invalidPlantingTiles = new List<TileDefinition>();

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true; // Changed to true for debugging
    
    // Cache for quick lookup of invalid tiles
    private HashSet<TileDefinition> invalidTilesSet = new HashSet<TileDefinition>();
    
    // Dictionary to track plant positions (using grid cell positions as keys)
    private Dictionary<Vector3Int, GameObject> plantsByGridPosition = new Dictionary<Vector3Int, GameObject>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Build the set of invalid tiles for faster lookup
        RebuildInvalidTilesSet();
    }

    private void Start()
    {
        // Initialize references if not set in inspector
        if (plantParent == null && EcosystemManager.Instance != null)
        {
            plantParent = EcosystemManager.Instance.plantParent;
        }
        
        if (tileInteractionManager == null)
        {
            tileInteractionManager = TileInteractionManager.Instance;
        }
        
        if (growthModifierManager == null)
        {
            growthModifierManager = PlantGrowthModifierManager.Instance;
        }
    }
    
    private void RebuildInvalidTilesSet()
    {
        invalidTilesSet.Clear();
        foreach (var tile in invalidPlantingTiles)
        {
            if (tile != null)
            {
                invalidTilesSet.Add(tile);
            }
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"PlantPlacementManager: Built invalid tiles set with {invalidTilesSet.Count} entries");
        }
    }
    
    private void OnValidate()
    {
        // Rebuild the set when changed in Inspector
        RebuildInvalidTilesSet();
    }

    // --- Position Management ---
    
    public bool IsPositionOccupied(Vector3Int gridPosition)
    {
        if (plantsByGridPosition.TryGetValue(gridPosition, out GameObject plant))
        {
            if (plant == null)
            {
                plantsByGridPosition.Remove(gridPosition);
                return false;
            }
            return true;
        }
        return false;
    }
    
    public bool IsTileValidForPlanting(TileDefinition tileDef)
    {
        if (tileDef == null)
            return false;
            
        return !invalidTilesSet.Contains(tileDef);
    }

    public void CleanupDestroyedPlants()
    {
        List<Vector3Int> keysToRemove = new List<Vector3Int>();
        
        foreach (var kvp in plantsByGridPosition)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            plantsByGridPosition.Remove(key);
        }
        
        if (showDebugMessages && keysToRemove.Count > 0)
        {
            Debug.Log($"PlantPlacementManager: Removed {keysToRemove.Count} destroyed plant references.");
        }
    }

    // --- FIXED: Seed-based planting methods ---

    /// <summary>
    /// FIXED: Try to plant using seed selection system
    /// </summary>
    public void TryPlantWithSeedSelection(Vector3Int gridPosition, Vector3 worldPosition, 
        System.Action onCompleted = null)
    {
        if (showDebugMessages)
            Debug.Log($"[PlantPlacementManager] TryPlantWithSeedSelection called at {gridPosition}");
        
        // Basic validation
        if (!CanPlantAtPosition(gridPosition))
        {
            if (showDebugMessages)
                Debug.Log("[PlantPlacementManager] Cannot plant at position - validation failed");
            onCompleted?.Invoke();
            return;
        }

        // CRITICAL FIX: Ensure SeedSelectionUI instance exists and is properly initialized
        if (SeedSelectionUI.Instance == null)
        {
            Debug.LogError("[PlantPlacementManager] SeedSelectionUI.Instance is null! Make sure SeedSelectionUI exists in the scene.");
            onCompleted?.Invoke();
            return;
        }
        
        if (PlayerGeneticsInventory.Instance == null)
        {
            Debug.LogError("[PlantPlacementManager] PlayerGeneticsInventory.Instance is null!");
            onCompleted?.Invoke();
            return;
        }

        var plantableSeeds = PlayerGeneticsInventory.Instance.GetPlantableSeeds();
        
        if (showDebugMessages)
            Debug.Log($"[PlantPlacementManager] Found {plantableSeeds.Count} plantable seeds");
        
        if (plantableSeeds.Count == 0)
        {
            if (showDebugMessages)
                Debug.Log("Cannot plant: No plantable seeds available in inventory.");
            onCompleted?.Invoke();
            return;
        }
        
        if (plantableSeeds.Count == 1)
        {
            // Auto-select single seed
            if (showDebugMessages)
                Debug.Log($"[PlantPlacementManager] Auto-selecting single seed: {plantableSeeds[0].seedName}");
            TryPlantSeedInstance(plantableSeeds[0], gridPosition, worldPosition);
            onCompleted?.Invoke();
        }
        else
        {
            // Show selection UI
            if (showDebugMessages)
                Debug.Log("[PlantPlacementManager] Showing seed selection UI for multiple seeds");
            
            SeedSelectionUI.Instance.ShowSeedSelection(selectedSeed => {
                if (showDebugMessages)
                    Debug.Log($"[PlantPlacementManager] Seed selected from UI: {selectedSeed?.seedName ?? "null"}");
                if (selectedSeed != null)
                {
                    TryPlantSeedInstance(selectedSeed, gridPosition, worldPosition);
                }
                onCompleted?.Invoke();
            });
        }
    }

    /// <summary>
    /// FIXED: Plant a specific seed instance
    /// </summary>
    public bool TryPlantSeedInstance(SeedInstance seedInstance, Vector3Int gridPosition, Vector3 worldPosition)
    {
        if (showDebugMessages)
            Debug.Log($"[PlantPlacementManager] TryPlantSeedInstance called for seed: {seedInstance?.seedName ?? "null"}");
        
        if (seedInstance == null)
        {
            if (showDebugMessages)
                Debug.LogError("Cannot plant: Seed instance is null!");
            return false;
        }

        if (!seedInstance.IsValidForPlanting())
        {
            if (showDebugMessages)
                Debug.Log($"Cannot plant: Seed '{seedInstance.seedName}' is not valid for planting.");
            return false;
        }

        // Check if position is still valid
        if (!CanPlantAtPosition(gridPosition))
        {
            if (showDebugMessages)
                Debug.Log($"Cannot plant: Position {gridPosition} is not valid for planting.");
            return false;
        }

        // Calculate planting position
        Vector3 plantingPosition = GetRandomizedPlantingPosition(worldPosition);
        
        if (showDebugMessages)
            Debug.Log($"[PlantPlacementManager] Planting at position: {plantingPosition}");
        
        // Spawn the plant
        GameObject plantObj = SpawnPlantFromSeed(seedInstance, plantingPosition);
        if (plantObj != null)
        {
            // Track the plant position
            plantsByGridPosition[gridPosition] = plantObj;
            
            // Register with growth modifier manager
            RegisterPlantWithModifiers(plantObj, gridPosition);
            
            // Remove seed from inventory
            if (PlayerGeneticsInventory.Instance != null)
            {
                PlayerGeneticsInventory.Instance.RemoveSeed(seedInstance);
                if (showDebugMessages)
                    Debug.Log($"Planted and consumed seed: {seedInstance.seedName}");
            }
            
            return true;
        }
        else
        {
            if (showDebugMessages)
                Debug.LogError($"Failed to spawn plant from seed: {seedInstance.seedName}");
        }
        return false;
    }

    // --- LEGACY: Original NodeGraph-based planting (kept for compatibility) ---

    public bool TryPlantSeed(Vector3Int gridPosition, Vector3 worldPosition)
    {
        // This is the old method - now redirects to seed selection
        TryPlantWithSeedSelection(gridPosition, worldPosition);
        return true; // Always return true as the new system handles validation internally
    }

    // --- Private Helper Methods ---

    private bool CanPlantAtPosition(Vector3Int gridPosition)
    {
        // Clean up any destroyed plants first
        CleanupDestroyedPlants();
        
        // Check if position is occupied
        if (IsPositionOccupied(gridPosition))
        {
            if (showDebugMessages)
                Debug.Log($"Cannot plant: Position {gridPosition} already has a plant.");
            return false;
        }
        
        // Check tile validity
        TileDefinition tileDef = null;
        if (tileInteractionManager != null)
        {
            tileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
            
            if (!IsTileValidForPlanting(tileDef))
            {
                if (showDebugMessages)
                {
                    string tileName = tileDef != null ? tileDef.displayName : "Unknown";
                    Debug.Log($"Cannot plant: Tile {tileName} is not valid for planting.");
                }
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// FIXED: Spawn a plant from a SeedInstance
    /// </summary>
    private GameObject SpawnPlantFromSeed(SeedInstance seedInstance, Vector3 position)
    {
        if (plantPrefab == null)
        {
            Debug.LogError("Cannot spawn plant: Plant prefab not assigned.");
            return null;
        }

        if (showDebugMessages)
            Debug.Log($"SpawnPlantFromSeed called: {seedInstance.seedName} at position: {position}");
    
        // Instantiate the plant prefab
        GameObject plantObj = Instantiate(plantPrefab, position, Quaternion.identity, plantParent);
        plantObj.name = $"Plant_{seedInstance.seedName}_{System.DateTime.Now:HHmmss}";

        // Get the PlantGrowth component
        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            // Convert seed to NodeGraph
            NodeGraph nodeGraph = seedInstance.ToNodeGraph();
            
            if (nodeGraph == null || nodeGraph.nodes == null || nodeGraph.nodes.Count == 0)
            {
                Debug.LogError($"Failed to convert seed '{seedInstance.seedName}' to NodeGraph or NodeGraph is empty!");
                Destroy(plantObj);
                return null;
            }

            if (showDebugMessages)
                Debug.Log($"Converted seed to NodeGraph with {nodeGraph.nodes.Count} nodes");

            // Initialize the plant
            growthComponent.InitializeAndGrow(nodeGraph);
            
            if (showDebugMessages)
                Debug.Log($"Plant spawned and initialized successfully: {plantObj.name}");
            
            return plantObj;
        }
        else
        {
            Debug.LogError("Plant prefab missing PlantGrowth component! Destroying spawned object.");
            Destroy(plantObj);
            return null;
        }
    }

    private void RegisterPlantWithModifiers(GameObject plantObj, Vector3Int gridPosition)
    {
        if (growthModifierManager != null && tileInteractionManager != null)
        {
            TileDefinition currentTile = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
            PlantGrowth plantGrowth = plantObj.GetComponent<PlantGrowth>();
            
            if (plantGrowth != null)
            {
                growthModifierManager.RegisterPlantTile(plantGrowth, currentTile);
                
                if (showDebugMessages)
                {
                    string tileDebugName = currentTile != null ? currentTile.displayName : "Unknown";
                    Debug.Log($"Plant registered with tile: {tileDebugName}");
                }
            }
        }
    }
    
    private Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition)
    {
        if (showDebugMessages)
            Debug.Log($"Starting position randomization from center: {centerPosition}, radius: {spawnRadius}, increment: {spawnRadiusIncrement}");
    
        if (spawnRadius < 0.01f)
        {
            return centerPosition;
        }
    
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        float randomDistance = Random.Range(0.05f, spawnRadius);
        
        float offsetX = direction.x * randomDistance;
        float offsetY = direction.y * randomDistance;
        
        if (spawnRadiusIncrement > 0.001f)
        {
            offsetX = Mathf.Round(offsetX / spawnRadiusIncrement) * spawnRadiusIncrement;
            offsetY = Mathf.Round(offsetY / spawnRadiusIncrement) * spawnRadiusIncrement;
        }
        
        Vector3 randomizedPosition = centerPosition + new Vector3(offsetX, offsetY, 0f);
        
        if (showDebugMessages)
            Debug.Log($"FINAL randomized position: {randomizedPosition}");
    
        return randomizedPosition;
    }
}
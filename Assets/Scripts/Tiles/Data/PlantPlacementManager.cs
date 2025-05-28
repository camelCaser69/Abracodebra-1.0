// FILE: Assets/Scripts/Tiles/Data/PlantPlacementManager.cs (UPDATED for Seed System)
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
    [SerializeField] private bool showDebugMessages = true;
    
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

    // --- Position Management (unchanged) ---
    
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

    // --- NEW: Seed-based planting methods ---

    /// <summary>
    /// NEW: Try to plant using seed selection system
    /// </summary>
    public void TryPlantWithSeedSelection(Vector3Int gridPosition, Vector3 worldPosition, 
        System.Action onCompleted = null)
    {
        // Basic validation
        if (!CanPlantAtPosition(gridPosition))
        {
            onCompleted?.Invoke();
            return;
        }

        // Use seed selection UI
        if (SeedSelectionUI.Instance != null && PlayerGeneticsInventory.Instance != null)
        {
            var plantableSeeds = PlayerGeneticsInventory.Instance.GetPlantableSeeds();
            
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
                TryPlantSeedInstance(plantableSeeds[0], gridPosition, worldPosition);
                onCompleted?.Invoke();
            }
            else
            {
                // Show selection UI
                SeedSelectionUI.Instance.ShowSeedSelection(selectedSeed => {
                    TryPlantSeedInstance(selectedSeed, gridPosition, worldPosition);
                    onCompleted?.Invoke();
                });
            }
        }
        else
        {
            if (showDebugMessages)
                Debug.LogError("Cannot plant: Seed selection system not available!");
            onCompleted?.Invoke();
        }
    }

    /// <summary>
    /// NEW: Plant a specific seed instance
    /// </summary>
    public bool TryPlantSeedInstance(SeedInstance seedInstance, Vector3Int gridPosition, Vector3 worldPosition)
    {
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
            return false;

        // Calculate planting position
        Vector3 plantingPosition = GetRandomizedPlantingPosition(worldPosition);
        
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
    /// NEW: Spawn a plant from a SeedInstance
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
            
            if (nodeGraph == null)
            {
                Debug.LogError($"Failed to convert seed '{seedInstance.seedName}' to NodeGraph!");
                Destroy(plantObj);
                return null;
            }

            // Initialize the plant
            growthComponent.InitializeAndGrow(nodeGraph);
            return plantObj;
        }
        else
        {
            Debug.LogError("Plant prefab missing PlantGrowth component! Destroying spawned object.");
            Destroy(plantObj);
            return null;
        }
    }

    /// <summary>
    /// LEGACY: Spawn a plant using the original NodeGraph system
    /// </summary>
    private GameObject SpawnPlant(NodeGraph graphToSpawn, Vector3 position)
    {
        if (plantPrefab == null)
        {
            Debug.LogError("Cannot spawn plant: Plant prefab not assigned.");
            return null;
        }

        if (showDebugMessages)
            Debug.Log($"SpawnPlant called with position: {position}");
    
        GameObject plantObj = Instantiate(plantPrefab, position, Quaternion.identity, plantParent);

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            NodeGraph graphCopy = CloneNodeGraph(graphToSpawn);
            growthComponent.InitializeAndGrow(graphCopy);
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

    // --- Legacy Helper Methods (kept for compatibility) ---

    private NodeGraph CloneNodeGraph(NodeGraph original)
    {
        NodeGraph copy = new NodeGraph();
        copy.nodes = new List<NodeData>();

        foreach (NodeData originalNode in original.nodes)
        {
            if (originalNode == null) continue;

            NodeData newNode = new NodeData
            {
                nodeId = originalNode.nodeId,
                nodeDisplayName = originalNode.nodeDisplayName,
                orderIndex = originalNode.orderIndex,
                canBeDeleted = originalNode.canBeDeleted
            };

            newNode.effects = CloneEffectsList(originalNode.effects);
            copy.nodes.Add(newNode);
        }

        return copy;
    }

    private List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
    {
        if (originalList == null) return new List<NodeEffectData>();

        List<NodeEffectData> newList = new List<NodeEffectData>();
        foreach (var originalEffect in originalList)
        {
            if (originalEffect == null) continue;

            NodeEffectData newEffect = new NodeEffectData
            {
                effectType = originalEffect.effectType,
                primaryValue = originalEffect.primaryValue,
                secondaryValue = originalEffect.secondaryValue,
                isPassive = originalEffect.isPassive,
                scentDefinitionReference = originalEffect.scentDefinitionReference
            };
            newList.Add(newEffect);
        }
        return newList;
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
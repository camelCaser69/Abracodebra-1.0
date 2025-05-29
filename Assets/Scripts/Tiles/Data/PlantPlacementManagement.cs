using System.Collections.Generic;
using UnityEngine;

public class PlantPlacementManager : MonoBehaviour
{
    public static PlantPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private NodeEditorGridController nodeEditorGrid;
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
        
        if (nodeEditorGrid == null)
        {
            nodeEditorGrid = NodeEditorGridController.Instance;
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

    // Check if a grid position is occupied by a plant
    public bool IsPositionOccupied(Vector3Int gridPosition)
    {
        // If we have a reference, check if it's still valid (not destroyed)
        if (plantsByGridPosition.TryGetValue(gridPosition, out GameObject plant))
        {
            if (plant == null)
            {
                // Plant has been destroyed, remove from dictionary
                plantsByGridPosition.Remove(gridPosition);
                return false;
            }
            return true;
        }
        return false;
    }
    
    // Check if a tile is valid for planting
    public bool IsTileValidForPlanting(TileDefinition tileDef)
    {
        // If the tile is null, it's not valid
        if (tileDef == null)
            return false;
            
        // Check if this tile is in our invalid set
        return !invalidTilesSet.Contains(tileDef);
    }

    // Clean up destroyed plants from our dictionary (call periodically if needed)
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

    // Try to plant a seed at the given grid position
    public bool TryPlantSeed(Vector3Int gridPosition, Vector3 worldPosition)
    {
        // Clean up any destroyed plants first
        CleanupDestroyedPlants();
        
        // Return false if position is already occupied
        if (IsPositionOccupied(gridPosition))
        {
            if (showDebugMessages)
            {
                Debug.Log($"Cannot plant: Position {gridPosition} already has a plant.");
            }
            return false;
        }
        
        // Get the tile at this position
        TileDefinition tileDef = null;
        if (tileInteractionManager != null)
        {
            tileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
            
            // Check if the tile is valid for planting
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

        // Get the current graph from the NodeEditorGridController
        if (nodeEditorGrid == null)
        {
            nodeEditorGrid = NodeEditorGridController.Instance;
            if (nodeEditorGrid == null)
            {
                Debug.LogError("Cannot plant: NodeEditorGridController not found.");
                return false;
            }
        }

        NodeGraph graphToSpawn = nodeEditorGrid.GetCurrentUIGraph();
        if (graphToSpawn == null || graphToSpawn.nodes == null || graphToSpawn.nodes.Count == 0)
        {
            if (showDebugMessages)
            {
                Debug.Log("Cannot plant: No nodes in UI graph to spawn.");
            }
            return false;
        }
        
        // Validate graph has a seed node
        bool seedFound = false;
        foreach (var node in graphToSpawn.nodes)
        {
            if (node != null && node.effects != null)
            {
                foreach (var effect in node.effects)
                {
                    if (effect != null && effect.effectType == NodeEffectType.SeedSpawn && effect.isPassive)
                    {
                        seedFound = true;
                        break;
                    }
                }
            }
            if (seedFound) break;
        }

        if (!seedFound)
        {
            if (showDebugMessages)
            {
                Debug.Log("Cannot plant: Node graph lacks a SeedSpawn effect.");
            }
            return false;
        }

        // Calculate randomized planting position based on our settings
        Vector3 plantingPosition = GetRandomizedPlantingPosition(worldPosition);
        
        // Spawn the plant at the randomized position
        GameObject plantObj = SpawnPlant(graphToSpawn, plantingPosition);
        if (plantObj != null)
        {
            // Track the plant position
            plantsByGridPosition[gridPosition] = plantObj;
            
            // Register the plant with the growth modifier manager
            if (growthModifierManager != null)
            {
                PlantGrowth plantGrowth = plantObj.GetComponent<PlantGrowth>();
                if (plantGrowth != null)
                {
                    growthModifierManager.RegisterPlantTile(plantGrowth, tileDef);
                    
                    if (showDebugMessages)
                    {
                        string tileDebugName = tileDef != null ? tileDef.displayName : "Unknown";
                        Debug.Log($"Plant registered with tile: {tileDebugName}");
                    }
                }
            }
            
            return true;
        }
        return false;
    }

    // Spawn a plant using the given node graph at the specified position
    private GameObject SpawnPlant(NodeGraph graphToSpawn, Vector3 position)
    {
        if (plantPrefab == null)
        {
            Debug.LogError("Cannot spawn plant: Plant prefab not assigned.");
            return null;
        }

        Debug.Log($"SpawnPlant called with position: {position}");
    
        // Instantiate the plant prefab
        GameObject plantObj = Instantiate(plantPrefab, position, Quaternion.identity, plantParent);
    
        // Verify the position was actually applied
        Debug.Log($"Plant instantiated at position: {plantObj.transform.position}");

        // Get the PlantGrowth component
        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            // Create a deep copy of the node graph to prevent modifications affecting the original
            NodeGraph graphCopy = CloneNodeGraph(graphToSpawn);

            // Initialize the plant with the deep copy
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

    // Create a deep copy of a NodeGraph
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

            // Deep copy effects
            newNode.effects = CloneEffectsList(originalNode.effects);
            copy.nodes.Add(newNode);
        }

        return copy;
    }

    // Create a deep copy of a list of NodeEffectData
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
    
    // Generate a randomized position within the given radius, with simple increments
    // Generate a randomized position within the given radius
    private Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition)
    {
        // First, let's add comprehensive debugging
        Debug.Log($"Starting position randomization from center: {centerPosition}, radius: {spawnRadius}, increment: {spawnRadiusIncrement}");
    
        // Critical check - if radius is too small, just return the center
        if (spawnRadius < 0.01f)
        {
            // Debug.LogWarning("Spawn radius too small (<0.01), using center position");
            return centerPosition;
        }
    
        // Generate a random angle in radians (0 to 2π)
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        Debug.Log($"Random angle: {randomAngle} radians ({randomAngle * Mathf.Rad2Deg} degrees)");
    
        // Convert angle to direction vector
        Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        Debug.Log($"Direction vector: {direction}");
    
        // Get a random distance within spawn radius
        float randomDistance = Random.Range(0.05f, spawnRadius);
        Debug.Log($"Random distance: {randomDistance}");
    
        // Calculate actual offset
        float offsetX = direction.x * randomDistance;
        float offsetY = direction.y * randomDistance;
        Debug.Log($"Raw offset: ({offsetX}, {offsetY})");
    
        // Apply increment if needed
        if (spawnRadiusIncrement > 0.001f)
        {
            float originalX = offsetX;
            float originalY = offsetY;
        
            offsetX = Mathf.Round(offsetX / spawnRadiusIncrement) * spawnRadiusIncrement;
            offsetY = Mathf.Round(offsetY / spawnRadiusIncrement) * spawnRadiusIncrement;
        
            Debug.Log($"Snapped offset: ({offsetX}, {offsetY}) from ({originalX}, {originalY})");
        }
    
        // Create the final position
        Vector3 randomizedPosition = centerPosition + new Vector3(offsetX, offsetY, 0f);
        Debug.Log($"FINAL randomized position: {randomizedPosition}");
    
        return randomizedPosition;
    }
}
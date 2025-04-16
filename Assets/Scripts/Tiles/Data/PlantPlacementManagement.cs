using System.Collections.Generic;
using UnityEngine;

public class PlantPlacementManager : MonoBehaviour
{
    public static PlantPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private NodeEditorGridController nodeEditorGrid;
    [SerializeField] private Transform plantParent;
    [SerializeField] private TileInteractionManager tileInteractionManager; // Add this reference
    [SerializeField] private PlantGrowthModifierManager growthModifierManager; // Add this reference


    [Header("Planting Settings")]
    [Tooltip("Maximum radius from cell center for random seed placement (in units)")]
    [SerializeField] private float spawnRadius = 0.25f;
    
    [Tooltip("Increment for position randomization (in pixels, for pixel-perfect placement)")]
    [SerializeField] private float spawnRadiusIncrement = 4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
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
        
        // Get the tile at this position
        TileDefinition tileDef = null;
        if (tileInteractionManager != null)
        {
            // Get the tile definition using TileInteractionManager's method (we'll need to make this public)
            tileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
        }
        
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
    
    private Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition)
    {
        if (spawnRadius <= 0f || spawnRadiusIncrement <= 0f)
        {
            return centerPosition; // No randomization if settings are invalid
        }
        
        // Get a random direction
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        
        // Get a random distance within spawn radius
        float randomDistance = Random.Range(0f, spawnRadius);
        
        // Round to the nearest increment for pixel-perfect placement
        // Convert units to pixels, round, then convert back to units
        float pixelsPerUnit = 1f / (spawnRadiusIncrement / 100f); // Adjust this if your game has a different PPU
        
        float offsetX = direction.x * randomDistance;
        float offsetY = direction.y * randomDistance;
        
        // Round to nearest pixel increment
        offsetX = Mathf.Round(offsetX * pixelsPerUnit) / pixelsPerUnit;
        offsetY = Mathf.Round(offsetY * pixelsPerUnit) / pixelsPerUnit;
        
        // Create the final position
        Vector3 randomizedPosition = centerPosition + new Vector3(offsetX, offsetY, 0f);
        
        if (showDebugMessages)
        {
            Debug.Log($"Randomized plant position: {randomizedPosition} (offset: {offsetX}, {offsetY})");
        }
        
        return randomizedPosition;
    }

    // Spawn a plant using the given node graph at the specified position
    private GameObject SpawnPlant(NodeGraph graphToSpawn, Vector3 position)
    {
        if (plantPrefab == null)
        {
            Debug.LogError("Cannot spawn plant: Plant prefab not assigned.");
            return null;
        }

        // Instantiate the plant prefab
        GameObject plantObj = Instantiate(plantPrefab, position, Quaternion.identity, plantParent);

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
}
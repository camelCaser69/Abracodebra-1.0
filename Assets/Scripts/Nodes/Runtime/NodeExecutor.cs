// FILE: Assets/Scripts/Nodes/Runtime/NodeExecutor.cs (UPDATED for Seed System)
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class NodeExecutor : MonoBehaviour
{
    [Header("Plant Spawning")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private GardenerController gardener;

    [Header("UI References (Legacy Support)")]
    [SerializeField] private NodeEditorGridController nodeEditorGrid; // Keep for fallback

    [Header("Debugging")]
    [SerializeField] private TMP_Text debugOutput;
    [SerializeField] private bool showDebugLogs = false;

    private void Update()
    {
        // Legacy spacebar planting (now shows seed selection)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AttemptPlantingWithSeedSelection();
        }
    }

    /// <summary>
    /// NEW: Attempts to plant using seed selection system
    /// </summary>
    public void AttemptPlantingWithSeedSelection()
    {
        // Validate basic requirements
        if (!ValidateBasicPlantingRequirements())
            return;

        // Use seed selection UI
        if (SeedSelectionUI.Instance != null)
        {
            SeedSelectionUI.Instance.AttemptAutoSelection(OnSeedSelectedForPlanting);
        }
        else
        {
            DebugLogError("Seed Selection UI not found! Cannot plant with seed selection.");
            // Fallback to legacy system
            SpawnPlantFromUIGraph();
        }
    }

    /// <summary>
    /// NEW: Called when a seed is selected for planting
    /// </summary>
    private void OnSeedSelectedForPlanting(SeedInstance selectedSeed)
    {
        if (selectedSeed == null)
        {
            DebugLogError("Selected seed is null!");
            return;
        }

        bool success = SpawnPlantFromSeedInstance(selectedSeed);
        
        if (success)
        {
            // Remove the seed from inventory (it was planted)
            if (PlayerGeneticsInventory.Instance != null)
            {
                PlayerGeneticsInventory.Instance.RemoveSeed(selectedSeed);
                DebugLog($"Planted and consumed seed: {selectedSeed.seedName}");
            }
        }
    }

    /// <summary>
    /// NEW: Spawns a plant from a SeedInstance
    /// </summary>
    public bool SpawnPlantFromSeedInstance(SeedInstance seedInstance)
    {
        // Validate seed instance
        if (seedInstance == null)
        {
            DebugLogError("Cannot spawn plant: SeedInstance is null!");
            return false;
        }

        if (!seedInstance.IsValidForPlanting())
        {
            DebugLogError($"Cannot spawn plant: Seed '{seedInstance.seedName}' is not valid for planting!");
            return false;
        }

        // Validate basic requirements
        if (!ValidateBasicPlantingRequirements())
            return false;

        DebugLog($"Spawning plant from seed: {seedInstance.seedName}");

        // Convert seed to NodeGraph
        NodeGraph graphToSpawn = seedInstance.ToNodeGraph();
        
        if (graphToSpawn == null || graphToSpawn.nodes == null || graphToSpawn.nodes.Count == 0)
        {
            DebugLogError($"Failed to convert seed '{seedInstance.seedName}' to NodeGraph!");
            return false;
        }

        // Spawn the plant
        return SpawnPlantFromNodeGraph(graphToSpawn, seedInstance.seedName);
    }

    /// <summary>
    /// LEGACY: Spawns plant from UI graph (kept for backward compatibility)
    /// </summary>
    public void SpawnPlantFromUIGraph()
    {
        // --- Validations ---
        if (nodeEditorGrid == null) { DebugLogError("Node Editor Grid Controller not assigned!"); return; }
        if (!ValidateBasicPlantingRequirements()) return;

        // Get the current graph state from the UI grid
        NodeGraph graphToSpawn = nodeEditorGrid.GetCurrentUIGraph();

        if (graphToSpawn == null || graphToSpawn.nodes == null || graphToSpawn.nodes.Count == 0) {
             DebugLog("No nodes in UI graph to spawn.");
             return;
        }

        // Validate if the graph is spawnable
        bool seedFound = graphToSpawn.nodes.Any(node => node != null && node.effects != null && 
            node.effects.Any(eff => eff != null && eff.effectType == NodeEffectType.SeedSpawn && eff.isPassive));
        
        if (!seedFound) {
            DebugLog("Cannot spawn plant: Node chain lacks a passive SeedSpawn effect.");
            return;
        }

        DebugLog($"Spawning plant from UI graph with {graphToSpawn.nodes.Count} nodes...");
        SpawnPlantFromNodeGraph(graphToSpawn, "UI Graph Plant");
    }

    /// <summary>
    /// Core plant spawning logic (used by both new and legacy systems)
    /// </summary>
    private bool SpawnPlantFromNodeGraph(NodeGraph graphToSpawn, string plantName)
    {
        // Determine spawn position and parent
        Vector2 spawnPos = gardener.GetPlantingPosition();
        Transform plantParent = EcosystemManager.Instance?.plantParent;

        // Instantiate the plant prefab
        GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity, plantParent);
        plantObj.name = $"Plant_{plantName}_{System.DateTime.Now:HHmmss}"; // Add timestamp for uniqueness

        // Get the PlantGrowth component
        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            // Create a DEEP COPY of the NodeGraph
            NodeGraph graphCopy = CloneNodeGraph(graphToSpawn);

            // Initialize the plant with the deep copy
            growthComponent.InitializeAndGrow(graphCopy);
            DebugLog($"Plant '{plantName}' spawned and initialized successfully.");
            return true;
        }
        else
        {
            // Log error and destroy invalid object if PlantGrowth is missing
            DebugLogError($"Prefab '{plantPrefab.name}' missing PlantGrowth component! Destroying spawned object.");
            Destroy(plantObj);
            return false;
        }
    }

    /// <summary>
    /// Validates basic requirements for planting
    /// </summary>
    private bool ValidateBasicPlantingRequirements()
    {
        if (plantPrefab == null) { DebugLogError("Plant prefab not assigned!"); return false; }
        if (gardener == null) { DebugLogError("Gardener Controller not assigned!"); return false; }
        return true;
    }

    /// <summary>
    /// Creates a deep copy of a NodeGraph
    /// </summary>
    private NodeGraph CloneNodeGraph(NodeGraph original)
    {
        NodeGraph graphCopy = new NodeGraph();
        graphCopy.nodes = new List<NodeData>(original.nodes.Count);

        foreach(NodeData originalNodeData in original.nodes)
        {
            if (originalNodeData == null) {
                DebugLogWarning("Encountered null NodeData in graph during copy. Skipping.");
                continue;
            }

             // Create a new NodeData instance
             NodeData newNodeData = new NodeData {
                nodeId = originalNodeData.nodeId,
                nodeDisplayName = originalNodeData.nodeDisplayName,
                orderIndex = originalNodeData.orderIndex,
                canBeDeleted = originalNodeData.canBeDeleted,
                effects = CloneEffectsList(originalNodeData.effects)
            };
            graphCopy.nodes.Add(newNodeData);
        }

        return graphCopy;
    }

    /// <summary>
    /// Creates a deep copy of a list of NodeEffectData
    /// </summary>
    private List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
    {
        if (originalList == null) return new List<NodeEffectData>();

        List<NodeEffectData> newList = new List<NodeEffectData>(originalList.Count);
        foreach(var originalEffect in originalList)
        {
            if(originalEffect == null) {
                DebugLogWarning("Encountered null NodeEffectData in list during copy. Skipping.");
                continue;
            }

             NodeEffectData newEffect = new NodeEffectData {
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

    // --- Helper methods for logging ---
    private void DebugLog(string msg) {
        if (showDebugLogs) Debug.Log($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }
    
    private void DebugLogError(string msg) {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
    
    private void DebugLogWarning(string msg) {
        if (showDebugLogs) Debug.LogWarning($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"WARNING: {msg}\n";
    }

    // --- Public API Methods ---

    /// <summary>
    /// Public method for external scripts to plant a specific seed
    /// </summary>
    public bool PlantSeed(SeedInstance seed)
    {
        return SpawnPlantFromSeedInstance(seed);
    }

    /// <summary>
    /// Public method to get available plantable seeds count
    /// </summary>
    public int GetPlantableSeedsCount()
    {
        if (PlayerGeneticsInventory.Instance == null)
            return 0;
            
        return PlayerGeneticsInventory.Instance.GetPlantableSeeds().Count;
    }
}
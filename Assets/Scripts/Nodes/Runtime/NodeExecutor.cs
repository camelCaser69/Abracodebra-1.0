// FILE: Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class NodeExecutor : MonoBehaviour
{
    [Header("UI Graph Source")]
    [SerializeField] private NodeEditorGridController nodeEditorGrid;

    [Header("Plant Spawning")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private GardenerController gardener;

    [Header("Debugging")]
    [SerializeField] private TMP_Text debugOutput;

    private void Update()
    {
        // Simple check for Space key press to spawn
        if (Input.GetKeyDown(KeyCode.Space))
        {
             SpawnPlantFromUIGraph();
        }
    }

    public void SpawnPlantFromUIGraph()
    {
        // --- Validations ---
        if (nodeEditorGrid == null) { DebugLogError("Node Editor Grid Controller not assigned!"); return; }
        if (plantPrefab == null) { DebugLogError("Plant prefab not assigned!"); return; }
        if (gardener == null) { DebugLogError("Gardener Controller not assigned!"); return; }

        // Get the current graph state from the UI grid
        NodeGraph graphToSpawn = nodeEditorGrid.GetCurrentUIGraph();

        if (graphToSpawn == null || graphToSpawn.nodes == null || graphToSpawn.nodes.Count == 0) {
             DebugLog("No nodes in UI graph to spawn.");
             return;
        }

        // Validate if the graph is spawnable (e.g., requires a SeedSpawn effect)
        bool seedFound = graphToSpawn.nodes.Any(node => node != null && node.effects != null && node.effects.Any(eff => eff != null && eff.effectType == NodeEffectType.SeedSpawn && eff.isPassive));
        if (!seedFound) {
            DebugLog("Cannot spawn plant: Node chain lacks a passive SeedSpawn effect.");
            return;
        }

        DebugLog($"Spawning plant from UI graph with {graphToSpawn.nodes.Count} nodes...");

        // Determine spawn position and parent
        Vector2 spawnPos = gardener.GetPlantingPosition();
        Transform plantParent = EcosystemManager.Instance?.plantParent; // Use optional chaining

        // Instantiate the plant prefab
        GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity, plantParent); // Assign parent during instantiate

        // Get the PlantGrowth component
        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            // --- Create a DEEP COPY of the NodeGraph ---
            // This is crucial so modifications to the UI graph don't affect running plants,
            // and vice-versa.
            NodeGraph graphCopy = new NodeGraph();
            graphCopy.nodes = new List<NodeData>(graphToSpawn.nodes.Count); // Initialize with capacity

            foreach(NodeData originalNodeData in graphToSpawn.nodes)
            {
                // Ensure original node data is not null
                if (originalNodeData == null) {
                    DebugLogWarning("Encountered null NodeData in UI graph during copy. Skipping.");
                    continue;
                }

                 // Create a new NodeData instance
                 NodeData newNodeData = new NodeData {
                    nodeId = originalNodeData.nodeId, // Copy ID (or generate new one?)
                    nodeDisplayName = originalNodeData.nodeDisplayName,
                    orderIndex = originalNodeData.orderIndex,
                    canBeDeleted = originalNodeData.canBeDeleted, // Copy runtime flags if needed
                    // Create a deep copy of the effects list using the *updated* helper method
                    effects = CloneEffectsList(originalNodeData.effects)
                };
                graphCopy.nodes.Add(newNodeData);
            }

            // Initialize the plant with the deep copy
            growthComponent.InitializeAndGrow(graphCopy);
            DebugLog("Plant spawned and initialized.");
        }
        else
        {
             // Log error and destroy invalid object if PlantGrowth is missing
             DebugLogError($"Prefab '{plantPrefab.name}' missing PlantGrowth component! Destroying spawned object.");
             Destroy(plantObj);
        }
    }

    /// <summary>
    /// Creates a deep copy of a list of NodeEffectData, including the ScentDefinition reference.
    /// </summary>
    private List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
    {
        if (originalList == null) return new List<NodeEffectData>(); // Handle null input list

        List<NodeEffectData> newList = new List<NodeEffectData>(originalList.Count);
        foreach(var originalEffect in originalList)
        {
            // Ensure original effect data is not null
            if(originalEffect == null) {
                DebugLogWarning("Encountered null NodeEffectData in list during copy. Skipping.");
                continue;
            }

             // Create a new NodeEffectData instance and copy all relevant fields
             NodeEffectData newEffect = new NodeEffectData {
                 effectType = originalEffect.effectType,
                 primaryValue = originalEffect.primaryValue,
                 secondaryValue = originalEffect.secondaryValue,
                 isPassive = originalEffect.isPassive,
                 // <<< FIXED: Explicitly copy the ScentDefinition reference >>>
                 scentDefinitionReference = originalEffect.scentDefinitionReference
             };
             newList.Add(newEffect);
        }
        return newList;
    }

    // Helper methods for logging
    private void DebugLog(string msg) {
        Debug.Log($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }
    private void DebugLogError(string msg) {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
     private void DebugLogWarning(string msg) {
        Debug.LogWarning($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"WARNING: {msg}\n";
    }
}
// FILE: Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class NodeExecutor : MonoBehaviour
{
    [Header("Plant Spawning")]
    [SerializeField] private GameObject plantPrefab;
    // Gardener reference might not be needed here if PlantPlacementManager handles position
    // [SerializeField] private GardenerController gardener; 

    [Header("Debugging")]
    [SerializeField] private TMP_Text debugOutput;

    // This method is now called by PlantPlacementManager or a similar system
    // when the player uses the SeedPouch tool.
    // It assumes NodeEditorGridController.Instance.GetCurrentSeedInSlot() provides the seed.
    public GameObject SpawnPlantFromSeedInSlot(Vector3 plantingPosition, Transform parentTransform)
    {
        // --- Validations ---
        if (NodeEditorGridController.Instance == null) { DebugLogError("Node Editor Grid Controller not found!"); return null; }
        if (plantPrefab == null) { DebugLogError("Plant prefab not assigned!"); return null; }

        NodeData seedNodeData = NodeEditorGridController.Instance.GetCurrentSeedInSlot();

        if (seedNodeData == null) {
            DebugLog("No seed in slot to plant.");
            return null;
        }
        if (!seedNodeData.IsSeed()) { // Should be guaranteed if it's in the seed slot, but double-check
            DebugLogError("Item in seed slot is not a valid seed!");
            return null;
        }

        DebugLog($"Attempting to plant seed '{seedNodeData.nodeDisplayName}' with {seedNodeData.storedSequence.nodes.Count} internal nodes...");

        // --- Construct the final NodeGraph for the plant ---
        NodeGraph finalGraphForPlant = new NodeGraph();
        finalGraphForPlant.nodes = new List<NodeData>();

        // 1. Add a CLONE of the seed node itself. Its effects provide base stats.
        // Ensure its orderIndex is suitable (e.g., 0 or handled by PlantGrowth if it re-orders)
        NodeData clonedSeedNodeInstance = new NodeData {
            nodeId = System.Guid.NewGuid().ToString(), // New ID for this plant instance
            nodeDisplayName = seedNodeData.nodeDisplayName,
            effects = CloneEffectsList(seedNodeData.effects), // Deep copy effects
            orderIndex = 0, // Seed node is conceptually the first
            canBeDeleted = false, // Planted nodes are part of the plant, not UI deletable
            storedSequence = new NodeGraph() // The planted instance doesn't need to store the sequence again
        };
        finalGraphForPlant.nodes.Add(clonedSeedNodeInstance);

        // 2. Add CLONES of nodes from the seed's storedSequence.
        int currentOrderIndex = 1;
        foreach (NodeData nodeInSeedSequence in seedNodeData.storedSequence.nodes.OrderBy(n => n.orderIndex))
        {
            if (nodeInSeedSequence == null) continue;
            NodeData clonedSequenceNodeInstance = new NodeData {
                nodeId = System.Guid.NewGuid().ToString(),
                nodeDisplayName = nodeInSeedSequence.nodeDisplayName,
                effects = CloneEffectsList(nodeInSeedSequence.effects),
                orderIndex = currentOrderIndex++,
                canBeDeleted = false,
                storedSequence = new NodeGraph()
            };
            finalGraphForPlant.nodes.Add(clonedSequenceNodeInstance);
        }
        
        DebugLog($"Constructed final graph for plant with {finalGraphForPlant.nodes.Count} total nodes (1 seed + {finalGraphForPlant.nodes.Count -1} from sequence).");

        // Instantiate the plant prefab
        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            // Initialize the plant with the constructed final graph
            // PlantGrowth.InitializeAndGrow should handle its own deep copy if it modifies the graph at runtime.
            // For safety, we pass a deep copy to PlantGrowth here as well.
            NodeGraph graphCopyForPlantGrowth = new NodeGraph();
            graphCopyForPlantGrowth.nodes = new List<NodeData>();
            foreach (NodeData nodeToCopy in finalGraphForPlant.nodes)
            {
                 graphCopyForPlantGrowth.nodes.Add(new NodeData {
                     nodeId = nodeToCopy.nodeId, // Can reuse ID here as it's for this specific plant
                     nodeDisplayName = nodeToCopy.nodeDisplayName,
                     effects = CloneEffectsList(nodeToCopy.effects), // Crucial: clone effects again
                     orderIndex = nodeToCopy.orderIndex,
                     canBeDeleted = nodeToCopy.canBeDeleted,
                     storedSequence = new NodeGraph() // No nested sequences for nodes within a plant graph
                 });
            }

            growthComponent.InitializeAndGrow(graphCopyForPlantGrowth);
            DebugLog($"Plant '{plantObj.name}' spawned and initialized with seed '{seedNodeData.nodeDisplayName}'.");
            return plantObj;
        }
        else
        {
             DebugLogError($"Prefab '{plantPrefab.name}' missing PlantGrowth component! Destroying spawned object.");
             Destroy(plantObj);
             return null;
        }
    }

    private List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
    {
        if (originalList == null) return new List<NodeEffectData>();
        List<NodeEffectData> newList = new List<NodeEffectData>(originalList.Count);
        foreach(var originalEffect in originalList) {
            if(originalEffect == null) continue;
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

    private void DebugLog(string msg) {
        Debug.Log($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }
    private void DebugLogError(string msg) {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
}
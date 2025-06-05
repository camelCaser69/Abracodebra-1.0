// FILE: Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class NodeExecutor : MonoBehaviour
{
    [Header("Plant Spawning")]
    [SerializeField] private GameObject plantPrefab;

    [Header("Debugging")]
    [SerializeField] private TMP_Text debugOutput;

    // Public static helper for cloning effects, useful in other classes too
    public static List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
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


    public GameObject SpawnPlantFromSeedInSlot(Vector3 plantingPosition, Transform parentTransform)
    {
        if (NodeEditorGridController.Instance == null) { DebugLogError("Node Editor Grid Controller not found!"); return null; }
        if (plantPrefab == null) { DebugLogError("Plant prefab not assigned!"); return null; }

        NodeData seedNodeDataInSlot = NodeEditorGridController.Instance.GetCurrentSeedInSlot();

        if (seedNodeDataInSlot == null) {
            DebugLog("No seed in slot to plant.");
            return null;
        }
        if (!seedNodeDataInSlot.IsSeed()) {
            DebugLogError("Item in seed slot is not a valid seed!");
            return null;
        }

        // Get a CLONE of the sequence currently being edited in the UI for this seed.
        // This represents the *intended* internal sequence of the seed for planting.
        NodeGraph sequenceForPlant = NodeEditorGridController.Instance.GetCurrentGraphInEditorForSpawning();

        DebugLog($"Attempting to plant seed '{seedNodeDataInSlot.nodeDisplayName}' with {sequenceForPlant.nodes.Count} internal nodes from editor...");

        NodeGraph finalGraphForPlant = new NodeGraph();
        finalGraphForPlant.nodes = new List<NodeData>();

        // 1. Add a CLONE of the seed node itself from the slot.
        NodeData clonedSeedNodeInstance = new NodeData {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = seedNodeDataInSlot.nodeDisplayName,
            effects = CloneEffectsList(seedNodeDataInSlot.effects),
            orderIndex = 0,
            canBeDeleted = false,
            storedSequence = new NodeGraph() // The instance on the plant doesn't store a sequence
        };
        finalGraphForPlant.nodes.Add(clonedSeedNodeInstance);

        // 2. Add CLONES of nodes from the editor's representation of the seed's sequence.
        int currentOrderIndex = 1;
        foreach (NodeData nodeInUISequence in sequenceForPlant.nodes.OrderBy(n => n.orderIndex)) // sequenceForPlant is already a clone
        {
            if (nodeInUISequence == null) continue;
            NodeData clonedSequenceNodeInstance = new NodeData {
                nodeId = System.Guid.NewGuid().ToString(), // New ID for this plant's node instance
                nodeDisplayName = nodeInUISequence.nodeDisplayName,
                effects = CloneEffectsList(nodeInUISequence.effects), // Effects are already cloned if GetCurrentGraphInEditorForSpawning works correctly
                orderIndex = currentOrderIndex++,
                canBeDeleted = false,
                storedSequence = new NodeGraph() // Nodes within the plant graph do not have sub-sequences
            };
            finalGraphForPlant.nodes.Add(clonedSequenceNodeInstance);
        }
        
        DebugLog($"Constructed final graph for plant with {finalGraphForPlant.nodes.Count} total nodes (1 seed + {finalGraphForPlant.nodes.Count -1} from sequence).");

        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            // The finalGraphForPlant is already composed of new NodeData instances with cloned effects.
            // PlantGrowth.InitializeAndGrow should ideally work with this directly.
            // If PlantGrowth further modifies the graph, it should manage its own copies.
            growthComponent.InitializeAndGrow(finalGraphForPlant);
            DebugLog($"Plant '{plantObj.name}' spawned and initialized with seed '{seedNodeDataInSlot.nodeDisplayName}'.");
            return plantObj;
        }
        else
        {
             DebugLogError($"Prefab '{plantPrefab.name}' missing PlantGrowth component! Destroying spawned object.");
             Destroy(plantObj);
             return null;
        }
    }

    // DebugLog and DebugLogError methods remain the same
    private void DebugLog(string msg) {
        Debug.Log($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }
    private void DebugLogError(string msg) {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
}
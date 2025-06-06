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
        
        // Ensure the seed from the slot has its sequence object ready (if it's a seed)
        seedNodeDataInSlot.EnsureSeedSequenceInitialized(); 

        NodeGraph sequenceForPlant = NodeEditorGridController.Instance.GetCurrentGraphInEditorForSpawning();

        DebugLog($"Attempting to plant seed '{seedNodeDataInSlot.nodeDisplayName}' with {sequenceForPlant.nodes.Count} internal nodes from editor...");

        NodeGraph finalGraphForPlant = new NodeGraph();
        finalGraphForPlant.nodes = new List<NodeData>();

        NodeData clonedSeedNodeInstance = new NodeData {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = seedNodeDataInSlot.nodeDisplayName,
            effects = CloneEffectsList(seedNodeDataInSlot.effects),
            orderIndex = 0,
            canBeDeleted = false,
            storedSequence = null // The seed node *instance on the plant* doesn't store the sequence again
        };
        finalGraphForPlant.nodes.Add(clonedSeedNodeInstance);

        int currentOrderIndex = 1;
        // sequenceForPlant.nodes are already new instances from GetCurrentGraphInEditorForSpawning,
        // and their storedSequence was set to null there.
        foreach (NodeData nodeInUISequenceClone in sequenceForPlant.nodes.OrderBy(n => n.orderIndex)) 
        {
            if (nodeInUISequenceClone == null) continue;
            // We can directly add these clones as their storedSequence should already be null
            nodeInUISequenceClone.nodeId = System.Guid.NewGuid().ToString(); // Give it a new runtime ID
            nodeInUISequenceClone.orderIndex = currentOrderIndex++;
            nodeInUISequenceClone.canBeDeleted = false;
            // storedSequence is already null from GetCurrentGraphInEditorForSpawning
            finalGraphForPlant.nodes.Add(nodeInUISequenceClone);
        }
        
        DebugLog($"Constructed final graph for plant with {finalGraphForPlant.nodes.Count} total nodes (1 seed + {finalGraphForPlant.nodes.Count -1} from sequence).");

        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
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
    
    public GameObject SpawnPlantFromInventorySeed(NodeData seedNodeData, Vector3 plantingPosition, Transform parentTransform)
    {
        if (seedNodeData == null || !seedNodeData.IsSeed())
        {
            DebugLogError("Invalid seed data provided for planting!");
            return null;
        }
        
        if (plantPrefab == null)
        {
            DebugLogError("Plant prefab not assigned!");
            return null;
        }

        // Ensure the seed from inventory has its sequence object ready (if it's a seed)
        seedNodeData.EnsureSeedSequenceInitialized();
        
        DebugLog($"Spawning plant from inventory seed '{seedNodeData.nodeDisplayName}' with {seedNodeData.storedSequence?.nodes?.Count ?? 0} internal nodes...");
        
        if (seedNodeData.storedSequence != null && seedNodeData.storedSequence.nodes != null)
        {
            DebugLog($"Seed's stored sequence contains:");
            foreach (var node in seedNodeData.storedSequence.nodes)
            {
                if (node != null)
                    DebugLog($"  - {node.nodeDisplayName} (order: {node.orderIndex})");
            }
        }
        else
        {
            DebugLog("Seed has no stored sequence or nodes (or it's null)!");
        }
        
        NodeGraph finalGraphForPlant = new NodeGraph();
        finalGraphForPlant.nodes = new List<NodeData>();
        
        NodeData clonedSeedNodeInstance = new NodeData
        {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = seedNodeData.nodeDisplayName,
            effects = CloneEffectsList(seedNodeData.effects),
            orderIndex = 0,
            canBeDeleted = false,
            storedSequence = null // Seed instance on plant doesn't re-store sequence
        };
        finalGraphForPlant.nodes.Add(clonedSeedNodeInstance);
        
        int currentOrderIndex = 1;
        if (seedNodeData.storedSequence != null && seedNodeData.storedSequence.nodes != null)
        {
            foreach (NodeData nodeInSeedSequence in seedNodeData.storedSequence.nodes.OrderBy(n => n.orderIndex))
            {
                if (nodeInSeedSequence == null) continue;
                // Create a fresh clone for the plant's graph
                NodeData clonedSequenceNodeInstance = new NodeData
                {
                    nodeId = System.Guid.NewGuid().ToString(), // New runtime ID
                    nodeDisplayName = nodeInSeedSequence.nodeDisplayName,
                    effects = CloneEffectsList(nodeInSeedSequence.effects), // Deep copy effects
                    orderIndex = currentOrderIndex++,
                    canBeDeleted = false,
                    storedSequence = null // Nodes in plant sequence don't have sub-sequences
                };
                finalGraphForPlant.nodes.Add(clonedSequenceNodeInstance);
            }
        }
        
        DebugLog($"Constructed final graph for plant with {finalGraphForPlant.nodes.Count} total nodes (1 seed + {finalGraphForPlant.nodes.Count - 1} from sequence).");
        
        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);
        
        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            growthComponent.InitializeAndGrow(finalGraphForPlant);
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

    private void DebugLog(string msg) {
        Debug.Log($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }
    private void DebugLogError(string msg) {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
    
    public static NodeData CloneNodeData(NodeData original, bool preserveStoredSequence = false)
    {
        if (original == null) return null;
    
        NodeData clone = new NodeData
        {
            nodeId = System.Guid.NewGuid().ToString(), // New ID for the clone
            nodeDisplayName = original.nodeDisplayName,
            effects = CloneEffectsList(original.effects),
            orderIndex = original.orderIndex,
            canBeDeleted = original.canBeDeleted,
            storedSequence = null // Start with null
        };
    
        // Only preserve stored sequence if explicitly requested AND it's a seed
        if (preserveStoredSequence && original.IsSeed() && original.storedSequence != null)
        {
            clone.storedSequence = new NodeGraph();
            clone.storedSequence.nodes = new List<NodeData>();
        
            // Clone nodes in the sequence but ensure they don't have their own sequences
            foreach (var node in original.storedSequence.nodes)
            {
                if (node == null) continue;
            
                NodeData innerClone = new NodeData
                {
                    nodeId = System.Guid.NewGuid().ToString(),
                    nodeDisplayName = node.nodeDisplayName,
                    effects = CloneEffectsList(node.effects),
                    orderIndex = node.orderIndex,
                    canBeDeleted = node.canBeDeleted,
                    storedSequence = null // CRITICAL: Inner nodes never have sequences
                };
            
                clone.storedSequence.nodes.Add(innerClone);
            }
        }
    
        // Final safety check
        clone.ForceCleanNestedSequences();
    
        return clone;
    }
}
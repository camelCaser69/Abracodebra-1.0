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

    public static NodeData CloneNodeData(NodeData original, bool preserveStoredSequence = false)
    {
        if (original == null) return null;
    
        NodeData clone = new NodeData
        {
            nodeId = System.Guid.NewGuid().ToString(), 
            nodeDisplayName = original.nodeDisplayName,
            effects = CloneEffectsList(original.effects), 
            orderIndex = original.orderIndex,
            canBeDeleted = original.canBeDeleted,
        };
        // clone._isContainedInSequence is false by default from constructor
    
        if (preserveStoredSequence && 
            original.IsPotentialSeedContainer() && 
            !original.IsContainedInSequence /* MODIFIED: Using public getter */ && 
            original.storedSequence != null)
        {
            clone.EnsureSeedSequenceInitialized(); 
            
            if (clone.storedSequence.nodes == null) 
            {
                clone.storedSequence.nodes = new List<NodeData>();
            }
    
            foreach (var nodeInOriginalSequence in original.storedSequence.nodes)
            {
                if (nodeInOriginalSequence == null) continue;
            
                NodeData innerClone = new NodeData
                {
                    nodeId = System.Guid.NewGuid().ToString(),
                    nodeDisplayName = nodeInOriginalSequence.nodeDisplayName,
                    effects = CloneEffectsList(nodeInOriginalSequence.effects),
                    orderIndex = nodeInOriginalSequence.orderIndex,
                    canBeDeleted = nodeInOriginalSequence.canBeDeleted,
                };
                innerClone.SetContainedInSequence(true); 
                
                clone.storedSequence.nodes.Add(innerClone);
            }
        }
        else if (preserveStoredSequence && clone.IsPotentialSeedContainer() && !clone.IsContainedInSequence /* MODIFIED: Using public getter */)
        {
            clone.EnsureSeedSequenceInitialized();
        }
        else
        {
            clone.ClearStoredSequence();
        }
    
        clone.CleanForSerialization(0, "NodeExecutorClonePost"); 
    
        return clone;
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
            DebugLogError($"Item '{seedNodeDataInSlot.nodeDisplayName}' in seed slot is not a valid seed container!");
            return null;
        }
        
        seedNodeDataInSlot.EnsureSeedSequenceInitialized(); 

        NodeGraph sequenceFromEditor = NodeEditorGridController.Instance.GetCurrentGraphInEditorForSpawning();

        DebugLog($"Attempting to plant seed '{seedNodeDataInSlot.nodeDisplayName}' with {sequenceFromEditor.nodes.Count} internal nodes from editor...");

        NodeGraph finalGraphForPlant = new NodeGraph();
        finalGraphForPlant.nodes = new List<NodeData>();

        NodeData clonedSeedNodeForPlant = CloneNodeData(seedNodeDataInSlot, false); 
        if (clonedSeedNodeForPlant == null) { DebugLogError("Failed to clone seed node for plant."); return null; }
        
        clonedSeedNodeForPlant.orderIndex = 0; 
        clonedSeedNodeForPlant.canBeDeleted = false; 
        finalGraphForPlant.nodes.Add(clonedSeedNodeForPlant);

        int currentOrderIndex = 1;
        foreach (NodeData nodeFromEditorSequence in sequenceFromEditor.nodes.OrderBy(n => n.orderIndex)) 
        {
            if (nodeFromEditorSequence == null) continue;
            
            NodeData clonedSequenceNodeForPlant = CloneNodeData(nodeFromEditorSequence, false);
            if(clonedSequenceNodeForPlant == null) { 
                // MODIFIED: Use Debug.LogWarning (standard Unity logging)
                Debug.LogWarning($"Failed to clone node '{nodeFromEditorSequence.nodeDisplayName}' from editor sequence."); 
                continue; 
            }

            clonedSequenceNodeForPlant.orderIndex = currentOrderIndex++;
            clonedSequenceNodeForPlant.canBeDeleted = false; 
            finalGraphForPlant.nodes.Add(clonedSequenceNodeForPlant);
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
    
    public GameObject SpawnPlantFromInventorySeed(NodeData seedData,
        Vector3  spawnPos,
        Transform parent)
    {
        // Validate -----------------------------------------------------------
        if (seedData == null || !seedData.IsSeed())
        {
            DebugLogError("SpawnPlantFromInventorySeed called with invalid seed.");
            return null;
        }
        if (plantPrefab == null)
        {
            DebugLogError("Plant prefab not assigned.");
            return null;
        }

        // Clone seed + internal sequence ------------------------------------
        seedData.EnsureSeedSequenceInitialized();

        NodeGraph graph = new NodeGraph { nodes = new List<NodeData>() };

        NodeData seedClone = CloneNodeData(seedData, false);
        seedClone.orderIndex   = 0;
        seedClone.canBeDeleted = false;
        graph.nodes.Add(seedClone);

        int order = 1;
        if (seedData.storedSequence?.nodes != null)
        {
            foreach (NodeData n in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex))
            {
                NodeData clone = CloneNodeData(n, false);
                if (clone == null) continue;
                clone.orderIndex   = order++;
                clone.canBeDeleted = false;
                graph.nodes.Add(clone);
            }
        }

        // Instantiate & initialise ------------------------------------------
        GameObject plantGO = Instantiate(plantPrefab, spawnPos, Quaternion.identity, parent);
        PlantGrowth growth = plantGO.GetComponent<PlantGrowth>();

        if (growth == null)
        {
            DebugLogError("Plant prefab missing PlantGrowth component.");
            Destroy(plantGO);
            return null;
        }

        growth.InitializeAndGrow(graph);
        return plantGO;
    }


    private void DebugLog(string msg) {
        // Commented out for less console spam during normal operation
        // if (Application.isPlaying) Debug.Log($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }

    private void DebugLogError(string msg) {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private TMP_Text debugOutput;

    public static List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
    {
        if (originalList == null) return new List<NodeEffectData>();
    
        List<NodeEffectData> newList = new List<NodeEffectData>(originalList.Count);
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
        
            // --- FIX: Perform a deep copy of seedData ---
            if (originalEffect.effectType == NodeEffectType.SeedSpawn && originalEffect.seedData != null)
            {
                newEffect.seedData = new SeedSpawnData
                {
                    growthSpeed = originalEffect.seedData.growthSpeed,
                    stemLengthMin = originalEffect.seedData.stemLengthMin,
                    stemLengthMax = originalEffect.seedData.stemLengthMax,
                    leafGap = originalEffect.seedData.leafGap,
                    leafPattern = originalEffect.seedData.leafPattern,
                    stemRandomness = originalEffect.seedData.stemRandomness,
                    energyStorage = originalEffect.seedData.energyStorage,
                    cooldown = originalEffect.seedData.cooldown,
                    castDelay = originalEffect.seedData.castDelay
                };
            }
        
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

        if (preserveStoredSequence && original.IsPotentialSeedContainer() && original.storedSequence != null)
        {
            clone.EnsureSeedSequenceInitialized();
            if (clone.storedSequence.nodes == null)
            {
                clone.storedSequence.nodes = new List<NodeData>();
            }

            foreach (var nodeInOriginalSequence in original.storedSequence.nodes)
            {
                if (nodeInOriginalSequence == null) continue;
                NodeData innerClone = CloneNodeData(nodeInOriginalSequence, false); // Inner nodes never preserve sequence
                innerClone.SetContainedInSequence(true);
                clone.storedSequence.nodes.Add(innerClone);
            }
        }
        else
        {
            clone.EnsureSeedSequenceInitialized(); // Ensures a valid (but possibly empty) sequence object exists
        }

        return clone;
    }

    public GameObject SpawnPlantFromSeedInSlot(Vector3 plantingPosition, Transform parentTransform)
    {
        if (NodeEditorGridController.Instance == null) { DebugLogError("Node Editor Grid Controller not found!"); return null; }
        if (plantPrefab == null) { DebugLogError("Plant prefab not assigned!"); return null; }

        NodeData seedNodeDataInSlot = NodeEditorGridController.Instance.GetCurrentSeedInSlot();

        if (seedNodeDataInSlot == null)
        {
            DebugLog("No seed in slot to plant.");
            return null;
        }
        if (!seedNodeDataInSlot.IsSeed())
        {
            DebugLogError($"Item '{seedNodeDataInSlot.nodeDisplayName}' in seed slot is not a valid seed container!");
            return null;
        }

        seedNodeDataInSlot.EnsureSeedSequenceInitialized();

        // This call should now work
        NodeGraph sequenceFromEditor = NodeEditorGridController.Instance.GetCurrentGraphInEditorForSpawning();

        DebugLog($"Attempting to plant seed '{seedNodeDataInSlot.nodeDisplayName}' with {sequenceFromEditor.nodes.Count} internal nodes from editor...");

        NodeGraph finalGraphForPlant = new NodeGraph { nodes = new List<NodeData>() };

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
            if (clonedSequenceNodeForPlant == null) continue;

            clonedSequenceNodeForPlant.orderIndex = currentOrderIndex++;
            clonedSequenceNodeForPlant.canBeDeleted = false;
            finalGraphForPlant.nodes.Add(clonedSequenceNodeForPlant);
        }

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

    public GameObject SpawnPlantFromInventorySeed(NodeData seedData, Vector3 spawnPos, Transform parent)
    {
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

        seedData.EnsureSeedSequenceInitialized();

        NodeGraph graph = new NodeGraph { nodes = new List<NodeData>() };

        NodeData seedClone = CloneNodeData(seedData, false);
        seedClone.orderIndex = 0;
        seedClone.canBeDeleted = false;
        graph.nodes.Add(seedClone);

        int order = 1;
        if (seedData.storedSequence?.nodes != null)
        {
            foreach (NodeData n in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex))
            {
                NodeData clone = CloneNodeData(n, false);
                if (clone == null) continue;
                clone.orderIndex = order++;
                clone.canBeDeleted = false;
                graph.nodes.Add(clone);
            }
        }

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

    private void DebugLog(string msg)
    {
        if (debugOutput != null) debugOutput.text += msg + "\n";
    }

    private void DebugLogError(string msg)
    {
        Debug.LogError($"[NodeExecutor] {msg}");
        if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n";
    }
}
// Assets/Scripts/PlantSystem/Execution/NodeExecutor.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using WegoSystem;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] GameObject plantPrefab;
    [SerializeField] TMP_Text debugOutput;

    public static List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalEffects)
    {
        if (originalEffects == null) return new List<NodeEffectData>();

        var clonedList = new List<NodeEffectData>();

        foreach (var effect in originalEffects)
        {
            if (effect == null) continue;

            var clonedEffect = new NodeEffectData
            {
                effectType = effect.effectType,
                primaryValue = effect.primaryValue,
                secondaryValue = effect.secondaryValue,
                consumedOnTrigger = effect.consumedOnTrigger,
            };

            if (effect.effectType == NodeEffectType.SeedSpawn && effect.seedData != null)
            {
                clonedEffect.seedData = new SeedSpawnData
                {
                    growthSpeed = effect.seedData.growthSpeed,
                    stemLengthMin = effect.seedData.stemLengthMin,
                    stemLengthMax = effect.seedData.stemLengthMax,
                    leafGap = effect.seedData.leafGap,
                    leafPattern = effect.seedData.leafPattern,
                    stemRandomness = effect.seedData.stemRandomness,
                    energyStorage = effect.seedData.energyStorage,
                    cooldown = effect.seedData.cooldown,
                    castDelay = effect.seedData.castDelay,
                    maxBerries = effect.seedData.maxBerries
                };
            }

            clonedList.Add(clonedEffect);
        }

        return clonedList;
    }

    public static NodeData CloneNode(NodeData original)
    {
        if (original == null) return null;

        var clone = new NodeData
        {
            nodeId = Guid.NewGuid().ToString(),
            definitionName = original.definitionName,
            nodeDisplayName = original.nodeDisplayName,
            effects = CloneEffectsList(original.effects),
            orderIndex = original.orderIndex,
            canBeDeleted = original.canBeDeleted
        };

        if (original.IsSeed() && original.storedSequence != null)
        {
            clone.EnsureSeedSequenceInitialized();

            foreach (var nodeInSequence in original.storedSequence.nodes)
            {
                if (nodeInSequence == null) continue;

                var sequenceNodeClone = CloneNodeWithoutSequence(nodeInSequence);
                sequenceNodeClone.SetPartOfSequence(true);
                clone.storedSequence.nodes.Add(sequenceNodeClone);
            }
        }

        return clone;
    }

    public static NodeData CloneNodeWithoutSequence(NodeData original)
    {
        if (original == null) return null;

        var clone = new NodeData
        {
            nodeId = Guid.NewGuid().ToString(),
            definitionName = original.definitionName,
            nodeDisplayName = original.nodeDisplayName,
            effects = CloneEffectsList(original.effects),
            orderIndex = original.orderIndex,
            canBeDeleted = original.canBeDeleted
        };

        if (Debug.isDebugBuild)
        {
            Debug.Log($"[NodeExecutor] CloneNodeWithoutSequence: '{original.nodeDisplayName}' - Original effects: {original.effects?.Count ?? 0}, Clone effects: {clone.effects?.Count ?? 0}");
        }

        return clone;
    }

    public GameObject SpawnPlantFromSeedInSlot(Vector3 plantingPosition, Transform parentTransform)
    {
        if (NodeEditorGridController.Instance == null)
        {
            Debug.LogError("[NodeExecutor] Node Editor Grid Controller not found!");
            return null;
        }

        if (plantPrefab == null)
        {
            Debug.LogError("[NodeExecutor] Plant prefab not assigned!");
            return null;
        }

        NodeData seedInSlot = NodeEditorGridController.Instance.GetCurrentSeedInSlot();

        if (seedInSlot == null)
        {
            Debug.Log("[NodeExecutor] No seed in slot to plant.");
            return null;
        }

        if (!seedInSlot.IsSeed())
        {
            Debug.LogError($"[NodeExecutor] Item '{seedInSlot.nodeDisplayName}' in seed slot is not a valid seed!");
            return null;
        }

        Debug.Log($"[NodeExecutor] Seed '{seedInSlot.nodeDisplayName}' has {seedInSlot.effects?.Count ?? 0} effects:");
        if (seedInSlot.effects != null)
        {
            foreach (var effect in seedInSlot.effects)
            {
                Debug.Log($"  - {effect.effectType} (passive: {effect.IsPassive})");
            }
        }

        NodeGraph editorSequence = NodeEditorGridController.Instance.GetCurrentGraphInEditorForSpawning();
        Debug.Log($"[NodeExecutor] Editor sequence has {editorSequence.nodes.Count} nodes");

        NodeGraph plantGraph = new NodeGraph { nodes = new List<NodeData>() };

        // 1. Add the seed itself to the graph
        NodeData seedClone = CloneNodeWithoutSequence(seedInSlot);
        seedClone.orderIndex = 0;
        seedClone.canBeDeleted = false; // The root seed cannot be deleted from a grown plant
        plantGraph.nodes.Add(seedClone);

        Debug.Log($"[NodeExecutor] Seed clone has {seedClone.effects?.Count ?? 0} effects");

        // 2. Add all nodes from the editor sequence
        int orderIndex = 1;
        foreach (NodeData editorNode in editorSequence.nodes.OrderBy(n => n.orderIndex))
        {
            if (editorNode == null) continue;

            Debug.Log($"[NodeExecutor] Editor node '{editorNode.nodeDisplayName}' has {editorNode.effects?.Count ?? 0} effects:");
            if (editorNode.effects != null)
            {
                foreach (var effect in editorNode.effects)
                {
                    Debug.Log($"  - {effect.effectType} (passive: {effect.IsPassive}, primary: {effect.primaryValue}, secondary: {effect.secondaryValue})");
                }
            }

            NodeData nodeClone = CloneNodeWithoutSequence(editorNode);
            nodeClone.orderIndex = orderIndex++;
            nodeClone.canBeDeleted = false; // Nodes in a grown plant are fixed
            plantGraph.nodes.Add(nodeClone);

            Debug.Log($"[NodeExecutor] Node clone has {nodeClone.effects?.Count ?? 0} effects");
        }

        Debug.Log($"[NodeExecutor] Final plant graph has {plantGraph.nodes.Count} nodes:");
        foreach (var node in plantGraph.nodes)
        {
            Debug.Log($"  - {node.nodeDisplayName} (order: {node.orderIndex}) with {node.effects?.Count ?? 0} effects");
            if (node.effects != null)
            {
                foreach (var effect in node.effects)
                {
                    Debug.Log($"    - {effect.effectType} (passive: {effect.IsPassive}, primary: {effect.primaryValue})");
                }
            }
        }

        GameObject plantObj = Instantiate(plantPrefab, plantingPosition, Quaternion.identity, parentTransform);

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(plantObj);
        }

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            growthComponent.InitializeAndGrow(plantGraph);
            Debug.Log($"[NodeExecutor] Plant spawned with seed '{seedInSlot.nodeDisplayName}'");
            return plantObj;
        }
        else
        {
            Debug.LogError($"[NodeExecutor] Plant prefab missing PlantGrowth component!");
            Destroy(plantObj);
            return null;
        }
    }

    public GameObject SpawnPlantFromInventorySeed(NodeData seedData, Vector3 spawnPos, Transform parent)
    {
        if (seedData == null || !seedData.IsSeed())
        {
            Debug.LogError("[NodeExecutor] Invalid seed data provided!");
            return null;
        }

        if (plantPrefab == null)
        {
            Debug.LogError("[NodeExecutor] Plant prefab not assigned!");
            return null;
        }

        NodeGraph plantGraph = new NodeGraph { nodes = new List<NodeData>() };

        // 1. Add a clone of the seed itself
        NodeData seedClone = CloneNodeWithoutSequence(seedData);
        seedClone.orderIndex = 0;
        seedClone.canBeDeleted = false;
        plantGraph.nodes.Add(seedClone);

        // 2. Add clones of nodes from the seed's stored sequence
        if (seedData.storedSequence?.nodes != null)
        {
            int orderIndex = 1;
            foreach (NodeData sequenceNode in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex))
            {
                if (sequenceNode == null) continue;

                NodeData nodeClone = CloneNodeWithoutSequence(sequenceNode);
                nodeClone.orderIndex = orderIndex++;
                nodeClone.canBeDeleted = false;
                plantGraph.nodes.Add(nodeClone);
            }
        }

        GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity, parent);

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(plantObj);
        }

        PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
        if (growthComponent != null)
        {
            growthComponent.InitializeAndGrow(plantGraph);
            return plantObj;
        }
        else
        {
            Debug.LogError("[NodeExecutor] Plant prefab missing PlantGrowth component!");
            Destroy(plantObj);
            return null;
        }
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
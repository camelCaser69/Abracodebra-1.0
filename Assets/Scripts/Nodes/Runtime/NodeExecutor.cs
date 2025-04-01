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
        if (Input.GetKeyDown(KeyCode.Space)) SpawnPlantFromUIGraph();
    }

    public void SpawnPlantFromUIGraph()
{
    if (nodeEditorGrid == null) { DebugLogError("Node Editor Grid Controller not assigned!"); return; }
    if (plantPrefab == null) { DebugLogError("Plant prefab not assigned!"); return; }
    if (gardener == null) { DebugLogError("Gardener Controller not assigned!"); return; }

    NodeGraph graphToSpawn = nodeEditorGrid.GetCurrentUIGraph();

    if (graphToSpawn == null || graphToSpawn.nodes == null || graphToSpawn.nodes.Count == 0) { DebugLog("No nodes in UI graph."); return; }

    // Use the renamed flag 'isPassive'
    bool seedFound = graphToSpawn.nodes.Any(node => node.effects != null && node.effects.Any(eff => eff.effectType == NodeEffectType.SeedSpawn && eff.isPassive)); 
    if (!seedFound) { DebugLog("Node chain lacks a passive SeedSpawn effect."); return; }

    DebugLog($"Spawning plant from UI graph with {graphToSpawn.nodes.Count} nodes...");
    Vector2 spawnPos = gardener.GetPlantingPosition();
    
    // Get parent for plants from EcosystemManager
    Transform plantParent = null;
    if (EcosystemManager.Instance != null && EcosystemManager.Instance.plantParent != null)
    {
        plantParent = EcosystemManager.Instance.plantParent;
    }
    
    // Instantiate with proper parent
    GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity, plantParent);

    PlantGrowth growthComponent = plantObj.GetComponent<PlantGrowth>();
    if (growthComponent != null)
    {
        NodeGraph graphCopy = new NodeGraph();
        graphCopy.nodes = new List<NodeData>();
        foreach(NodeData originalNodeData in graphToSpawn.nodes)
        {
             NodeData newNodeData = new NodeData {
                nodeId = originalNodeData.nodeId,
                nodeDisplayName = originalNodeData.nodeDisplayName,
                orderIndex = originalNodeData.orderIndex,
                effects = CloneEffectsList(originalNodeData.effects) // Deep copy effects
            };
            graphCopy.nodes.Add(newNodeData);
        }
        growthComponent.InitializeAndGrow(graphCopy);
        DebugLog("Plant spawned and initialized.");
    }
    else { DebugLogError($"Prefab '{plantPrefab.name}' missing PlantGrowth!"); Destroy(plantObj); }
}

    private List<NodeEffectData> CloneEffectsList(List<NodeEffectData> originalList)
    {
        if (originalList == null) return new List<NodeEffectData>();
        List<NodeEffectData> newList = new List<NodeEffectData>(originalList.Count);
        foreach(var eff in originalList)
        {
             NodeEffectData newEff = new NodeEffectData {
                 effectType = eff.effectType,
                 primaryValue = eff.primaryValue,
                 secondaryValue = eff.secondaryValue,
                 isPassive = eff.isPassive // Use the renamed flag
             };
             newList.Add(newEff);
        }
        return newList;
    }

    private void DebugLog(string msg) { Debug.Log($"[NodeExecutor] {msg}"); if (debugOutput != null) debugOutput.text += msg + "\n"; }
    private void DebugLogError(string msg) { Debug.LogError($"[NodeExecutor] {msg}"); if (debugOutput != null) debugOutput.text += $"ERROR: {msg}\n"; }
}
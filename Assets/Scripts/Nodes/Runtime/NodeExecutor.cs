using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class NodeExecutor : MonoBehaviour
{
    [Header("Execution Settings")]
    public NodeGraph currentGraph;
    public TMP_Text debugOutput;
    
    [Header("Plant Spawning")]
    public GameObject plantPrefab;
    public GardenerController gardener;

    // >>> ADD THIS <<<
    private void Update()
    {
        // Press Space to run the node chain
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteGraph();
        }
    }

    // >>> ADD THIS <<<
    public void SetGraph(NodeGraph graph)
    {
        currentGraph = graph;
    }

    public void ExecuteGraph()
    {
        if (currentGraph == null || currentGraph.nodes.Count == 0)
        {
            DebugLog("No nodes to execute!");
            return;
        }

        // Sort nodes by orderIndex (left-to-right)
        var sortedNodes = currentGraph.nodes.OrderBy(n => n.orderIndex).ToList();

        float accumulatedEnergyStorage = 0f;
        float accumulatedPhotosynthesis = 0f;
        bool seedFound = false;

        foreach (var node in sortedNodes)
        {
            DebugLog($"Processing node: {node.nodeDisplayName}");
            foreach (var eff in node.effects)
            {
                if (eff.effectType == NodeEffectType.EnergyStorage)
                    accumulatedEnergyStorage += eff.primaryValue;
                else if (eff.effectType == NodeEffectType.EnergyPhotosynthesis)
                    accumulatedPhotosynthesis += eff.primaryValue;
                else if (eff.effectType == NodeEffectType.SeedSpawn)
                    seedFound = true;
            }
        }

        if (seedFound)
        {
            SpawnPlant(accumulatedEnergyStorage, accumulatedPhotosynthesis);
        }
        else
        {
            DebugLog("No seed found in chain. Plant not spawned.");
        }
        
        DebugLog("Execution complete.");
    }

    private void SpawnPlant(float energyStorage, float photosynthesis)
    {
        if (plantPrefab == null)
        {
            DebugLog("Plant prefab not assigned!");
            return;
        }
        if (gardener == null)
        {
            DebugLog("No GardenerController found!");
            return;
        }
        Vector2 spawnPos = gardener.GetPlantingPosition();
        GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity);
        PlantGrowth growth = plantObj.GetComponent<PlantGrowth>();
        if (growth != null)
        {
            growth.maxEnergy = energyStorage;
            growth.basePhotosynthesis = photosynthesis;
        }
        DebugLog("Plant spawned.");
    }

    private void DebugLog(string msg)
    {
        Debug.Log(msg);
        if (debugOutput != null)
            debugOutput.text += msg + "\n";
    }
}

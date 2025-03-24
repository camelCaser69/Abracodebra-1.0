using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private NodeGraph currentGraph;
    [Header("Debug Settings")]
    public float waitTimeBetweenNodes = 0.5f;
    public TMP_Text debugOutput;

    [Header("Plant Prefab (assigned in inspector)")]
    public GameObject plantPrefab;  // Assign your PlantPrefab here

    private Dictionary<HexCoords, NodeData> coordsMap;
    private HashSet<string> visited;

    // For accumulating energy info from the BFS chain:
    private float accumulatedEnergyStorage  = 0f;
    private float accumulatedPhotosynthesis = 0f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ExecuteGraph();
    }

    public void SetGraph(NodeGraph graph)
    {
        currentGraph = graph;
        Debug.Log("[NodeExecutor] Graph set. Node count=" + (graph != null ? graph.nodes.Count : 0));
    }

    public NodeGraph GetGraph() => currentGraph;

    public void ExecuteGraph()
    {
        if (currentGraph == null || currentGraph.nodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No graph or no nodes to execute!");
            return;
        }
        StopAllCoroutines();
        ClearDebug();
        BuildCoordsMap();
        visited = new HashSet<string>();

        // Reset accumulators in case we want a fresh sum each time.
        accumulatedEnergyStorage  = 0f;
        accumulatedPhotosynthesis = 0f;

        List<NodeData> startNodes = FindStartNodes();
        if (startNodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No start nodes found. Aborting BFS.");
            return;
        }
        foreach (var startNode in startNodes)
            StartCoroutine(RunChainBFS(startNode));
    }

    private void BuildCoordsMap()
    {
        coordsMap = new Dictionary<HexCoords, NodeData>();
        foreach (var node in currentGraph.nodes)
            coordsMap[node.coords] = node;
    }

    private List<NodeData> FindStartNodes()
    {
        List<NodeData> result = new List<NodeData>();
        foreach (var node in currentGraph.nodes)
        {
            if (!HasInbound(node))
                result.Add(node);
        }
        return result;
    }

    private bool HasInbound(NodeData node)
    {
        foreach (var port in node.ports)
        {
            if (!port.isInput) continue;

            int sideIndex = (int)port.side;
            int oppSide   = (sideIndex + 3) % 6;
            HexCoords neighborCoords = HexCoords.GetNeighbor(node.coords, oppSide);
            if (!coordsMap.ContainsKey(neighborCoords)) 
                continue;

            var neighborNode = coordsMap[neighborCoords];
            bool hasOutputMatch = neighborNode.ports.Any(p => !p.isInput && (int)p.side == oppSide);
            if (hasOutputMatch)
                return true;
        }
        return false;
    }

    private IEnumerator RunChainBFS(NodeData startNode)
    {
        Queue<HexCoords> queue = new Queue<HexCoords>();
        queue.Enqueue(startNode.coords);

        while (queue.Count > 0)
        {
            HexCoords coords = queue.Dequeue();
            if (!coordsMap.ContainsKey(coords)) 
                continue;

            NodeData node = coordsMap[coords];
            if (visited.Contains(node.nodeId)) 
                continue;

            visited.Add(node.nodeId);

            yield return new WaitForSeconds(waitTimeBetweenNodes);
            ProcessNode(node);

            // Follow output sides
            foreach (var port in node.ports)
            {
                if (!port.isInput)
                {
                    int sIndex = (int)port.side;
                    HexCoords neighborCoords = HexCoords.GetNeighbor(coords, sIndex);
                    if (!coordsMap.ContainsKey(neighborCoords)) 
                        continue;

                    var neighborNode = coordsMap[neighborCoords];
                    int oppIndex = (sIndex + 3) % 6;
                    bool hasInputMatch = neighborNode.ports.Any(p => p.isInput && (int)p.side == oppIndex);
                    if (hasInputMatch && !visited.Contains(neighborNode.nodeId))
                        queue.Enqueue(neighborCoords);
                }
            }
        }
        LogDebug("[NodeExecutor] BFS from start node completed.");
    }

    private void ProcessNode(NodeData node)
    {
        LogDebug($"[NodeExecutor] Processing node '{node.nodeDisplayName}' at coords ({node.coords.q}, {node.coords.r}).");

        // Accumulate Energy Storage and Photosynthesis from ANY node effect 
        // in the BFS chain.
        foreach (var eff in node.effects)
        {
            if (eff.effectType == NodeEffectType.EnergyStorage)
            {
                accumulatedEnergyStorage += eff.effectValue;
            }
            else if (eff.effectType == NodeEffectType.EnergyPhotosynthesis)
            {
                accumulatedPhotosynthesis += eff.effectValue;
            }
        }

        // If it's a Seed node, spawn the plant now using the BFS accumulators.
        var seedEffect = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.Seed);
        if (seedEffect != null)
        {
            SpawnPlant(seedEffect);
        }
    }

    private void SpawnPlant(NodeEffectData seedEffect)
    {
        if (plantPrefab == null)
        {
            LogDebug("[NodeExecutor] plantPrefab is not assigned in the inspector!");
            return;
        }
        var gardener = FindObjectOfType<GardenerController>();
        if (gardener == null)
        {
            LogDebug("[NodeExecutor] No GardenerController found. Can't spawn plant.");
            return;
        }

        Vector2 spawnPos = gardener.GetPlantingPosition();
        GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity);
        PlantGrowth growth = plantObj.GetComponent<PlantGrowth>();

        if (growth != null)
        {
            // standard seed data
            growth.stemMinLength = Mathf.RoundToInt(seedEffect.effectValue);
            growth.stemMaxLength = Mathf.RoundToInt(seedEffect.secondaryValue);
            growth.growthSpeed   = seedEffect.extra1;
            growth.leafGap       = Mathf.RoundToInt(seedEffect.extra2);
            growth.leafPattern      = seedEffect.leafPattern; 
            growth.growthRandomness = seedEffect.growthRandomness;

            // new "energy" data from BFS sums
            growth.maxEnergy       = accumulatedEnergyStorage;   // sum of all energy storages
            growth.basePhotosynthesis = accumulatedPhotosynthesis; // sum of all photosynthesis rates

            LogDebug($"[NodeExecutor] Spawned plant => " +
                     $"Seed(min={growth.stemMinLength},max={growth.stemMaxLength},spd={growth.growthSpeed},gap={growth.leafGap},pat={growth.leafPattern},rand={growth.growthRandomness}), " +
                     $"Energy(max={growth.maxEnergy}), Photosynthesis(base={growth.basePhotosynthesis})");
        }
        else
        {
            LogDebug("[NodeExecutor] PlantGrowth missing on plantPrefab.");
        }

        // If you want each new seed to get its own accumulators, 
        // reset them after spawning:
        accumulatedEnergyStorage  = 0f;
        accumulatedPhotosynthesis = 0f;
    }

    private void ClearDebug()
    {
        if (debugOutput)
            debugOutput.text = "";
    }

    private void LogDebug(string msg)
    {
        Debug.Log(msg);
        if (debugOutput)
            debugOutput.text += msg + "\n";
    }
}

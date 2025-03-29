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

        // If it's a SeedSpawn node, spawn the plant now using the BFS accumulators and collect plant parameters
        var seedSpawnEffect = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.SeedSpawn);
        if (seedSpawnEffect != null)
        {
            SpawnPlant(node);
        }
    }

    private void SpawnPlant(NodeData node)
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

    // Step A: Collect all relevant effect data from node
    float minStem = 3f;
    float maxStem = 6f;
    float speed = 1f;            // Growth speed
    float gap = 1f;              // Leaf gap
    float pattern = 0f;          // Leaf pattern
    float randomness = 0f;       // Stem randomness

    // We parse each effect to see if it matches the known effect types
    foreach (var eff in node.effects)
    {
        switch (eff.effectType)
        {
            case NodeEffectType.StemLength:
                minStem = eff.effectValue;
                maxStem = eff.secondaryValue;
                break;
            case NodeEffectType.GrowthSpeed:
                speed = eff.effectValue;
                break;
            case NodeEffectType.LeafGap:
                gap = eff.effectValue;
                break;
            case NodeEffectType.LeafPattern:
                pattern = eff.effectValue;
                break;
            case NodeEffectType.StemRandomness:
                randomness = eff.effectValue;
                break;
            default:
                // Possibly ignore or handle other effect types here
                break;
        }
    }

    // Step B: Actually spawn the plant
    Vector2 spawnPos = gardener.GetPlantingPosition();
    GameObject plantObj = Instantiate(plantPrefab, spawnPos, Quaternion.identity);

    // Step C: Parent the plant using EcosystemManager
    if (EcosystemManager.Instance != null && EcosystemManager.Instance.plantParent != null)
    {
        if (EcosystemManager.Instance.sortPlantsBySpecies)
        {
            // For plants, assume a "Plant" subfolder
            Transform speciesParent = EcosystemManager.Instance.plantParent.Find("Plant");
            if (speciesParent == null)
            {
                GameObject subParent = new GameObject("Plant");
                subParent.transform.SetParent(EcosystemManager.Instance.plantParent);
                speciesParent = subParent.transform;
            }
            plantObj.transform.SetParent(speciesParent);
        }
        else
        {
            plantObj.transform.SetParent(EcosystemManager.Instance.plantParent);
        }
    }

    // Step D: Apply the BFS accumulations and effect data to PlantGrowth
    PlantGrowth growth = plantObj.GetComponent<PlantGrowth>();
    if (growth != null)
    {
        // Convert to int if needed
        growth.stemMinLength = Mathf.RoundToInt(minStem);
        growth.stemMaxLength = Mathf.RoundToInt(maxStem);
        growth.growthSpeed   = speed;
        growth.leafGap       = Mathf.RoundToInt(gap);
        growth.leafPattern   = Mathf.RoundToInt(pattern);
        
        // Our BFS accumulations: pass them in
        growth.growthRandomness = randomness;
        growth.maxEnergy        = accumulatedEnergyStorage;   // BFS sum from ProcessNode
        growth.basePhotosynthesis = accumulatedPhotosynthesis; // BFS sum from ProcessNode
    }
    else
    {
        LogDebug("[NodeExecutor] PlantGrowth missing on plantPrefab.");
    }

    // Step E: Reset BFS sums or continue to accumulate for multi-seed spawns
    // accumulatedEnergyStorage = 0f;
    // accumulatedPhotosynthesis = 0f;
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
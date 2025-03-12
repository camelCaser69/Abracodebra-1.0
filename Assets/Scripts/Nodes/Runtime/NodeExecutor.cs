using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private NodeGraph currentGraph;

    [Header("Debug Settings")]
    public float waitTimeBetweenNodes = 0.5f; // Pause for step-by-step debug
    public TMP_Text debugOutput; // Optional on-screen text

    private void Update()
    {
        // 1) Real-time mana recharge for all nodes with ManaStorage + ManaRechargeRate
        RechargeManaOverTime();

        // 2) Press SPACE => run chain execution
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteGraph();
        }
    }

    private void RechargeManaOverTime()
    {
        if (currentGraph == null) return;

        // For each node in the graph, if it has a ManaStorage effect and optionally a ManaRechargeRate effect,
        // we add recharge * Time.deltaTime to the node's current mana, clamped by capacity.
        foreach (var node in currentGraph.nodes)
        {
            var storageEff = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
            if (storageEff != null)
            {
                float capacity = storageEff.effectValue;       // max capacity
                float current = storageEff.secondaryValue;    // current mana
                float rechargeRate = 0f;

                // If the same node has a separate effect for ManaRechargeRate, add it
                var rechargeEff = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaRechargeRate);
                if (rechargeEff != null)
                {
                    rechargeRate = rechargeEff.effectValue; // user-defined rate
                }

                // Increase current by rechargeRate * dt, clamp to capacity
                current += rechargeRate * Time.deltaTime;
                current = Mathf.Clamp(current, 0f, capacity);

                // Write back into secondaryValue so the NodeView shows the updated current
                storageEff.secondaryValue = current;
            }
        }
    }

    public void SetGraph(NodeGraph graph)
    {
        currentGraph = graph;
    }

    public NodeGraph GetGraph() => currentGraph;

    public void ExecuteGraph()
    {
        if (currentGraph == null || currentGraph.nodes.Count == 0)
        {
            Debug.LogWarning("[NodeExecutor] No graph or no nodes to execute!");
            return;
        }
        StopAllCoroutines();
        StartCoroutine(RunChainCoroutine());
    }

    private IEnumerator RunChainCoroutine()
    {
        ClearDebugOutput();

        // BFS or any chain-building approach here:
        // For example, gather all start nodes (in-degree=0), then BFS to produce a "chain"
        // (We've simplified it for demonstration. Adjust as needed for multi-output.)
        Dictionary<string, int> inboundCount = BuildInboundCount();
        List<NodeData> startNodes = currentGraph.nodes
            .Where(n => inboundCount[n.nodeId] == 0)
            .ToList();

        if (startNodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No start nodes found. Aborting run.");
            yield break;
        }

        // BFS from all starts
        Queue<NodeData> queue = new Queue<NodeData>();
        HashSet<string> visited = new HashSet<string>();
        foreach (var s in startNodes) queue.Enqueue(s);

        List<NodeData> chain = new List<NodeData>();

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (visited.Contains(curr.nodeId)) continue;
            visited.Add(curr.nodeId);
            chain.Add(curr);

            if (currentGraph.adjacency != null && currentGraph.adjacency.ContainsKey(curr.nodeId))
            {
                foreach (var childId in currentGraph.adjacency[curr.nodeId])
                {
                    if (inboundCount[childId] > 0)
                    {
                        inboundCount[childId]--;
                        if (inboundCount[childId] == 0)
                        {
                            var childNode = currentGraph.nodes.FirstOrDefault(n => n.nodeId == childId);
                            if (childNode != null) queue.Enqueue(childNode);
                        }
                    }
                }
            }
        }

        // Log the chain: Node1 -> Node2 -> ...
        string chainLog = string.Join(" -> ", chain.Select(n => n.nodeDisplayName));
        LogDebug("[NodeExecutor] Chain: " + chainLog);

        // BFS Execution
        float totalDamage = 0f;
        float totalManaCost = 0f;
        List<string> skippedNodes = new List<string>();

        foreach (var node in chain)
        {
            yield return new WaitForSeconds(waitTimeBetweenNodes);

            // Gather ManaStorage effect if present (we do NOT reset or override it)
            var storageEff = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
            float capacity = (storageEff != null) ? storageEff.effectValue : 0f;
            float current = (storageEff != null) ? storageEff.secondaryValue : 0f;

            // Gather ManaCost
            float cost = node.effects
                .Where(e => e.effectType == NodeEffectType.ManaCost)
                .Sum(e => e.effectValue);

            if (cost > 0 && current < cost)
            {
                LogDebug($"[NodeExecutor] Skipping node '{node.nodeDisplayName}' (insufficient mana: {current}/{cost}).");
                skippedNodes.Add(node.nodeDisplayName);
            }
            else
            {
                current -= cost;
                totalManaCost += cost;

                float damage = node.effects
                    .Where(e => e.effectType == NodeEffectType.Damage)
                    .Sum(e => e.effectValue);
                totalDamage += damage;

                LogDebug($"[NodeExecutor] Executed '{node.nodeDisplayName}': cost={cost}, damage={damage}, leftoverMana={current}/{capacity}");
            }

            // Write back updated mana
            if (storageEff != null)
            {
                storageEff.secondaryValue = Mathf.Clamp(current, 0f, capacity);
            }

            // If this node is an output node, log final
            bool isOutput = node.effects.Any(e => e.effectType == NodeEffectType.Output);
            if (isOutput)
            {
                LogDebug($"[NodeExecutor] Output node '{node.nodeDisplayName}' reached.");
                LogDebug($"[NodeExecutor] Total mana cost: {totalManaCost}");
                LogDebug($"[NodeExecutor] Skipped nodes: {(skippedNodes.Count > 0 ? string.Join(", ", skippedNodes) : "None")}");
                LogDebug($"[NodeExecutor] Final damage: {totalDamage}, final mana: {current}/{capacity}");
            }
        }

        LogDebug("[NodeExecutor] BFS execution complete.");
    }

    // Build inbound edges based on adjacency
    private Dictionary<string, int> BuildInboundCount()
    {
        Dictionary<string, int> inboundCount = new Dictionary<string, int>();
        foreach (var node in currentGraph.nodes)
            inboundCount[node.nodeId] = 0;

        if (currentGraph.adjacency != null)
        {
            foreach (var kvp in currentGraph.adjacency)
            {
                foreach (var childId in kvp.Value)
                {
                    inboundCount[childId]++;
                }
            }
        }
        return inboundCount;
    }

    private void ClearDebugOutput()
    {
        if (debugOutput)
            debugOutput.text = "";
    }

    private void LogDebug(string msg)
    {
        Debug.Log(msg);
        if (debugOutput)
        {
            debugOutput.text += msg + "\n";
        }
    }
}

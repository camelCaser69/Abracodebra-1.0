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

    private float totalDamage = 0f;
    private float totalManaCost = 0f;
    private List<string> skippedNodes = new List<string>();

    private void Update()
    {
        RechargeNodesOverTime();

        if (Input.GetKeyDown(KeyCode.Space))
            ExecuteGraph();
    }

    // Recharge each node's local mana if it has a ManaStorage effect.
    private void RechargeNodesOverTime()
    {
        if (currentGraph == null) return;

        foreach (var node in currentGraph.nodes)
        {
            var storage = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
            if (storage != null)
            {
                float cap = storage.effectValue;
                float cur = storage.secondaryValue;

                var rateEff = node.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaRechargeRate);
                float rate = (rateEff != null) ? rateEff.effectValue : 0f;

                cur += rate * Time.deltaTime;
                if (cur > cap) cur = cap;

                storage.secondaryValue = cur;
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

        // Reset accumulators.
        totalDamage = 0f;
        totalManaCost = 0f;
        skippedNodes.Clear();

        StopAllCoroutines();
        StartCoroutine(RunChainCoroutine());
    }

    private IEnumerator RunChainCoroutine()
    {
        ClearDebugOutput();

        Dictionary<string, int> inboundCount = BuildInboundCount();
        List<NodeData> startNodes = currentGraph.nodes.Where(n => inboundCount[n.nodeId] == 0).ToList();
        if (startNodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No start nodes found. Aborting run.");
            yield break;
        }

        // Build a chain using BFS.
        Queue<NodeData> queue = new Queue<NodeData>();
        HashSet<string> visited = new HashSet<string>();
        foreach (var s in startNodes)
            queue.Enqueue(s);

        List<NodeData> chain = new List<NodeData>();
        while (queue.Count > 0)
        {
            NodeData curr = queue.Dequeue();
            if (visited.Contains(curr.nodeId))
                continue;
            visited.Add(curr.nodeId);
            chain.Add(curr);

            if (currentGraph.adjacency != null && currentGraph.adjacency.ContainsKey(curr.nodeId))
            {
                foreach (var childId in currentGraph.adjacency[curr.nodeId])
                {
                    inboundCount[childId]--;
                    if (inboundCount[childId] <= 0)
                    {
                        NodeData child = currentGraph.nodes.FirstOrDefault(n => n.nodeId == childId);
                        if (child != null)
                            queue.Enqueue(child);
                    }
                }
            }
        }

        string chainLog = string.Join(" -> ", chain.Select(n => n.nodeDisplayName));
        LogDebug("[NodeExecutor] Chain: " + chainLog);

        foreach (var node in chain)
        {
            yield return new WaitForSeconds(waitTimeBetweenNodes);

            float cost = node.effects.Where(e => e.effectType == NodeEffectType.ManaCost)
                                     .Sum(e => e.effectValue);

            if (cost <= 0f)
            {
                float dmg = node.effects.Where(e => e.effectType == NodeEffectType.Damage)
                                        .Sum(e => e.effectValue);
                totalDamage += dmg;
                LogDebug($"[NodeExecutor] Executed '{node.nodeDisplayName}' with no cost, damage={Mathf.Floor(dmg)}");
            }
            else
            {
                // Require an explicit mana connection.
                if (!currentGraph.manaConnections.ContainsKey(node.nodeId))
                {
                    LogDebug($"[NodeExecutor] Skipping '{node.nodeDisplayName}' - no mana source connected.");
                    skippedNodes.Add(node.nodeDisplayName);
                    continue;
                }

                string sourceId = currentGraph.manaConnections[node.nodeId];
                NodeData sourceNode = currentGraph.nodes.FirstOrDefault(n => n.nodeId == sourceId);
                if (sourceNode == null)
                {
                    LogDebug($"[NodeExecutor] Skipping '{node.nodeDisplayName}' - upstream mana node not found.");
                    skippedNodes.Add(node.nodeDisplayName);
                    continue;
                }

                var storageEff = sourceNode.effects.FirstOrDefault(e => e.effectType == NodeEffectType.ManaStorage);
                if (storageEff == null)
                {
                    LogDebug($"[NodeExecutor] Skipping '{node.nodeDisplayName}' - source node '{sourceNode.nodeDisplayName}' has no ManaStorage.");
                    skippedNodes.Add(node.nodeDisplayName);
                    continue;
                }

                float cap = storageEff.effectValue;
                float cur = storageEff.secondaryValue;

                if (cur < cost)
                {
                    LogDebug($"[NodeExecutor] Skipping '{node.nodeDisplayName}' (insufficient mana in '{sourceNode.nodeDisplayName}': {Mathf.Floor(cur)}/{cost}).");
                    skippedNodes.Add(node.nodeDisplayName);
                    continue;
                }
                else
                {
                    cur -= cost;
                    storageEff.secondaryValue = cur;
                    totalManaCost += cost;

                    float dmg = node.effects.Where(e => e.effectType == NodeEffectType.Damage)
                                            .Sum(e => e.effectValue);
                    totalDamage += dmg;

                    LogDebug($"[NodeExecutor] '{node.nodeDisplayName}' executed: cost={Mathf.Floor(cost)}, damage={Mathf.Floor(dmg)}, source='{sourceNode.nodeDisplayName}', leftoverMana={Mathf.Floor(cur)}/{Mathf.Floor(cap)}");
                }
            }

            bool isOutput = node.effects.Any(e => e.effectType == NodeEffectType.Output);
            if (isOutput)
            {
                LogDebug($"[NodeExecutor] Output node '{node.nodeDisplayName}' reached.");
                LogDebug($"[NodeExecutor] Total mana cost: {Mathf.Floor(totalManaCost)}");
                string skipStr = (skippedNodes.Count > 0) ? string.Join(", ", skippedNodes) : "None";
                LogDebug($"[NodeExecutor] Skipped nodes: {skipStr}");
                LogDebug($"[NodeExecutor] Final damage: {Mathf.Floor(totalDamage)}");

                // Instead of directly calling PlayerWizard, let the output node handle it.
                NodeView outputView = FindNodeViewById(node.nodeId);
                if (outputView != null)
                {
                    OutputNodeEffect outputEffect = outputView.GetComponent<OutputNodeEffect>();
                    if (outputEffect != null)
                        outputEffect.Activate(totalDamage);
                    else
                        Debug.LogWarning("[NodeExecutor] Output node effect not found on NodeView.");
                }
            }
        }

        LogDebug("[NodeExecutor] BFS execution complete.");
    }

    // Instead of using a stored list, we find all NodeViews in the scene.
    private NodeView FindNodeViewById(string nodeId)
    {
        NodeView[] views = GameObject.FindObjectsOfType<NodeView>();
        foreach (var view in views)
        {
            if (view.GetNodeData().nodeId == nodeId)
                return view;
        }
        return null;
    }

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
            debugOutput.text += msg + "\n";
    }
}

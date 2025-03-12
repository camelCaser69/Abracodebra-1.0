using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class NodeExecutor : MonoBehaviour
{
    public float waitTimeBetweenNodes = 0.5f;
    public TMP_Text debugOutput; // Optional on-screen log display

    [SerializeField] private NodeGraph currentGraph;

    private float totalDamage = 0f;
    private float currentMana = 0f; // Unique mana pool from a ManaStorage node
    private float maxMana = 0f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteGraph();
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
            Debug.LogWarning("No graph or no nodes to execute!");
            return;
        }
        StopAllCoroutines();
        StartCoroutine(RunChainCoroutine());
    }

    private IEnumerator RunChainCoroutine()
    {
        ClearDebugOutput();
        totalDamage = 0f;

        // 1) Find the output node (assumes at least one exists)
        var outputNodes = currentGraph.nodes
            .Where(n => n.effects.Any(e => e.effectType == NodeEffectType.Output))
            .ToList();

        if (outputNodes.Count == 0)
        {
            LogDebug("No Output node found. Aborting run.");
            yield break;
        }

        // For simplicity, use the first output node.
        NodeData outputNode = outputNodes[0];

        // 2) Gather all nodes in the chain that lead to this output node.
        List<NodeData> chainNodes = GetNodesLeadingTo(outputNode);
        if (chainNodes.Count == 0)
        {
            LogDebug("No chain leading to output node. Aborting run.");
            yield break;
        }

        // Reverse chain so that chainNodes[0] is the starting node.
        chainNodes.Reverse();

        // 3) Identify the ManaStorage node in the chain (if any).
        // We'll assume the starting node is a ManaStorage node.
        NodeData manaSource = chainNodes.FirstOrDefault(n => n.manaStorageCapacity > 0f);
        if (manaSource == null)
        {
            LogDebug("No Mana Storage node found in chain. Aborting run.");
            yield break;
        }
        maxMana = manaSource.manaStorageCapacity;
        currentMana = manaSource.currentManaStorage;

        LogDebug($"[NodeExecutor] Starting chain with Mana: {currentMana}/{maxMana}");

        // 4) Execute the chain in order.
        foreach (var node in chainNodes)
        {
            yield return new WaitForSeconds(waitTimeBetweenNodes);

            // Sum ManaCost effects for this node.
            float manaCost = node.effects
                .Where(e => e.effectType == NodeEffectType.ManaCost)
                .Sum(e => e.effectValue);

            if (currentMana < manaCost)
            {
                LogDebug($"Node '{node.nodeDisplayName}' cannot run. Needs {manaCost} mana, only {currentMana} available.");
                continue;
            }
            currentMana -= manaCost;

            // Sum Damage effects for this node.
            float damage = node.effects
                .Where(e => e.effectType == NodeEffectType.Damage)
                .Sum(e => e.effectValue);
            totalDamage += damage;

            LogDebug($"Node '{node.nodeDisplayName}' executed: ManaCost={manaCost}, Damage={damage}, RemainingMana={currentMana}");
            
            // Optionally, update the node's displayed mana storage if this node is a ManaStorage node.
            if (node.manaStorageCapacity > 0f)
            {
                // Here, you could update the NodeView for this node (not shown).
                LogDebug($"[ManaStorage] '{node.nodeDisplayName}' now has {currentMana}/{maxMana} mana.");
            }

            // If this node has the Output effect, consider it the end of the chain.
            if (node.effects.Any(e => e.effectType == NodeEffectType.Output))
            {
                LogDebug($"[Output] Output node reached: FinalDamage={totalDamage}, FinalMana={currentMana}");
                // Here you can trigger the visual spell cast.
            }
        }

        LogDebug($"[NodeExecutor] Chain execution finished. TotalDamage={totalDamage}, RemainingMana={currentMana}");
    }

    /// <summary>
    /// Recursively gathers all nodes that lead to the given output node.
    /// Returns a list in reverse order (output -> start).
    /// </summary>
    private List<NodeData> GetNodesLeadingTo(NodeData endNode)
    {
        List<NodeData> chain = new List<NodeData>();
        RecursiveGather(endNode, chain);
        return chain;
    }

    private void RecursiveGather(NodeData node, List<NodeData> chain)
    {
        if (!chain.Contains(node))
            chain.Add(node);

        foreach (var inp in node.inputs)
        {
            foreach (var connectedPortId in inp.connectedPortIds)
            {
                NodeData sourceNode = currentGraph.nodes.FirstOrDefault(
                    n => n.outputs.Any(o => o.portId == connectedPortId)
                );
                if (sourceNode != null && !chain.Contains(sourceNode))
                {
                    RecursiveGather(sourceNode, chain);
                }
            }
        }
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

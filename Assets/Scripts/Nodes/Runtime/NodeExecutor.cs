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
    public TMP_Text debugOutput; // optional on-screen text

    private void Update()
    {
        // Press SPACE to run the chain
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
            Debug.LogWarning("[NodeExecutor] No graph or no nodes to execute!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RunChainCoroutine());
    }

    private IEnumerator RunChainCoroutine()
    {
        ClearDebugOutput();

        // 1) Find an Output node
        var outputNodes = currentGraph.nodes
            .Where(n => n.effects.Any(e => e.effectType == NodeEffectType.Output))
            .ToList();

        if (outputNodes.Count == 0)
        {
            LogDebug("[NodeExecutor] No Output node found. Aborting run.");
            yield break;
        }

        // For simplicity, pick the first output node
        NodeData outputNode = outputNodes[0];

        // 2) Gather all nodes leading to this output (including itself)
        var chain = new List<NodeData>();
        RecursiveGatherUpstream(outputNode, chain);

        if (chain.Count == 0)
        {
            LogDebug("[NodeExecutor] No chain leading to output node. Aborting run.");
            yield break;
        }

        // chain is reversed (output -> up). Let's re-reverse it (start -> ... -> output).
        chain.Reverse();

        LogDebug($"[NodeExecutor] Found {chain.Count} nodes in chain from start to output.");

        // 3) We'll keep track of a "localMana" that can be updated if we encounter a node with ManaStorage
        float localMana = 0f;
        float localCapacity = 0f;
        float totalDamage = 0f;

        // 4) Execute chain in forward order
        foreach (var node in chain)
        {
            yield return new WaitForSeconds(waitTimeBetweenNodes);

            // If this node has a ManaStorage effect, it overrides localMana
            if (node.manaStorageCapacity > 0f)
            {
                localCapacity = node.manaStorageCapacity;
                localMana = node.currentManaStorage;
                LogDebug($"[NodeExecutor] Node '{node.nodeDisplayName}' sets localMana to {localMana}/{localCapacity}");
            }

            // ManaCost
            float manaCost = node.effects
                .Where(e => e.effectType == NodeEffectType.ManaCost)
                .Sum(e => e.effectValue);

            bool canPay = (localMana >= manaCost);
            if (manaCost > 0f && !canPay)
            {
                LogDebug($"[NodeExecutor] Not enough mana for '{node.nodeDisplayName}'. Required {manaCost}, have {localMana}. Skipping node's damage but continuing chain...");
                // We do not stop the chain, just skip applying this node's damage
            }
            else
            {
                // Subtract cost
                localMana -= manaCost;

                // Damage
                float dmg = node.effects
                    .Where(e => e.effectType == NodeEffectType.Damage)
                    .Sum(e => e.effectValue);
                totalDamage += dmg;

                LogDebug($"[NodeExecutor] Node '{node.nodeDisplayName}' executed: cost={manaCost}, damage={dmg}, leftoverMana={localMana}/{localCapacity}");
            }

            // If node has Output effect, we log final result
            bool isOutput = node.effects.Any(e => e.effectType == NodeEffectType.Output);
            if (isOutput)
            {
                LogDebug($"[NodeExecutor] Output node '{node.nodeDisplayName}' reached. FinalDamage={totalDamage}, finalMana={localMana}");
                // Potentially spawn a spell effect here
            }
        }

        LogDebug("[NodeExecutor] Done executing chain.");
    }

    /// <summary>
    /// Recursively gathers all nodes that feed into 'currentNode' (via "General" ports).
    /// Each node is added once to 'chain'.
    /// </summary>
    private void RecursiveGatherUpstream(NodeData currentNode, List<NodeData> chain)
    {
        if (!chain.Contains(currentNode))
            chain.Add(currentNode);

        // For each input port of currentNode
        foreach (var inputPort in currentNode.inputs)
        {
            // If port type != General, skip
            // (Use your enum-based check here)
            if (inputPort.portType != PortType.General)
                continue;

            // Each connectedPortId is from an output port of some other node
            foreach (var connectedId in inputPort.connectedPortIds)
            {
                // Find which node has an output with this ID
                var sourceNode = currentGraph.nodes.FirstOrDefault(n =>
                    n.outputs.Any(o => o.portId == connectedId && o.portType == PortType.General));

                if (sourceNode != null && !chain.Contains(sourceNode))
                {
                    // Recurse upstream
                    RecursiveGatherUpstream(sourceNode, chain);
                }
            }
        }
    }

    private void ClearDebugOutput()
    {
        if (debugOutput) debugOutput.text = "";
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

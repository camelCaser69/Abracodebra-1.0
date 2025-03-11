// Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
using System.Collections;
using UnityEngine;
using TMPro;

public class NodeExecutor : MonoBehaviour
{
    [Header("Mana Settings")]
    public float maxMana = 10f;
    public float currentMana = 10f;
    public float manaRegenRate = 1f; // Mana per second

    [Header("Execution Settings")]
    public float waitTimeBetweenNodes = 0.5f; // Pause between nodes
    public TMP_Text manaDisplay; // Assign in Inspector to show "[Current]/[Max]"

    [SerializeField] private NodeGraph currentGraph;

    private float accumulatedDamage = 0f;

    private void Update()
    {
        // Regenerate mana each frame
        currentMana = Mathf.Min(currentMana + (manaRegenRate * Time.deltaTime), maxMana);

        // Update mana display if assigned
        if (manaDisplay != null)
        {
            manaDisplay.text = $"{(int)currentMana} / {maxMana}";
        }
    }

    // Called to start node execution
    public void ExecuteGraph()
    {
        if (currentGraph == null || currentGraph.nodes.Count == 0)
        {
            Debug.LogWarning("No graph or no nodes to execute!");
            return;
        }
        accumulatedDamage = 0f;
        StartCoroutine(ProcessGraphCoroutine());
    }

    private IEnumerator ProcessGraphCoroutine()
    {
        // Simple approach: iterate through each node in the graph linearly
        for (int i = 0; i < currentGraph.nodes.Count; i++)
        {
            NodeData node = currentGraph.nodes[i];

            // Wait so we can visually see the step-by-step flow
            yield return new WaitForSeconds(waitTimeBetweenNodes);

            // Check mana
            float cost = node.manaCost;
            if (currentMana < cost)
            {
                Debug.Log($"[NodeExecutor] Not enough mana to process node '{node.nodeDisplayName}'. Needed {cost}, have {currentMana}.");
                continue; // Skip or break, depends on your game logic
            }

            // Subtract mana cost
            currentMana -= cost;

            // Add damage
            accumulatedDamage += node.damageAdd;

            // Debug info
            Debug.Log($"[NodeExecutor] Processed node '{node.nodeDisplayName}' at index {i}, cost={cost}, totalDamage={accumulatedDamage}, remainingMana={currentMana}");
        }

        Debug.Log($"[NodeExecutor] Finished processing graph. Final damage={accumulatedDamage}.");
    }

    public void SetGraph(NodeGraph graph)
    {
        currentGraph = graph;
    }

    public NodeGraph GetGraph()
    {
        return currentGraph;
    }
}

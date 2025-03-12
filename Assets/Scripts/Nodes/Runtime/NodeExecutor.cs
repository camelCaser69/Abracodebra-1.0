// Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
using System.Collections;
using UnityEngine;
using TMPro;
using System.Linq;   // for .Where, .Sum

public class NodeExecutor : MonoBehaviour
{
    [Header("Mana Settings")]
    public float maxMana = 10f;
    public float currentMana = 10f;
    public float manaRegenRate = 1f;

    [Header("Execution Settings")]
    public float waitTimeBetweenNodes = 0.5f;
    public TMP_Text manaDisplay;

    [SerializeField] private NodeGraph currentGraph;

    private float accumulatedDamage = 0f;

    private void Update()
    {
        // Regenerate mana
        currentMana = Mathf.Min(currentMana + (manaRegenRate * Time.deltaTime), maxMana);
        if (manaDisplay)
        {
            manaDisplay.text = $"{(int)currentMana} / {maxMana}";
        }

        // Press Space => run
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteGraph();
        }
    }

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
        for (int i = 0; i < currentGraph.nodes.Count; i++)
        {
            NodeData node = currentGraph.nodes[i];

            // Wait for debug visualization
            yield return new WaitForSeconds(waitTimeBetweenNodes);

            // Sum all ManaCost effects
            float totalManaCost = node.effects
                .Where(e => e.effectType == NodeEffectType.ManaCost)
                .Sum(e => e.effectValue);

            // Check if we can afford
            if (currentMana < totalManaCost)
            {
                Debug.Log($"[NodeExecutor] Not enough mana for node '{node.nodeDisplayName}'. Need {totalManaCost}, have {currentMana}.");
                continue;
            }

            // Subtract
            currentMana -= totalManaCost;

            // Sum damage
            float totalDamage = node.effects
                .Where(e => e.effectType == NodeEffectType.Damage)
                .Sum(e => e.effectValue);

            accumulatedDamage += totalDamage;

            // Log each effect
            foreach (var eff in node.effects)
            {
                Debug.Log($"[NodeExecutor] Node '{node.nodeDisplayName}' applying {eff.effectType}({eff.effectValue}).");
            }
            Debug.Log($"[NodeExecutor] After node '{node.nodeDisplayName}', totalDamage={accumulatedDamage}, remainingMana={currentMana}");
        }

        Debug.Log($"[NodeExecutor] Finished processing. Final damage={accumulatedDamage}.");
    }

    public void SetGraph(NodeGraph graph) => currentGraph = graph;
    public NodeGraph GetGraph() => currentGraph;
}

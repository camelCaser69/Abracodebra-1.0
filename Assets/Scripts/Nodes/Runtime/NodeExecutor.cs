/* Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
   MonoBehaviour that runs or interprets a NodeGraph at runtime (e.g., spells). */

using UnityEngine;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private NodeGraph currentGraph;

    // Example method for "executing" the graph logic. Expand or modify for your spells.
    public void ExecuteGraph()
    {
        if (currentGraph == null) return;

        foreach (var node in currentGraph.nodes)
        {
            // Pseudocode:
            // if (node.nodeType == "Damage") { ... }
            // else if (node.nodeType == "Mana") { ... }
            // etc.

            // This is where you'd connect the node system to actual wizard/spell behaviors.
        }
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
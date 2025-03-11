// Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
// MonoBehaviour that runs or interprets a NodeGraph at runtime (e.g., for spell logic).

using UnityEngine;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private NodeGraph currentGraph;

    // Example: This method demonstrates how you might "execute" the graph logic.
    // Expand or modify according to your gameplay needs.
    public void ExecuteGraph()
    {
        if (currentGraph == null) return;

        // Example approach: a topological sort or BFS could go here.
        // For each node in currentGraph.nodes, check nodeType and do something.

        foreach (var node in currentGraph.nodes)
        {
            // Pseudocode:
            // if (node.nodeType == "Damage") { ... }
            // if (node.nodeType == "Cooldown") { ... }
            // and so on
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
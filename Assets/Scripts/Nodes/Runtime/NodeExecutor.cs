/* Assets/Scripts/Nodes/Runtime/NodeExecutor.cs
   MonoBehaviour that runs or interprets a NodeGraph at runtime (e.g., spells). */

using UnityEngine;

public class NodeExecutor : MonoBehaviour
{
    [SerializeField] private NodeGraph currentGraph;

    // Example method for executing the node graph.
    public void ExecuteGraph()
    {
        if (currentGraph == null) return;

        foreach (var node in currentGraph.nodes)
        {
            // Implement your execution logic here.
            // Example: if (node.nodeDisplayName == "Mana Source") { ... }
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

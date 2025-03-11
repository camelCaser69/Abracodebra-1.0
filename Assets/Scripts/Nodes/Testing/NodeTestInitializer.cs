using UnityEngine;

public class NodeTestInitializer : MonoBehaviour
{
    [SerializeField] private NodeEditorController editorController;

    private NodeGraph testGraph;

    void Start()
    {
        // Create an empty graph
        testGraph = new NodeGraph();
        
        // Create a sample node
        NodeData nodeA = new NodeData();
        nodeA.nodeDisplayName = "TestNode"; // Use nodeDisplayName instead of nodeType
        nodeA.editorPosition = Vector2.zero;
        
        // Optionally add default ports if needed
        // nodeA.inputs.Add(new NodePort { portName = "In", portType = "General" });
        // nodeA.outputs.Add(new NodePort { portName = "Out", portType = "General" });
        
        testGraph.nodes.Add(nodeA);
        
        // Load the graph into the editor
        editorController.LoadGraph(testGraph);
    }
}
// Assets/Scripts/Nodes/Testing/NodeTestInitializer.cs
using UnityEngine;
// If NodeEditorController is in a specific namespace, include it, e.g.:
// using YourProject.Nodes.UI;

public class NodeTestInitializer : MonoBehaviour
{
    [SerializeField] private NodeEditorController editorController;

    private NodeGraph testGraph;

    void Start()
    {
        testGraph = new NodeGraph();
        // Load the empty graph into the editor
        editorController.LoadGraph(testGraph);
    }
}
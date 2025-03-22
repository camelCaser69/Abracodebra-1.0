using UnityEngine;

public class NodeTestInitializer : MonoBehaviour
{
    [SerializeField] private NodeEditorController editorController;
    [SerializeField] private NodeExecutor nodeExecutor;

    private NodeGraph testGraph;

    private void Start()
    {
        // 1) Create a new NodeGraph (or load an existing one, if you prefer).
        testGraph = new NodeGraph();

        // 2) Make the editor load that graph, so new nodes appear in the UI.
        if (editorController != null)
        {
            editorController.LoadGraph(testGraph);
        }
        else
        {
            Debug.LogWarning("[NodeTestInitializer] editorController is missing.");
        }

        // 3) Pass the SAME graph to the executor.
        if (nodeExecutor != null)
        {
            nodeExecutor.SetGraph(testGraph);
        }
        else
        {
            Debug.LogWarning("[NodeTestInitializer] nodeExecutor is missing.");
        }
    }
}
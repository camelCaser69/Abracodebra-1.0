// Assets/Scripts/Nodes/UI/NodeEditorController.cs
// Manages the creation and arrangement of NodeView objects in the editor.

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Editor Setup")]
    [SerializeField] private RectTransform editorCanvas;        // The parent canvas area for the node views.
    [SerializeField] private GameObject nodeViewPrefab;         // Prefab for NodeView.
    [SerializeField] private GameObject connectionViewPrefab;   // Prefab for NodeConnectionView.

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    // Load an existing graph or set a new graph
    public void LoadGraph(NodeGraph graph)
    {
        currentGraph = graph;
        ClearExistingViews();

        if (currentGraph == null) return;

        // For each node in the graph, create a NodeView
        foreach (NodeData node in currentGraph.nodes)
        {
            CreateNodeView(node);
        }
        // TODO: Create connection lines once NodeView positions/ports are set
    }

    private void CreateNodeView(NodeData data)
    {
        // Instantiate prefab
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        // Example color logic (if using NodeTypeDefinition, you could fetch color from that)
        Color nodeColor = Color.gray;

        // Initialize view
        view.Initialize(data, nodeColor);

        // Position example (random or set from saved data)
        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero; // Or from data if you store node positions

        spawnedNodeViews.Add(view);
    }

    // Clears existing NodeViews before loading a new graph
    private void ClearExistingViews()
    {
        foreach (NodeView view in spawnedNodeViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        spawnedNodeViews.Clear();
    }

    // Called when user scrolls (zoom in/out)
    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = editorCanvas.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);

        editorCanvas.localScale = Vector3.one * newScale;
    }

    // Called when user drags (panning the editor area)
    public void OnDrag(PointerEventData eventData)
    {
        editorCanvas.anchoredPosition += eventData.delta;
    }

    // Additional methods to create or remove nodes from the graph at runtime...
}

// Assets/Scripts/Nodes/UI/NodeEditorController.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Editor Setup")]
    [SerializeField] private RectTransform editorCanvas; // The main area for node views.
    [SerializeField] private GameObject nodeViewPrefab;  // Prefab for NodeView.
    [SerializeField] private GameObject connectionViewPrefab; // Prefab for connection lines.

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // Holds available NodeDefinition assets.

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    // For right-click context menu.
    private bool showContextMenu;
    private Vector2 contextMenuPosition;

    // For active connection dragging.
    private List<NodeConnectionView> activeConnections = new List<NodeConnectionView>();

    public RectTransform EditorCanvas { get { return editorCanvas; } }

    public void LoadGraph(NodeGraph graph)
    {
        currentGraph = graph;
        ClearExistingViews();

        if (currentGraph == null) return;

        foreach (NodeData node in currentGraph.nodes)
            CreateNodeView(node);
    }

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        // Lookup color and display name from node data.
        Color nodeColor = Color.gray;
        string displayName = data.nodeDisplayName;

        view.Initialize(data, nodeColor, displayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        spawnedNodeViews.Add(view);
        return view;
    }

    private void ClearExistingViews()
    {
        foreach (NodeView view in spawnedNodeViews)
            if (view != null) Destroy(view.gameObject);
        spawnedNodeViews.Clear();
    }

    // Zoom functionality.
    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = editorCanvas.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        editorCanvas.localScale = Vector3.one * newScale;
    }

    // Pan functionality.
    public void OnDrag(PointerEventData eventData)
    {
        editorCanvas.anchoredPosition += eventData.delta;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            showContextMenu = true;
            contextMenuPosition = Input.mousePosition;
        }
    }

    private void OnGUI()
    {
        if (showContextMenu && definitionLibrary != null && definitionLibrary.definitions.Count > 0)
        {
            Vector2 guiPos = new Vector2(contextMenuPosition.x, Screen.height - contextMenuPosition.y);
            float menuHeight = 20 + (definitionLibrary.definitions.Count * 25);
            Rect menuRect = new Rect(guiPos.x, guiPos.y, 180, menuHeight);

            GUI.Box(menuRect, "Add Node");

            float yOffset = 20f;
            foreach (NodeDefinition def in definitionLibrary.definitions)
            {
                Rect itemRect = new Rect(menuRect.x, menuRect.y + yOffset, 180, 25);
                if (GUI.Button(itemRect, def.displayName))
                {
                    CreateNodeAtMouse(def);
                    showContextMenu = false;
                }
                yOffset += 25f;
            }
        }
    }

    private void CreateNodeAtMouse(NodeDefinition definition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            editorCanvas, Input.mousePosition, null, out Vector2 localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;

        foreach (var portDef in definition.ports)
        {
            NodePort nodePort = new NodePort();
            nodePort.portName = portDef.portName;
            nodePort.portType = portDef.portType;

            if (portDef.isInput)
                newNode.inputs.Add(nodePort);
            else
                newNode.outputs.Add(nodePort);
        }

        currentGraph.nodes.Add(newNode);
        CreateNodeView(newNode);
    }

    // Methods for connection handling called by PinView.

    public NodeConnectionView StartConnectionFromPin(PinView sourcePin)
    {
        GameObject connObj = Instantiate(connectionViewPrefab, editorCanvas);
        NodeConnectionView connection = connObj.GetComponent<NodeConnectionView>();

        RectTransform sourceRect = sourcePin.GetComponent<RectTransform>();
        connection.SetConnectionPoints(sourceRect, sourceRect); // Initially, both endpoints at source.
        activeConnections.Add(connection);
        return connection;
    }

    public void CompleteConnection(PinView sourcePin, PinView targetPin, NodeConnectionView connection)
    {
        RectTransform targetRect = targetPin.GetComponent<RectTransform>();
        connection.SetConnectionPoints(sourcePin.GetComponent<RectTransform>(), targetRect);

        // Update port data: add target pin's ID to source pin's connections.
        if (sourcePin.port != null && targetPin.port != null)
            sourcePin.port.connectedPortIds.Add(targetPin.port.portId);
    }

    public void CancelConnection(NodeConnectionView connection)
    {
        activeConnections.Remove(connection);
        Destroy(connection.gameObject);
    }
}


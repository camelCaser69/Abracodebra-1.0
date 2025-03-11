// Assets/Scripts/Nodes/UI/NodeEditorController.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Editor Setup")]
    [SerializeField] private RectTransform editorCanvas;
    [SerializeField] private GameObject nodeViewPrefab;
    [SerializeField] private GameObject connectionViewPrefab;

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; 
    // Instead of "availableNodeTypes", we have a library of NodeDefinitions

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    // Right-click context menu
    private bool showContextMenu;
    private Vector2 contextMenuPosition;

    // Load or set a new graph
    public void LoadGraph(NodeGraph graph)
    {
        currentGraph = graph;
        ClearExistingViews();

        if (currentGraph == null) return;

        // Recreate each node's NodeView
        foreach (NodeData node in currentGraph.nodes)
        {
            CreateNodeView(node);
        }
    }

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        // We'll set the color and name from the NodeData if we have it
        // or we could look it up via nodeDefinitionId, but let's keep it simple:

        Color nodeColor = Color.gray; // default
        string displayName = data.nodeDisplayName;

        view.Initialize(data, nodeColor, displayName);

        // Move to saved position
        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        // Now we also want to spawn the "pins" on the left/right for inputs/outputs.
        view.GeneratePins(data.inputs, data.outputs);

        spawnedNodeViews.Add(view);
        return view;
    }

    // Clears existing NodeViews
    private void ClearExistingViews()
    {
        foreach (NodeView view in spawnedNodeViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        spawnedNodeViews.Clear();
    }

    // Zoom
    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = editorCanvas.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        editorCanvas.localScale = Vector3.one * newScale;
    }

    // Pan
    public void OnDrag(PointerEventData eventData)
    {
        editorCanvas.anchoredPosition += eventData.delta;
    }

    // Detect right-click
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
            Vector2 guiPosition = new Vector2(contextMenuPosition.x, Screen.height - contextMenuPosition.y);
            float height = 20 + (definitionLibrary.definitions.Count * 25);
            Rect menuRect = new Rect(guiPosition.x, guiPosition.y, 180, height);

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
        // Convert screen space -> local space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            editorCanvas, Input.mousePosition, null, out Vector2 localPos);

        // Create new NodeData from the NodeDefinition
        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;

        // Create ports from NodeDefinition
        foreach (var portDef in definition.ports)
        {
            NodePort nodePort = new NodePort();
            nodePort.portName = portDef.portName;
            nodePort.portType = portDef.portType;

            if (portDef.isInput)
            {
                newNode.inputs.Add(nodePort);
            }
            else
            {
                newNode.outputs.Add(nodePort);
            }
        }

        // Add the new node to the currentGraph
        currentGraph.nodes.Add(newNode);

        // Create a NodeView for it
        CreateNodeView(newNode);
    }
}

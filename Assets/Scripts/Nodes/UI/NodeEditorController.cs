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

    // For connection dragging.
    private NodeConnectionView activeConnectionLine;
    private PinView sourcePin;

    // For right-click context menu.
    private bool showContextMenu;
    private Vector2 contextMenuPosition;

    public RectTransform EditorCanvas => editorCanvas;

    private void Awake()
    {
        // Ensure there's a NodeGraph to work with.
        if (currentGraph == null)
        {
            currentGraph = new NodeGraph();
        }
    }

    private void Update()
    {
        // Right-click detection.
        if (Input.GetMouseButtonDown(1))
        {
            showContextMenu = true;
            contextMenuPosition = Input.mousePosition;
        }
    }

    private void OnGUI()
    {
        // Display the context menu if requested.
        if (showContextMenu && definitionLibrary != null && definitionLibrary.definitions.Count > 0)
        {
            // Convert screen position to GUI space.
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

    public void LoadGraph(NodeGraph graph)
    {
        currentGraph = graph;
        ClearExistingViews();

        if (currentGraph == null)
            return;

        foreach (NodeData node in currentGraph.nodes)
        {
            CreateNodeView(node);
        }
    }

    private void ClearExistingViews()
    {
        foreach (var view in spawnedNodeViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }
        spawnedNodeViews.Clear();
    }

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        // Use a default color and display name from the node data.
        Color nodeColor = Color.gray;
        string displayName = data.nodeDisplayName;

        view.Initialize(data, nodeColor, displayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        spawnedNodeViews.Add(view);
        return view;
    }

    // Assets/Scripts/Nodes/UI/NodeEditorController.cs
    private void CreateNodeAtMouse(NodeDefinition definition)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(editorCanvas, Input.mousePosition, null, out localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;

        // Copy all effects from NodeDefinition
        foreach (var defEffect in definition.effects)
        {
            NodeEffectData effectCopy = new NodeEffectData
            {
                effectType = defEffect.effectType,
                effectValue = defEffect.effectValue
            };
            newNode.effects.Add(effectCopy);
        }

        // Copy ports
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


    // ======================= Connection Drag Logic =======================

    public void StartConnectionDrag(PinView source, PointerEventData eventData)
    {
        Debug.Log("[NodeEditor] StartConnectionDrag called!");

        sourcePin = source;

        // Instantiate the connection line
        GameObject connObj = Instantiate(connectionViewPrefab, editorCanvas);
        if (connObj == null)
        {
            Debug.LogError("[NodeEditor] Connection prefab failed to instantiate!");
            return;
        }

        activeConnectionLine = connObj.GetComponent<NodeConnectionView>();
        if (activeConnectionLine == null)
        {
            Debug.LogError("[NodeEditor] NodeConnectionView component not found on prefab!");
            return;
        }

        // Assign start position
        RectTransform sourceRect = source.GetComponent<RectTransform>();
        activeConnectionLine.SetStartRect(sourceRect);

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(editorCanvas, eventData.position, eventData.pressEventCamera, out localMousePos);
        activeConnectionLine.SetEndPosition(localMousePos, editorCanvas);

        Debug.Log("[NodeEditor] Connection line initialized.");
    }


    public void UpdateConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        if (activeConnectionLine == null)
            return;

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(editorCanvas, eventData.position, eventData.pressEventCamera, out localMousePos);
        activeConnectionLine.SetEndPosition(localMousePos, editorCanvas);
    }

    public void EndConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        if (activeConnectionLine == null)
            return;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        PinView targetPin = null;

        foreach (var r in results)
        {
            targetPin = r.gameObject.GetComponent<PinView>();
            if (targetPin != null && targetPin.isInput)
                break;
            else
                targetPin = null;
        }

        if (targetPin != null)
        {
            RectTransform targetRect = targetPin.GetComponent<RectTransform>(); // Declare targetRect here.
            activeConnectionLine.SetEndRect(targetRect);

            // Assign the line's target pin for deletion handling.
            activeConnectionLine.targetPin = targetPin;

            sourcePin.port.connectedPortIds.Add(targetPin.port.portId);

            activeConnectionLine = null;
            sourcePin = null;
        }
        else
        {
            CancelActiveConnectionLine();
        }
    }


    private void CancelActiveConnectionLine()
    {
        if (activeConnectionLine)
        {
            Destroy(activeConnectionLine.gameObject);
            activeConnectionLine = null;
            sourcePin = null;
        }
    }

    // ======================= Zoom and Pan Logic =======================

    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = editorCanvas.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        editorCanvas.localScale = Vector3.one * newScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        editorCanvas.anchoredPosition += eventData.delta;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Editor Setup")]
    [SerializeField] private RectTransform editorCanvas;          // The container for nodes
    [SerializeField] private GameObject nodeViewPrefab;           // Prefab for NodeView
    [SerializeField] private GameObject connectionViewPrefab;     // Prefab for NodeConnectionView (with UICubicBezier)

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // Contains NodeDefinition assets

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph;              // Must have adjacency & manaConnections dictionaries

    // Optional: a reference to the NodeExecutor so we can update its graph.
    [SerializeField] private NodeExecutor executor;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    // For connection dragging:
    private NodeConnectionView draggingLine;
    private PinView sourcePin;

    // Context menu variables for adding nodes:
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;

    private void Start()
    {
        // Create a new graph if none assigned.
        if (currentGraph == null)
        {
            currentGraph = new NodeGraph();
        }

        // Try to find NodeExecutor if not manually assigned.
        if (executor == null)
        {
            executor = UnityEngine.Object.FindFirstObjectByType<NodeExecutor>();
            if (executor == null)
                Debug.LogWarning("[NodeEditorController] No NodeExecutor found in scene.");
            else
                executor.SetGraph(currentGraph);
        }
    }

    private void Update()
    {
        // Right-click to show context menu:
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

    /// <summary>
    /// Creates a new node from the given NodeDefinition at the current mouse position.
    /// </summary>
    private void CreateNodeAtMouse(NodeDefinition definition)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(editorCanvas, Input.mousePosition, null, out localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;
        newNode.backgroundColor = definition.backgroundColor; // Ensure NodeData has a backgroundColor field.

        // Copy effects from definition.
        foreach (var defEffect in definition.effects)
        {
            NodeEffectData effectCopy = new NodeEffectData
            {
                effectType = defEffect.effectType,
                effectValue = defEffect.effectValue,
                secondaryValue = defEffect.secondaryValue
            };
            newNode.effects.Add(effectCopy);
        }

        // Copy ports from definition.
        foreach (var portDef in definition.ports)
        {
            NodePort nodePort = new NodePort
            {
                portName = portDef.portName,
                portType = portDef.portType
            };

            if (portDef.isInput)
                newNode.inputs.Add(nodePort);
            else
                newNode.outputs.Add(nodePort);
        }

        currentGraph.nodes.Add(newNode);
        CreateNodeView(newNode);

        // Update executor with the current graph.
        if (executor != null)
            executor.SetGraph(currentGraph);
    }

    /// <summary>
    /// Instantiates a NodeView from nodeViewPrefab and initializes it with the given NodeData.
    /// </summary>
    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        Color nodeColor = data.backgroundColor;
        string displayName = data.nodeDisplayName;
        
        view.Initialize(data, nodeColor, displayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        spawnedNodeViews.Add(view);
        return view;
    }

    // --- Connection Dragging Methods ---

    public void StartConnectionDrag(PinView source, PointerEventData eventData)
    {
        Debug.Log("[NodeEditor] StartConnectionDrag called!");
        sourcePin = source;

        // Instantiate a new connection line prefab (allowing multiple connections).
        GameObject lineObj = Instantiate(connectionViewPrefab, editorCanvas);
        draggingLine = lineObj.GetComponent<NodeConnectionView>();

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        draggingLine.sourcePin = source;
        draggingLine.StartPreview(sourceRect);
    }

    public void UpdateConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        // The NodeConnectionView preview is updated in its own Update() using Input.mousePosition.
    }

    public void EndConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        if (draggingLine == null)
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
            RectTransform targetRect = targetPin.GetComponent<RectTransform>();
            draggingLine.targetPin = targetPin;
            draggingLine.FinalizeConnection(targetRect);

            // Update connections: if both pins are Mana ports, record mana connection.
            if (sourcePin.port.portType == PortType.Mana && targetPin.port.portType == PortType.Mana)
            {
                string sourceId = GetNodeIdFromPin(sourcePin);
                string targetId = GetNodeIdFromPin(targetPin);
                if (currentGraph.manaConnections == null)
                    currentGraph.manaConnections = new Dictionary<string, string>();
                currentGraph.manaConnections[targetId] = sourceId;
            }
            // Else if both pins are General, update general adjacency.
            else if (sourcePin.port.portType == PortType.General && targetPin.port.portType == PortType.General)
            {
                string sourceId = GetNodeIdFromPin(sourcePin);
                string targetId = GetNodeIdFromPin(targetPin);
                if (currentGraph.adjacency == null)
                    currentGraph.adjacency = new Dictionary<string, List<string>>();
                if (!currentGraph.adjacency.ContainsKey(sourceId))
                    currentGraph.adjacency[sourceId] = new List<string>();
                if (!currentGraph.adjacency[sourceId].Contains(targetId))
                    currentGraph.adjacency[sourceId].Add(targetId);
            }

            Debug.Log("[NodeEditor] Connection finalized.");
        }
        else
        {
            Debug.Log("[NodeEditor] Connection canceled.");
            Destroy(draggingLine.gameObject);
        }

        draggingLine = null;
        sourcePin = null;
    }

    // --- Zooming & Panning ---

    public void OnScroll(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnScroll: {eventData.scrollDelta}");
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = editorCanvas.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        editorCanvas.localScale = Vector3.one * newScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        editorCanvas.anchoredPosition += eventData.delta;
    }

    // --- Helper Methods ---

    public string GetNodeIdFromPin(PinView pin)
    {
        NodeView nodeView = pin.GetComponentInParent<NodeView>();
        return nodeView.GetNodeData().nodeId;
    }

    public NodeGraph CurrentGraph => currentGraph;

    private void ClearExistingViews()
    {
        foreach (var view in spawnedNodeViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }
        spawnedNodeViews.Clear();
    }
    
    public void LoadGraph(NodeGraph graph)
    {
        currentGraph = graph;
        ClearExistingViews();
        if (currentGraph == null) return;
        foreach (NodeData node in currentGraph.nodes)
        {
            CreateNodeView(node);
        }
    }

}

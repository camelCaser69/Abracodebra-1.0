using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Editor Window Setup")]
    [Tooltip("This is the fixed window (NodeEditorWindow) that has a RectMask2D and CanvasGroup.")]
    [SerializeField] private RectTransform windowRect; // Should be this GameObject's RectTransform

    [Header("Content Container")]
    [Tooltip("This is the child RectTransform that holds nodes and connection lines.")]
    [SerializeField] private RectTransform contentRect; // All nodes and lines are spawned here

    [Header("Prefabs")]
    [SerializeField] private GameObject nodeViewPrefab; // Prefab for NodeView
    [SerializeField] private GameObject connectionViewPrefab; // Prefab for NodeConnectionView (with UICubicBezier)

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // For right-click creation

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph; // Stores nodes, adjacency, manaConnections

    [Header("Optional")]
    [SerializeField] private NodeExecutor executor; // Optional: updates graph in NodeExecutor

    [Header("Panning/Zoom Settings")]
    [SerializeField] private float contentMargin = 20f; // Margin inside the window

    // For connection dragging:
    private NodeConnectionView draggingLine;
    private PinView sourcePin;

    // Context menu variables:
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    // CanvasGroup for toggling visibility
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Use the windowRect from this GameObject.
        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        // Ensure this GameObject (window) has a CanvasGroup.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Ensure this window has an Image for pointer events.
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
            img.raycastTarget = true;
        }
    }

    private void Start()
    {
        if (currentGraph == null)
            currentGraph = new NodeGraph();

        if (executor == null)
        {
            executor = GameObject.FindFirstObjectByType<NodeExecutor>();
            if (executor != null)
                executor.SetGraph(currentGraph);
            else
                Debug.LogWarning("[NodeEditorController] No NodeExecutor found in scene.");
        }
    }

    private void Update()
    {
        // Toggle window visibility with TAB.
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleVisibility();

        // Open context menu on right-click.
        if (Input.GetMouseButtonDown(1))
        {
            showContextMenu = true;
            contextMenuPosition = Input.mousePosition;
        }
    }

    private void ToggleVisibility()
    {
        if (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
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
    /// Creates a new node from a definition at the current mouse position.
    /// </summary>
    private void CreateNodeAtMouse(NodeDefinition definition)
    {
        Vector2 localPos;
        // Convert screen position to local point within the contentRect (child container).
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, Input.mousePosition, null, out localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;
        newNode.backgroundColor = definition.backgroundColor;

        // Copy effects.
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

        // Copy ports.
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

        if (executor != null)
            executor.SetGraph(currentGraph);
    }

    /// <summary>
    /// Instantiates a NodeView and initializes it.
    /// </summary>
    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, contentRect);
        NodeView view = nodeObj.GetComponent<NodeView>();

        view.Initialize(data, data.backgroundColor, data.nodeDisplayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        // Auto-add OutputNodeEffect if needed.
        if (data.effects.Any(e => e.effectType == NodeEffectType.Output))
        {
            if (nodeObj.GetComponent<OutputNodeEffect>() == null)
                nodeObj.AddComponent<OutputNodeEffect>();
        }

        spawnedNodeViews.Add(view);
        return view;
    }

    // --- Connection Dragging Methods ---

    public void StartConnectionDrag(PinView source, PointerEventData eventData)
    {
        Debug.Log("[NodeEditor] StartConnectionDrag called!");
        sourcePin = source;

        GameObject lineObj = Instantiate(connectionViewPrefab, contentRect);
        draggingLine = lineObj.GetComponent<NodeConnectionView>();

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        draggingLine.sourcePin = source;
        draggingLine.StartPreview(sourceRect);
    }

    public void UpdateConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        // The preview line in NodeConnectionView updates itself using Input.mousePosition.
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

            if (sourcePin.port.portType == PortType.Mana && targetPin.port.portType == PortType.Mana)
            {
                string sourceId = GetNodeIdFromPin(sourcePin);
                string targetId = GetNodeIdFromPin(targetPin);
                if (currentGraph.manaConnections == null)
                    currentGraph.manaConnections = new Dictionary<string, string>();
                currentGraph.manaConnections[targetId] = sourceId;
            }
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
    // Zoom and pan apply to contentRect.
    public void OnScroll(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnScroll: {eventData.scrollDelta}");
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = contentRect.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        contentRect.localScale = Vector3.one * newScale;

        ClampContentPosition();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnDrag: {eventData.delta}");
        contentRect.anchoredPosition += eventData.delta;

        ClampContentPosition();
    }

    // Clamps the contentRect's anchoredPosition so that it stays within the window (with a margin).
    private void ClampContentPosition()
    {
        Vector2 windowSize = windowRect.rect.size;
        Vector2 contentSize = contentRect.rect.size * contentRect.localScale.x; // assuming uniform scale
        Vector2 currentPos = contentRect.anchoredPosition;

        // If content is smaller than window, center it.
        float clampedX = currentPos.x;
        float clampedY = currentPos.y;

        if (contentSize.x < windowSize.x)
            clampedX = 0;
        else
        {
            float maxX = (contentSize.x - windowSize.x) / 2 - contentMargin;
            float minX = -maxX;
            clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
        }

        if (contentSize.y < windowSize.y)
            clampedY = 0;
        else
        {
            float maxY = (contentSize.y - windowSize.y) / 2 - contentMargin;
            float minY = -maxY;
            clampedY = Mathf.Clamp(currentPos.y, minY, maxY);
        }

        contentRect.anchoredPosition = new Vector2(clampedX, clampedY);
    }

    // --- Load Graph ---

    public void LoadGraph(NodeGraph graph)
    {
        currentGraph = graph;
        ClearExistingViews();
        if (currentGraph == null) return;

        foreach (var node in currentGraph.nodes)
        {
            CreateNodeView(node);
        }

        if (executor != null)
            executor.SetGraph(currentGraph);
    }

    // --- Helper Methods ---

    public string GetNodeIdFromPin(PinView pin)
    {
        NodeView view = pin.GetComponentInParent<NodeView>();
        return view.GetNodeData().nodeId;
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
}

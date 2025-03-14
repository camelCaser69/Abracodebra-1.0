using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Window & Content Setup")]
    [Tooltip("The fixed window (NodeEditorWindow) with RectMask2D, which toggles visibility.")]
    [SerializeField] private RectTransform windowRect; // This is NodeEditorWindow (the parent panel)
    
    [Tooltip("The inner content panel where nodes and connections are spawned.")]
    [SerializeField] private RectTransform contentRect; // This should be a child of windowRect

    [Header("Prefabs")]
    [SerializeField] private GameObject nodeViewPrefab; // Prefab for NodeView
    [SerializeField] private GameObject connectionViewPrefab; // Prefab for NodeConnectionView (with UICubicBezier)

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // Contains NodeDefinition assets

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph; // Must have adjacency & manaConnections dictionaries

    [Header("Optional")]
    [SerializeField] private NodeExecutor executor; // Optional: updates graph for execution

    [Header("Panning/Zoom Settings")]
    [SerializeField] private float contentMargin = 20f; // Minimum margin between content and window borders

    // For connection dragging:
    private NodeConnectionView draggingLine;
    private PinView sourcePin;

    // Context menu variables for adding nodes:
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    private CanvasGroup canvasGroup; // For toggling window visibility

    private void Awake()
    {
        // Use this GameObject's RectTransform as windowRect if not assigned.
        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        // Ensure this window has a CanvasGroup for visibility toggling.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Ensure the window has a transparent Image so it receives pointer events.
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

        // Initially ensure content panel is large enough.
        EnsureContentPanelSize();
    }

    private void Update()
    {
        // Toggle visibility with TAB key.
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
    /// Creates a new node from a NodeDefinition at the current mouse position (converted into contentRect space).
    /// </summary>
    private void CreateNodeAtMouse(NodeDefinition definition)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, Input.mousePosition, null, out localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;
        newNode.backgroundColor = definition.backgroundColor;

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
        EnsureContentPanelSize();

        if (executor != null)
            executor.SetGraph(currentGraph);
    }

    /// <summary>
    /// Instantiates a NodeView from nodeViewPrefab and initializes it.
    /// </summary>
    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, contentRect);
        NodeView view = nodeObj.GetComponent<NodeView>();

        view.Initialize(data, data.backgroundColor, data.nodeDisplayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        // Auto-add OutputNodeEffect if node has an Output effect.
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
        // NodeConnectionView handles its preview update via Input.mousePosition.
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
    // Zoom and pan are applied to contentRect so that the window (NodeEditorWindow) stays fixed.
    public void OnScroll(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnScroll: {eventData.scrollDelta}");
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = contentRect.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        contentRect.localScale = Vector3.one * newScale;

        // Ensure content panel remains large enough.
        EnsureContentPanelSize();
    }

    public void OnDrag(PointerEventData eventData)
    {
    //    Debug.Log($"[NodeEditor] OnDrag: {eventData.delta}");
        contentRect.anchoredPosition += eventData.delta;
        EnsureContentPanelSize();
    }

    // Dynamically ensures contentRect is always larger than windowRect by a defined margin.
    private void EnsureContentPanelSize()
    {
        if (windowRect == null || contentRect == null)
            return;

        Vector2 windowSize = windowRect.rect.size;
        Vector2 minSize = windowSize + new Vector2(contentMargin * 2, contentMargin * 2);

        // Optionally, also enlarge content based on nodes' positions:
        if (spawnedNodeViews.Count > 0)
        {
            Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
            foreach (var view in spawnedNodeViews)
            {
                RectTransform rt = view.GetComponent<RectTransform>();
                Vector2 pos = rt.anchoredPosition;
                Vector2 size = rt.rect.size;
                minPos = Vector2.Min(minPos, pos - size * 0.5f);
                maxPos = Vector2.Max(maxPos, pos + size * 0.5f);
            }
            Vector2 nodesBounds = maxPos - minPos;
            minSize = Vector2.Max(minSize, nodesBounds + new Vector2(contentMargin * 2, contentMargin * 2));
        }

        // Set contentRect sizeDelta if needed.
        Vector2 currentSize = contentRect.sizeDelta;
        float newWidth = Mathf.Max(currentSize.x, minSize.x);
        float newHeight = Mathf.Max(currentSize.y, minSize.y);
        contentRect.sizeDelta = new Vector2(newWidth, newHeight);
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

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Window & Content Setup")]
    [Tooltip("The fixed window (NodeEditorWindow) with RectMask2D & CanvasGroup.")]
    [SerializeField] private RectTransform windowRect; // Parent window panel

    [Tooltip("The inner content panel where nodes and connections are spawned.")]
    [SerializeField] private RectTransform contentRect; // Child container for nodes

    [Header("Prefabs")]
    [SerializeField] private GameObject nodeViewPrefab; // Prefab for NodeView
    [SerializeField] private GameObject connectionViewPrefab; // Prefab for connection (with UICubicBezier)

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // For context menu

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph; // Contains nodes, adjacency, manaConnections

    [Header("Optional")]
    [SerializeField] private NodeExecutor executor; // Optional: updates graph

    [Header("Panning/Zoom Settings")]
    [SerializeField] private float contentMargin = 20f; // Minimum margin for content panel

    // For connection dragging:
    private NodeConnectionView draggingLine;
    private PinView sourcePin;

    // Context menu variables:
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image bg = GetComponent<Image>();
        if (bg == null)
        {
            bg = gameObject.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0);
            bg.raycastTarget = true;
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

        EnsureContentPanelSize();
    }

    private void Update()
    {
        // Hide context menu on left-click.
        if (Input.GetMouseButtonDown(0))
            showContextMenu = false;

        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleVisibility();

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NodeSelectable.CurrentSelected != null)
                DeleteSelectedNode();
        }

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

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, contentRect);
        NodeView view = nodeObj.GetComponent<NodeView>();

        view.Initialize(data, data.backgroundColor, data.nodeDisplayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        if (data.effects.Any(e => e.effectType == NodeEffectType.Output))
        {
            if (nodeObj.GetComponent<OutputNodeEffect>() == null)
                nodeObj.AddComponent<OutputNodeEffect>();
        }

        spawnedNodeViews.Add(view);
        return view;
    }

    // --- Deletion Logic ---
    private void DeleteSelectedNode()
    {
        NodeView selectedView = NodeSelectable.CurrentSelected.GetComponent<NodeView>();
        if (selectedView == null)
            return;

        string nodeId = selectedView.GetNodeData().nodeId;

        currentGraph.nodes.RemoveAll(n => n.nodeId == nodeId);

        if (currentGraph.adjacency != null)
        {
            currentGraph.adjacency.Remove(nodeId);
            foreach (var kvp in currentGraph.adjacency)
            {
                kvp.Value.RemoveAll(childId => childId == nodeId);
            }
        }

        if (currentGraph.manaConnections != null)
        {
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in currentGraph.manaConnections)
            {
                if (kvp.Key == nodeId || kvp.Value == nodeId)
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var key in keysToRemove)
                currentGraph.manaConnections.Remove(key);
        }

        // Remove any connection lines referencing this node.
        NodeConnectionView[] allLines = GameObject.FindObjectsOfType<NodeConnectionView>();
        foreach (var line in allLines)
        {
            if (line.sourcePin != null)
            {
                NodeView srcView = line.sourcePin.GetComponentInParent<NodeView>();
                if (srcView == null || srcView.GetNodeData().nodeId == nodeId)
                {
                    Destroy(line.gameObject);
                    continue;
                }
            }
            if (line.targetPin != null)
            {
                NodeView tgtView = line.targetPin.GetComponentInParent<NodeView>();
                if (tgtView == null || tgtView.GetNodeData().nodeId == nodeId)
                {
                    Destroy(line.gameObject);
                    continue;
                }
            }
        }

        Destroy(NodeSelectable.CurrentSelected);
        NodeSelectable.CurrentSelected = null;
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
        // Preview is updated within the connection line's own Update()
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
    public void OnScroll(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnScroll: {eventData.scrollDelta}");
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = contentRect.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        contentRect.localScale = Vector3.one * newScale;
        EnsureContentPanelSize();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnDrag: {eventData.delta}");
        contentRect.anchoredPosition += eventData.delta;
        EnsureContentPanelSize();
    }

    private void EnsureContentPanelSize()
    {
        if (windowRect == null || contentRect == null)
            return;

        Vector2 windowSize = windowRect.rect.size;
        Vector2 minSize = windowSize + new Vector2(contentMargin * 2, contentMargin * 2);

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

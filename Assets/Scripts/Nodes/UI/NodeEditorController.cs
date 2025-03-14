using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Node Editor References")]
    [SerializeField] private RectTransform editorCanvas; // The container for nodes
    [SerializeField] private GameObject nodeViewPrefab;  // Prefab for NodeView
    [SerializeField] private GameObject connectionViewPrefab; // Prefab for NodeConnectionView (with UICubicBezier)

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // Contains NodeDefinition assets

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph; // Must have adjacency & manaConnections dictionaries

    [Header("Optional")]
    [SerializeField] private NodeExecutor executor; // Optional reference to NodeExecutor

    // For connection dragging:
    private NodeConnectionView draggingLine;
    private PinView sourcePin;

    // Context menu variables for adding nodes:
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    private CanvasGroup canvasGroup;
    private RectTransform rootTransform;

    private void Awake()
    {
        rootTransform = GetComponent<RectTransform>();
        if (rootTransform == null)
        {
            Debug.LogError("[NodeEditorController] This GameObject must have a RectTransform!");
            return;
        }
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Ensure this panel has a transparent Image to receive pointer events.
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
    }

    private void Update()
    {
        // Toggle node editor visibility on TAB key.
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleVisibility();

        // Right-click for context menu.
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(editorCanvas, Input.mousePosition, null, out localPos);

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

        if (executor != null)
            executor.SetGraph(currentGraph);
    }

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        view.Initialize(data, data.backgroundColor, data.nodeDisplayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        // --- NEW: If this node contains an Output effect and doesn't already have an OutputNodeEffect,
        // add the OutputNodeEffect component automatically.
        if (data.effects.Any(e => e.effectType == NodeEffectType.Output))
        {
            if (nodeObj.GetComponent<OutputNodeEffect>() == null)
            {
                nodeObj.AddComponent<OutputNodeEffect>();
            }
        }
        // --- END NEW

        spawnedNodeViews.Add(view);
        return view;
    }

    // --- Connection Dragging Methods ---

    public void StartConnectionDrag(PinView source, PointerEventData eventData)
    {
        Debug.Log("[NodeEditor] StartConnectionDrag called!");
        sourcePin = source;

        GameObject lineObj = Instantiate(connectionViewPrefab, editorCanvas);
        draggingLine = lineObj.GetComponent<NodeConnectionView>();

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        draggingLine.sourcePin = source;
        draggingLine.StartPreview(sourceRect);
    }

    public void UpdateConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        // NodeConnectionView handles its own preview via Input.mousePosition.
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
        float newScale = rootTransform.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        rootTransform.localScale = Vector3.one * newScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnDrag: {eventData.delta}");
        rootTransform.anchoredPosition += eventData.delta;
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

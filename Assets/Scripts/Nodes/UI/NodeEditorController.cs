using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Node Editor References")]
    [Tooltip("Spawned node prefabs go here.")]
    [SerializeField] private GameObject nodeViewPrefab;

    [Tooltip("Spawned connection lines (NodeConnectionView) go here.")]
    [SerializeField] private GameObject connectionViewPrefab;

    [Tooltip("If you have a NodeDefinitionLibrary, assign it here for right-click creation.")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;

    [Tooltip("The NodeGraph that stores all nodes & adjacency.")]
    [SerializeField] private NodeGraph currentGraph;

    [Tooltip("Optional link to NodeExecutor so we can pass updated graphs to it.")]
    [SerializeField] private NodeExecutor executor;

    // For connection dragging
    private NodeConnectionView draggingLine;
    private PinView sourcePin;

    // Context menu
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();

    private void Awake()
    {
        // Ensure there's a transparent Image on this panel so it receives pointer events.
        var thisRect = GetComponent<RectTransform>();
        if (thisRect == null)
        {
            Debug.LogError("[NodeEditorController] The GameObject must have a RectTransform!");
            return;
        }

        // If no Image is present, add one so we can receive scroll & drag events.
        var image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            image.color = new Color(1,1,1,0);   // fully transparent
            image.raycastTarget = true;
        }
    }

    private void Start()
    {
        // If no graph assigned, create an empty one
        if (currentGraph == null)
        {
            currentGraph = new NodeGraph();
        }

        // If no executor assigned, try to find one
        if (executor == null)
        {
            executor = GameObject.FindFirstObjectByType<NodeExecutor>();
            if (executor != null)
                executor.SetGraph(currentGraph);
            else
                Debug.LogWarning("[NodeEditorController] No NodeExecutor found in the scene.");
        }
    }

    private void Update()
    {
        // Right-click for context menu
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
        // Convert screen to local position
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, Input.mousePosition, null, out localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;
        newNode.backgroundColor = definition.backgroundColor;

        // Copy effects
        foreach (var defEffect in definition.effects)
        {
            NodeEffectData eff = new NodeEffectData
            {
                effectType = defEffect.effectType,
                effectValue = defEffect.effectValue,
                secondaryValue = defEffect.secondaryValue
            };
            newNode.effects.Add(eff);
        }

        // Copy ports
        foreach (var portDef in definition.ports)
        {
            NodePort nPort = new NodePort
            {
                portName = portDef.portName,
                portType = portDef.portType
            };
            if (portDef.isInput)
                newNode.inputs.Add(nPort);
            else
                newNode.outputs.Add(nPort);
        }

        currentGraph.nodes.Add(newNode);
        CreateNodeView(newNode);

        // Update executor
        if (executor != null)
            executor.SetGraph(currentGraph);
    }

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, transform); // parent is NodeEditorPanel
        NodeView view = nodeObj.GetComponent<NodeView>();

        view.Initialize(data, data.backgroundColor, data.nodeDisplayName);

        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;

        view.GeneratePins(data.inputs, data.outputs);

        spawnedNodeViews.Add(view);
        return view;
    }

    // Connection Dragging

    public void StartConnectionDrag(PinView source, PointerEventData eventData)
    {
        Debug.Log("[NodeEditor] StartConnectionDrag");
        sourcePin = source;

        GameObject lineObj = Instantiate(connectionViewPrefab, transform);
        draggingLine = lineObj.GetComponent<NodeConnectionView>();

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        draggingLine.sourcePin = source;
        draggingLine.StartPreview(sourceRect);
    }

    public void UpdateConnectionDrag(PinView draggingPin, PointerEventData eventData)
    {
        // NodeConnectionView handles the preview
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

            // If both pins are Mana => record in manaConnections
            if (sourcePin.port.portType == PortType.Mana && targetPin.port.portType == PortType.Mana)
            {
                string sourceId = GetNodeIdFromPin(sourcePin);
                string targetId = GetNodeIdFromPin(targetPin);
                if (currentGraph.manaConnections == null)
                    currentGraph.manaConnections = new Dictionary<string, string>();
                currentGraph.manaConnections[targetId] = sourceId;
            }
            // If both pins are General => record adjacency
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

            Debug.Log("[NodeEditor] Connection finalized");
        }
        else
        {
            Debug.Log("[NodeEditor] Connection canceled");
            Destroy(draggingLine.gameObject);
        }

        draggingLine = null;
        sourcePin = null;
    }

    // Zoom (mouse wheel) & Pan (drag the panel)
    public void OnScroll(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnScroll => {eventData.scrollDelta}");
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = transform.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.4f, 2f);
        transform.localScale = Vector3.one * newScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[NodeEditor] OnDrag => {eventData.delta}");
        RectTransform rt = (RectTransform)transform;
        rt.anchoredPosition += eventData.delta;
    }

    // LoadGraph for NodeTestInitializer or other usage
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

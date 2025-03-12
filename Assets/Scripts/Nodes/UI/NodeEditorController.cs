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

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph;

    private List<NodeView> spawnedNodeViews = new List<NodeView>();
    
    // For dragging connections
    private NodeConnectionView activeConnectionLine;
    private PinView sourcePin;

    private bool showContextMenu;
    private Vector2 contextMenuPosition;

    // 1) We find a NodeExecutor in the scene to unify references
    private NodeExecutor executor;

    private void Start()
    {
        // If no graph, create a new one
        if (currentGraph == null)
            currentGraph = new NodeGraph();

        // Find a NodeExecutor
        executor = FindFirstObjectByType<NodeExecutor>();
        if (executor == null)
        {
            Debug.LogWarning("[NodeEditorController] No NodeExecutor found in scene.");
        }
        else
        {
            // Assign the same graph
            executor.SetGraph(currentGraph);
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

    private void Awake()
    {
        // Ensure there's a NodeGraph to work with.
        if (currentGraph == null)
        {
            currentGraph = new NodeGraph();
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

    private void ClearExistingViews()
    {
        foreach (var view in spawnedNodeViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        spawnedNodeViews.Clear();
    }

    private NodeView CreateNodeView(NodeData data)
    {
        GameObject nodeObj = Instantiate(nodeViewPrefab, editorCanvas);
        NodeView view = nodeObj.GetComponent<NodeView>();

        // Simple color & name
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            editorCanvas, Input.mousePosition, null, out localPos);

        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = definition.displayName;
        newNode.editorPosition = localPos;

        // Copy effects: if it's a ManaStorage effect, copy to dedicated fields; else, add normally.
        foreach (var defEffect in definition.effects)
        {
            if (defEffect.effectType == NodeEffectType.ManaStorage)
            {
                newNode.manaStorageCapacity = defEffect.effectValue;
                newNode.currentManaStorage = defEffect.secondaryValue; // Starting mana
            }
            else
            {
                NodeEffectData effectCopy = new NodeEffectData
                {
                    effectType = defEffect.effectType,
                    effectValue = defEffect.effectValue,
                    secondaryValue = defEffect.secondaryValue
                };
                newNode.effects.Add(effectCopy);
            }
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

        // Optionally re-assign the graph to the executor.
        if (executor != null)
            executor.SetGraph(currentGraph);
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
    
    private void OnNodesFinishedCreating()
    {
        // If your current graph is done, pass it to NodeExecutor
        executor.SetGraph(currentGraph);
    }

}

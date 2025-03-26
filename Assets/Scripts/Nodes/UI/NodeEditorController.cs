using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NodeEditorController : MonoBehaviour, IScrollHandler, IDragHandler
{
    [Header("Window & Content Setup")]
    [SerializeField] private RectTransform windowRect; // Panel with RectMask2D
    [SerializeField] private RectTransform contentRect; // Panel that holds nodes

    [Header("Prefabs")]
    [SerializeField] private GameObject nodeViewPrefab; // Must have NodeView component

    [Header("Node Definitions")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;

    [Header("Runtime Graph Reference")]
    [SerializeField] private NodeGraph currentGraph;
    
    [Header("Startup Settings")]
    [Tooltip("Should the node editor be visible when the game starts?")]
    
    [Header("Zoom and Panning")]
    [SerializeField] private float startingZoomMultiplier = 1f; // Default zoom at start
    [SerializeField] private float contentMargin = 20f;         // Margin around nodes

    public bool startVisible = true;
    private bool showContextMenu = false;
    private Vector2 contextMenuPosition;
    private List<NodeView> spawnedNodeViews = new List<NodeView>();
    private CanvasGroup canvasGroup;
    public RectTransform ContentRect => contentRect;


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

        // Set initial zoom.
        contentRect.localScale = Vector3.one * startingZoomMultiplier;
        EnsureContentPanelSize();
    
        // Set initial visibility based on the flag
        if (!startVisible)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        // Hide context menu on left-click if outside.
        if (Input.GetMouseButtonDown(0))
        {
            if (showContextMenu && definitionLibrary != null && definitionLibrary.definitions != null)
            {
                Vector2 guiPos = new Vector2(contextMenuPosition.x, Screen.height - contextMenuPosition.y);
                float menuHeight = 20 + (definitionLibrary.definitions.Count * 25);
                Rect menuRect = new Rect(guiPos.x, guiPos.y, 180, menuHeight);

                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                if (!menuRect.Contains(mousePos))
                    showContextMenu = false;
            }
        }
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

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (!menuRect.Contains(Event.current.mousePosition))
                    showContextMenu = false;
            }

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
    // 1. Convert screen coords → local coords in contentRect
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        contentRect, Input.mousePosition, null, out localPos);

    // 2. Convert local coords → hex coords (flat-top style)
    float hexSizeValue = (HexGridManager.Instance != null) 
        ? HexGridManager.Instance.hexSize 
        : 50f;

    // If your HexCoords.WorldToHex expects localPos to be the same coordinate space 
    // as used in your HexCoords logic, we can feed localPos directly:
    HexCoords hc = HexCoords.WorldToHex(localPos, hexSizeValue);

    // 3. Convert hex coords → local snapped coords
    Vector2 snappedLocal = hc.HexToWorld(hexSizeValue);

    // 4. Create the new node data
    NodeData newNode = new NodeData
    {
        nodeDisplayName = definition.displayName,
        backgroundColor = definition.backgroundColor,
        description = definition.description,
        coords = hc,
        editorPosition = snappedLocal // This is the local position inside contentRect
    };

    // Copy the definition’s effects & ports
    foreach (var defEffect in definition.effects)
    {
        NodeEffectData effectCopy = new NodeEffectData
        {
            effectType = defEffect.effectType,
            effectValue = defEffect.effectValue,
            secondaryValue = defEffect.secondaryValue,
            extra1 = defEffect.extra1,
            extra2 = defEffect.extra2,
            leafPattern = defEffect.leafPattern,         // NEW: copy leafPattern
            growthRandomness = defEffect.growthRandomness    // NEW: copy growthRandomness
        };
        newNode.effects.Add(effectCopy);
    }

    foreach (var portDef in definition.ports)
    {
        NodePort nodePort = new NodePort
        {
            isInput  = portDef.isInput,
            portType = portDef.portType,
            side     = portDef.side
        };
        newNode.ports.Add(nodePort);
    }

    // 5. Add node to graph and spawn the node view
    currentGraph.nodes.Add(newNode);
    CreateNodeView(newNode);
    EnsureContentPanelSize();
}




    public NodeView CreateNodeView(NodeData data)
    {
        if (nodeViewPrefab == null)
        {
            Debug.LogError("[NodeEditorController] nodeViewPrefab is not assigned!");
            return null;
        }
        GameObject nodeObj = Instantiate(nodeViewPrefab, contentRect);
        NodeView view = nodeObj.GetComponent<NodeView>();
        if (view == null)
        {
            Debug.LogError("[NodeEditorController] The instantiated node prefab does not have a NodeView component!");
            return null;
        }
        view.Initialize(data, data.backgroundColor, data.nodeDisplayName);
        RectTransform rt = nodeObj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.editorPosition;
        view.GeneratePins(data.ports);

        // If node has an Output effect, attach the OutputNodeEffect script
        if (data.effects.Any(e => e.effectType == NodeEffectType.Output))
        {
            if (nodeObj.GetComponent<OutputNodeEffect>() == null)
                nodeObj.AddComponent<OutputNodeEffect>();
        }

        spawnedNodeViews.Add(view);
        return view;
    }


    private void DeleteSelectedNode()
    {
        NodeView selectedView = NodeSelectable.CurrentSelected.GetComponent<NodeView>();
        if (selectedView == null)
            return;
        string nodeId = selectedView.GetNodeData().nodeId;
        currentGraph.nodes.RemoveAll(n => n.nodeId == nodeId);
        Destroy(NodeSelectable.CurrentSelected);
        NodeSelectable.CurrentSelected = null;
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;
        float newScale = contentRect.localScale.x + scrollDelta * 0.05f;
        newScale = Mathf.Clamp(newScale, 0.5f, 2f);
        contentRect.localScale = Vector3.one * newScale;
        EnsureContentPanelSize();
    }

    public void OnDrag(PointerEventData eventData)
    {
        contentRect.anchoredPosition += eventData.delta;
        EnsureContentPanelSize();
    }

    private void EnsureContentPanelSize()
    {
        if (windowRect == null || contentRect == null)
            return;

        // Remove null entries from spawnedNodeViews
        spawnedNodeViews = spawnedNodeViews.Where(v => v != null).ToList();

        Vector2 windowSize = windowRect.rect.size;
        Vector2 minSize = windowSize + new Vector2(contentMargin * 2, contentMargin * 2);

        if (spawnedNodeViews.Count > 0)
        {
            Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
            foreach (var view in spawnedNodeViews)
            {
                if (view == null)
                    continue;

                RectTransform rt = view.GetComponent<RectTransform>();
                Vector2 pos = rt.anchoredPosition;
                Vector2 size = rt.rect.size;
                minPos = Vector2.Min(minPos, pos - size * 0.5f);
                maxPos = Vector2.Max(maxPos, pos + size * 0.5f);
            }
            Vector2 bounds = maxPos - minPos;
            minSize = Vector2.Max(minSize, bounds + new Vector2(contentMargin * 2, contentMargin * 2));
        }

        Vector2 currSize = contentRect.sizeDelta;
        float newW = Mathf.Max(currSize.x, minSize.x);
        float newH = Mathf.Max(currSize.y, minSize.y);
        contentRect.sizeDelta = new Vector2(newW, newH);
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
        foreach (var nd in currentGraph.nodes)
            CreateNodeView(nd);
    }
}

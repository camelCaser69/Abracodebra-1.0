using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class NodeEditorGridController : MonoBehaviour
{
    public static NodeEditorGridController Instance { get; private set; }

    [Header("Grid Layout & Appearance")]
    [SerializeField][Min(1)] private int emptyCellsCount = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Empty Cell Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    [SerializeField] private Vector3 emptyCellScale = Vector3.one;

    [Header("Node Visuals")]
    [SerializeField] private GameObject nodeViewPrefab;
    [SerializeField] private Vector3 nodeImageScale = Vector3.one;
    [SerializeField] private Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Header("Node Definitions & Interaction")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;
    [SerializeField] private TMP_Dropdown nodeDropdown;

    // REMOVED: The direct reference to NodeExecutor is no longer needed here.
    // The NodeExecutor script will find this controller via Instance or assignment.
    // [Header("Execution")]
    // [SerializeField] private NodeExecutor nodeExecutor;

    [Header("UI Toggle")]
    [SerializeField] private GameObject gridUIParent;


    private List<NodeCell> nodeCells = new List<NodeCell>();
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;
    private NodeGraph _uiGraphRepresentation = new NodeGraph(); // Internal storage


    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Vector3 NodeImageScale => nodeImageScale;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor;
    public Color EmptyCellColor => emptyCellColor;
    public NodeGraph GetCurrentUIGraph() => _uiGraphRepresentation; // Public getter for NodeExecutor


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) Debug.LogError("...");
        if (gridUIParent == null) Debug.LogWarning("...");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);
        else Debug.LogWarning("...");
        if (definitionLibrary == null) Debug.LogError("...");

        CreateCells();
        RefreshGraph();
    }

    private void CreateCells()
    {
        foreach (Transform child in transform) { if (child.gameObject != this.gameObject && child.GetComponent<NodeEditorGridController>() == null) Destroy(child.gameObject); }
        nodeCells.Clear();
        NodeCell.ClearSelection();

        float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
        float startX = -(totalWidth / 2f);
        float startY = -(cellSize.y / 2f);

        for (int i = 0; i < emptyCellsCount; i++)
        {
            GameObject cellGO = new GameObject($"Cell_{i}");
            RectTransform rt = cellGO.AddComponent<RectTransform>();
            cellGO.transform.SetParent(transform, false);
            rt.sizeDelta = cellSize;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0f); // Using bottom-left pivot
            float xPos = startX + i * (cellSize.x + cellMargin);
            float yPos = startY;
            rt.anchoredPosition = new Vector2(xPos, yPos);

            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;
            // rt.localScale = emptyCellScale; // Keep commented out

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage);
            nodeCells.Add(cellLogic);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleGridUI();
        if (Input.GetKeyDown(KeyCode.Delete)) { if (NodeCell.CurrentlySelectedCell != null) { NodeCell selected = NodeCell.CurrentlySelectedCell; selected.RemoveNode(); RefreshGraph(); } }
        if (Input.GetKeyDown(KeyCode.Escape)) { if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown(); }
    }

    public void ToggleGridUI()
    {
        if (gridUIParent != null) { bool currentState = gridUIParent.activeSelf; gridUIParent.SetActive(!currentState); if (!gridUIParent.activeSelf && nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown(); if (!gridUIParent.activeSelf) NodeCell.ClearSelection(); }
        else Debug.LogWarning("...");
    }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (nodeDropdown == null) { Debug.LogError("..."); return; }
        if (definitionLibrary == null || definitionLibrary.definitions == null) { Debug.LogError("..."); return; }

        StopCoroutine("ShowDropdownCoroutine");
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

    private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
    {
        // Build Options
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Select Node..."));
        var sortedDefinitions = definitionLibrary.definitions.Where(def => def != null).OrderBy(def => def.displayName).ToList();
        foreach (var def in sortedDefinitions) { TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(); option.text = def.displayName; option.image = def.thumbnail; options.Add(option); }
        nodeDropdown.ClearOptions(); nodeDropdown.AddOptions(options);

        // Setup Listener
        nodeDropdown.onValueChanged.RemoveAllListeners();
        nodeDropdown.onValueChanged.AddListener((selectedIndex) => { OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions); });

        // Position
        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dropdownRect.parent as RectTransform, eventData.position, _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, out Vector2 localPos);
        dropdownRect.localPosition = localPos;

        // Activate
        if (!nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(true);

        // Wait
        yield return null;

        // Show
        try {
            if (nodeDropdown.template == null) { Debug.LogError("..."); HideDropdown(); yield break; }
            nodeDropdown.Show();
        } catch (System.NullReferenceException nre) { Debug.LogError($"... NRE Show() ... {nre.Message}", nodeDropdown.gameObject); HideDropdown(); yield break; }

        nodeDropdown.value = 0; nodeDropdown.RefreshShownValue();
    }


    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefs)
    {
        HideDropdown();

        if (selectedIndex > 0) {
            int definitionIndex = selectedIndex - 1;
            if (definitionIndex >= 0 && definitionIndex < sortedDefs.Count) {
                NodeDefinition selectedDef = sortedDefs[definitionIndex];
                targetCell.AssignNode(selectedDef);
                NodeCell.SelectCell(targetCell);
                RefreshGraph(); // Refresh UI graph representation
            }
        }
    }

    public void HideDropdown()
    {
        if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(false);
    }


    public void RefreshGraph()
    {
        if (_uiGraphRepresentation == null) _uiGraphRepresentation = new NodeGraph();
        _uiGraphRepresentation.nodes.Clear();
        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex))
        {
            NodeData data = cell.GetNodeData();
            if (data != null)
            {
                data.orderIndex = cell.CellIndex;
                _uiGraphRepresentation.nodes.Add(data);
            }
        }
    }


    public bool HandleNodeDrop(NodeDraggable draggedDraggable, NodeCell originalCell, Vector2 screenPosition)
    {
        NodeCell targetCell = FindCellAtScreenPosition(screenPosition);
        bool changed = false;

        if (targetCell != null && originalCell != null)
        {
            NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
            NodeData draggedData = draggedView?.GetNodeData();

            if (draggedView == null || draggedData == null) { draggedDraggable.ResetPosition(); return false; }
            if (targetCell == originalCell) { draggedDraggable.ResetPosition(); NodeCell.SelectCell(targetCell); return true; }

            NodeView existingViewInTarget = targetCell.GetNodeView();
            NodeData existingDataInTarget = targetCell.GetNodeData();

            NodeCell.ClearSelection();
            originalCell.ClearNodeReference();

            if (existingViewInTarget != null && existingDataInTarget != null)
            {
                NodeDraggable existingDraggable = existingViewInTarget.GetComponent<NodeDraggable>();
                originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget);
                if (existingDraggable != null) existingDraggable.SnapToCell(originalCell);
            }

            targetCell.AssignNodeView(draggedView, draggedData);
            draggedDraggable.SnapToCell(targetCell);

            NodeCell.SelectCell(targetCell);
            changed = true;
        }
        else
        {
            draggedDraggable.ResetPosition();
            if (originalCell != null && originalCell.HasNode()) { NodeCell.SelectCell(originalCell); }
            else { NodeCell.ClearSelection(); }
        }

        if (changed) RefreshGraph(); // Refresh UI graph representation

        return changed;
    }


    private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
    {
        NodeCell foundCell = null;
        foreach (var cell in nodeCells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            bool contains = RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition, _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera );
            if (contains) { foundCell = cell; break; }
        }
        return foundCell;
    }


     #if UNITY_EDITOR
     void OnDrawGizmos()
     {
         if (!Application.isPlaying && TryGetComponent<RectTransform>(out var rt))
         {
             Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
             Vector3 center = rt.position;
             float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
             float startX_for_gizmo;
             if(rt.pivot == Vector2.zero) startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f);
             else startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f);

             for (int i = 0; i < emptyCellsCount; i++)
             {
                 float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                 Vector3 cellCenterWorld = center + (Vector3)(rt.rotation * new Vector3(xOffset * rt.lossyScale.x, 0, 0) );
                 Vector3 gizmoSize = new Vector3(cellSize.x * rt.lossyScale.x, cellSize.y * rt.lossyScale.y, 0.1f);
                 Matrix4x4 rotationMatrix = Matrix4x4.TRS(cellCenterWorld, rt.rotation, Vector3.one);
                 Gizmos.matrix = rotationMatrix;
                 Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                 Gizmos.matrix = Matrix4x4.identity;
             }
         }
     }
     #endif
}
// FILE: Assets/Scripts/Nodes/UI/NodeEditorGridController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class NodeEditorGridController : MonoBehaviour
{
    public static NodeEditorGridController Instance { get; private set; }

    [Header("Grid Layout & Appearance")]
    [SerializeField][Min(1)] private int emptyCellsCount = 8; // For the main sequence
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

    [Header("UI References")]
    [Tooltip("The UI GameObject (Panel) that acts as the main container for the Node Editor. This will be controlled by UIManager.")]
    [SerializeField] public GameObject gridUIParent;
    [Tooltip("The Transform within gridUIParent where sequence cell GameObjects should be created.")]
    [SerializeField] private Transform cellContainer;

    private List<NodeCell> nodeCells = new List<NodeCell>();
    private Canvas _rootCanvas;
    private NodeGraph _uiGraphRepresentation = new NodeGraph();

    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Vector3 NodeImageScale => nodeImageScale;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor;
    public Color EmptyCellColor => emptyCellColor;
    public NodeGraph GetCurrentUIGraph() => _uiGraphRepresentation;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null) _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);

        if (gridUIParent == null) Debug.LogError("[NodeEditorGridController] Grid UI Parent not assigned.", gameObject);
        if (cellContainer == null) Debug.LogError("[NodeEditorGridController] Cell Container not assigned.", gameObject);
        if (nodeDropdown == null) Debug.LogWarning("[NodeEditorGridController] Node Dropdown not assigned.", gameObject);
        if (definitionLibrary == null) Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned!", gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);

        if (cellContainer != null && definitionLibrary != null)
        {
            CreateCells();
            SpawnInitialNodes();
            RefreshGraph();
        }
        else
        {
            Debug.LogError("[NodeEditorGridController] Cannot initialize sequence grid - Cell Container or Definition Library is missing.", gameObject);
        }
    }

    private void CreateCells()
    {
        if (cellContainer == null) return;

        foreach (Transform child in cellContainer)
        {
            if (child.GetComponent<NodeCell>() != null) Destroy(child.gameObject);
        }
        nodeCells.Clear();
        NodeCell.ClearSelection();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogWarning("[NodeEditorGridController] Cell Container for sequence does not have a GridLayoutGroup. Using manual layout. Consider adding GridLayoutGroup.", cellContainer.gameObject);
            RectTransform containerRect = cellContainer.GetComponent<RectTransform>();
            if (containerRect == null) { Debug.LogError("CellContainer needs a RectTransform for manual layout!", cellContainer.gameObject); return; }
            float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
            float startX = -(totalWidth / 2f) + (cellSize.x / 2f);
            float startY = 0;
            for (int i = 0; i < emptyCellsCount; i++)
            {
                GameObject cellGO = new GameObject($"SequenceCell_{i}");
                RectTransform rt = cellGO.AddComponent<RectTransform>();
                cellGO.transform.SetParent(cellContainer, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = cellSize;
                rt.anchoredPosition = new Vector2(startX + i * (cellSize.x + cellMargin), startY);
                rt.localScale = emptyCellScale;
                SetupCellComponents(cellGO, i);
            }
        }
        else // Using GridLayoutGroup
        {
            gridLayout.cellSize = cellSize; // Ensure GridLayout settings match scriptable ones
            gridLayout.spacing = new Vector2(cellMargin, cellMargin);
            // Ensure other GridLayoutGroup settings (constraint, constraintCount, padding, childAlignment) are set correctly in Inspector.
            for (int i = 0; i < emptyCellsCount; i++)
            {
                GameObject cellGO = new GameObject($"SequenceCell_{i}");
                cellGO.transform.SetParent(cellContainer, false);
                // If using LayoutElement for more control with GridLayoutGroup:
                // LayoutElement le = cellGO.AddComponent<LayoutElement>();
                // le.preferredWidth = cellSize.x; le.preferredHeight = cellSize.y;
                SetupCellComponents(cellGO, i);
            }
        }
    }

    private void SetupCellComponents(GameObject cellGO, int index)
    {
        Image cellImage = cellGO.AddComponent<Image>();
        cellImage.sprite = emptyCellSprite;
        cellImage.color = emptyCellColor;
        cellImage.raycastTarget = true;
        NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
        cellLogic.Init(index, this, null, cellImage); // For sequence cells, inventoryController is null
        nodeCells.Add(cellLogic);
    }

    private void SpawnInitialNodes()
    {
        if (definitionLibrary == null || definitionLibrary.initialNodes == null) return;
        if (nodeCells.Count == 0) { Debug.LogWarning("[NodeEditorGridController] Cannot spawn initial nodes - cells not created."); return; }

        foreach (var config in definitionLibrary.initialNodes)
        {
            if (config.nodeDefinition == null) { Debug.LogWarning("InitialNodeConfig has null NodeDefinition. Skipping."); continue; }
            if (config.cellIndex < 0 || config.cellIndex >= nodeCells.Count) { Debug.LogWarning($"InitialNodeConfig for '{config.nodeDefinition.name}' has invalid cellIndex {config.cellIndex}. Skipping."); continue; }
            NodeCell targetCell = nodeCells[config.cellIndex];
            if (targetCell.HasNode()) { Debug.LogWarning($"InitialNodeConfig for '{config.nodeDefinition.name}' targets occupied cell {config.cellIndex}. Skipping."); continue; }
            
            targetCell.AssignNode(config.nodeDefinition);
            NodeView spawnedView = targetCell.GetNodeView();
            if (spawnedView != null)
            {
                NodeDraggable draggable = spawnedView.GetComponent<NodeDraggable>();
                if (draggable != null) draggable.enabled = config.canMove;
                NodeData spawnedData = targetCell.GetNodeData();
                if (spawnedData != null) spawnedData.canBeDeleted = config.canDelete;
            }
        }
    }

    void Update()
    {
        if (gridUIParent != null && gridUIParent.activeInHierarchy) // Only process input if this UI is active
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (NodeCell.CurrentlySelectedCell != null && !NodeCell.CurrentlySelectedCell.IsInventoryCell) // Ensure it's a sequence cell
                {
                    NodeCell selected = NodeCell.CurrentlySelectedCell;
                    NodeData data = selected.GetNodeData();
                    if (data != null && data.canBeDeleted)
                    {
                        selected.RemoveNode();
                        RefreshGraph();
                    }
                    else if (data != null && !data.canBeDeleted) Debug.Log($"Node '{data.nodeDisplayName}' in sequence cannot be deleted.");
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown();
                else if (NodeCell.CurrentlySelectedCell != null) NodeCell.ClearSelection(); // Deselect if dropdown not open
            }
        }
    }

    public void ShowNodeEditor() { if (gridUIParent != null) gridUIParent.SetActive(true); }
    public void HideNodeEditor() { if (gridUIParent != null) { gridUIParent.SetActive(false); if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown(); NodeCell.ClearSelection(); } }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (cell.IsInventoryCell) return; // Don't open for inventory cells
        if (nodeDropdown == null) { Debug.LogError("[NodeEditorGridController] Node Dropdown not assigned."); return; }
        if (definitionLibrary == null || definitionLibrary.definitions == null) { Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned/empty."); return; }
        StopCoroutine("ShowDropdownCoroutine"); // Ensure only one runs
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

    private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Select Node..."));
        var sortedDefinitions = definitionLibrary.definitions.Where(def => def != null).OrderBy(def => def.displayName).ToList();
        foreach (var def in sortedDefinitions) { options.Add(new TMP_Dropdown.OptionData { text = def.displayName, image = def.thumbnail }); }
        
        nodeDropdown.ClearOptions(); nodeDropdown.AddOptions(options);
        nodeDropdown.onValueChanged.RemoveAllListeners(); // Important to prevent multiple calls
        nodeDropdown.onValueChanged.AddListener((selectedIndex) => { OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions); });

        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        if (_rootCanvas != null && dropdownRect.parent is RectTransform parentRect) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, _rootCanvas.worldCamera, out Vector2 localPos);
            dropdownRect.localPosition = localPos;
        } else { Debug.LogWarning("[NodeEditorGridController] Cannot accurately position dropdown: RootCanvas or parent RectTransform issue."); }
        
        if (!nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(true);
        yield return null; // Wait for layout potentially
        
        try { 
            if (nodeDropdown.template == null) { Debug.LogError("Node Dropdown template is null!", nodeDropdown.gameObject); HideDropdown(); yield break; }
            nodeDropdown.Show(); 
        }
        catch (System.NullReferenceException nre) { Debug.LogError($"Error showing dropdown (template issue?): {nre.Message}", nodeDropdown.gameObject); HideDropdown(); yield break; }
        
        nodeDropdown.value = 0; nodeDropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefinitions)
    {
        HideDropdown();
        if (selectedIndex > 0) { // 0 is "Select Node..."
            int definitionIndexInSortedList = selectedIndex - 1;
            if (definitionIndexInSortedList >= 0 && definitionIndexInSortedList < sortedDefinitions.Count) {
                NodeDefinition selectedDef = sortedDefinitions[definitionIndexInSortedList];
                if (selectedDef != null) { 
                    targetCell.AssignNode(selectedDef); // Assigns to sequence cell
                    NodeCell.SelectCell(targetCell);    
                    RefreshGraph();                     
                }
            } else { Debug.LogError($"[NodeEditorGridController] Dropdown selection index ({selectedIndex}) invalid for sorted definition list."); }
        }
    }

    public void HideDropdown() { 
        if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) { 
            if (nodeDropdown.IsExpanded) nodeDropdown.Hide(); 
            nodeDropdown.gameObject.SetActive(false); 
        } 
    }

    public void RefreshGraph()
    {
        if (_uiGraphRepresentation == null) _uiGraphRepresentation = new NodeGraph();
        _uiGraphRepresentation.nodes.Clear();
        if (nodeCells != null) {
            foreach (var cell in nodeCells.OrderBy(c => c.CellIndex)) {
                NodeData data = cell.GetNodeData();
                if (data != null) { data.orderIndex = cell.CellIndex; _uiGraphRepresentation.nodes.Add(data); }
            }
        }
    }
    
    public NodeCell GetCellAtIndex(int index)
    {
        if (nodeCells == null) { Debug.LogError("[NodeEditorGridController] nodeCells list is null. Cannot GetCellAtIndex."); return null; }
        if (index >= 0 && index < nodeCells.Count) return nodeCells[index];
        Debug.LogWarning($"[NodeEditorGridController] GetCellAtIndex: Index {index} out of bounds (0-{(nodeCells.Count > 0 ? nodeCells.Count - 1 : -1)}). Returning null.");
        return null;
    }

    public void HandleDropOnSequenceCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSequenceCell)
{
    if (draggedDraggable == null || originalCell == null || targetSequenceCell == null || targetSequenceCell.IsInventoryCell)
    {
        draggedDraggable?.ResetPosition(); // Reset if invalid parameters
        return;
    }

    NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
    NodeDefinition draggedDef = draggedView?.GetNodeDefinition();

    if (draggedView == null || draggedDef == null) {
        Debug.LogError("[NodeEditorGridController] HandleDropOnSequenceCell: Dragged object missing NodeView/Definition.", draggedDraggable.gameObject);
        draggedDraggable.ResetPosition();
        return;
    }

    // --- Case: Dropped back onto its original sequence cell ---
    if (targetSequenceCell == originalCell && !originalCell.IsInventoryCell) // Ensure it's a sequence cell
    {
        draggedDraggable.SnapToCell(originalCell); // Explicitly snap it back to its origin
        NodeCell.SelectCell(originalCell); // Re-select it
        // No actual data change in the graph, so RefreshGraph() might be optional but harmless.
        // RefreshGraph(); 
        return;
    }

    // --- Scenario 1: Dragged from Inventory TO Sequence ---
    if (originalCell.IsInventoryCell)
    {
        InventoryGridController.Instance?.RemoveGeneFromInventory(originalCell);

        NodeData existingDataInTargetSeq = targetSequenceCell.GetNodeData();
        NodeView existingViewInTargetSeq = targetSequenceCell.GetNodeView();

        if (existingViewInTargetSeq != null && existingDataInTargetSeq != null)
        {
            NodeDefinition defFromSeqTarget = existingViewInTargetSeq.GetNodeDefinition();
            targetSequenceCell.ClearNodeReference();
            InventoryGridController.Instance?.ReturnGeneToInventory(defFromSeqTarget, null);
        }
        targetSequenceCell.AssignNode(draggedDef);
        // AssignNode creates a new view. The old draggedDraggable (from inventory) is now effectively gone.
        // The new view in targetSequenceCell needs its draggable snapped.
        targetSequenceCell.GetNodeView()?.GetComponent<NodeDraggable>()?.SnapToCell(targetSequenceCell);
        NodeCell.SelectCell(targetSequenceCell);
    }
    // --- Scenario 2: Dragged from Sequence TO a DIFFERENT Sequence Cell ---
    else // !originalCell.IsInventoryCell && targetSequenceCell != originalCell
    {
        NodeView existingViewInTarget = targetSequenceCell.GetNodeView();
        NodeData existingDataInTarget = targetSequenceCell.GetNodeData();
        NodeCell.ClearSelection();
        originalCell.ClearNodeReference(); // Original cell is now empty

        if (existingViewInTarget != null && existingDataInTarget != null) { // If target cell was occupied
            NodeDraggable draggableFromTarget = existingViewInTarget.GetComponent<NodeDraggable>();
            originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget); // Move item from target to original
            draggableFromTarget?.SnapToCell(originalCell);
        }
        // Assign the dragged node to the target cell
        NodeData draggedNodeData = draggedView.GetNodeData();
        targetSequenceCell.AssignNodeView(draggedView, draggedNodeData);
        draggedDraggable.SnapToCell(targetSequenceCell); // Snap the dragged item to the new target cell
        NodeCell.SelectCell(targetSequenceCell);
    }
    RefreshGraph();
}

    private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
    {
        NodeCell foundCell = null;
        if (cellContainer == null || _rootCanvas == null) return null;
        foreach (Transform cellTransform in cellContainer) {
            NodeCell cell = cellTransform.GetComponent<NodeCell>(); if (cell == null) continue;
            RectTransform cellRect = cell.GetComponent<RectTransform>(); if (cellRect == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition, _rootCanvas.worldCamera))
            { foundCell = cell; break; }
        }
        return foundCell;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        if (cellContainer == null || !cellContainer.gameObject.activeInHierarchy) return;
        if (cellContainer.TryGetComponent<RectTransform>(out var containerRect)) {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Matrix4x4 originalMatrix = Gizmos.matrix;
            GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null && gridLayout.enabled) { // Check if GridLayoutGroup is enabled
                // For GridLayoutGroup, just draw a box around the container as precise cell positions are dynamic
                Vector3[] corners = new Vector3[4]; containerRect.GetWorldCorners(corners);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawLine(corners[0], corners[1]); Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]); Gizmos.DrawLine(corners[3], corners[0]);
            } else { // Manual Layout Gizmos
                float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
                float startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f); float startY_for_gizmo = 0;
                for (int i = 0; i < emptyCellsCount; i++) {
                    float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                    Vector3 localCellCenter = new Vector3(xOffset, startY_for_gizmo, 0);
                    Vector3 worldCellCenter = cellContainer.TransformPoint(localCellCenter);
                    Vector3 gizmoSize = new Vector3(cellSize.x * cellContainer.lossyScale.x, cellSize.y * cellContainer.lossyScale.y, 0.1f);
                    Gizmos.matrix = Matrix4x4.TRS(worldCellCenter, cellContainer.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                }
            }
            Gizmos.matrix = originalMatrix;
        }
    }
    #endif
}
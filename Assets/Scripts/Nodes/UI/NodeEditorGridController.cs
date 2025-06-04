// FILE: Assets/Scripts/Nodes/UI/NodeEditorGridController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[RequireComponent(typeof(RectTransform))] // This component itself might not need a RectTransform if it's on a manager GO
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
    [SerializeField] private Vector3 emptyCellScale = Vector3.one; // For the cell GameObjects if not using GridLayout

    [Header("Node Visuals")]
    [SerializeField] private GameObject nodeViewPrefab; // Prefab for NodeView instances
    [SerializeField] private Vector3 nodeImageScale = Vector3.one; // Scale for the thumbnail inside NodeView
    [SerializeField] private Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Header("Node Definitions & Interaction")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;
    [SerializeField] private TMP_Dropdown nodeDropdown; // For adding new nodes to empty sequence cells

    [Header("UI References")]
    [Tooltip("The UI GameObject (Panel) that acts as the main container for the Node Editor. This will be controlled by UIManager.")]
    [SerializeField] public GameObject gridUIParent; // The Panel (e.g., NodeEditor_MainPanel) that UIManager will show/hide
    [Tooltip("The Transform within gridUIParent where sequence cell GameObjects should be created. Should have a GridLayoutGroup or manual layout.")]
    [SerializeField] private Transform cellContainer; // e.g., CellContainer_NodeEditor

    private List<NodeCell> nodeCells = new List<NodeCell>(); // For the main sequence
    private Canvas _rootCanvas; // The root canvas of the UI
    private NodeGraph _uiGraphRepresentation = new NodeGraph(); // Runtime representation of the sequence

    // Public accessors
    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Vector3 NodeImageScale => nodeImageScale;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor;
    public Color EmptyCellColor => emptyCellColor;
    public NodeGraph GetCurrentUIGraph() => _uiGraphRepresentation;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Find the root canvas this controller is part of
        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null) _rootCanvas = FindObjectOfType<Canvas>(); // Fallback if not directly parented
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);

        // Validate essential references
        if (gridUIParent == null) Debug.LogError("[NodeEditorGridController] Grid UI Parent (panel to toggle) not assigned.", gameObject);
        if (cellContainer == null) Debug.LogError("[NodeEditorGridController] Cell Container (parent for sequence cells) not assigned.", gameObject);
        if (nodeDropdown == null) Debug.LogWarning("[NodeEditorGridController] Node Dropdown not assigned (add node functionality will be limited).", gameObject);
        if (definitionLibrary == null) Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned!", gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false); // Hide dropdown initially

        if (cellContainer != null && definitionLibrary != null)
        {
            CreateCells(); // Create the sequence cells
            SpawnInitialNodes(); // Populate with any predefined initial nodes
            RefreshGraph(); // Build the initial _uiGraphRepresentation
        }
        else
        {
            Debug.LogError("[NodeEditorGridController] Cannot initialize sequence grid - Cell Container or Definition Library is missing.", gameObject);
        }
        // gridUIParent's visibility is controlled by UIManager.
    }

    private void CreateCells()
    {
        if (cellContainer == null) return;

        foreach (Transform child in cellContainer) // Clear any existing cells from previous runs/editor
        {
            if (child.GetComponent<NodeCell>() != null) Destroy(child.gameObject);
        }
        nodeCells.Clear();
        NodeCell.ClearSelection(); // Clear static selection state

        // Layout: Using GridLayoutGroup on cellContainer is recommended for easier management.
        // If using GridLayoutGroup, ensure its settings (CellSize, Spacing, Constraint) are correct.
        // If manual layout is preferred (as in earlier versions of this script):
        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogWarning("[NodeEditorGridController] Cell Container does not have a GridLayoutGroup. Using manual layout based on cell count and size. Consider adding GridLayoutGroup for easier management.");
            // Manual layout logic (if not using GridLayoutGroup)
            RectTransform containerRect = cellContainer.GetComponent<RectTransform>();
            if (containerRect == null) { Debug.LogError("CellContainer needs a RectTransform for manual layout!", cellContainer); return; }
            float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
            float startX = -(totalWidth / 2f) + (cellSize.x / 2f); // Assuming center pivot for container
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
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = new Vector2(cellMargin, cellMargin);
            // Ensure constraint and constraintCount are set correctly in Inspector for GridLayoutGroup

            for (int i = 0; i < emptyCellsCount; i++)
            {
                GameObject cellGO = new GameObject($"SequenceCell_{i}");
                // RectTransform rt = cellGO.AddComponent<RectTransform>(); // Not strictly needed if LayoutElement used
                cellGO.transform.SetParent(cellContainer, false);
                // cellGO.AddComponent<LayoutElement>(); // Helps GridLayoutGroup
                SetupCellComponents(cellGO, i);
            }
        }
    }

    private void SetupCellComponents(GameObject cellGO, int index)
    {
        Image cellImage = cellGO.AddComponent<Image>();
        cellImage.sprite = emptyCellSprite;
        cellImage.color = emptyCellColor;
        cellImage.raycastTarget = true; // Essential for drop detection

        NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
        // Initialize for a sequence cell (NodeEditorGridController is 'this', InventoryGridController is 'null')
        cellLogic.Init(index, this, null, cellImage);
        nodeCells.Add(cellLogic);
    }


    private void SpawnInitialNodes()
    {
        if (definitionLibrary == null || definitionLibrary.initialNodes == null) return;
        if (nodeCells.Count == 0)
        {
             Debug.LogWarning("[NodeEditorGridController] Cannot spawn initial nodes - sequence cells haven't been created.");
             return;
        }

        foreach (var config in definitionLibrary.initialNodes)
        {
            if (config.nodeDefinition == null)
            {
                Debug.LogWarning($"Initial node config has null NodeDefinition. Skipping.");
                continue;
            }
            if (config.cellIndex < 0 || config.cellIndex >= nodeCells.Count)
            {
                Debug.LogWarning($"Initial node config for '{config.nodeDefinition.name}' has invalid cell index ({config.cellIndex}). Max index is {nodeCells.Count - 1}. Skipping.");
                continue;
            }

            NodeCell targetCell = nodeCells[config.cellIndex];
            if (targetCell.HasNode())
            {
                Debug.LogWarning($"Initial node config for '{config.nodeDefinition.name}' targets cell {config.cellIndex}, but it's already occupied. Skipping.");
                continue;
            }

            targetCell.AssignNode(config.nodeDefinition); // This uses the NodeCell's AssignNode method

            NodeView spawnedView = targetCell.GetNodeView();
            if (spawnedView != null)
            {
                NodeDraggable draggable = spawnedView.GetComponent<NodeDraggable>();
                if (draggable != null)
                {
                    draggable.enabled = config.canMove; // Control if initial nodes are draggable
                }
                NodeData spawnedData = targetCell.GetNodeData();
                if (spawnedData != null)
                {
                    spawnedData.canBeDeleted = config.canDelete; // Control if initial nodes can be deleted
                }
            }
        }
    }

    void Update()
    {
        // Delete Node Handling (only if this UI panel is active)
        if (Input.GetKeyDown(KeyCode.Delete) && gridUIParent != null && gridUIParent.activeInHierarchy)
        {
            if (NodeCell.CurrentlySelectedCell != null && !NodeCell.CurrentlySelectedCell.IsInventoryCell)
            {
                NodeCell selected = NodeCell.CurrentlySelectedCell;
                NodeData data = selected.GetNodeData();

                if (data != null && data.canBeDeleted)
                {
                    selected.RemoveNode(); // NodeCell handles unhighlighting
                    RefreshGraph();
                }
                else if (data != null && !data.canBeDeleted)
                {
                     Debug.Log($"Node '{data.nodeDisplayName}' in sequence cannot be deleted.");
                }
            }
        }

        // Dropdown Escape Handling & Deselection (only if this UI panel is active)
        if (Input.GetKeyDown(KeyCode.Escape) && gridUIParent != null && gridUIParent.activeInHierarchy)
        {
            if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf)
            {
                HideDropdown();
            }
            else if (NodeCell.CurrentlySelectedCell != null) // If something is selected in sequence
            {
                 NodeCell.ClearSelection();
            }
        }
    }

    // These can be called by UIManager or other systems if direct control over visibility is needed
    public void ShowNodeEditor()
    {
        if (gridUIParent != null) gridUIParent.SetActive(true);
    }
    public void HideNodeEditor()
    {
        if (gridUIParent != null)
        {
            gridUIParent.SetActive(false);
            if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown();
            NodeCell.ClearSelection();
        }
    }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (cell.IsInventoryCell) return; // Dropdown only for sequence cells
        if (nodeDropdown == null) { Debug.LogError("[NodeEditorGridController] Node Dropdown not assigned."); return; }
        if (definitionLibrary == null || definitionLibrary.definitions == null) { Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned/empty."); return; }

        StopCoroutine("ShowDropdownCoroutine"); // Stop any previous instance
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

    private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Select Node...")); // Placeholder first option

        var sortedDefinitions = definitionLibrary.definitions
                                    .Where(def => def != null) // Filter out any nulls in the library
                                    .OrderBy(def => def.displayName)
                                    .ToList();

        foreach (var def in sortedDefinitions)
        {
            options.Add(new TMP_Dropdown.OptionData { text = def.displayName, image = def.thumbnail });
        }

        nodeDropdown.ClearOptions();
        nodeDropdown.AddOptions(options);

        nodeDropdown.onValueChanged.RemoveAllListeners(); // Clear previous listeners
        nodeDropdown.onValueChanged.AddListener((selectedIndex) => {
            OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions);
        });

        // Position and show dropdown
        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        if (_rootCanvas != null && dropdownRect.parent is RectTransform parentRect) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
                out Vector2 localPos
            );
            dropdownRect.localPosition = localPos;
        } else {
            Debug.LogWarning("[NodeEditorGridController] Cannot position dropdown accurately without RootCanvas or parent RectTransform.");
        }


        if (!nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(true);
        yield return null; // Wait a frame for layout to update if needed

        try
        {
            if (nodeDropdown.template == null) {
                Debug.LogError("Node Dropdown template is not assigned in the Inspector!", nodeDropdown.gameObject);
                HideDropdown();
                yield break;
            }
            nodeDropdown.Show(); // This opens the dropdown list
        }
        catch (System.NullReferenceException nre) // Catch potential errors during Show()
        {
             Debug.LogError($"Error showing dropdown: {nre.Message}. Is the template valid?", nodeDropdown.gameObject);
            HideDropdown(); // Ensure it's hidden on error
            yield break;
        }

        nodeDropdown.value = 0; // Set to the "Select Node..." placeholder
        nodeDropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefinitions)
    {
        HideDropdown(); // Hide after selection

        if (selectedIndex > 0) // Index 0 is "Select Node..."
        {
            int definitionIndexInSortedList = selectedIndex - 1;
            if (definitionIndexInSortedList >= 0 && definitionIndexInSortedList < sortedDefinitions.Count)
            {
                NodeDefinition selectedDef = sortedDefinitions[definitionIndexInSortedList];
                if (selectedDef != null)
                {
                    targetCell.AssignNode(selectedDef); // Assigns to the sequence cell
                    NodeCell.SelectCell(targetCell);    // Select the newly added node
                    RefreshGraph();                     // Update the internal graph representation
                }
            }
            else {
                 Debug.LogError($"[NodeEditorGridController] Dropdown selection index ({selectedIndex}) resulted in an out-of-bounds index ({definitionIndexInSortedList}) for the sorted definition list.");
            }
        }
    }

    public void HideDropdown()
    {
        if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf)
        {
            if (nodeDropdown.IsExpanded) nodeDropdown.Hide(); // Hide the list if open
            nodeDropdown.gameObject.SetActive(false); // Then hide the main dropdown object
        }
    }

    public void RefreshGraph()
    {
        if (_uiGraphRepresentation == null) _uiGraphRepresentation = new NodeGraph();
        _uiGraphRepresentation.nodes.Clear();

        if (nodeCells != null) // Ensure nodeCells list exists
        {
            foreach (var cell in nodeCells.OrderBy(c => c.CellIndex)) // Process in order
            {
                NodeData data = cell.GetNodeData();
                if (data != null)
                {
                    data.orderIndex = cell.CellIndex; // Ensure order index is correct
                    _uiGraphRepresentation.nodes.Add(data);
                }
            }
        }
    }
    
    public NodeCell GetCellAtIndex(int index)
    {
        if (nodeCells == null)
        {
            Debug.LogError("[NodeEditorGridController] nodeCells list is null. Cannot GetCellAtIndex.");
            return null;
        }
        if (index >= 0 && index < nodeCells.Count)
        {
            return nodeCells[index];
        }
        Debug.LogWarning($"[NodeEditorGridController] GetCellAtIndex: Index {index} out of bounds (0-{(nodeCells.Count > 0 ? nodeCells.Count - 1 : -1)}). Returning null.");
        return null;
    }

    public void HandleDropOnSequenceCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSequenceCell)
    {
        if (draggedDraggable == null || originalCell == null || targetSequenceCell == null || targetSequenceCell.IsInventoryCell)
        {
            draggedDraggable?.ResetPosition();
            return;
        }

        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition(); // Definition of the item being dragged

        if (draggedView == null || draggedDef == null) {
            Debug.LogError("[NodeEditorGridController] HandleDropOnSequenceCell: Dragged object missing NodeView or NodeDefinition!", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition();
            return;
        }

        // --- Scenario 1: Dragged from Inventory TO Sequence ---
        if (originalCell.IsInventoryCell)
        {
            // A. Remove from inventory source first. This makes the inventory cell empty.
            InventoryGridController.Instance?.RemoveGeneFromInventory(originalCell);

            NodeData existingDataInTargetSeq = targetSequenceCell.GetNodeData();
            NodeView existingViewInTargetSeq = targetSequenceCell.GetNodeView();

            // B. If target sequence cell is occupied, attempt to move its content to inventory
            if (existingViewInTargetSeq != null && existingDataInTargetSeq != null)
            {
                NodeDefinition defFromSeqTarget = existingViewInTargetSeq.GetNodeDefinition();
                targetSequenceCell.ClearNodeReference(); // Clear the target sequence cell
                
                // Try to return the item that was in targetSequenceCell to inventory
                InventoryGridController.Instance?.ReturnGeneToInventory(defFromSeqTarget, null); // null hint = find first empty inventory slot
                // Note: If inventory is full, ReturnGeneToInventory will log a warning and the item might be "lost" or
                // a more robust ReturnGeneToInventory could return a bool for success.
            }

            // C. Assign the new node (from inventory's NodeDefinition) to the sequence cell.
            // This creates a new NodeData instance for the sequence.
            targetSequenceCell.AssignNode(draggedDef);
            // The NodeView and NodeDraggable are created anew by AssignNode.
            // We need to get the new draggable and snap it.
            NodeView newViewInSequence = targetSequenceCell.GetNodeView();
            newViewInSequence?.GetComponent<NodeDraggable>()?.SnapToCell(targetSequenceCell);

            NodeCell.SelectCell(targetSequenceCell); // Select the newly placed node

            // The original draggedDraggable (which was from inventory and its view) is now orphaned if not destroyed by RemoveGeneFromInventory
            // RemoveGeneFromInventory should destroy the view. If not, do it here.
            // If RemoveGeneFromInventory only clears data, we need to destroy: Destroy(draggedDraggable.gameObject);
            // Assuming RemoveGeneFromInventory's NodeCell.RemoveNode() handles destroying the GameObject.
        }
        // --- Scenario 2: Dragged from Sequence TO Sequence (Re-ordering) ---
        else // !originalCell.IsInventoryCell
        {
            if (targetSequenceCell == originalCell) { // Dropped on itself
                draggedDraggable.ResetPosition();
                NodeCell.SelectCell(targetSequenceCell); // Re-select
                // No graph change needed if dropped on self, but RefreshGraph is harmless
                RefreshGraph();
                return;
            }

            NodeView existingViewInTarget = targetSequenceCell.GetNodeView();
            NodeData existingDataInTarget = targetSequenceCell.GetNodeData(); // This is the NodeData of item in target

            NodeCell.ClearSelection(); // Deselect before manipulation

            // 1. Clear the node from the original sequence cell (doesn't destroy, just unlinks)
            originalCell.ClearNodeReference();

            // 2. If target sequence cell was occupied, move its contents to the original cell
            if (existingViewInTarget != null && existingDataInTarget != null) {
                NodeDraggable draggableFromTarget = existingViewInTarget.GetComponent<NodeDraggable>();
                originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget); // Move item from target to original slot
                draggableFromTarget?.SnapToCell(originalCell); // Update its draggable state
            }

            // 3. Assign the dragged node (which already has its NodeData) to the target sequence cell
            NodeData draggedNodeData = draggedView.GetNodeData(); // Get the NodeData of the item being dragged
            targetSequenceCell.AssignNodeView(draggedView, draggedNodeData);
            draggedDraggable.SnapToCell(targetSequenceCell); // Update draggable state of dragged item

            NodeCell.SelectCell(targetSequenceCell); // Select the node in its new position
        }
        RefreshGraph(); // Update the _uiGraphRepresentation after any change
    }


    private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
    {
        NodeCell foundCell = null;
        if (cellContainer == null || _rootCanvas == null) return null;

        foreach (Transform cellTransform in cellContainer)
        {
            NodeCell cell = cellTransform.GetComponent<NodeCell>();
            if (cell == null) continue;

            RectTransform cellRect = cell.GetComponent<RectTransform>();
            if (cellRect == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                cellRect, screenPosition,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera))
            {
                foundCell = cell;
                break;
            }
        }
        return foundCell;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isPlaying) return; // Only draw in editor when not playing
        if (cellContainer == null || !cellContainer.gameObject.activeInHierarchy) return; // Only if container is visible

        if (cellContainer.TryGetComponent<RectTransform>(out var containerRect))
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f); // Lighter green for gizmos
            Matrix4x4 originalMatrix = Gizmos.matrix; // Store current matrix

            // If using GridLayoutGroup, iterate through its actual children positions
            GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                // This is tricky as GridLayoutGroup positions children dynamically.
                // A simpler gizmo might just draw bounds around the cellContainer.
                // For per-cell gizmos with GridLayout, one would need to simulate its layout.
                // Let's draw a simpler bounding box for the container if GridLayout is present.
                Vector3[] corners = new Vector3[4];
                containerRect.GetWorldCorners(corners);
                Gizmos.matrix = Matrix4x4.identity; // Use world space for corners
                Gizmos.DrawLine(corners[0], corners[1]);
                Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]);
                Gizmos.DrawLine(corners[3], corners[0]);

            }
            else // Manual Layout Gizmos (from before)
            {
                float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
                float startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f);
                float startY_for_gizmo = 0;

                for (int i = 0; i < emptyCellsCount; i++)
                {
                    float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                    Vector3 localCellCenter = new Vector3(xOffset, startY_for_gizmo, 0);
                    Vector3 worldCellCenter = cellContainer.TransformPoint(localCellCenter);
                    Vector3 gizmoSize = new Vector3(cellSize.x * cellContainer.lossyScale.x, cellSize.y * cellContainer.lossyScale.y, 0.1f);
                    Gizmos.matrix = Matrix4x4.TRS(worldCellCenter, cellContainer.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                }
            }
            Gizmos.matrix = originalMatrix; // Restore original matrix
        }
    }
    #endif
}
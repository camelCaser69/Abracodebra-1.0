using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Required for Image component
using TMPro;
using System.Linq;

// Ensure this component requires a RectTransform
[RequireComponent(typeof(RectTransform))]
public class NodeEditorGridController : MonoBehaviour
{
    [Header("Grid Layout & Appearance")]
    [Tooltip("Number of empty cells (slots) to create.")]
    [SerializeField][Min(1)] private int emptyCellsCount = 8;

    [Tooltip("Functional size (width/height) of a single cell.")]
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);

    [Tooltip("Space between adjacent cells.")]
    [SerializeField] private float cellMargin = 10f;

    [Header("Empty Cell Visuals")]
    [Tooltip("Sprite to display for empty cells.")]
    [SerializeField] private Sprite emptyCellSprite;
    [Tooltip("Color tint for the empty cell sprite.")]
    [SerializeField] private Color emptyCellColor = Color.white;
    [Tooltip("Scale multiplier for the empty cell sprite.")]
    [SerializeField] private Vector3 emptyCellScale = Vector3.one;

    [Header("Node Visuals")]
    [Tooltip("Default prefab for displaying a node (NodeView component required). Used if NodeDefinition.nodeViewPrefab is null.")]
    [SerializeField] private GameObject nodeViewPrefab; // Renamed from defaultNodeViewPrefab for clarity
    [Tooltip("Scale multiplier for the node's primary image (thumbnail).")]
    [SerializeField] private Vector3 nodeImageScale = Vector3.one;
    [Tooltip("Color tint for the node's primary image (thumbnail).")]
    [SerializeField] private Color nodeImageColor = Color.white;

    [Header("Selection Visuals")]
    [Tooltip("Color tint for the empty cell background when selected.")]
    [SerializeField] private Color selectedCellColor = new Color(0.8f, 0.9f, 1f, 1f);
    [Tooltip("Color tint for the empty cell background when not selected.")]
    [SerializeField] private Color defaultCellColor = Color.white; // This will be updated in Awake

    [Header("Node Definitions & Interaction")]
    [Tooltip("Library containing all available Node Definitions.")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;
    [Tooltip("TMP_Dropdown UI element for selecting nodes (should be inactive by default).")]
    [SerializeField] private TMP_Dropdown nodeDropdown;

    [Header("Execution")]
    [Tooltip("Reference to the NodeExecutor to update its graph.")]
    [SerializeField] private NodeExecutor nodeExecutor;

    // Runtime Data
    private List<NodeCell> nodeCells = new List<NodeCell>();
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;

    // Public accessors
    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Vector3 NodeImageScale => nodeImageScale;
    public Color NodeImageColor => nodeImageColor;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedCellColor => selectedCellColor; // Expose for NodeCell
    public Color DefaultCellColor => defaultCellColor; // Expose for NodeCell

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null)
        {
            Debug.LogError("[NodeEditorGridController] Could not find root Canvas parent!");
        }
        // Ensure default cell color matches the initial empty cell color setting
        defaultCellColor = emptyCellColor;
    }

    void Start()
    {
        if (nodeDropdown != null)
            nodeDropdown.gameObject.SetActive(false);
        else
            Debug.LogWarning("[NodeEditorGridController] Node Dropdown is not assigned.");

        if (definitionLibrary == null)
            Debug.LogError("[NodeEditorGridController] Node Definition Library is not assigned.");

        if (nodeExecutor == null)
            Debug.LogWarning("[NodeEditorGridController] Node Executor is not assigned.");
        else
            nodeExecutor.SetGraph(new NodeGraph()); // Initialize with an empty graph

        CreateCells();
        RefreshGraph(); // Initial graph state
    }

    private void CreateCells()
    {
        // Clear previous children if any
        foreach (Transform child in transform)
        {
            // Only destroy gameobjects, not potentially other components on this controller GO
             if (child.gameObject != this.gameObject && child.GetComponent<NodeEditorGridController>() == null)
             {
                 Destroy(child.gameObject);
             }
        }
        nodeCells.Clear();

        // Calculate total width required for all cells and margins
        float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
        // Calculate starting X position to center the row within this RectTransform
        float startX = -(totalWidth / 2f) + (cellSize.x / 2f);

        for (int i = 0; i < emptyCellsCount; i++)
        {
            // Create a new GameObject for the cell
            GameObject cellGO = new GameObject($"Cell_{i}");
            RectTransform rt = cellGO.AddComponent<RectTransform>();
            cellGO.transform.SetParent(transform, false); // Parent to this controller's transform

            // --- Setup RectTransform ---
            rt.sizeDelta = cellSize;
            rt.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f); // Center pivot
            // Calculate position relative to the center
            float xPos = startX + i * (cellSize.x + cellMargin);
            rt.anchoredPosition = new Vector2(xPos, 0f);

            // --- Add Image for Empty Cell Background ---
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = defaultCellColor; // Use default color initially
            cellImage.raycastTarget = true; // MUST be true for clicks to be received by NodeCell
            rt.localScale = emptyCellScale; // Apply scale

            // --- Add NodeCell Logic Component ---
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            // Pass the controller reference for accessing colors
            cellLogic.Init(i, this, cellImage);

            nodeCells.Add(cellLogic);
        }
    }

    // --- Update Method for Deletion ---
    void Update()
    {
        // Check for Delete key press
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            // Check if a cell is currently selected
            if (NodeCell.CurrentlySelectedCell != null)
            {
                Debug.Log($"Delete key pressed. Removing node from selected cell: {NodeCell.CurrentlySelectedCell.CellIndex}");
                NodeCell selected = NodeCell.CurrentlySelectedCell;
                NodeCell.ClearSelection(); // Deselect first
                selected.RemoveNode(); // Remove the node from the previously selected cell
                RefreshGraph(); // Update the logical graph
            }
        }
    }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        // **Error Prevention:** Double-check dropdown and library existence
        if (nodeDropdown == null) {
            Debug.LogError("[NodeEditorGridController] Dropdown is null!");
            return;
        }
         if (definitionLibrary == null || definitionLibrary.definitions == null) {
             Debug.LogError("[NodeEditorGridController] Definition Library is null or empty!");
             return;
         }
        // **Error Prevention:** Check if dropdown template exists (common cause of NRE in Show())
        if (nodeDropdown.template == null) {
             Debug.LogError("[NodeEditorGridController] Dropdown 'Template' is not assigned or is missing! Check the Dropdown GameObject in the Inspector.", nodeDropdown.gameObject);
             return;
        }

        // Build dropdown options
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Select Node...")); // Placeholder first option

        // Sort definitions alphabetically for user convenience (optional)
        var sortedDefinitions = definitionLibrary.definitions
                                    .Where(def => def != null) // Filter out null entries
                                    .OrderBy(def => def.displayName)
                                    .ToList();

        foreach (var def in sortedDefinitions)
        {
            // Create the OptionData using the standard constructor with text and sprite
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
            option.text = def.displayName;
            option.image = def.thumbnail; // Use the thumbnail sprite
            options.Add(option);
        }

        nodeDropdown.ClearOptions();
        nodeDropdown.AddOptions(options);

        // Important: Store which cell triggered the dropdown
        nodeDropdown.onValueChanged.RemoveAllListeners(); // Clear previous listeners
        nodeDropdown.onValueChanged.AddListener((selectedIndex) => {
            // Pass the selected definition index AND the target cell
            OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions);
        });

        // Position and activate the dropdown
        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dropdownRect.parent as RectTransform, // The parent RectTransform of the dropdown
            eventData.position,                   // The screen position of the click
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, // Camera for perspective/world space canvas
            out Vector2 localPos);
        dropdownRect.localPosition = localPos; // Position the dropdown

        nodeDropdown.gameObject.SetActive(true);

        // --- Call Show() - The location of the original error ---
        // If the error persists here, the issue is very likely the Dropdown's internal setup (Template/Blocker)
        try {
            nodeDropdown.Show(); // Force the dropdown list to open
        } catch (System.NullReferenceException nre) {
             Debug.LogError($"[NodeEditorGridController] NullReferenceException when calling nodeDropdown.Show()! This usually means the Dropdown's 'Template' or 'Blocker' is not set up correctly. Check the Dropdown GameObject named '{nodeDropdown.gameObject.name}' in the Inspector.\nError: {nre.Message}\nStackTrace:\n{nre.StackTrace}", nodeDropdown.gameObject);
             nodeDropdown.gameObject.SetActive(false); // Hide it again if Show() failed
             return; // Stop execution here if it failed
        }
        // --- End of Show() call ---

        nodeDropdown.value = 0; // Reset selection to the placeholder
        nodeDropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefs)
    {
        nodeDropdown.gameObject.SetActive(false); // Hide dropdown immediately

        // Index 0 is the placeholder ("Select Node...")
        if (selectedIndex > 0)
        {
            // Adjust index to match the list (since we added a placeholder at index 0)
            int definitionIndex = selectedIndex - 1;
            if (definitionIndex >= 0 && definitionIndex < sortedDefs.Count)
            {
                NodeDefinition selectedDef = sortedDefs[definitionIndex];
                // Assign the selected node definition to the cell
                targetCell.AssignNode(selectedDef);
                // Select the cell *after* assigning the node
                NodeCell.SelectCell(targetCell); // Select the cell where the node was added
                RefreshGraph(); // Update the logical graph
            }
        }
    }

    public void RefreshGraph()
    {
        if (nodeExecutor == null || nodeExecutor.currentGraph == null)
        {
             if (nodeExecutor == null) Debug.LogWarning("[NodeEditorGridController] Node Executor is null, cannot refresh graph.");
             return;
        }

        nodeExecutor.currentGraph.nodes.Clear();
        // Ensure cells are processed in their visual order (left-to-right)
        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex))
        {
            NodeData data = cell.GetNodeData();
            if (data != null)
            {
                data.orderIndex = cell.CellIndex; // Update order index based on cell position
                nodeExecutor.currentGraph.nodes.Add(data);
            }
        }
         // Optional: Log the updated graph
         // Debug.Log($"[NodeEditorGridController] Graph refreshed. Node count: {nodeExecutor.currentGraph.nodes.Count}");
    }

    public bool HandleNodeDrop(NodeDraggable draggedDraggable, NodeCell originalCell, Vector2 screenPosition)
    {
        NodeCell targetCell = FindCellAtScreenPosition(screenPosition);

        // Case 1: Dropped onto a valid cell
        if (targetCell != null && originalCell != null)
        {
            NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
            NodeData draggedData = draggedView?.GetNodeData();

            if (draggedView == null || draggedData == null)
            {
                 Debug.LogError("[NodeEditorGridController] Dragged item is missing NodeView or NodeData.");
                 // Attempt to reset position if something went wrong
                 draggedDraggable.ResetPosition();
                 return false;
            }

            // If dropping onto the same cell, just reset its position
            if (targetCell == originalCell)
            {
                draggedDraggable.ResetPosition();
                NodeCell.SelectCell(targetCell); // Re-select the cell
                return true; // Technically handled, just no change
            }

            // --- Perform Swap or Move ---
            NodeView existingViewInTarget = targetCell.GetNodeView();
            NodeData existingDataInTarget = targetCell.GetNodeData();

            // Clear current selection before modifying cells
            NodeCell.ClearSelection(); // Clear selection during swap

            // Step 1: Clear the original cell (remove reference, don't destroy view yet)
            originalCell.ClearNodeReference();

            // Step 2: If target cell is occupied, move its node to the original cell
            if (existingViewInTarget != null && existingDataInTarget != null)
            {
                NodeDraggable existingDraggable = existingViewInTarget.GetComponent<NodeDraggable>();
                originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget); // Assign existing node to original cell
                if (existingDraggable != null) existingDraggable.SnapToCell(originalCell); // Update parent and position
            }

            // Step 3: Move the dragged node to the target cell
            targetCell.AssignNodeView(draggedView, draggedData); // Assign dragged node to target cell
            draggedDraggable.SnapToCell(targetCell); // Update parent and position

            // Re-select the cell that the *dragged* node ended up in
            NodeCell.SelectCell(targetCell);

            RefreshGraph(); // Update the logical graph after the swap/move
            return true;
        }

        // Case 2: Dropped outside any valid cell
        // Reset the dragged node to its original position and parent
        draggedDraggable.ResetPosition();

        // If dropped outside, re-select original cell if it was selected
        if (NodeCell.CurrentlySelectedCell == null && originalCell != null) {
           // A simple approach: just re-select the original cell if nothing else is selected.
           NodeCell.SelectCell(originalCell);
        } else if (NodeCell.CurrentlySelectedCell == null) {
            // If no cell ended up selected, clear just in case
             NodeCell.ClearSelection();
        }

        return false;
    }

    private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
    {
        foreach (var cell in nodeCells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            // Use camera for correct calculation in World Space or Screen Space - Camera canvases
            if (RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition, _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera))
            {
                return cell;
            }
        }
        return null; // No cell found at the position
    }

     #if UNITY_EDITOR
     // Draw gizmos in the editor to visualize cell placement
     void OnDrawGizmos()
     {
         if (!Application.isPlaying) // Only draw if not playing, as positions are set in Start
         {
             Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
             RectTransform rt = GetComponent<RectTransform>();
             Vector3 center = rt.position; // Use world position

             float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
             float startX = -(totalWidth / 2f) + (cellSize.x / 2f);

             for (int i = 0; i < emptyCellsCount; i++)
             {
                 float xOffset = startX + i * (cellSize.x + cellMargin);
                 // Calculate world position for the gizmo
                 // Note: This assumes the controller's pivot is centered (0.5, 0.5)
                 Vector3 cellCenterWorld = center + rt.right * xOffset * rt.lossyScale.x; // Adjust for scale
                 // Convert cellSize from UI units to world units (approximate)
                 Vector3 gizmoSize = new Vector3(cellSize.x * rt.lossyScale.x, cellSize.y * rt.lossyScale.y, 0.1f);
                 Gizmos.DrawWireCube(cellCenterWorld, gizmoSize);
             }
         }
     }
     #endif
}
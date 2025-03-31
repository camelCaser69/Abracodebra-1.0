using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
    [Tooltip("Color tint for the empty cell sprite (the slot background).")]
    [SerializeField] private Color emptyCellColor = Color.white;
    [Tooltip("Scale multiplier for the empty cell sprite.")]
    [SerializeField] private Vector3 emptyCellScale = Vector3.one;

    [Header("Node Visuals")]
    [Tooltip("Default prefab for displaying a node (NodeView component required). Used if NodeDefinition.nodeViewPrefab is null.")]
    [SerializeField] private GameObject nodeViewPrefab;
    [Tooltip("Scale multiplier for the node's primary image (thumbnail).")]
    [SerializeField] private Vector3 nodeImageScale = Vector3.one;
    // REMOVED: Global nodeImageColor, now defined per NodeDefinition
    [Tooltip("The background color applied to a NodeView when it is selected.")]
    [SerializeField] private Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f); // Eg., Light Yellow

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
    // public Color NodeImageColor => nodeImageColor; // Removed
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor; // Expose for NodeView
    public Color EmptyCellColor => emptyCellColor; // Expose for NodeCell initialization

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Could not find root Canvas parent!");
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);
        else Debug.LogWarning("[NodeEditorGridController] Node Dropdown is not assigned.");

        if (definitionLibrary == null) Debug.LogError("[NodeEditorGridController] Node Definition Library is not assigned.");

        if (nodeExecutor == null) Debug.LogWarning("[NodeEditorGridController] Node Executor is not assigned.");
        else nodeExecutor.SetGraph(new NodeGraph());

        CreateCells();
        RefreshGraph();
    }

    private void CreateCells()
{
    // Clear previous children if any
    foreach (Transform child in transform)
    {
        if (child.gameObject != this.gameObject && child.GetComponent<NodeEditorGridController>() == null)
        {
            Destroy(child.gameObject);
        }
    }
    nodeCells.Clear();
    NodeCell.ClearSelection();

    // --- Calculate based on Bottom-Left Pivot ---
    // Total width calculation remains the same
    float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);

    // Calculate the starting X position for the *bottom-left corner* of the first cell,
    // assuming the *parent* NodeEditorGridController RectTransform has its pivot at (0.5, 0.5) (Center)
    // Start at the center, move left by half the total width to get the left edge
    float startX = -(totalWidth / 2f);

    // We also need to consider the vertical position if needed, assume centered vertically for now (Y=0 relative to parent center)
    float startY = -(cellSize.y / 2f); // Start Y for bottom edge relative to parent center

    for (int i = 0; i < emptyCellsCount; i++)
    {
        GameObject cellGO = new GameObject($"Cell_{i}");
        RectTransform rt = cellGO.AddComponent<RectTransform>();
        cellGO.transform.SetParent(transform, false);

        // --- Setup RectTransform with (0, 0) Pivot ---
        rt.sizeDelta = cellSize;
        // Set Anchor to center of parent (common setup)
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        // *** SET PIVOT TO BOTTOM-LEFT ***
        rt.pivot = new Vector2(0f, 0f);

        // Calculate the position of the bottom-left corner relative to the parent's center anchor
        float xPos = startX + i * (cellSize.x + cellMargin);
        float yPos = startY; // Keep Y position constant for the bottom edge
        rt.anchoredPosition = new Vector2(xPos, yPos);

        // Debug.Log($"Creating Cell_{i}: Pivot={rt.pivot}, AnchoredPos={rt.anchoredPosition}, Size={rt.sizeDelta}");

        // --- Add Image Component ---
        Image cellImage = cellGO.AddComponent<Image>();
        cellImage.sprite = emptyCellSprite;
        cellImage.color = emptyCellColor;
        cellImage.raycastTarget = true; // Keep this true
        // rt.localScale = emptyCellScale; // Keep commented out for now

        // --- Add NodeCell Logic Component ---
        NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
        cellLogic.Init(i, this, cellImage);

        nodeCells.Add(cellLogic);
    }
}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NodeCell.CurrentlySelectedCell != null)
            {
                NodeCell selected = NodeCell.CurrentlySelectedCell;
                // Selection is cleared automatically within RemoveNode if needed
                selected.RemoveNode();
                RefreshGraph();
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
    // **Error Prevention:** Check if dropdown template exists (Can't check if inactive, but good practice if active)
    // if (nodeDropdown.gameObject.activeInHierarchy && nodeDropdown.template == null) {
    //      Debug.LogError("[NodeEditorGridController] Dropdown 'Template' is not assigned or is missing! Check the Dropdown GameObject in the Inspector.", nodeDropdown.gameObject);
    //      return;
    // }

    // --- Build Options (Code Unchanged) ---
    List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
    options.Add(new TMP_Dropdown.OptionData("Select Node..."));
    var sortedDefinitions = definitionLibrary.definitions
                                .Where(def => def != null)
                                .OrderBy(def => def.displayName)
                                .ToList();
    foreach (var def in sortedDefinitions)
    {
        TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
        option.text = def.displayName;
        option.image = def.thumbnail;
        options.Add(option);
    }
    nodeDropdown.ClearOptions();
    nodeDropdown.AddOptions(options);

    // --- Setup Listener (Code Unchanged) ---
    nodeDropdown.onValueChanged.RemoveAllListeners();
    nodeDropdown.onValueChanged.AddListener((selectedIndex) => {
        OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions);
    });

    // --- Position Dropdown (Code Unchanged) ---
    RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        dropdownRect.parent as RectTransform, eventData.position,
        _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
        out Vector2 localPos);
    dropdownRect.localPosition = localPos;


    // --- Activate and Show ---
    // 1. Ensure the GameObject is active BEFORE trying to Show()
    if (!nodeDropdown.gameObject.activeSelf) // Check if inactive
    {
         // Debug.Log("Activating Dropdown GameObject.");
        nodeDropdown.gameObject.SetActive(true);
    }

    // 2. Now call Show()
    try {
        // Debug.Log("Calling Dropdown.Show()");
        nodeDropdown.Show(); // Force the dropdown list to open
    } catch (System.NullReferenceException nre) {
         Debug.LogError($"[NodeEditorGridController] NRE calling nodeDropdown.Show()! Check Dropdown setup ('{nodeDropdown.gameObject.name}'). Is its 'Template' assigned?\nError: {nre.Message}\n{nre.StackTrace}", nodeDropdown.gameObject);
         // Optionally hide it again if Show() failed
         // nodeDropdown.gameObject.SetActive(false);
         return;
    }
    // --- End of Show() call ---

    // 3. Reset value AFTER showing (so it shows the placeholder)
    nodeDropdown.value = 0;
    nodeDropdown.RefreshShownValue();
}

    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefs)
    {
        // Hide dropdown immediately after a value is chosen or list is closed
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);

        if (selectedIndex > 0)
        {
            int definitionIndex = selectedIndex - 1;
            if (definitionIndex >= 0 && definitionIndex < sortedDefs.Count)
            {
                NodeDefinition selectedDef = sortedDefs[definitionIndex];
                targetCell.AssignNode(selectedDef);
                NodeCell.SelectCell(targetCell);
                RefreshGraph();
            }
        }
    }

    public void RefreshGraph()
    {
        if (nodeExecutor == null || nodeExecutor.currentGraph == null) return;

        nodeExecutor.currentGraph.nodes.Clear();
        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex))
        {
            NodeData data = cell.GetNodeData();
            if (data != null)
            {
                data.orderIndex = cell.CellIndex;
                nodeExecutor.currentGraph.nodes.Add(data);
            }
        }
    }

    public bool HandleNodeDrop(NodeDraggable draggedDraggable, NodeCell originalCell, Vector2 screenPosition)
    {
        NodeCell targetCell = FindCellAtScreenPosition(screenPosition);

        if (targetCell != null && originalCell != null)
        {
            NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
            NodeData draggedData = draggedView?.GetNodeData();

            if (draggedView == null || draggedData == null)
            {
                 draggedDraggable.ResetPosition();
                 return false;
            }

            if (targetCell == originalCell)
            {
                draggedDraggable.ResetPosition();
                NodeCell.SelectCell(targetCell);
                return true;
            }

            NodeView existingViewInTarget = targetCell.GetNodeView();
            NodeData existingDataInTarget = targetCell.GetNodeData();

            // Clear selection visually before the swap happens
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

            // Re-select the cell where the dragged node ended up
            NodeCell.SelectCell(targetCell);

            RefreshGraph();
            return true;
        }

        // Dropped outside valid cells
        draggedDraggable.ResetPosition();

        // If dropped outside, attempt to re-select the original cell
        if (originalCell != null && originalCell.HasNode()) {
           NodeCell.SelectCell(originalCell);
        } else {
            NodeCell.ClearSelection(); // Or clear if original is now empty/invalid
        }

        return false;
    }

    private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
    {
        NodeCell foundCell = null; // Keep track of found cell
        foreach (var cell in nodeCells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            bool contains = RectTransformUtility.RectangleContainsScreenPoint(
                cellRect,
                screenPosition,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera
            );

            if (contains)
            {
                // --- Add Log ---
                Debug.Log($"[FindCellAtScreenPosition] Screen Point {screenPosition} FOUND in Cell_{cell.CellIndex}. Pivot={cellRect.pivot}, Rect={cellRect.rect}");
                foundCell = cell;
                break; // Stop checking once found
            }
        }
        // Optional: Log if no cell was found
        // if(foundCell == null) Debug.Log($"[FindCellAtScreenPosition] Screen Point {screenPosition} NOT FOUND in any cell.");

        return foundCell;
    }

     #if UNITY_EDITOR
     void OnDrawGizmos()
     {
         if (!Application.isPlaying)
         {
             Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
             RectTransform rt = GetComponent<RectTransform>();
             Vector3 center = rt.position;
             if (rt == null) return; // Avoid error if RectTransform not found yet

             float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
             float startX = -(totalWidth / 2f) + (cellSize.x / 2f);

             for (int i = 0; i < emptyCellsCount; i++)
             {
                 float xOffset = startX + i * (cellSize.x + cellMargin);
                 Vector3 cellCenterWorld = center + (Vector3)(rt.rotation * new Vector3(xOffset, 0, 0) * rt.lossyScale.x); // Apply rotation and scale
                 Vector3 gizmoSize = new Vector3(cellSize.x * rt.lossyScale.x, cellSize.y * rt.lossyScale.y, 0.1f);
                 // Draw rotated cube
                 Matrix4x4 rotationMatrix = Matrix4x4.TRS(cellCenterWorld, rt.rotation, Vector3.one);
                 Gizmos.matrix = rotationMatrix;
                 Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                 Gizmos.matrix = Matrix4x4.identity; // Reset matrix
             }
         }
     }
     #endif
}
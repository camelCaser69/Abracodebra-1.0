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
    [SerializeField] private NodeDefinitionLibrary definitionLibrary; // Keep this reference
    [SerializeField] private TMP_Dropdown nodeDropdown;

    [Header("UI Toggle")]
    [SerializeField] private GameObject gridUIParent;


    private List<NodeCell> nodeCells = new List<NodeCell>();
    private RectTransform _rectTransform;
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

        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);
        if (gridUIParent == null) Debug.LogWarning("[NodeEditorGridController] Grid UI Parent not assigned. UI Toggling might not work.", gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);
        else Debug.LogWarning("[NodeEditorGridController] Node Dropdown not assigned.", gameObject);
        if (definitionLibrary == null) Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned!", gameObject);

        CreateCells();
        SpawnInitialNodes(); // <<< CALL NEW METHOD
        RefreshGraph(); // Refresh after potentially spawning initial nodes
    }

    private void CreateCells()
    {
        // Clear existing cells if any (e.g., during hot reload in editor)
        foreach (Transform child in transform) {
            // Avoid destroying self or essential components if they are children by mistake
            if (child.GetComponent<NodeCell>() != null) {
                Destroy(child.gameObject);
            }
        }
        nodeCells.Clear();
        NodeCell.ClearSelection(); // Ensure no selection persists

        float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
        // Calculate start based on pivot (assuming center pivot 0.5, 0.5)
        float startX = -(totalWidth / 2f) + (cellSize.x / 2f);
        // If using bottom-left pivot (0,0): float startX = 0;
        // Adjust Y based on pivot too
        float startY = 0; // Assuming center Y pivot

        for (int i = 0; i < emptyCellsCount; i++)
        {
            GameObject cellGO = new GameObject($"Cell_{i}");
            RectTransform rt = cellGO.AddComponent<RectTransform>();
            cellGO.transform.SetParent(transform, false);

            // Set anchors and pivot (e.g., center)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = cellSize; // Set size

            // Calculate position
            float xPos = startX + i * (cellSize.x + cellMargin);
            float yPos = startY;
            rt.anchoredPosition = new Vector2(xPos, yPos);

            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true; // Important for drop detection
            // rt.localScale = emptyCellScale; // Apply scale if needed

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage);
            nodeCells.Add(cellLogic);
        }
    }

    /// <summary>
    /// Spawns nodes defined in the NodeDefinitionLibrary's initialNodes list.
    /// </summary>
    private void SpawnInitialNodes() // <<< NEW METHOD
    {
        if (definitionLibrary == null || definitionLibrary.initialNodes == null)
        {
            // Debug.Log("No initial nodes defined in the library.");
            return;
        }

        foreach (var config in definitionLibrary.initialNodes)
        {
            // Validate Node Definition
            if (config.nodeDefinition == null)
            {
                Debug.LogWarning($"Initial node config has null NodeDefinition. Skipping.");
                continue;
            }

            // Validate Cell Index
            if (config.cellIndex < 0 || config.cellIndex >= nodeCells.Count)
            {
                Debug.LogWarning($"Initial node config for '{config.nodeDefinition.name}' has invalid cell index ({config.cellIndex}). Max index is {nodeCells.Count - 1}. Skipping.");
                continue;
            }

            // Get Target Cell
            NodeCell targetCell = nodeCells[config.cellIndex];
            if (targetCell.HasNode())
            {
                Debug.LogWarning($"Initial node config for '{config.nodeDefinition.name}' targets cell {config.cellIndex}, but it's already occupied. Skipping.");
                continue;
            }

            // Spawn the Node
            // Debug.Log($"Spawning initial node '{config.nodeDefinition.name}' in cell {config.cellIndex}. Move:{config.canMove}, Delete:{config.canDelete}");
            targetCell.AssignNode(config.nodeDefinition);

            // Apply Move/Delete settings
            NodeView spawnedView = targetCell.GetNodeView();
            if (spawnedView != null)
            {
                // Control Movability
                NodeDraggable draggable = spawnedView.GetComponent<NodeDraggable>();
                if (draggable != null)
                {
                    draggable.enabled = config.canMove;
                } else if (config.canMove) {
                     Debug.LogWarning($"Initial node '{config.nodeDefinition.name}' in cell {config.cellIndex} is set to 'canMove=true' but its prefab is missing the NodeDraggable component.", spawnedView.gameObject);
                }


                // Control Deletability
                NodeData spawnedData = targetCell.GetNodeData();
                if (spawnedData != null)
                {
                    spawnedData.canBeDeleted = config.canDelete;
                }
            }
        }
    }


    void Update()
    {
        // UI Toggle
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleGridUI();

        // --- Delete Node Handling (Modified) ---
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NodeCell.CurrentlySelectedCell != null)
            {
                NodeCell selected = NodeCell.CurrentlySelectedCell;
                NodeData data = selected.GetNodeData(); // Get the node data instance

                // Check if the node exists AND if its 'canBeDeleted' flag is true
                if (data != null && data.canBeDeleted) // <<< CHECK ADDED
                {
                    // Debug.Log($"Deleting node '{data.nodeDisplayName}' from cell {selected.CellIndex}.");
                    selected.RemoveNode(); // RemoveNode handles deselection
                    RefreshGraph(); // Update internal graph representation
                }
                else if (data != null && !data.canBeDeleted)
                {
                    // Optional: Give visual/audio feedback that deletion is blocked
                     Debug.Log($"Node '{data.nodeDisplayName}' cannot be deleted.");
                    // Example: Play a short "error" sound or flash the node briefly
                }
                // If data is null (empty cell selected), do nothing
            }
        }

        // Dropdown Escape Handling
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf)
            {
                HideDropdown();
            }
             // Also deselect node if Escape is pressed and dropdown isn't active?
             else if (NodeCell.CurrentlySelectedCell != null) {
                 NodeCell.ClearSelection();
             }
        }
    }

    // --- ToggleGridUI --- (Keep existing method)
    public void ToggleGridUI()
    {
        if (gridUIParent != null) {
            bool currentState = gridUIParent.activeSelf;
            gridUIParent.SetActive(!currentState);
            if (!gridUIParent.activeSelf) { // If hiding UI
                 if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown();
                 NodeCell.ClearSelection(); // Deselect nodes when UI hides
            }
        }
        else Debug.LogWarning("[NodeEditorGridController] Grid UI Parent not assigned.");
    }

    // --- OnEmptyCellRightClicked --- (Keep existing method)
    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (nodeDropdown == null) { Debug.LogError("[NodeEditorGridController] Node Dropdown not assigned."); return; }
        if (definitionLibrary == null || definitionLibrary.definitions == null) { Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned or has no definitions."); return; }

        // Stop potentially existing dropdown coroutine to prevent conflicts
        StopCoroutine("ShowDropdownCoroutine");
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

    // --- ShowDropdownCoroutine --- (Keep existing method)
     private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
     {
         // Build Options
         List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
         options.Add(new TMP_Dropdown.OptionData("Select Node...")); // Placeholder option
         // Sort definitions alphabetically for the dropdown
         var sortedDefinitions = definitionLibrary.definitions
                                     .Where(def => def != null) // Filter out null entries
                                     .OrderBy(def => def.displayName)
                                     .ToList();
         foreach (var def in sortedDefinitions) {
             TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
             option.text = def.displayName;
             option.image = def.thumbnail; // Use thumbnail as icon
             options.Add(option);
         }
         nodeDropdown.ClearOptions();
         nodeDropdown.AddOptions(options);

         // Setup Listener (Use lambda to capture cell and sorted list)
         nodeDropdown.onValueChanged.RemoveAllListeners(); // Clear previous listeners
         nodeDropdown.onValueChanged.AddListener((selectedIndex) => {
             OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions); // Pass necessary context
         });

         // Position the Dropdown near the click position
         RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
         RectTransformUtility.ScreenPointToLocalPointInRectangle(
             dropdownRect.parent as RectTransform, // Parent RectTransform
             eventData.position,                   // Screen click position
             _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, // Camera for non-overlay canvas
             out Vector2 localPos);
         dropdownRect.localPosition = localPos;

         // Activate Dropdown GameObject if not already active
         if (!nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(true);

         // Wait one frame to allow UI layout to update before showing
         yield return null;

         // Show the dropdown options list
         try {
             // Ensure template is assigned before showing
             if (nodeDropdown.template == null) {
                 Debug.LogError("Dropdown template is not assigned in the Inspector!", nodeDropdown.gameObject);
                 HideDropdown();
                 yield break;
             }
             nodeDropdown.Show();
         } catch (System.NullReferenceException nre) {
             // Catch potential NRE if something internal goes wrong
              Debug.LogError($"Error showing dropdown: {nre.Message}", nodeDropdown.gameObject);
             HideDropdown();
             yield break;
         }

         // Set initial value and refresh display
         nodeDropdown.value = 0; // Select the "Select Node..." placeholder
         nodeDropdown.RefreshShownValue();
     }

    // --- OnDropdownValueChanged --- (Keep existing method)
     private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefs)
     {
         HideDropdown(); // Hide dropdown immediately after selection

         // Index 0 is the placeholder "Select Node..."
         if (selectedIndex > 0) {
             // Adjust index to match the sortedDefs list (since dropdown index 1 corresponds to list index 0)
             int definitionIndex = selectedIndex - 1;
             if (definitionIndex >= 0 && definitionIndex < sortedDefs.Count) {
                 NodeDefinition selectedDef = sortedDefs[definitionIndex];
                 if (selectedDef != null) {
                      // Debug.Log($"Assigning node '{selectedDef.name}' to cell {targetCell.CellIndex}");
                     targetCell.AssignNode(selectedDef); // Assign the chosen node
                     NodeCell.SelectCell(targetCell);   // Select the newly placed node
                     RefreshGraph();                    // Update the internal graph representation
                 }
             } else {
                  Debug.LogError($"Dropdown selection index ({selectedIndex}) resulted in an out-of-bounds index ({definitionIndex}) for the definition list.");
             }
         }
         // If selectedIndex is 0, do nothing (user clicked the placeholder)
     }

    // --- HideDropdown --- (Keep existing method)
    public void HideDropdown()
    {
        if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf)
        {
            nodeDropdown.Hide(); // Hide the options list
            nodeDropdown.gameObject.SetActive(false); // Deactivate the main dropdown object
        }
    }

    // --- RefreshGraph --- (Keep existing method)
    public void RefreshGraph()
    {
        if (_uiGraphRepresentation == null) _uiGraphRepresentation = new NodeGraph();
        _uiGraphRepresentation.nodes.Clear();
        // Ensure cells are ordered correctly by index when building the graph
        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex))
        {
            NodeData data = cell.GetNodeData();
            if (data != null)
            {
                // Update orderIndex just in case it changed (though it shouldn't if not moved)
                data.orderIndex = cell.CellIndex;
                _uiGraphRepresentation.nodes.Add(data);
            }
        }
        // Optional: Add debug log to see the refreshed graph node count
        // Debug.Log($"UI Graph Refreshed. Node Count: {_uiGraphRepresentation.nodes.Count}");
    }

    // --- HandleNodeDrop --- (Keep existing method)
     public bool HandleNodeDrop(NodeDraggable draggedDraggable, NodeCell originalCell, Vector2 screenPosition)
     {
         NodeCell targetCell = FindCellAtScreenPosition(screenPosition);
         bool changed = false;

         if (targetCell != null && originalCell != null)
         {
             // Get data from the node being dragged
             NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
             NodeData draggedData = draggedView?.GetNodeData();

             // Ensure we have valid data to move
             if (draggedView == null || draggedData == null) {
                 Debug.LogError("Dragged object missing NodeView or NodeData!", draggedDraggable.gameObject);
                 draggedDraggable.ResetPosition(); // Reset if invalid
                 return false;
             }

             // Case 1: Dropped back onto the original cell
             if (targetCell == originalCell) {
                 draggedDraggable.ResetPosition(); // Snap back cleanly
                 NodeCell.SelectCell(targetCell); // Reselect it
                 return false; // No change in layout
             }

             // --- Proceed with swap/move ---
             NodeView existingViewInTarget = targetCell.GetNodeView();
             NodeData existingDataInTarget = targetCell.GetNodeData();

             // Clear selection during swap
             NodeCell.ClearSelection();

             // 1. Clear the original cell's reference to the node being moved
             originalCell.ClearNodeReference();

             // 2. If the target cell had a node, move it to the original cell
             if (existingViewInTarget != null && existingDataInTarget != null) {
                 NodeDraggable existingDraggable = existingViewInTarget.GetComponent<NodeDraggable>();
                 // Assign the existing node to the original cell
                 originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget);
                 // Snap its visual representation
                 if (existingDraggable != null) existingDraggable.SnapToCell(originalCell);
             }

             // 3. Assign the dragged node to the target cell
             targetCell.AssignNodeView(draggedView, draggedData);
             draggedDraggable.SnapToCell(targetCell); // Snap its visual representation

             // 4. Select the cell where the node was dropped
             NodeCell.SelectCell(targetCell);
             changed = true;
         }
         else // Dropped outside grid or invalid original cell
         {
             // Reset the dragged node to its original position
             draggedDraggable.ResetPosition();
             // Reselect the original cell if it still has the node (it should after ResetPosition)
             if (originalCell != null && originalCell.HasNode()) {
                 NodeCell.SelectCell(originalCell);
             } else {
                 NodeCell.ClearSelection(); // Should not happen often here
             }
         }

         // If the layout changed, refresh the internal graph representation
         if (changed) RefreshGraph();

         return changed;
     }

    // --- FindCellAtScreenPosition --- (Keep existing method)
     private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
     {
         NodeCell foundCell = null;
         // Check all cells
         foreach (var cell in nodeCells)
         {
             RectTransform cellRect = cell.GetComponent<RectTransform>();
             // Use RectTransformUtility for accurate checks across different canvas modes
             bool contains = RectTransformUtility.RectangleContainsScreenPoint(
                 cellRect,
                 screenPosition,
                 _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera
             );
             if (contains) {
                 foundCell = cell;
                 break; // Found the cell under the cursor
             }
         }
         return foundCell;
     }

    // --- OnDrawGizmos --- (Keep existing method)
     #if UNITY_EDITOR
     void OnDrawGizmos()
     {
         // Draw gizmos only in the editor and when not playing
         if (!Application.isPlaying && TryGetComponent<RectTransform>(out var rt))
         {
             Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Semi-transparent green

             float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
             // Adjust start position based on pivot (assuming 0.5, 0.5)
             float startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f);
             float startY_for_gizmo = 0; // Assuming center Y pivot

             // Store original matrix
             Matrix4x4 originalMatrix = Gizmos.matrix;

             for (int i = 0; i < emptyCellsCount; i++)
             {
                 // Calculate center position of the cell in local space relative to pivot
                 float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                 Vector3 localCellCenter = new Vector3(xOffset, startY_for_gizmo, 0);

                 // Transform local center to world space, considering rotation and scale
                 Vector3 worldCellCenter = rt.TransformPoint(localCellCenter);

                 // Calculate gizmo size based on RectTransform's lossy scale
                 Vector3 gizmoSize = new Vector3(cellSize.x * rt.lossyScale.x, cellSize.y * rt.lossyScale.y, 0.1f);

                 // Set Gizmos matrix to handle rotation
                 Gizmos.matrix = Matrix4x4.TRS(worldCellCenter, rt.rotation, Vector3.one);

                 // Draw wire cube centered at the transformed position (local origin within the matrix)
                 Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
             }
             // Restore original Gizmos matrix
             Gizmos.matrix = originalMatrix;
         }
     }
     #endif
}
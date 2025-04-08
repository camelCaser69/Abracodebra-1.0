using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class NodeEditorGridController : MonoBehaviour // Keep this script on the persistent "UIManager" or similar object
{
    public static NodeEditorGridController Instance { get; private set; }

    [Header("Grid Layout & Appearance")]
    [SerializeField][Min(1)] private int emptyCellsCount = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Empty Cell Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    [SerializeField] private Vector3 emptyCellScale = Vector3.one; // Still potentially useful for cell GO scale

    [Header("Node Visuals")]
    [SerializeField] private GameObject nodeViewPrefab;
    [SerializeField] private Vector3 nodeImageScale = Vector3.one; // For the image *inside* the node view
    [SerializeField] private Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Header("Node Definitions & Interaction")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;
    [SerializeField] private TMP_Dropdown nodeDropdown;

    [Header("UI References")]
    [Tooltip("The UI GameObject (Panel) that gets toggled visible/hidden.")]
    [SerializeField] private GameObject gridUIParent; // The Panel to show/hide
    [Tooltip("The Transform within the UI Panel where cell GameObjects should be created.")]
    [SerializeField] private Transform cellContainer; // The container for cell GOs (e.g., 'GridContainer')


    private List<NodeCell> nodeCells = new List<NodeCell>();
    // Removed _rectTransform as it's less relevant now the script isn't on the grid panel itself
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

        // Get components relative to this script's GameObject
        _rootCanvas = GetComponentInParent<Canvas>(); // Find the root canvas
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);

        // Validate essential references
        if (gridUIParent == null) Debug.LogError("[NodeEditorGridController] Grid UI Parent (the panel to toggle) not assigned.", gameObject);
        if (cellContainer == null) Debug.LogError("[NodeEditorGridController] Cell Container (parent for cells) not assigned.", gameObject);
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

        // Check if references are set before proceeding
        if (cellContainer != null && definitionLibrary != null)
        {
            CreateCells();
            SpawnInitialNodes(); // Call after cells are created
            RefreshGraph(); // Refresh after potentially spawning initial nodes
        }
        else
        {
            Debug.LogError("[NodeEditorGridController] Cannot initialize grid - Cell Container or Definition Library is missing.", gameObject);
        }
    }

    private void CreateCells()
    {
        // Ensure cellContainer is assigned
        if (cellContainer == null)
        {
            Debug.LogError("[NodeEditorGridController] Cannot create cells - Cell Container is not assigned.", gameObject);
            return;
        }

        // Clear existing cells from the container
        foreach (Transform child in cellContainer) // Iterate through the actual container
        {
            if (child.GetComponent<NodeCell>() != null)
            {
                Destroy(child.gameObject);
            }
        }
        nodeCells.Clear();
        NodeCell.ClearSelection();

        // Layout calculations (assuming cellContainer uses appropriate layout components or manual positioning)
        // If using GridLayoutGroup on cellContainer, positioning is automatic.
        // If positioning manually based on cellContainer's RectTransform:
        RectTransform containerRect = cellContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
             Debug.LogError("[NodeEditorGridController] Cell Container needs a RectTransform for manual layout calculations.", cellContainer.gameObject);
             return; // Cannot proceed with manual layout
        }

        // Example Manual Layout (adjust based on containerRect's pivot/anchors):
        // Assuming center pivot (0.5, 0.5) for the container
        float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
        float startX = -(totalWidth / 2f) + (cellSize.x / 2f);
        float startY = 0; // Assuming horizontal layout centered vertically

        for (int i = 0; i < emptyCellsCount; i++)
        {
            GameObject cellGO = new GameObject($"Cell_{i}");
            RectTransform rt = cellGO.AddComponent<RectTransform>();

            // *** Parent to the designated cellContainer ***
            cellGO.transform.SetParent(cellContainer, false);

            // --- Manual Positioning --- (Comment out if using GridLayoutGroup)
            // Set anchors and pivot (e.g., center) for the cell itself
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = cellSize;

            // Calculate position relative to container
            float xPos = startX + i * (cellSize.x + cellMargin);
            float yPos = startY;
            rt.anchoredPosition = new Vector2(xPos, yPos);
            // --- End Manual Positioning ---

            // Apply scale if needed
            rt.localScale = emptyCellScale;

            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage);
            nodeCells.Add(cellLogic);

            // If using GridLayoutGroup on cellContainer, you might not need manual positioning.
            // The GridLayoutGroup component handles size and spacing. Just ensure cellGO has a LayoutElement if needed.
            // Example (if using GridLayout):
            // LayoutElement le = cellGO.GetComponent<LayoutElement>() ?? cellGO.AddComponent<LayoutElement>();
            // le.preferredWidth = cellSize.x;
            // le.preferredHeight = cellSize.y;
        }
    }

    private void SpawnInitialNodes()
    {
        if (definitionLibrary == null || definitionLibrary.initialNodes == null)
        {
            return;
        }
        if (nodeCells.Count == 0)
        {
             Debug.LogWarning("[NodeEditorGridController] Cannot spawn initial nodes - cells haven't been created yet (check for earlier errors).");
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

            targetCell.AssignNode(config.nodeDefinition);

            NodeView spawnedView = targetCell.GetNodeView();
            if (spawnedView != null)
            {
                NodeDraggable draggable = spawnedView.GetComponent<NodeDraggable>();
                if (draggable != null)
                {
                    draggable.enabled = config.canMove;
                } else if (config.canMove) {
                     Debug.LogWarning($"Initial node '{config.nodeDefinition.name}' in cell {config.cellIndex} is set to 'canMove=true' but its prefab is missing the NodeDraggable component.", spawnedView.gameObject);
                }

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
        if (Input.GetKeyDown(KeyCode.Tab))
        {
             ToggleGridUI(); // This should now work correctly
        }

        // Delete Node Handling
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NodeCell.CurrentlySelectedCell != null)
            {
                NodeCell selected = NodeCell.CurrentlySelectedCell;
                NodeData data = selected.GetNodeData();

                if (data != null && data.canBeDeleted)
                {
                    selected.RemoveNode();
                    RefreshGraph();
                }
                else if (data != null && !data.canBeDeleted)
                {
                     Debug.Log($"Node '{data.nodeDisplayName}' cannot be deleted.");
                }
            }
        }

        // Dropdown Escape Handling
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf)
            {
                HideDropdown();
            }
             else if (NodeCell.CurrentlySelectedCell != null) {
                 NodeCell.ClearSelection();
             }
        }
    }

    public void ToggleGridUI()
    {
        if (gridUIParent != null)
        {
            bool currentState = gridUIParent.activeSelf;
            gridUIParent.SetActive(!currentState); // This toggles the assigned panel

            if (!gridUIParent.activeSelf) // If hiding UI
            {
                 if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown();
                 NodeCell.ClearSelection(); // Deselect nodes when UI hides
            }
        }
        else Debug.LogWarning("[NodeEditorGridController] Grid UI Parent not assigned.");
    }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (nodeDropdown == null) { Debug.LogError("[NodeEditorGridController] Node Dropdown not assigned."); return; }
        if (definitionLibrary == null || definitionLibrary.definitions == null) { Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned or has no definitions."); return; }

        StopCoroutine("ShowDropdownCoroutine");
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

     private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
     {
         List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
         options.Add(new TMP_Dropdown.OptionData("Select Node..."));
         var sortedDefinitions = definitionLibrary.definitions
                                     .Where(def => def != null)
                                     .OrderBy(def => def.displayName)
                                     .ToList();
         foreach (var def in sortedDefinitions) {
             TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
             option.text = def.displayName;
             option.image = def.thumbnail;
             options.Add(option);
         }
         nodeDropdown.ClearOptions();
         nodeDropdown.AddOptions(options);

         nodeDropdown.onValueChanged.RemoveAllListeners();
         nodeDropdown.onValueChanged.AddListener((selectedIndex) => {
             OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions);
         });

         RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
         RectTransformUtility.ScreenPointToLocalPointInRectangle(
             dropdownRect.parent as RectTransform,
             eventData.position,
             _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
             out Vector2 localPos);
         dropdownRect.localPosition = localPos;

         if (!nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(true);

         yield return null;

         try {
             if (nodeDropdown.template == null) {
                 Debug.LogError("Dropdown template is not assigned in the Inspector!", nodeDropdown.gameObject);
                 HideDropdown();
                 yield break;
             }
             nodeDropdown.Show();
         } catch (System.NullReferenceException nre) {
              Debug.LogError($"Error showing dropdown: {nre.Message}", nodeDropdown.gameObject);
             HideDropdown();
             yield break;
         }

         nodeDropdown.value = 0;
         nodeDropdown.RefreshShownValue();
     }

     private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefs)
     {
         HideDropdown();

         if (selectedIndex > 0) {
             int definitionIndex = selectedIndex - 1;
             if (definitionIndex >= 0 && definitionIndex < sortedDefs.Count) {
                 NodeDefinition selectedDef = sortedDefs[definitionIndex];
                 if (selectedDef != null) {
                     targetCell.AssignNode(selectedDef);
                     NodeCell.SelectCell(targetCell);
                     RefreshGraph();
                 }
             } else {
                  Debug.LogError($"Dropdown selection index ({selectedIndex}) resulted in an out-of-bounds index ({definitionIndex}) for the definition list.");
             }
         }
     }

    public void HideDropdown()
    {
        if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf)
        {
            nodeDropdown.Hide();
            nodeDropdown.gameObject.SetActive(false);
        }
    }

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

             if (draggedView == null || draggedData == null) {
                 Debug.LogError("Dragged object missing NodeView or NodeData!", draggedDraggable.gameObject);
                 draggedDraggable.ResetPosition();
                 return false;
             }

             if (targetCell == originalCell) {
                 draggedDraggable.ResetPosition();
                 NodeCell.SelectCell(targetCell);
                 return false;
             }

             NodeView existingViewInTarget = targetCell.GetNodeView();
             NodeData existingDataInTarget = targetCell.GetNodeData();

             NodeCell.ClearSelection();

             originalCell.ClearNodeReference();

             if (existingViewInTarget != null && existingDataInTarget != null) {
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
             if (originalCell != null && originalCell.HasNode()) {
                 NodeCell.SelectCell(originalCell);
             } else {
                 NodeCell.ClearSelection();
             }
         }

         if (changed) RefreshGraph();

         return changed;
     }

     private NodeCell FindCellAtScreenPosition(Vector2 screenPosition)
     {
         NodeCell foundCell = null;
         if (cellContainer == null) return null; // Cannot find cells if container is missing

         // Check all NodeCell components *within the specified container*
         foreach (Transform cellTransform in cellContainer)
         {
             NodeCell cell = cellTransform.GetComponent<NodeCell>();
             if (cell == null) continue; // Skip if child isn't a NodeCell

             RectTransform cellRect = cell.GetComponent<RectTransform>();
             if (cellRect == null) continue; // Skip if cell doesn't have a RectTransform

             bool contains = RectTransformUtility.RectangleContainsScreenPoint(
                 cellRect,
                 screenPosition,
                 _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera
             );
             if (contains) {
                 foundCell = cell;
                 break;
             }
         }
         return foundCell;
     }

     #if UNITY_EDITOR
     void OnDrawGizmos()
     {
         // Draw gizmos only in the editor and when not playing
         // Check if cellContainer is assigned before drawing
         if (!Application.isPlaying && cellContainer != null && cellContainer.TryGetComponent<RectTransform>(out var containerRect))
         {
             Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Semi-transparent green

             // Use the same layout logic as CreateCells (example for manual layout)
             float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
             float startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f); // Relative to container center pivot
             float startY_for_gizmo = 0;

             // Store original matrix
             Matrix4x4 originalMatrix = Gizmos.matrix;

             for (int i = 0; i < emptyCellsCount; i++)
             {
                 // Calculate center position of the cell in container's local space
                 float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                 Vector3 localCellCenter = new Vector3(xOffset, startY_for_gizmo, 0);

                 // Transform local center to world space using the CELL CONTAINER's transform
                 Vector3 worldCellCenter = cellContainer.TransformPoint(localCellCenter);

                 // Calculate gizmo size based on CELL CONTAINER's lossy scale
                 Vector3 gizmoSize = new Vector3(cellSize.x * cellContainer.lossyScale.x, cellSize.y * cellContainer.lossyScale.y, 0.1f);

                 // Set Gizmos matrix to handle CELL CONTAINER's rotation
                 Gizmos.matrix = Matrix4x4.TRS(worldCellCenter, cellContainer.rotation, Vector3.one);

                 // Draw wire cube centered at the transformed position
                 Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
             }
             // Restore original Gizmos matrix
             Gizmos.matrix = originalMatrix;
         }
         // Optional: Draw a box around the container itself
         // else if (!Application.isPlaying && cellContainer != null && cellContainer.TryGetComponent<RectTransform>(out containerRect)) {
         //     Gizmos.color = Color.yellow;
         //     // Simplified world space box based on container rect
         //     Vector3[] corners = new Vector3[4];
         //     containerRect.GetWorldCorners(corners);
         //     Vector3 center = (corners[0] + corners[2]) * 0.5f;
         //     Vector3 size = new Vector3(Mathf.Abs(corners[0].x - corners[2].x), Mathf.Abs(corners[0].y - corners[2].y), 0.1f);
         //     Gizmos.matrix = Matrix4x4.TRS(center, containerRect.rotation, Vector3.one);
         //     Gizmos.DrawWireCube(Vector3.zero, size);
         //     Gizmos.matrix = Matrix4x4.identity;
         // }
     }
     #endif
}
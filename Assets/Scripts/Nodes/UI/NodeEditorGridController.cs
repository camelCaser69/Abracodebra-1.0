// FILE: Assets\Scripts\Nodes\UI\NodeEditorGridController.cs
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

    [Header("UI References")]
    [Tooltip("The UI GameObject (Panel) that acts as the main container for the Node Editor. This will be controlled by UIManager.")] // MODIFIED TOOLTIP
    [SerializeField] public GameObject gridUIParent; // MADE PUBLIC FOR UIManager assignment. This is the panel to show/hide.
    [Tooltip("The Transform within the UI Panel where cell GameObjects should be created.")]
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

        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);

        if (gridUIParent == null) Debug.LogError("[NodeEditorGridController] Grid UI Parent (the panel to toggle) not assigned. UIManager will not be able to control its visibility.", gameObject);
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

        if (cellContainer != null && definitionLibrary != null)
        {
            CreateCells();
            SpawnInitialNodes();
            RefreshGraph();
        }
        else
        {
            Debug.LogError("[NodeEditorGridController] Cannot initialize grid - Cell Container or Definition Library is missing.", gameObject);
        }

        // IMPORTANT: Ensure the gridUIParent is initially hidden if UIManager will control it from a hidden state.
        // Or, UIManager's first HandleRunStateChanged will correctly set its visibility.
        // For safety, if gridUIParent is directly assigned to planningPanel, UIManager will handle it.
    }

    private void CreateCells()
    {
        if (cellContainer == null)
        {
            Debug.LogError("[NodeEditorGridController] Cannot create cells - Cell Container is not assigned.", gameObject);
            return;
        }

        foreach (Transform child in cellContainer)
        {
            if (child.GetComponent<NodeCell>() != null)
            {
                Destroy(child.gameObject);
            }
        }
        nodeCells.Clear();
        NodeCell.ClearSelection();

        RectTransform containerRect = cellContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
             Debug.LogError("[NodeEditorGridController] Cell Container needs a RectTransform for manual layout calculations.", cellContainer.gameObject);
             return;
        }

        float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
        float startX = -(totalWidth / 2f) + (cellSize.x / 2f);
        float startY = 0;

        for (int i = 0; i < emptyCellsCount; i++)
        {
            GameObject cellGO = new GameObject($"Cell_{i}");
            RectTransform rt = cellGO.AddComponent<RectTransform>();
            cellGO.transform.SetParent(cellContainer, false);

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = cellSize;
            float xPos = startX + i * (cellSize.x + cellMargin);
            float yPos = startY;
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.localScale = emptyCellScale;

            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage);
            nodeCells.Add(cellLogic);
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
        // REMOVED: Tab key logic for ToggleGridUI()

        // Delete Node Handling
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NodeCell.CurrentlySelectedCell != null && gridUIParent != null && gridUIParent.activeInHierarchy) // ADDED: Check if UI is active
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
             else if (NodeCell.CurrentlySelectedCell != null && gridUIParent != null && gridUIParent.activeInHierarchy) { // ADDED: Check if UI is active
                 NodeCell.ClearSelection();
             }
        }
    }

    // MODIFIED: This method might no longer be needed if UIManager directly controls gridUIParent.
    // If UIManager needs to call this, it can be kept. Otherwise, it can be removed.
    // For now, let's assume UIManager sets gridUIParent.SetActive() directly.
    // public void ToggleGridUI()
    // {
    //     if (gridUIParent != null)
    //     {
    //         bool currentState = gridUIParent.activeSelf;
    //         gridUIParent.SetActive(!currentState);

    //         if (!gridUIParent.activeSelf)
    //         {
    //              if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown();
    //              NodeCell.ClearSelection();
    //         }
    //     }
    //     else Debug.LogWarning("[NodeEditorGridController] Grid UI Parent not assigned.");
    // }

    // Public methods to explicitly show/hide, callable by UIManager if needed,
    // though direct panel activation by UIManager is simpler.
    public void ShowNodeEditor()
    {
        if (gridUIParent != null)
        {
            gridUIParent.SetActive(true);
        }
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
         if (cellContainer == null) return null;

         foreach (Transform cellTransform in cellContainer)
         {
             NodeCell cell = cellTransform.GetComponent<NodeCell>();
             if (cell == null) continue;

             RectTransform cellRect = cell.GetComponent<RectTransform>();
             if (cellRect == null) continue;

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
         if (!Application.isPlaying && cellContainer != null && cellContainer.TryGetComponent<RectTransform>(out var containerRect))
         {
             Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
             float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
             float startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f);
             float startY_for_gizmo = 0;
             Matrix4x4 originalMatrix = Gizmos.matrix;
             for (int i = 0; i < emptyCellsCount; i++)
             {
                 float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                 Vector3 localCellCenter = new Vector3(xOffset, startY_for_gizmo, 0);
                 Vector3 worldCellCenter = cellContainer.TransformPoint(localCellCenter);
                 Vector3 gizmoSize = new Vector3(cellSize.x * cellContainer.lossyScale.x, cellSize.y * cellContainer.lossyScale.y, 0.1f);
                 Gizmos.matrix = Matrix4x4.TRS(worldCellCenter, cellContainer.rotation, Vector3.one);
                 Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
             }
             Gizmos.matrix = originalMatrix;
         }
     }
     #endif
}
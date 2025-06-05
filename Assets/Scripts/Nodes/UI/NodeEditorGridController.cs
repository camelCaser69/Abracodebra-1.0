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

    [Header("Grid Layout & Appearance (Sequence Editor)")]
    [SerializeField][Min(1)] private int emptyCellsCount = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f); // Also used for seed slot
    [SerializeField] private float cellMargin = 10f; // Also used for seed slot if its container has layout

    [Header("Empty Cell Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    [SerializeField] private Vector3 emptyCellScale = Vector3.one;

    [Header("Node Visuals")]
    [SerializeField] private GameObject nodeViewPrefab;
    [SerializeField] private Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Header("Node Definitions & Interaction")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;
    [SerializeField] private TMP_Dropdown nodeDropdown;

    [Header("UI References")]
    [Tooltip("The UI GameObject (Panel) that acts as the main container for the Node Editor sequence. This will be controlled by UIManager.")]
    [SerializeField] public GameObject gridUIParent;
    [Tooltip("The Transform within gridUIParent where sequence cell GameObjects should be created.")]
    [SerializeField] private Transform cellContainer;

    [Header("Seed Slot Specifics")]
    [Tooltip("Assign the Transform that will parent the dynamically created Seed Slot cell. Ensure its layout (e.g., via Horizontal/Vertical Layout Group or manual RectTransform positioning) is correct.")]
    [SerializeField] private Transform seedSlotContainer;

    private List<NodeCell> nodeCells = new List<NodeCell>();
    private NodeCell _actualSeedSlotCell;
    private Canvas _rootCanvas;
    private NodeGraph _uiGraphRepresentation = new NodeGraph();

    public GameObject NodeViewPrefab => nodeViewPrefab;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor;
    public Color EmptyCellColor => emptyCellColor;
    public NodeGraph GetCurrentGraphInEditor() => _uiGraphRepresentation;
    public NodeData GetCurrentSeedInSlot() => _actualSeedSlotCell != null ? _actualSeedSlotCell.GetNodeData() : null;
    public NodeCell SeedSlotCell => _actualSeedSlotCell;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null) _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);

        if (gridUIParent == null) Debug.LogError("[NodeEditorGridController] Grid UI Parent (sequence editor panel) not assigned.", gameObject);
        if (cellContainer == null) Debug.LogError("[NodeEditorGridController] Cell Container (for sequence) not assigned.", gameObject);
        if (nodeDropdown == null) Debug.LogWarning("[NodeEditorGridController] Node Dropdown not assigned.", gameObject);
        if (definitionLibrary == null) Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned!", gameObject);
        
        if (seedSlotContainer == null)
        {
            Debug.LogError("[NodeEditorGridController] Seed Slot Container is not assigned in the Inspector! Seed slot will not be created.", gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);

        if (seedSlotContainer != null)
        {
            CreateSeedSlot();
        }

        if (cellContainer != null && definitionLibrary != null)
        {
            CreateSequenceCells();
            HideNodeEditorPanel();
            RefreshGraph();
        }
        else
        {
            Debug.LogError("[NodeEditorGridController] Cannot initialize sequence grid - Cell Container or Definition Library is missing.", gameObject);
        }
    }

    private void CreateSeedSlot()
    {
        if (seedSlotContainer == null) return;
        foreach (Transform child in seedSlotContainer) Destroy(child.gameObject);

        // Create GameObject with RectTransform
        GameObject seedCellGO = new GameObject("SeedSlot_Cell", typeof(RectTransform)); // <<< Ensure RectTransform
        seedCellGO.transform.SetParent(seedSlotContainer, false);

        RectTransform rt = seedCellGO.GetComponent<RectTransform>(); // This will now exist
        // If the seedSlotContainer has a Layout Group (e.g., Horizontal/Vertical),
        // the sizeDelta might be controlled by the layout group.
        // Setting it here might be overridden or might be necessary if no layout group.
        rt.sizeDelta = cellSize;
        // For manual positioning if no layout group:
        // rt.anchorMin = new Vector2(0.5f, 0.5f);
        // rt.anchorMax = new Vector2(0.5f, 0.5f);
        // rt.pivot = new Vector2(0.5f, 0.5f);
        // rt.anchoredPosition = Vector2.zero; // Center in parent, or set specific position

        Image cellImage = seedCellGO.AddComponent<Image>();
        cellImage.sprite = emptyCellSprite;
        cellImage.color = emptyCellColor;
        cellImage.raycastTarget = true;

        NodeCell cellLogic = seedCellGO.AddComponent<NodeCell>();
        cellLogic.InitAsSeedSlot(this, cellImage);
        _actualSeedSlotCell = cellLogic;

        Debug.Log("[NodeEditorGridController] Seed Slot cell created and initialized.", _actualSeedSlotCell.gameObject);
    }

    private void CreateSequenceCells()
    {
        if (cellContainer == null) return;

        foreach (Transform child in cellContainer)
        {
            NodeCell nc = child.GetComponent<NodeCell>();
            if (nc != null && !nc.IsSeedSlot) Destroy(child.gameObject);
        }
        nodeCells.Clear();
        NodeCell.ClearSelection();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogWarning("[NodeEditorGridController] Sequence Cell Container does not have a GridLayoutGroup. Manual layout may be incorrect.", cellContainer.gameObject);
            RectTransform containerRect = cellContainer.GetComponent<RectTransform>();
            if (containerRect == null) { Debug.LogError("CellContainer needs a RectTransform for manual layout!", cellContainer.gameObject); return; }
            float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
            float startX = -(totalWidth / 2f) + (cellSize.x / 2f);
            for (int i = 0; i < emptyCellsCount; i++)
            {
                GameObject cellGO = new GameObject($"SequenceCell_{i}", typeof(RectTransform)); // <<< Ensure RectTransform
                cellGO.transform.SetParent(cellContainer, false);

                RectTransform rt = cellGO.GetComponent<RectTransform>();
                rt.sizeDelta = cellSize;
                rt.localScale = emptyCellScale;
                // Manual positioning for sequence cells if no grid layout
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(startX + i * (cellSize.x + cellMargin), 0);
                
                SetupCellComponents(cellGO, i);
            }
        }
        else 
        {
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = new Vector2(cellMargin, cellMargin);
            // For GridLayoutGroup, it's better to let it control the size.
            // We might not even need to set rt.sizeDelta explicitly if LayoutElement isn't used.
            for (int i = 0; i < emptyCellsCount; i++)
            {
                // Create GameObject with RectTransform
                GameObject cellGO = new GameObject($"SequenceCell_{i}", typeof(RectTransform)); // <<< Ensure RectTransform
                cellGO.transform.SetParent(cellContainer, false);
                // Let GridLayoutGroup handle size and position.
                // We can still set scale if needed: cellGO.transform.localScale = emptyCellScale;
                SetupCellComponents(cellGO, i);
            }
        }
    }
    
    private void SetupCellComponents(GameObject cellGO, int index) // For sequence cells
    {
        Image cellImage = cellGO.AddComponent<Image>();
        cellImage.sprite = emptyCellSprite;
        cellImage.color = emptyCellColor;
        cellImage.raycastTarget = true;
        NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
        cellLogic.Init(index, this, null, cellImage);
        nodeCells.Add(cellLogic);
    }

    void Update()
    {
        if (gridUIParent != null && gridUIParent.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (NodeCell.CurrentlySelectedCell != null && 
                    !NodeCell.CurrentlySelectedCell.IsInventoryCell && 
                    NodeCell.CurrentlySelectedCell != _actualSeedSlotCell) // <<< Check against _actualSeedSlotCell
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
                else if (NodeCell.CurrentlySelectedCell != null) NodeCell.ClearSelection();
            }
        }
    }
    
    public void ShowNodeEditorPanel() { if (gridUIParent != null) gridUIParent.SetActive(true); }
    public void HideNodeEditorPanel() { if (gridUIParent != null) { gridUIParent.SetActive(false); if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown(); NodeCell.ClearSelection(); } }

    public void LoadSequenceFromSeed(NodeData seedData)
    {
        if (_actualSeedSlotCell == null) { Debug.LogError("Seed slot not initialized. Cannot load sequence."); return; }

        if (seedData == null || !seedData.IsSeed())
        {
            Debug.LogError("[NodeEditorGridController] LoadSequenceFromSeed: Invalid or non-seed data.", gameObject);
            HideNodeEditorPanel();
            return;
        }

        ClearSequenceEditorCells(); 

        if (seedData.storedSequence != null && seedData.storedSequence.nodes != null)
        {
            foreach (NodeData nodeDataInSeed in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex))
            {
                if (nodeDataInSeed == null) continue;
                if (nodeDataInSeed.orderIndex >= 0 && nodeDataInSeed.orderIndex < nodeCells.Count)
                {
                    NodeCell targetCell = nodeCells[nodeDataInSeed.orderIndex];
                    NodeDefinition def = definitionLibrary.definitions.FirstOrDefault(d => d.displayName == nodeDataInSeed.nodeDisplayName); 
                    
                    if (def != null)
                    {
                        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : this.NodeViewPrefab;
                        if (prefabToInstantiate == null) {
                            Debug.LogError($"No NodeView prefab for '{def.displayName}'.", gameObject);
                            continue;
                        }
                        GameObject nodeViewGO = Instantiate(prefabToInstantiate, targetCell.transform);
                        NodeView view = nodeViewGO.GetComponent<NodeView>();
                        if (view != null)
                        {
                            view.Initialize(nodeDataInSeed, def, this);
                            targetCell.AssignNodeView(view, nodeDataInSeed);
                            
                            NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
                            draggable.Initialize(this, null, targetCell);
                        }
                        else { Destroy(nodeViewGO); }
                    }
                    else Debug.LogWarning($"Could not find NodeDefinition for '{nodeDataInSeed.nodeDisplayName}' while loading seed.");
                }
            }
        }
        ShowNodeEditorPanel();
        RefreshGraph();
        Debug.Log($"[NodeEditorGridController] Loaded sequence from seed '{seedData.nodeDisplayName}'.");
    }

    public void UnloadSeedFromSlot()
    {
        if (_actualSeedSlotCell == null) { Debug.LogWarning("Seed slot not initialized. Cannot unload."); return; }
        
        ClearSequenceEditorCells();
        HideNodeEditorPanel();
        _uiGraphRepresentation.nodes.Clear();
        Debug.Log("[NodeEditorGridController] Unloaded seed and cleared editor.");
    }

    private void ClearSequenceEditorCells()
    {
        foreach (NodeCell cell in nodeCells)
        {
            cell.RemoveNode();
        }
        NodeCell.ClearSelection();
    }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (cell.IsInventoryCell || cell == _actualSeedSlotCell) return; // <<< Check against _actualSeedSlotCell
        if (nodeDropdown == null) { Debug.LogError("[NodeEditorGridController] Node Dropdown not assigned."); return; }
        if (definitionLibrary == null || definitionLibrary.definitions == null) { Debug.LogError("[NodeEditorGridController] Node Definition Library not assigned/empty."); return; }
        StopCoroutine("ShowDropdownCoroutine");
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

    private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Select Node...") };
        var sortedDefinitions = definitionLibrary.definitions
                                    .Where(def => def != null && !def.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn)) 
                                    .OrderBy(def => def.displayName)
                                    .ToList();
        foreach (var def in sortedDefinitions) options.Add(new TMP_Dropdown.OptionData { text = def.displayName, image = def.thumbnail });
        
        nodeDropdown.ClearOptions(); nodeDropdown.AddOptions(options);
        nodeDropdown.onValueChanged.RemoveAllListeners();
        nodeDropdown.onValueChanged.AddListener((selectedIndex) => { OnDropdownValueChanged(selectedIndex, cell, sortedDefinitions); });

        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        if (_rootCanvas != null && dropdownRect.parent is RectTransform parentRect) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, _rootCanvas.worldCamera, out Vector2 localPos);
            dropdownRect.localPosition = localPos;
        } else Debug.LogWarning("[NodeEditorGridController] Cannot accurately position dropdown.");
        
        if (!nodeDropdown.gameObject.activeSelf) nodeDropdown.gameObject.SetActive(true);
        yield return null;
        
        try { 
            if (nodeDropdown.template == null) { Debug.LogError("Node Dropdown template is null!", nodeDropdown.gameObject); HideDropdown(); yield break; }
            nodeDropdown.Show(); 
        }
        catch (System.NullReferenceException nre) { Debug.LogError($"Error showing dropdown: {nre.Message}", nodeDropdown.gameObject); HideDropdown(); yield break; }
        
        nodeDropdown.value = 0; nodeDropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefinitions)
    {
        HideDropdown();
        if (selectedIndex > 0) {
            int definitionIndexInSortedList = selectedIndex - 1;
            if (definitionIndexInSortedList >= 0 && definitionIndexInSortedList < sortedDefinitions.Count) {
                NodeDefinition selectedDef = sortedDefinitions[definitionIndexInSortedList];
                if (selectedDef != null) { 
                    targetCell.AssignNode(selectedDef); 
                    NodeCell.SelectCell(targetCell);    
                    RefreshGraph();                     
                }
            } else Debug.LogError($"Dropdown selection index {selectedIndex} invalid.");
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
        if (_actualSeedSlotCell == null) { return; }

        if (_uiGraphRepresentation == null) _uiGraphRepresentation = new NodeGraph();
        _uiGraphRepresentation.nodes.Clear();
        if (nodeCells != null) { 
            foreach (var cell in nodeCells.OrderBy(c => c.CellIndex)) {
                NodeData data = cell.GetNodeData();
                if (data != null) { data.orderIndex = cell.CellIndex; _uiGraphRepresentation.nodes.Add(data); }
            }
        }

        NodeData currentSeed = GetCurrentSeedInSlot();
        if (currentSeed != null && currentSeed.IsSeed())
        {
            currentSeed.storedSequence.nodes = new List<NodeData>(_uiGraphRepresentation.nodes);
        }
    }
    
    public NodeCell GetCellAtIndex(int index) 
    {
        if (nodeCells == null) { Debug.LogError("[NodeEditorGridController] nodeCells list is null."); return null; }
        if (index >= 0 && index < nodeCells.Count) return nodeCells[index];
        Debug.LogWarning($"[NodeEditorGridController] GetCellAtIndex: Index {index} out of bounds for sequence cells.");
        return null;
    }

    public void HandleDropOnSeedSlot(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSeedSlotCell)
    {
        if (_actualSeedSlotCell == null || targetSeedSlotCell != _actualSeedSlotCell) {
             Debug.LogError("HandleDropOnSeedSlot called on an incorrect or uninitialized cell.", targetSeedSlotCell.gameObject);
             draggedDraggable.ResetPosition();
             return;
        }

        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeData draggedData = draggedView?.GetNodeData();

        if (draggedView == null || draggedData == null || !draggedData.IsSeed())
        {
            Debug.LogWarning($"[NodeEditorGridController] Item dropped on seed slot is not a valid seed. Resetting drag.", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition();
            return;
        }
        
        NodeData existingSeedInSlotData = _actualSeedSlotCell.GetNodeData();
        if (existingSeedInSlotData != null)
        {
            NodeView existingSeedViewInSlot = _actualSeedSlotCell.GetNodeView();
            _actualSeedSlotCell.ClearNodeReference(); 
            InventoryGridController.Instance.ReturnGeneToInventory(existingSeedViewInSlot, existingSeedInSlotData);
            Debug.Log($"Returned '{existingSeedInSlotData.nodeDisplayName}' from seed slot to inventory.");
        }

        if (originalCell.IsInventoryCell)
        {
            InventoryGridController.Instance.RemoveGeneFromInventory(originalCell); 
        }
        else
        {
            Debug.LogError($"[NodeEditorGridController] A seed '{draggedData.nodeDisplayName}' was dragged from non-inventory cell ({originalCell.gameObject.name}) to seed slot. Original cell cleared.", originalCell.gameObject);
            originalCell.RemoveNode();
        }

        _actualSeedSlotCell.AssignNodeView(draggedView, draggedData);
        draggedDraggable.SnapToCell(_actualSeedSlotCell);
        
        LoadSequenceFromSeed(draggedData);
        Debug.Log($"Seed '{draggedData.nodeDisplayName}' placed in slot. Stored sequence has {draggedData.storedSequence.nodes.Count} nodes.");
    }


    public void HandleDropOnSequenceCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSequenceCell)
    {
        if (_actualSeedSlotCell == null) { Debug.LogError("Seed slot not initialized. Sequence operations may fail."); }

        if (draggedDraggable == null || originalCell == null || targetSequenceCell == null || targetSequenceCell.IsInventoryCell || targetSequenceCell == _actualSeedSlotCell)
        {
            draggedDraggable?.ResetPosition(); return;
        }

        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition();
        NodeData draggedData = draggedView?.GetNodeData();

        if (draggedView == null || (draggedDef == null && draggedData == null) ) {
            Debug.LogError("[NodeEditorGridController] HandleDropOnSequenceCell: Dragged object missing View/Def/Data.", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition(); return;
        }
        if (draggedDef != null && draggedDef.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn)) {
            Debug.LogWarning("Cannot place Seed definitions into a seed's sequence. Resetting drag.");
            draggedDraggable.ResetPosition();
            return;
        }

        if (targetSequenceCell == originalCell && !originalCell.IsInventoryCell) {
            draggedDraggable.SnapToCell(originalCell); NodeCell.SelectCell(originalCell); return;
        }

        if (originalCell.IsInventoryCell) {
            InventoryGridController.Instance?.RemoveGeneFromInventory(originalCell);
            NodeView existingViewInTargetSeq = targetSequenceCell.GetNodeView();
            if (existingViewInTargetSeq != null) {
                NodeDefinition defFromSeqTarget = existingViewInTargetSeq.GetNodeDefinition();
                NodeData dataFromSeqTarget = targetSequenceCell.GetNodeData();
                targetSequenceCell.ClearNodeReference(); 
                InventoryGridController.Instance.ReturnGeneToInventory(existingViewInTargetSeq, dataFromSeqTarget);
            }
            targetSequenceCell.AssignNode(draggedDef);
            targetSequenceCell.GetNodeView()?.GetComponent<NodeDraggable>()?.SnapToCell(targetSequenceCell);
            NodeCell.SelectCell(targetSequenceCell);
        } 
        else if (originalCell == _actualSeedSlotCell) 
        {
            Debug.LogWarning("Cannot drag the main Seed from the Seed Slot directly into its own sequence editor. Resetting drag.");
            draggedDraggable.ResetPosition();
            return;
        }
        else { // Sequence TO Sequence
            NodeView existingViewInTarget = targetSequenceCell.GetNodeView(); 
            NodeData existingDataInTarget = targetSequenceCell.GetNodeData();
            NodeCell.ClearSelection(); 
            originalCell.ClearNodeReference();

            if (existingViewInTarget != null) {
                NodeDraggable draggableFromTarget = existingViewInTarget.GetComponent<NodeDraggable>();
                originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget);
                draggableFromTarget?.SnapToCell(originalCell);
            }
            targetSequenceCell.AssignNodeView(draggedView, draggedData);
            draggedDraggable.SnapToCell(targetSequenceCell); 
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
        // Also check the seed slot
        if (foundCell == null && _actualSeedSlotCell != null)
        {
            RectTransform seedSlotRect = _actualSeedSlotCell.GetComponent<RectTransform>();
            if (seedSlotRect != null && RectTransformUtility.RectangleContainsScreenPoint(seedSlotRect, screenPosition, _rootCanvas.worldCamera))
            {
                return _actualSeedSlotCell;
            }
        }
        return foundCell;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isPlaying) return; // Only draw in editor mode when not playing
        
        // Gizmo for Sequence Cell Container
        if (cellContainer != null && cellContainer.gameObject.activeInHierarchy && cellContainer.TryGetComponent<RectTransform>(out var containerRect))
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f); 
            Matrix4x4 originalMatrix = Gizmos.matrix;
            
            GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null && gridLayout.enabled) {
                Vector3[] corners = new Vector3[4]; 
                containerRect.GetWorldCorners(corners);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawLine(corners[0], corners[1]); Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]); Gizmos.DrawLine(corners[3], corners[0]);
            } else { // Manual Layout Gizmos for sequence cells
                float totalWidth = (emptyCellsCount * cellSize.x) + ((emptyCellsCount - 1) * cellMargin);
                float startX_for_gizmo = -(totalWidth / 2f) + (cellSize.x / 2f); 
                for (int i = 0; i < emptyCellsCount; i++) {
                    float xOffset = startX_for_gizmo + i * (cellSize.x + cellMargin);
                    Vector3 localCellCenter = new Vector3(xOffset, 0, 0);
                    Vector3 worldCellCenter = cellContainer.TransformPoint(localCellCenter);
                    Vector3 gizmoSize = new Vector3(cellSize.x * cellContainer.lossyScale.x, cellSize.y * cellContainer.lossyScale.y, 0.1f);
                    Gizmos.matrix = Matrix4x4.TRS(worldCellCenter, cellContainer.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                }
            }
            Gizmos.matrix = originalMatrix;
        }


        // Gizmo for Seed Slot Container
        if (seedSlotContainer != null && seedSlotContainer.gameObject.activeInHierarchy && seedSlotContainer.TryGetComponent<RectTransform>(out var seedContainerRect))
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.35f); // Magenta for seed slot container
            Vector3[] corners = new Vector3[4];
            seedContainerRect.GetWorldCorners(corners);
            Matrix4x4 originalMatrix = Gizmos.matrix; // Store it again just in case
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(corners[0], corners[1]); Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]); Gizmos.DrawLine(corners[3], corners[0]);
            Gizmos.matrix = originalMatrix;
        }
    }
    #endif
}
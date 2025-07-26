using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;

public class NodeEditorGridController : MonoBehaviour
{
    public static NodeEditorGridController Instance { get; private set; }

    [Header("Grid & Cell Layout")]
    [SerializeField][Min(1)] private int emptyCellsCount = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Visuals & Prefabs")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    [SerializeField] private Vector3 emptyCellScale = Vector3.one;
    [Tooltip("The single, unified prefab with an ItemView component, used for all nodes in the editor.")]
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Header("Dependencies")]
    [SerializeField] private NodeDefinitionLibrary definitionLibrary;
    [SerializeField] private TMP_Dropdown nodeDropdown;
    [SerializeField] public GameObject gridUIParent;
    [SerializeField] private Transform cellContainer;
    [SerializeField] private Transform seedSlotContainer;

    private readonly List<NodeCell> nodeCells = new List<NodeCell>();
    private NodeCell _actualSeedSlotCell;
    private Canvas _rootCanvas;
    private readonly NodeGraph _currentlyEditedSequence = new NodeGraph();

    public GameObject InventoryItemPrefab => inventoryItemPrefab;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor;
    public Color EmptyCellColor => emptyCellColor;
    public NodeData GetCurrentSeedInSlot() => _actualSeedSlotCell?.GetNodeData();
    public NodeCell SeedSlotCell => _actualSeedSlotCell;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas ?? FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeEditorGridController] Root Canvas not found!", gameObject);
        if (inventoryItemPrefab == null) Debug.LogError("[NodeEditorGridController] Inventory Item Prefab is not assigned!", gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (nodeDropdown != null) nodeDropdown.gameObject.SetActive(false);
        if (seedSlotContainer != null) CreateSeedSlot();
        if (cellContainer != null && definitionLibrary != null)
        {
            CreateSequenceCells();
            HideNodeEditorPanel();
        }
    }

    // --- ADDED METHOD: Restored from original functionality ---
    // In NodeEditorGridController.cs, replace GetCurrentGraphInEditorForSpawning with this debug version:

    public NodeGraph GetCurrentGraphInEditorForSpawning() {
        NodeGraph clone = new NodeGraph { nodes = new List<NodeData>() };
    
        Debug.Log($"[NodeEditor] GetCurrentGraphInEditorForSpawning - Editor has {_currentlyEditedSequence?.nodes?.Count ?? 0} nodes");
    
        if (_currentlyEditedSequence?.nodes != null) {
            foreach (var nodeData in _currentlyEditedSequence.nodes) {
                if (nodeData == null) continue;
            
                // Debug log the original node
                Debug.Log($"[NodeEditor] Processing node '{nodeData.nodeDisplayName}' with {nodeData.effects?.Count ?? 0} effects:");
                if (nodeData.effects != null) {
                    foreach (var effect in nodeData.effects) {
                        Debug.Log($"  - {effect.effectType} (passive: {effect.isPassive}, primary: {effect.primaryValue})");
                    }
                }
            
                // Create a clean clone without any stored sequences
                NodeData clonedNode = new NodeData {
                    nodeId = nodeData.nodeId,
                    nodeDisplayName = nodeData.nodeDisplayName,
                    effects = NodeExecutor.CloneEffectsList(nodeData.effects),
                    orderIndex = nodeData.orderIndex,
                    canBeDeleted = nodeData.canBeDeleted,
                    // storedSequence is intentionally null - nodes in editor don't have sequences
                };
            
                // Debug log the cloned node
                Debug.Log($"[NodeEditor] Cloned node has {clonedNode.effects?.Count ?? 0} effects");
            
                clone.nodes.Add(clonedNode);
            }
        }
    
        Debug.Log($"[NodeEditor] Returning graph with {clone.nodes.Count} nodes");
        return clone;
    }

    private void CreateSeedSlot()
    {
        foreach (Transform child in seedSlotContainer) Destroy(child.gameObject);

        GameObject seedCellGO = new GameObject("SeedSlot_Cell", typeof(RectTransform));
        seedCellGO.transform.SetParent(seedSlotContainer, false);
        seedCellGO.GetComponent<RectTransform>().sizeDelta = cellSize;
        Image cellImage = seedCellGO.AddComponent<Image>();
        cellImage.sprite = emptyCellSprite;
        cellImage.color = emptyCellColor;
        NodeCell cellLogic = seedCellGO.AddComponent<NodeCell>();
        cellLogic.InitAsSeedSlot(this, cellImage);
        _actualSeedSlotCell = cellLogic;
    }

    private void CreateSequenceCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        nodeCells.Clear();
        NodeCell.ClearSelection();
        
        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);

        for (int i = 0; i < emptyCellsCount; i++)
        {
            GameObject cellGO = new GameObject($"SequenceCell_{i}", typeof(RectTransform));
            cellGO.transform.SetParent(cellContainer, false);
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage);
            nodeCells.Add(cellLogic);
        }
    }

    void Update()
    {
        if (gridUIParent != null && gridUIParent.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Delete) && NodeCell.CurrentlySelectedCell != null)
            {
                NodeCell selected = NodeCell.CurrentlySelectedCell;
                NodeData data = selected.GetNodeData();
                if (data != null && data.canBeDeleted)
                {
                    selected.RemoveNode();
                    RefreshGraphAndUpdateSeed();
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) HideDropdown();
                else if (NodeCell.CurrentlySelectedCell != null) NodeCell.ClearSelection();
            }
        }
    }
    
    public void LoadSequenceFromSeed(NodeData seedData) {
        if (_actualSeedSlotCell == null || seedData == null || !seedData.IsSeed()) {
            HideNodeEditorPanel();
            _currentlyEditedSequence.nodes.Clear();
            return;
        }
        
        // Ensure seed has initialized sequence
        seedData.EnsureSeedSequenceInitialized();
        
        // Clear current editor
        ClearSequenceEditorCells();
        _currentlyEditedSequence.nodes.Clear();
        
        // Load nodes from seed's stored sequence
        if (seedData.storedSequence?.nodes != null) {
            foreach (NodeData storedNode in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex)) {
                if (storedNode == null || storedNode.orderIndex >= nodeCells.Count) continue;
                
                // Find the matching definition
                NodeCell targetCell = nodeCells[storedNode.orderIndex];
                NodeDefinition definition = definitionLibrary.definitions
                    .FirstOrDefault(d => d.displayName == storedNode.nodeDisplayName);
                
                if (definition != null) {
                    // AssignNode creates a new view and node instance
                    targetCell.AssignNode(definition);
                    _currentlyEditedSequence.nodes.Add(targetCell.GetNodeData());
                }
            }
        }
        
        ShowNodeEditorPanel();
    }

    public void UnloadSeedFromSlot()
    {
        ClearSequenceEditorCells();
        HideNodeEditorPanel();
        _currentlyEditedSequence.nodes.Clear();
    }

    public void OnEmptyCellRightClicked(NodeCell cell, PointerEventData eventData)
    {
        if (cell.IsInventoryCell || cell == _actualSeedSlotCell || nodeDropdown == null) return;
        StopCoroutine(nameof(ShowDropdownCoroutine));
        StartCoroutine(ShowDropdownCoroutine(cell, eventData));
    }

    private IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
    {
        var sortedDefinitions = definitionLibrary.definitions
            .Where(def => def != null && !def.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn))
            .OrderBy(def => def.displayName)
            .ToList();
            
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Select Node...") };
        options.AddRange(sortedDefinitions.Select(def => new TMP_Dropdown.OptionData { text = def.displayName, image = def.thumbnail }));

        nodeDropdown.ClearOptions();
        nodeDropdown.AddOptions(options);
        nodeDropdown.onValueChanged.RemoveAllListeners();
        nodeDropdown.onValueChanged.AddListener((index) => OnDropdownValueChanged(index, cell, sortedDefinitions));

        RectTransformUtility.ScreenPointToLocalPointInRectangle(nodeDropdown.transform.parent as RectTransform, eventData.position, _rootCanvas.worldCamera, out Vector2 localPos);
        (nodeDropdown.transform as RectTransform).localPosition = localPos;
        nodeDropdown.gameObject.SetActive(true);
        yield return null;
        nodeDropdown.Show();
        nodeDropdown.value = 0;
        nodeDropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefinitions)
    {
        HideDropdown();
        if (selectedIndex > 0 && (selectedIndex - 1) < sortedDefinitions.Count)
        {
            NodeDefinition selectedDef = sortedDefinitions[selectedIndex - 1];
            targetCell.AssignNode(selectedDef);
            NodeCell.SelectCell(targetCell);
            RefreshGraphAndUpdateSeed();
        }
    }

     public void RefreshGraphAndUpdateSeed()
    {
        if (_actualSeedSlotCell == null) return;

        Debug.Log("[NodeEditor] RefreshGraphAndUpdateSeed - Rebuilding sequence");

        _currentlyEditedSequence.nodes.Clear();

        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex))
        {
            if (cell.HasItem())
            {
                NodeData dataFromCell = cell.GetNodeData();
                if (dataFromCell == null) continue;

                Debug.Log($"[NodeEditor] Cell {cell.CellIndex} has node '{dataFromCell.nodeDisplayName}' with {dataFromCell.effects?.Count ?? 0} effects:");
                if (dataFromCell.effects != null)
                {
                    foreach (var effect in dataFromCell.effects)
                    {
                        Debug.Log($"  - {effect.effectType} (passive: {effect.isPassive})");
                    }
                }

                dataFromCell.orderIndex = cell.CellIndex;
                dataFromCell.ClearStoredSequence();
                _currentlyEditedSequence.nodes.Add(dataFromCell);
            }
        }

        Debug.Log($"[NodeEditor] Sequence rebuilt with {_currentlyEditedSequence.nodes.Count} nodes");

        NodeData currentSeedInSlot = GetCurrentSeedInSlot();

        if (currentSeedInSlot != null && currentSeedInSlot.IsSeed())
        {
            Debug.Log($"[NodeEditor] Updating seed '{currentSeedInSlot.nodeDisplayName}' stored sequence");

            currentSeedInSlot.EnsureSeedSequenceInitialized();

            currentSeedInSlot.storedSequence.nodes.Clear();

            foreach (NodeData editorNode in _currentlyEditedSequence.nodes)
            {
                if (editorNode == null) continue;

                NodeData nodeForStorage = new NodeData
                {
                    nodeId = editorNode.nodeId,
                    definitionName = editorNode.definitionName, // THE FIX IS HERE
                    nodeDisplayName = editorNode.nodeDisplayName,
                    effects = NodeExecutor.CloneEffectsList(editorNode.effects),
                    orderIndex = editorNode.orderIndex,
                    canBeDeleted = editorNode.canBeDeleted
                };

                Debug.Log($"[NodeEditor] Storing node '{nodeForStorage.nodeDisplayName}' with def name '{nodeForStorage.definitionName}' in seed");

                nodeForStorage.SetPartOfSequence(true);

                currentSeedInSlot.storedSequence.nodes.Add(nodeForStorage);
            }

            Debug.Log($"[NodeEditor] Seed now contains {currentSeedInSlot.storedSequence.nodes.Count} nodes");
        }
    }

    public void HandleDropOnSeedSlot(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSeedSlotCell)
    {
        if (_actualSeedSlotCell == null || targetSeedSlotCell != _actualSeedSlotCell)
        {
            draggedDraggable.ResetPosition(); return;
        }

        ItemView draggedView = draggedDraggable.GetComponent<ItemView>();
        NodeData draggedData = draggedView?.GetNodeData();

        if (draggedView == null || draggedData == null || !draggedData.IsSeed())
        {
            draggedDraggable.ResetPosition(); return;
        }

        ItemView existingSeedViewInSlot = _actualSeedSlotCell.GetItemView();
        if (existingSeedViewInSlot != null)
        {
            RefreshGraphAndUpdateSeed();
            NodeData existingSeedInSlotData = _actualSeedSlotCell.GetNodeData();
            _actualSeedSlotCell.ClearNodeReference();
            InventoryGridController.Instance.ReturnGeneToInventory(existingSeedViewInSlot, existingSeedInSlotData);
        }

        if (originalCell.IsInventoryCell)
        {
            InventoryGridController.Instance.RemoveGeneFromInventory(originalCell);
        }
        else
        {
            originalCell.RemoveNode();
        }

        draggedData.EnsureSeedSequenceInitialized();
        _actualSeedSlotCell.AssignItemView(draggedView, draggedData, null);
        draggedDraggable.SnapToCell(_actualSeedSlotCell);
        LoadSequenceFromSeed(draggedData);
    }
    
    public void HandleDropOnSequenceCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSequenceCell)
    {
        if (draggedDraggable == null || originalCell == null || targetSequenceCell == null || targetSequenceCell.IsInventoryCell || targetSequenceCell.IsSeedSlot)
        {
            draggedDraggable?.ResetPosition(); return;
        }

        ItemView draggedView = draggedDraggable.GetComponent<ItemView>();
        if (draggedView == null) { draggedDraggable.ResetPosition(); return; }

        NodeDefinition draggedNodeDef = draggedView.GetNodeDefinition();
        NodeData draggedData = draggedView.GetNodeData();

        if (draggedNodeDef != null && draggedNodeDef.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn))
        {
            draggedDraggable.ResetPosition(); return;
        }

        if (originalCell.IsInventoryCell)
        {
            InventoryGridController.Instance?.RemoveGeneFromInventory(originalCell);
            ItemView existingViewInTargetSeq = targetSequenceCell.GetItemView();
            if (existingViewInTargetSeq != null)
            {
                InventoryGridController.Instance.ReturnGeneToInventory(existingViewInTargetSeq, targetSequenceCell.GetNodeData());
                targetSequenceCell.ClearNodeReference();
            }
            targetSequenceCell.AssignNode(draggedNodeDef);
            NodeCell.SelectCell(targetSequenceCell);
            Destroy(draggedDraggable.gameObject);
        }
        else if (!originalCell.IsSeedSlot)
        {
            ItemView existingViewInTarget = targetSequenceCell.GetItemView();
            NodeData existingDataInTarget = targetSequenceCell.GetNodeData();

            originalCell.ClearNodeReference();

            if (existingViewInTarget != null)
            {
                originalCell.AssignItemView(existingViewInTarget, existingDataInTarget, null);
                existingViewInTarget.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
            }
            targetSequenceCell.AssignItemView(draggedView, draggedData, null);
            draggedDraggable.SnapToCell(targetSequenceCell);
            NodeCell.SelectCell(targetSequenceCell);
        }
        else
        {
            draggedDraggable.ResetPosition();
        }

        RefreshGraphAndUpdateSeed();
    }

    private void HideNodeEditorPanel() { if (gridUIParent != null) gridUIParent.SetActive(false); }
    private void ShowNodeEditorPanel() { if (gridUIParent != null) gridUIParent.SetActive(true); }
    private void ClearSequenceEditorCells() { foreach (NodeCell cell in nodeCells) { cell.RemoveNode(); } }
    public void HideDropdown() { if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) { nodeDropdown.Hide(); nodeDropdown.gameObject.SetActive(false); } }
    public NodeCell GetCellAtIndex(int index) => (index >= 0 && index < nodeCells.Count) ? nodeCells[index] : null;
}
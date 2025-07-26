// Assets/Scripts/PlantSystem/UI/NodeEditorGridController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;

public class NodeEditorGridController : MonoBehaviour
{
    public static NodeEditorGridController Instance { get; set; }

    [Header("Grid Layout")]
    [SerializeField][Min(1)] int emptyCellsCount = 8;
    [SerializeField] Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] float cellMargin = 10f;

    [Header("Visuals")]
    [SerializeField] Sprite emptyCellSprite;
    [SerializeField] Color emptyCellColor = Color.white;
    [SerializeField] Vector3 emptyCellScale = Vector3.one;
    [SerializeField] GameObject inventoryItemPrefab;
    [SerializeField] Color selectedNodeBackgroundColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Header("References")]
    [SerializeField] NodeDefinitionLibrary definitionLibrary;
    [SerializeField] TMP_Dropdown nodeDropdown;
    [SerializeField] public GameObject gridUIParent;
    [SerializeField] Transform cellContainer;
    [SerializeField] Transform seedSlotContainer;

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

    public NodeGraph GetCurrentGraphInEditorForSpawning()
    {
        NodeGraph clone = new NodeGraph { nodes = new List<NodeData>() };

        if (_currentlyEditedSequence?.nodes != null)
        {
            foreach (var nodeData in _currentlyEditedSequence.nodes)
            {
                if (nodeData == null) continue;

                NodeData clonedNode = new NodeData
                {
                    nodeId = nodeData.nodeId,
                    nodeDisplayName = nodeData.nodeDisplayName,
                    effects = NodeExecutor.CloneEffectsList(nodeData.effects),
                    orderIndex = nodeData.orderIndex,
                    canBeDeleted = nodeData.canBeDeleted,
                };

                clone.nodes.Add(clonedNode);
            }
        }
        
        return clone;
    }

    void CreateSeedSlot()
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

    void CreateSequenceCells()
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

    public void LoadSequenceFromSeed(NodeData seedData)
    {
        if (_actualSeedSlotCell == null || seedData == null || !seedData.IsSeed())
        {
            HideNodeEditorPanel();
            _currentlyEditedSequence.nodes.Clear();
            return;
        }

        seedData.EnsureSeedSequenceInitialized();

        ClearSequenceEditorCells();
        _currentlyEditedSequence.nodes.Clear();

        if (seedData.storedSequence?.nodes != null)
        {
            foreach (NodeData storedNode in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex))
            {
                if (storedNode == null || storedNode.orderIndex >= nodeCells.Count) continue;

                NodeCell targetCell = nodeCells[storedNode.orderIndex];
                NodeDefinition definition = definitionLibrary.definitions
                    .FirstOrDefault(d => d.name == storedNode.definitionName);

                if (definition != null)
                {
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

    IEnumerator ShowDropdownCoroutine(NodeCell cell, PointerEventData eventData)
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

    void OnDropdownValueChanged(int selectedIndex, NodeCell targetCell, List<NodeDefinition> sortedDefinitions)
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
        
        _currentlyEditedSequence.nodes.Clear();

        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex))
        {
            if (cell.HasItem())
            {
                NodeData dataFromCell = cell.GetNodeData();
                if (dataFromCell == null) continue;
                
                dataFromCell.orderIndex = cell.CellIndex;
                dataFromCell.ClearStoredSequence();
                _currentlyEditedSequence.nodes.Add(dataFromCell);
            }
        }
        
        NodeData currentSeedInSlot = GetCurrentSeedInSlot();

        if (currentSeedInSlot != null && currentSeedInSlot.IsSeed())
        {
            currentSeedInSlot.EnsureSeedSequenceInitialized();
            
            currentSeedInSlot.storedSequence.nodes.Clear();
            
            foreach (NodeData editorNode in _currentlyEditedSequence.nodes)
            {
                if (editorNode == null) continue;
                
                NodeData nodeForStorage = new NodeData
                {
                    nodeId = editorNode.nodeId,
                    definitionName = editorNode.definitionName,
                    nodeDisplayName = editorNode.nodeDisplayName,
                    effects = NodeExecutor.CloneEffectsList(editorNode.effects),
                    orderIndex = editorNode.orderIndex,
                    canBeDeleted = editorNode.canBeDeleted
                };

                nodeForStorage.SetPartOfSequence(true);

                currentSeedInSlot.storedSequence.nodes.Add(nodeForStorage);
            }
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
            // --- FIX IS HERE ---
            // We must not destroy the ItemView being dragged, only detach it from its original cell.
            originalCell.ClearNodeReference();
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
        
        if (draggedNodeDef != null && draggedNodeDef.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn))
        {
            draggedDraggable.ResetPosition(); return;
        }

        if (originalCell.IsInventoryCell)
        {
            NodeData draggedData = draggedView.GetNodeData();
            ItemView viewInTarget = targetSequenceCell.GetItemView();

            if (viewInTarget == null)
            {
                originalCell.RemoveNode();
                targetSequenceCell.AssignNode(draggedNodeDef);
            }
            else
            {
                NodeData dataInTarget = targetSequenceCell.GetNodeData();
                NodeDraggable draggableInTarget = viewInTarget.GetComponent<NodeDraggable>();
                
                originalCell.ClearNodeReference();
                targetSequenceCell.ClearNodeReference();
                
                originalCell.AssignItemView(viewInTarget, dataInTarget, null);
                draggableInTarget?.Initialize(InventoryGridController.Instance, originalCell);
                draggableInTarget?.SnapToCell(originalCell);
                
                targetSequenceCell.AssignItemView(draggedView, draggedData, null);
                draggedDraggable.Initialize(this, targetSequenceCell);
                draggedDraggable.SnapToCell(targetSequenceCell);
            }
        }
        else if (!originalCell.IsSeedSlot)
        {
            NodeData draggedData = draggedView.GetNodeData();
            ItemView viewInTarget = targetSequenceCell.GetItemView();

            if (viewInTarget == null)
            {
                originalCell.ClearNodeReference();
                targetSequenceCell.AssignItemView(draggedView, draggedData, null);
                draggedDraggable.SnapToCell(targetSequenceCell);
            }
            else
            {
                NodeData dataInTarget = targetSequenceCell.GetNodeData();
                NodeDraggable draggableInTarget = viewInTarget.GetComponent<NodeDraggable>();

                originalCell.ClearNodeReference();
                targetSequenceCell.ClearNodeReference();

                originalCell.AssignItemView(viewInTarget, dataInTarget, null);
                draggableInTarget?.SnapToCell(originalCell);

                targetSequenceCell.AssignItemView(draggedView, draggedData, null);
                draggedDraggable.SnapToCell(targetSequenceCell);
            }
            NodeCell.SelectCell(targetSequenceCell);
        }
        else
        {
            draggedDraggable.ResetPosition();
        }

        RefreshGraphAndUpdateSeed();
    }

    void HideNodeEditorPanel() { if (gridUIParent != null) gridUIParent.SetActive(false); }
    void ShowNodeEditorPanel() { if (gridUIParent != null) gridUIParent.SetActive(true); }
    void ClearSequenceEditorCells() { foreach (NodeCell cell in nodeCells) { cell.RemoveNode(); } }
    public void HideDropdown() { if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) { nodeDropdown.Hide(); nodeDropdown.gameObject.SetActive(false); } }
    public NodeCell GetCellAtIndex(int index) => (index >= 0 && index < nodeCells.Count) ? nodeCells[index] : null;
}
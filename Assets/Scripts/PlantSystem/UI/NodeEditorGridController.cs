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
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

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
    [SerializeField] public GameObject gridUIParent;
    [SerializeField] private Transform cellContainer;

    [Header("Seed Slot Specifics")]
    [SerializeField] private Transform seedSlotContainer;

    private List<NodeCell> nodeCells = new List<NodeCell>();
    private NodeCell _actualSeedSlotCell;
    private Canvas _rootCanvas;
    
    [System.NonSerialized] 
    private NodeGraph _currentlyEditedSequence = new NodeGraph();


    public GameObject NodeViewPrefab => nodeViewPrefab;
    public NodeDefinitionLibrary DefinitionLibrary => definitionLibrary;
    public Color SelectedNodeBackgroundColor => selectedNodeBackgroundColor;
    public Color EmptyCellColor => emptyCellColor;

    public NodeGraph GetCurrentGraphInEditorForSpawning()
    {
        NodeGraph clone = new NodeGraph();
        clone.nodes = new List<NodeData>();
        if (_currentlyEditedSequence != null && _currentlyEditedSequence.nodes != null)
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
                    storedSequence = null // Nodes *within* a sequence for spawning DO NOT have their own stored sequences
                };
                clone.nodes.Add(clonedNode);
            }
        }
        return clone;
    }

    public NodeData GetCurrentSeedInSlot() => _actualSeedSlotCell != null ? _actualSeedSlotCell.GetNodeData() : null;
    public NodeCell SeedSlotCell => _actualSeedSlotCell;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_currentlyEditedSequence == null) _currentlyEditedSequence = new NodeGraph();


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

        GameObject seedCellGO = new GameObject("SeedSlot_Cell", typeof(RectTransform));
        seedCellGO.transform.SetParent(seedSlotContainer, false);

        RectTransform rt = seedCellGO.GetComponent<RectTransform>();
        rt.sizeDelta = cellSize;

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
                GameObject cellGO = new GameObject($"SequenceCell_{i}", typeof(RectTransform)); 
                cellGO.transform.SetParent(cellContainer, false);

                RectTransform rt_cell = cellGO.GetComponent<RectTransform>(); 
                rt_cell.sizeDelta = cellSize;
                rt_cell.localScale = emptyCellScale;
                rt_cell.anchorMin = new Vector2(0.5f, 0.5f); rt_cell.anchorMax = new Vector2(0.5f, 0.5f);
                rt_cell.pivot = new Vector2(0.5f, 0.5f);
                rt_cell.anchoredPosition = new Vector2(startX + i * (cellSize.x + cellMargin), 0);
                
                SetupCellComponents(cellGO, i);
            }
        }
        else 
        {
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = new Vector2(cellMargin, cellMargin);
            for (int i = 0; i < emptyCellsCount; i++)
            {
                GameObject cellGO = new GameObject($"SequenceCell_{i}", typeof(RectTransform));
                cellGO.transform.SetParent(cellContainer, false);
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
                    NodeCell.CurrentlySelectedCell != _actualSeedSlotCell)
                {
                    NodeCell selected = NodeCell.CurrentlySelectedCell;
                    NodeData data = selected.GetNodeData();
                    if (data != null && data.canBeDeleted)
                    {
                        selected.RemoveNode();
                        RefreshGraphAndUpdateSeed(); 
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
        _currentlyEditedSequence.nodes.Clear(); 
        return;
    }
    
    seedData.EnsureSeedSequenceInitialized(); // Ensure the seed being loaded has its sequence object ready

    ClearSequenceEditorCells();
    _currentlyEditedSequence.nodes.Clear(); 

    if (seedData.storedSequence != null && seedData.storedSequence.nodes != null)
    {
        foreach (NodeData nodeDataInSeedSequence in seedData.storedSequence.nodes.OrderBy(n => n.orderIndex))
        {
            if (nodeDataInSeedSequence == null) continue;
            
            // CRITICAL: Ensure nodes loaded from a seed's sequence don't have their own sequences
            nodeDataInSeedSequence.ClearStoredSequence();
            
            if (nodeDataInSeedSequence.orderIndex >= 0 && nodeDataInSeedSequence.orderIndex < nodeCells.Count)
            {
                NodeCell targetCell = nodeCells[nodeDataInSeedSequence.orderIndex];
                NodeDefinition def = definitionLibrary.definitions.FirstOrDefault(d => d.displayName == nodeDataInSeedSequence.nodeDisplayName);
                
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
                        // Double-check: nodeDataInSeedSequence is an element of a sequence, its own storedSequence should be null.
                        nodeDataInSeedSequence.ClearStoredSequence(); // Ensure it's null

                        view.Initialize(nodeDataInSeedSequence, def, this);
                        targetCell.AssignNodeView(view, nodeDataInSeedSequence); 
                        
                        NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
                        draggable.Initialize(this, null, targetCell);

                        _currentlyEditedSequence.nodes.Add(nodeDataInSeedSequence);
                    }
                    else { Destroy(nodeViewGO); }
                }
                else Debug.LogWarning($"Could not find NodeDefinition for '{nodeDataInSeedSequence.nodeDisplayName}' while loading seed.");
            }
        }
        _currentlyEditedSequence.nodes = _currentlyEditedSequence.nodes.OrderBy(n => n.orderIndex).ToList();
    }
    ShowNodeEditorPanel();
    Debug.Log($"[NodeEditorGridController] Loaded sequence from seed '{seedData.nodeDisplayName}'. Editor has {_currentlyEditedSequence.nodes.Count} nodes.");
}

    public void UnloadSeedFromSlot()
    {
        if (_actualSeedSlotCell == null) { Debug.LogWarning("Seed slot not initialized. Cannot unload."); return; }
        
        ClearSequenceEditorCells();
        HideNodeEditorPanel();
        _currentlyEditedSequence.nodes.Clear();
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
        if (cell.IsInventoryCell || cell == _actualSeedSlotCell) return;
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
                    targetCell.AssignNode(selectedDef); // AssignNode in NodeCell will create a new NodeData
                                                        // which by default will have storedSequence = null
                    NodeCell.SelectCell(targetCell);    
                    RefreshGraphAndUpdateSeed(); 
                }
            } else Debug.LogError($"Dropdown selection index {selectedIndex} invalid.");
        }
    }

    public void HideDropdown()
    { 
        if (nodeDropdown != null && nodeDropdown.gameObject.activeSelf) { 
            if (nodeDropdown.IsExpanded) nodeDropdown.Hide(); 
            nodeDropdown.gameObject.SetActive(false); 
        } 
    }

    // FILE: Assets/Scripts/Nodes/UI/NodeEditorGridController.cs
// Find the RefreshGraphAndUpdateSeed method and update it:

public void RefreshGraphAndUpdateSeed()
{
    if (_actualSeedSlotCell == null) { return; }

    if (_currentlyEditedSequence == null) _currentlyEditedSequence = new NodeGraph();
    _currentlyEditedSequence.nodes.Clear();

    if (nodeCells != null) { 
        foreach (var cell in nodeCells.OrderBy(c => c.CellIndex)) {
            NodeData dataFromCell = cell.GetNodeData(); 
            if (dataFromCell != null) {
                dataFromCell.orderIndex = cell.CellIndex;
                // Nodes in the UI sequence editor should not have their own sub-sequences.
                dataFromCell.ClearStoredSequence(); // Ensure it's null
                _currentlyEditedSequence.nodes.Add(dataFromCell);
            }
        }
    }

    NodeData currentSeedInSlot = GetCurrentSeedInSlot();
    if (currentSeedInSlot != null && currentSeedInSlot.IsSeed())
    {
        currentSeedInSlot.EnsureSeedSequenceInitialized(); // Make sure the seed's sequence object exists
        currentSeedInSlot.storedSequence.nodes.Clear();

        foreach (NodeData uiNodeData in _currentlyEditedSequence.nodes)
        {
            if (uiNodeData == null) continue;
            NodeData storedNodeCopy = new NodeData
            {
                nodeId = uiNodeData.nodeId, 
                nodeDisplayName = uiNodeData.nodeDisplayName,
                effects = NodeExecutor.CloneEffectsList(uiNodeData.effects), 
                orderIndex = uiNodeData.orderIndex,
                canBeDeleted = uiNodeData.canBeDeleted,
                storedSequence = null // CRITICAL: Nodes *within* the seed's stored sequence are not seeds themselves
                                      // and thus must have a null storedSequence.
            };
            
            // EXTRA SAFETY: Force-clear the storedSequence even if it was accidentally set
            storedNodeCopy.ClearStoredSequence();
            
            currentSeedInSlot.storedSequence.nodes.Add(storedNodeCopy);
        }
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
            RefreshGraphAndUpdateSeed(); 

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

        // DraggedData is a seed, ensure its sequence object is ready
        draggedData.EnsureSeedSequenceInitialized();

        _actualSeedSlotCell.AssignNodeView(draggedView, draggedData);
        draggedDraggable.SnapToCell(_actualSeedSlotCell);
        
        LoadSequenceFromSeed(draggedData); 
        Debug.Log($"Seed '{draggedData.nodeDisplayName}' placed in slot. Its storedSequence has {draggedData.storedSequence?.nodes?.Count ?? 0} nodes. Editor has {_currentlyEditedSequence.nodes.Count} nodes.");
    }


    public void HandleDropOnSequenceCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetSequenceCell)
    {
        if (_actualSeedSlotCell == null) { Debug.LogError("Seed slot not initialized. Sequence operations may fail."); }

        if (draggedDraggable == null || originalCell == null || targetSequenceCell == null || targetSequenceCell.IsInventoryCell || targetSequenceCell == _actualSeedSlotCell)
        {
            draggedDraggable?.ResetPosition(); return;
        }

        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition(); // If from inventory
        NodeData draggedData = draggedView?.GetNodeData();    // If from sequence or seed slot

        if (draggedView == null || (draggedDef == null && draggedData == null) ) {
            Debug.LogError("[NodeEditorGridController] HandleDropOnSequenceCell: Dragged object missing View/Def/Data.", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition(); return;
        }
        // Prevent dropping a seed definition (which implies it's a container for a sequence) into a sequence.
        if (draggedDef != null && draggedDef.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn)) {
            Debug.LogWarning("Cannot place Seed definitions into a seed's sequence. Resetting drag.");
            draggedDraggable.ResetPosition();
            return;
        }
         // Also prevent dragging a NodeData that IS a seed into the sequence.
        if (draggedData != null && draggedData.IsSeed()) {
             Debug.LogWarning("Cannot place a Seed item into another seed's sequence. Resetting drag.");
             draggedDraggable.ResetPosition();
             return;
        }


        if (targetSequenceCell == originalCell && !originalCell.IsInventoryCell) {
            draggedDraggable.SnapToCell(originalCell); NodeCell.SelectCell(originalCell); return;
        }

        if (originalCell.IsInventoryCell) 
        { 
            InventoryGridController.Instance?.RemoveGeneFromInventory(originalCell); 
        
            NodeView existingViewInTargetSeq = targetSequenceCell.GetNodeView();
            if (existingViewInTargetSeq != null) 
            {
                NodeData dataFromSeqTarget = targetSequenceCell.GetNodeData(); 
                targetSequenceCell.ClearNodeReference(); 
                InventoryGridController.Instance.ReturnGeneToInventory(existingViewInTargetSeq, dataFromSeqTarget); 
            }
        
            // Create a new NodeData for the sequence that CANNOT be a seed
            NodeData sequenceNodeData = new NodeData
            {
                nodeId = System.Guid.NewGuid().ToString(),
                nodeDisplayName = draggedDef.displayName,
                effects = draggedDef.CloneEffects(),
                orderIndex = targetSequenceCell.CellIndex,
                canBeDeleted = true,
                storedSequence = null // MUST be null for sequence nodes
            };
        
            // Extra safety: remove any SeedSpawn effects to prevent it from being a seed
            if (sequenceNodeData.effects != null)
            {
                sequenceNodeData.effects.RemoveAll(e => e != null && e.effectType == NodeEffectType.SeedSpawn);
            }
        
            targetSequenceCell.AssignNodeFromData(sequenceNodeData, draggedDef); // New method
            targetSequenceCell.GetNodeView()?.GetComponent<NodeDraggable>()?.SnapToCell(targetSequenceCell);
            NodeCell.SelectCell(targetSequenceCell);
            Destroy(draggedDraggable.gameObject); 
        } 
        else if (originalCell == _actualSeedSlotCell) 
        {
            Debug.LogWarning("Cannot drag the main Seed from the Seed Slot directly into its own sequence editor. Resetting drag.");
            draggedDraggable.ResetPosition();
            return;
        }
        else { 
            NodeView existingViewInTarget = targetSequenceCell.GetNodeView(); 
            NodeData existingDataInTarget = targetSequenceCell.GetNodeData();
            NodeCell.ClearSelection(); 
            originalCell.ClearNodeReference();

            if (existingViewInTarget != null) {
                NodeDraggable draggableFromTarget = existingViewInTarget.GetComponent<NodeDraggable>();
                // existingDataInTarget is a node from the sequence, its storedSequence should be null.
                originalCell.AssignNodeView(existingViewInTarget, existingDataInTarget); 
                draggableFromTarget?.SnapToCell(originalCell);
            }
            // draggedData is also a node from the sequence, its storedSequence should be null.
            targetSequenceCell.AssignNodeView(draggedView, draggedData); 
            draggedDraggable.SnapToCell(targetSequenceCell); 
            NodeCell.SelectCell(targetSequenceCell);
        }
        RefreshGraphAndUpdateSeed(); 
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
        if (Application.isPlaying) return; 
        
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
            } else { 
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

        if (seedSlotContainer != null && seedSlotContainer.gameObject.activeInHierarchy && seedSlotContainer.TryGetComponent<RectTransform>(out var seedContainerRect))
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.35f); 
            Vector3[] corners = new Vector3[4];
            seedContainerRect.GetWorldCorners(corners);
            Matrix4x4 originalMatrix = Gizmos.matrix; 
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(corners[0], corners[1]); Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]); Gizmos.DrawLine(corners[3], corners[0]);
            Gizmos.matrix = originalMatrix;
        }
    }
    #endif
}
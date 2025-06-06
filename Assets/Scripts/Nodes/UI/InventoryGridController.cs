// FILE: Assets/Scripts/Nodes/UI/InventoryGridController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; private set; }

    [Header("Grid Layout & Appearance")]
    [SerializeField][Min(1)] public int inventoryRows = 2;
    [SerializeField][Min(1)] public int inventoryColumns = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Cell Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    
    [Header("Tool Auto-Population")]
    [SerializeField] private ToolDefinition[] availableTools;

    [Header("Node Visuals (Shared or Specific)")]
    [SerializeField] private GameObject nodeViewPrefab;
    [SerializeField] private GameObject toolViewPrefab; // NEW: Add this field
    [Tooltip("Uniform scale factor for the gene/item image (thumbnail) within NodeViews. Default is 1.")]
    [SerializeField] private float nodeGlobalImageScale = 1f;
    [Tooltip("Padding to shrink the raycast area of node images. Negative values shrink. Default 0.")]
    [SerializeField] private float nodeImageRaycastPadding = 0f;

    [Header("UI References")]
    [SerializeField] private Transform cellContainer;
    [SerializeField] private Canvas _rootCanvas;

    [Header("Debugging")]
    [SerializeField] private bool logInventoryChanges = true;

    private List<NodeCell> inventoryCells = new List<NodeCell>();

    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Color EmptyCellColor => emptyCellColor;
    public float NodeGlobalImageScale => nodeGlobalImageScale;
    public float NodeImageRaycastPadding => nodeImageRaycastPadding;
    
    // Get total slots
    public int TotalSlots => inventoryRows * inventoryColumns;
    
    // FIXED: Add property to get actual count of created cells
    public int ActualCellCount => inventoryCells?.Count ?? 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    
        if (cellContainer == null) Debug.LogError("[InventoryGridController] Cell Container not assigned!", gameObject);
        if (nodeViewPrefab == null) Debug.LogError("[InventoryGridController] Node View Prefab not assigned!", gameObject);
        if (toolViewPrefab == null) Debug.LogWarning("[InventoryGridController] Tool View Prefab not assigned! Will use Node View Prefab for tools.", gameObject);
        if (_rootCanvas == null) Debug.LogError("[InventoryGridController] Root Canvas not assigned!", gameObject);
    }

    void Start()
    {
        if (cellContainer != null)
        {
            CreateInventoryCells();
            PopulateInitialGenesFromLibrary();
        }
    }

    private void CreateInventoryCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        inventoryCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) {
            Debug.LogError("[InventoryGridController] Cell Container MUST have GridLayoutGroup.", cellContainer.gameObject);
            return;
        }
        
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = inventoryColumns;

        int totalCells = inventoryRows * inventoryColumns;
        Debug.Log($"[InventoryGridController] Creating {totalCells} inventory cells ({inventoryRows}x{inventoryColumns})");
        
        for (int i = 0; i < totalCells; i++)
        {
            GameObject cellGO = new GameObject($"InventoryCell_{i}", typeof(RectTransform));
            cellGO.transform.SetParent(cellContainer, false);
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage); 
            inventoryCells.Add(cellLogic);
        }
        
        Debug.Log($"[InventoryGridController] Created {inventoryCells.Count} inventory cells successfully");
    }

    private void PopulateInitialGenesFromLibrary()
    {
        if (NodeEditorGridController.Instance == null || NodeEditorGridController.Instance.DefinitionLibrary == null) {
            Debug.LogWarning("[InventoryGridController] Cannot populate: NodeEditorGridController or DefinitionLibrary not ready.");
            return;
        }

        // FIXED: First add auto-add tools
        if (availableTools != null)
        {
            foreach (var tool in availableTools)
            {
                if (tool != null && tool.autoAddToInventory)
                {
                    bool success = AddToolToInventory(tool);
                    if (logInventoryChanges)
                    {
                        string prefabUsed = toolViewPrefab != null ? "ToolView prefab" : "NodeView prefab";
                        Debug.Log($"[Inventory] Auto-added tool '{tool.displayName}' using {prefabUsed}: {(success ? "Success" : "Failed - Inventory Full")}");
                    }
                }
            }
        }

        // Then add gene definitions (if there's space remaining)
        NodeDefinitionLibrary lib = NodeEditorGridController.Instance.DefinitionLibrary;
        if (lib.definitions != null) {
            int remainingSlots = TotalSlots - GetUsedSlotCount();
            var definitionsToAdd = lib.definitions.Where(d => d != null).Take(remainingSlots).ToList();
        
            foreach (var def in definitionsToAdd)
            {
                AddGeneToInventoryFromDefinition(def, null); 
            }
        
            if (definitionsToAdd.Count == 0 && lib.definitions.Count > 0)
                Debug.LogWarning("[InventoryGridController] No space remaining for gene definitions after adding tools.");
        }
    }
    
    private bool AddToolToInventory(ToolDefinition tool)
    {
        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
        if (emptyCell == null)
        {
            if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add tool '{tool.displayName}', inventory full.");
            return false;
        }
    
        // Create NodeData for the tool
        NodeData toolNodeData = new NodeData()
        {
            nodeId = tool.name + "_tool_" + System.Guid.NewGuid().ToString(),
            nodeDisplayName = tool.displayName,
            effects = new List<NodeEffectData>(),
            orderIndex = -1, 
            canBeDeleted = false,
            storedSequence = new NodeGraph()
        };
    
        // Create the tool view
        GameObject toolViewGO = CreateToolView(tool, emptyCell.transform);
        if (toolViewGO != null)
        {
            // Initialize the ToolView component
            ToolView toolView = toolViewGO.GetComponent<ToolView>();
            if (toolView != null)
            {
                toolView.Initialize(toolNodeData, tool);
            }
            else
            {
                Debug.LogError($"[Inventory] Failed to get ToolView component on {toolViewGO.name}");
                Destroy(toolViewGO);
                return false;
            }
        
            // Setup draggable behavior
            NodeDraggable draggable = toolViewGO.GetComponent<NodeDraggable>() ?? toolViewGO.AddComponent<NodeDraggable>();
            draggable.Initialize(invCtrl: this, startingCell: emptyCell);
        
            // Assign to cell
            emptyCell.AssignToolView(toolViewGO, toolNodeData, tool);
        
            if (logInventoryChanges) 
                Debug.Log($"[Inventory] Added tool '{tool.displayName}' to inv cell {emptyCell.CellIndex}.");
            return true;
        }
    
        Debug.LogError($"[Inventory] Failed to create tool view for '{tool.displayName}'");
        return false;
    }

    private GameObject CreateToolView(ToolDefinition tool, Transform parent)
    {
        // FIXED: Use dedicated ToolView prefab if available, fallback to NodeView prefab
        GameObject prefabToUse = toolViewPrefab != null ? toolViewPrefab : nodeViewPrefab;
    
        if (prefabToUse == null)
        {
            Debug.LogError($"[Inventory] No ToolView prefab or NodeView prefab assigned for creating tool '{tool.displayName}'!");
            return null;
        }
    
        GameObject toolView = Instantiate(prefabToUse, parent);
        toolView.name = $"ToolView_{tool.displayName}";
    
        // If using NodeView prefab, convert it to ToolView
        if (prefabToUse == nodeViewPrefab)
        {
            // Remove NodeView component if it exists
            NodeView existingNodeView = toolView.GetComponent<NodeView>();
            if (existingNodeView != null)
            {
                DestroyImmediate(existingNodeView);
            }
        
            // Add ToolView component
            ToolView toolViewComponent = toolView.AddComponent<ToolView>();
        }
        // If using ToolView prefab, it should already have ToolView component
        else
        {
            ToolView toolViewComponent = toolView.GetComponent<ToolView>();
            if (toolViewComponent == null)
            {
                Debug.LogError($"[Inventory] ToolView prefab '{prefabToUse.name}' is missing ToolView component!");
                toolViewComponent = toolView.AddComponent<ToolView>();
            }
        }
    
        return toolView;
    }

    // Get used slot count
    private int GetUsedSlotCount()
    {
        return inventoryCells.Count(cell => cell.HasNode());
    }

    // <<< ENHANCED: Tries to use targetCellHint if provided and empty >>>
    public bool AddGeneToInventoryFromDefinition(NodeDefinition geneDef, NodeCell targetCellHint)
{
    if (geneDef == null) return false;

    NodeCell cellToUse = null;

    // If a target cell hint is provided and it's actually an empty inventory cell, use it.
    if (targetCellHint != null && targetCellHint.IsInventoryCell && !targetCellHint.HasNode())
    {
        cellToUse = targetCellHint;
        if (logInventoryChanges) Debug.Log($"[Inventory AddGene] Using provided targetCellHint: {targetCellHint.CellIndex}");
    }
    else
    {
        // Fallback: find the first available empty cell
        cellToUse = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
    }
    
    if (cellToUse != null)
    {
        NodeData inventoryNodeData = new NodeData() {
            nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
            nodeDisplayName = geneDef.displayName,
            effects = geneDef.CloneEffects(),
            orderIndex = -1, 
            canBeDeleted = false,
            storedSequence = new NodeGraph() // This should be empty for regular nodes, but populated for seeds
        };

        // IMPORTANT: If this is a seed, it needs its stored sequence!
        // For now, seeds added from definitions start with empty sequences
        // The sequence gets populated when edited in the seed slot
        
        GameObject nodeViewGO = Instantiate(nodeViewPrefab, cellToUse.transform);
        NodeView view = nodeViewGO.GetComponent<NodeView>();
        if (view == null) { Destroy(nodeViewGO); return false; }
        
        view.Initialize(inventoryNodeData, geneDef, null);

        NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: cellToUse);

        cellToUse.AssignNodeView(view, inventoryNodeData);
        
        if (logInventoryChanges) 
        {
            Debug.Log($"[Inventory] Added '{geneDef.displayName}' to inv cell {cellToUse.CellIndex}. IsSeed: {inventoryNodeData.IsSeed()}");
            if (inventoryNodeData.IsSeed())
            {
                Debug.Log($"[Inventory] Seed's stored sequence has {inventoryNodeData.storedSequence?.nodes?.Count ?? 0} nodes");
            }
        }
        return true;
    }

    if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add '{geneDef.displayName}', inventory full or no suitable cell found.");
    return false;
}
    
    public InventoryBarItem GetItemAtIndex(int index)
    {
        // FIXED: Add bounds checking
        if (index >= 0 && index < inventoryCells.Count)
        {
            var cell = inventoryCells[index];
            if (cell.HasNode())
            {
                var toolDef = cell.GetToolDefinition();
                if (toolDef != null)
                {
                    return InventoryBarItem.FromTool(toolDef, cell.GetNodeView()?.gameObject);
                }
                else
                {
                    var nodeData = cell.GetNodeData();
                    var nodeView = cell.GetNodeView();
                    var nodeDef = nodeView?.GetNodeDefinition();
                
                    if (nodeData != null && nodeDef != null)
                    {
                        return InventoryBarItem.FromNode(nodeData, nodeDef, nodeView.gameObject);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[InventoryGridController] GetItemAtIndex: Index {index} out of bounds (0-{inventoryCells.Count-1})");
        }
        return null;
    }

    // This method takes an *existing* NodeView and its *existing* NodeData (e.g., from seed slot or sequence editor)
    // and places it into an inventory slot.
    public void ReturnGeneToInventory(NodeView geneViewToReturn, NodeData geneDataToReturn)
    {
        if (geneViewToReturn == null || geneDataToReturn == null)
        {
            Debug.LogError("[InventoryGridController] ReturnGeneToInventory: geneView or geneData is null.");
            if (geneViewToReturn != null) Destroy(geneViewToReturn.gameObject);
            return;
        }

        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
        if (emptyCell == null)
        {
            if (logInventoryChanges) Debug.LogWarning($"[Inventory] Full. Cannot return '{geneDataToReturn.nodeDisplayName}'. Destroying item.");
            Destroy(geneViewToReturn.gameObject);
            return;
        }

        // Update NodeData properties for inventory context
        geneDataToReturn.orderIndex = -1;
        geneDataToReturn.canBeDeleted = false;

        emptyCell.AssignNodeView(geneViewToReturn, geneDataToReturn);

        NodeDraggable draggable = geneViewToReturn.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = geneViewToReturn.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: emptyCell);
        draggable.SnapToCell(emptyCell);

        if (logInventoryChanges) Debug.Log($"[Inventory] Returned '{geneDataToReturn.nodeDisplayName}' to inv cell {emptyCell.CellIndex}. IsSeed: {geneDataToReturn.IsSeed()}, StoredSeqCount: {geneDataToReturn.storedSequence?.nodes?.Count ?? 0}");
    }

    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        if (inventoryCell == null || !inventoryCell.HasNode() || !inventoryCell.IsInventoryCell) return;
        NodeData removedData = inventoryCell.GetNodeData();
        if (removedData != null) {
            if (logInventoryChanges) Debug.Log($"[Inventory] Dragged out '{removedData.nodeDisplayName}' from inv cell {inventoryCell.CellIndex}");
        }
        inventoryCell.ClearNodeReference();
    }

    public NodeCell FindInventoryCellAtScreenPosition(Vector2 screenPosition)
    {
        if (_rootCanvas == null) return null;
        foreach (NodeCell cell in inventoryCells) {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition, _rootCanvas.worldCamera)) {
                return cell;
            }
        }
        return null;
    }

    public void HandleDropOnInventoryCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetInventoryCell)
{
    if (draggedDraggable == null || originalCell == null || targetInventoryCell == null || !targetInventoryCell.IsInventoryCell) {
        if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Pre-condition fail. Dragged: {draggedDraggable != null}, Orig: {originalCell != null}, Target: {targetInventoryCell != null}, TargetIsInv: {targetInventoryCell?.IsInventoryCell}. Resetting.");
        draggedDraggable?.ResetPosition(); return;
    }

    // FIXED: Handle both NodeView and ToolView
    NodeView draggedNodeView = draggedDraggable.GetComponent<NodeView>();
    ToolView draggedToolView = draggedDraggable.GetComponent<ToolView>();
    NodeData draggedData = null;
    NodeDefinition draggedDef = null;
    ToolDefinition draggedToolDef = null;

    if (draggedNodeView != null)
    {
        draggedData = draggedNodeView.GetNodeData();
        draggedDef = draggedNodeView.GetNodeDefinition();
    }
    else if (draggedToolView != null)
    {
        draggedData = draggedToolView.GetNodeData();
        draggedToolDef = draggedToolView.GetToolDefinition();
    }

    if (draggedData == null || (draggedDef == null && draggedToolDef == null)) {
        Debug.LogError($"[InventoryGridController] HandleDrop: Dragged object missing View/Data/Definition! DraggedNodeView: {draggedNodeView != null}, DraggedToolView: {draggedToolView != null}, DraggedData: {draggedData != null}, DraggedDef: {draggedDef != null}, DraggedToolDef: {draggedToolDef != null}. Resetting.", draggedDraggable.gameObject);
        draggedDraggable.ResetPosition(); return;
    }

    if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Attempting drop. Original Cell - IsInv: {originalCell.IsInventoryCell}, IsSeedSlot: {originalCell.IsSeedSlot}. Target Cell: {targetInventoryCell.CellIndex} (HasNode: {targetInventoryCell.HasNode()})");

    // --- Case 1: Node dragged FROM SEED SLOT TO INVENTORY ---
    if (!originalCell.IsInventoryCell && originalCell.IsSeedSlot)
    {
        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: SeedSlot -> Inventory. Seed: '{draggedData.nodeDisplayName}'");

        NodeCell actualTargetInvCell = targetInventoryCell;
        if (actualTargetInvCell.HasNode()) 
        {
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target Inv Cell {actualTargetInvCell.CellIndex} is occupied by {actualTargetInvCell.GetNodeData()?.nodeDisplayName}. Finding empty slot.");
            actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
        }

        if (actualTargetInvCell == null) { 
            if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Inventory Full. Cannot return Seed '{draggedData.nodeDisplayName}'. Resetting drag to original seed slot.");
            draggedDraggable.ResetPosition(); 
            return;
        }
        
        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target inventory cell for seed is: {actualTargetInvCell.CellIndex}. Unloading from Node Editor.");
        
        NodeEditorGridController.Instance.UnloadSeedFromSlot();
        
        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Clearing original seed slot cell ({originalCell.CellIndex}, IsSeedSlot: {originalCell.IsSeedSlot}).");
        originalCell.ClearNodeReference(); 

        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Assigning dragged seed '{draggedData.nodeDisplayName}' (View: {draggedNodeView.gameObject.name}) to inventory cell {actualTargetInvCell.CellIndex}.");
        actualTargetInvCell.AssignNodeView(draggedNodeView, draggedData); 
        
        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Snapping '{draggedData.nodeDisplayName}' to inventory cell {actualTargetInvCell.CellIndex}.");
        draggedDraggable.SnapToCell(actualTargetInvCell); 

        if (logInventoryChanges) Debug.Log($"[Inventory] Returned Seed '{draggedData.nodeDisplayName}' from Seed Slot to inv cell {actualTargetInvCell.CellIndex}. StoredSeqCount: {draggedData.storedSequence?.nodes?.Count ?? 0}");
    }
    // --- Case 2: Node dragged FROM SEQUENCE EDITOR TO INVENTORY ---
    else if (!originalCell.IsInventoryCell && !originalCell.IsSeedSlot)
    {
        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: SequenceEditor -> Inventory. Gene: '{draggedDef.displayName}' being dropped on InvCell: {targetInventoryCell.CellIndex}");
        
        NodeCell actualTargetInvCell = targetInventoryCell;

        if (actualTargetInvCell.HasNode()) {
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target InvCell {actualTargetInvCell.CellIndex} for sequence item is occupied by {actualTargetInvCell.GetNodeData()?.nodeDisplayName}. Finding first available empty slot.");
            actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
        }
        
        if (actualTargetInvCell == null) {
            if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Inventory Full. Cannot move '{draggedDef.displayName}' from sequence. Resetting drag.");
            draggedDraggable.ResetPosition();
            return;
        }
        
        NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
        NodeEditorGridController.Instance?.RefreshGraphAndUpdateSeed();

        AddGeneToInventoryFromDefinition(draggedDef, actualTargetInvCell); 
        Destroy(draggedDraggable.gameObject);

        if (logInventoryChanges) Debug.Log($"[Inventory] Moved '{draggedDef.displayName}' from seq cell {originalCell.CellIndex} to inv cell {actualTargetInvCell.CellIndex}.");
    }
    // --- Case 3: From INVENTORY to INVENTORY (Swapping or Moving) ---
    else if (originalCell.IsInventoryCell)
    {
        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: Inventory -> Inventory. Item: '{draggedData.nodeDisplayName}'");
        if (targetInventoryCell == originalCell) {
            if (logInventoryChanges) Debug.Log("[Inventory HandleDrop] Dropped on self in inventory. Resetting.");
            draggedDraggable.ResetPosition(); return;
        }

        if (targetInventoryCell.HasNode()) { 
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Swapping with item in target cell {targetInventoryCell.CellIndex}.");
            
            // FIXED: Handle swapping with both NodeView and ToolView
            NodeView viewInTargetCell = targetInventoryCell.GetNodeView();
            ToolView toolViewInTargetCell = null;
            
            if (viewInTargetCell == null)
            {
                // Check if it's a tool view
                GameObject targetCellGO = null;
                foreach (Transform child in targetInventoryCell.transform)
                {
                    if (child.GetComponent<ToolView>() != null)
                    {
                        toolViewInTargetCell = child.GetComponent<ToolView>();
                        targetCellGO = child.gameObject;
                        break;
                    }
                }
                
                if (toolViewInTargetCell != null)
                {
                    NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                    ToolDefinition toolDefInTarget = targetInventoryCell.GetToolDefinition();
                    
                    originalCell.ClearNodeReference();
                    originalCell.AssignToolView(targetCellGO, dataInTargetCell, toolDefInTarget);
                    targetCellGO.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                }
            }
            else
            {
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                
                originalCell.ClearNodeReference(); 
                originalCell.AssignNodeView(viewInTargetCell, dataInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
            }
            
            targetInventoryCell.ClearNodeReference(); 
        } else { 
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Moving to empty target cell {targetInventoryCell.CellIndex}.");
            originalCell.ClearNodeReference(); 
        }
        
        // FIXED: Assign based on whether it's a tool or node
        if (draggedToolView != null)
        {
            targetInventoryCell.AssignToolView(draggedToolView.gameObject, draggedData, draggedToolDef);
        }
        else
        {
            targetInventoryCell.AssignNodeView(draggedNodeView, draggedData);
        }
        
        draggedDraggable.SnapToCell(targetInventoryCell);
        if (logInventoryChanges) Debug.Log($"[Inventory] Moved inv item '{draggedData.nodeDisplayName}' from cell {originalCell.CellIndex} to cell {targetInventoryCell.CellIndex}.");
    }
    else
    {
        Debug.LogWarning($"[InventoryGridController HandleDrop] Unhandled drop scenario. Original IsInv: {originalCell.IsInventoryCell}, Original IsSeedSlot: {originalCell.IsSeedSlot}. Target IsInv: {targetInventoryCell.IsInventoryCell}. Resetting.", gameObject);
        draggedDraggable.ResetPosition();
    }
}

    public NodeCell GetInventoryCellAtIndex(int index)
    {
        // FIXED: Add bounds checking
        if (index >= 0 && index < inventoryCells.Count) return inventoryCells[index];
        
        Debug.LogWarning($"[InventoryGridController] GetInventoryCellAtIndex: Index {index} out of bounds (0-{inventoryCells.Count-1})");
        return null;
    }
}
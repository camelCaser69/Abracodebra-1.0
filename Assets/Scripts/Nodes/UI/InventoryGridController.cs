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
    [SerializeField] private GameObject toolViewPrefab; 
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
    
    public int TotalSlots => inventoryRows * inventoryColumns;
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
    
        NodeData toolNodeData = new NodeData()
        {
            nodeId = tool.name + "_tool_" + System.Guid.NewGuid().ToString(),
            nodeDisplayName = tool.displayName,
            effects = new List<NodeEffectData>(), 
            orderIndex = -1, 
            canBeDeleted = false,
            storedSequence = null // Explicitly null for tools
        };
        // toolNodeData.IsSeed() will be false, so EnsureSeedSequenceInitialized() would also null it.
    
        GameObject toolViewGO = CreateToolView(tool, emptyCell.transform);
        if (toolViewGO != null)
        {
            ToolView toolView = toolViewGO.GetComponent<ToolView>();
            if (toolView != null)
            {
                // ToolView.Initialize will also ensure toolNodeData.storedSequence is null.
                toolView.Initialize(toolNodeData, tool);
            }
            else
            {
                Debug.LogError($"[Inventory] Failed to get ToolView component on {toolViewGO.name}");
                Destroy(toolViewGO);
                return false;
            }
        
            NodeDraggable draggable = toolViewGO.GetComponent<NodeDraggable>() ?? toolViewGO.AddComponent<NodeDraggable>();
            draggable.Initialize(invCtrl: this, startingCell: emptyCell);
        
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
        GameObject prefabToUse = toolViewPrefab != null ? toolViewPrefab : nodeViewPrefab;
    
        if (prefabToUse == null)
        {
            Debug.LogError($"[Inventory] No ToolView prefab or NodeView prefab assigned for creating tool '{tool.displayName}'!");
            return null;
        }
    
        GameObject toolViewInstance = Instantiate(prefabToUse, parent);
        toolViewInstance.name = $"ToolView_{tool.displayName}";
    
        if (prefabToUse == nodeViewPrefab)
        {
            NodeView existingNodeView = toolViewInstance.GetComponent<NodeView>();
            if (existingNodeView != null)
            {
                DestroyImmediate(existingNodeView); // Use DestroyImmediate if in editor context or for cleaner removal
            }
            toolViewInstance.AddComponent<ToolView>();
        }
        else
        {
            ToolView toolViewComponent = toolViewInstance.GetComponent<ToolView>();
            if (toolViewComponent == null)
            {
                Debug.LogError($"[Inventory] ToolView prefab '{prefabToUse.name}' is missing ToolView component!");
                toolViewInstance.AddComponent<ToolView>();
            }
        }
    
        return toolViewInstance;
    }

    private int GetUsedSlotCount()
    {
        return inventoryCells.Count(cell => cell.HasNode());
    }

    public bool AddGeneToInventoryFromDefinition(NodeDefinition geneDef, NodeCell targetCellHint)
{
    if (geneDef == null) return false;

    NodeCell cellToUse = null;

    if (targetCellHint != null && targetCellHint.IsInventoryCell && !targetCellHint.HasNode())
    {
        cellToUse = targetCellHint;
        // if (logInventoryChanges) Debug.Log($"[Inventory AddGene] Using provided targetCellHint: {targetCellHint.CellIndex}");
    }
    else
    {
        cellToUse = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
    }
    
    if (cellToUse != null)
    {
        NodeData inventoryNodeData = new NodeData() {
            nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
            nodeDisplayName = geneDef.displayName,
            effects = geneDef.CloneEffects(), // Effects are cloned
            orderIndex = -1, 
            canBeDeleted = false, // Typically false for library items
            // _storedSequence will be null by default
            // _isContainedInSequence will be false by default
        };

        // If the definition is for a seed, initialize its sequence storage.
        inventoryNodeData.EnsureSeedSequenceInitialized(); // This checks IsPotentialSeedContainer & !_isContainedInSequence
        
        // CRITICAL: Clean. This will also ensure children in sequence are marked as contained.
        inventoryNodeData.CleanForSerialization(0, "InvGridAddGene"); 
        
        GameObject nodeViewGO = Instantiate(nodeViewPrefab, cellToUse.transform);
        NodeView view = nodeViewGO.GetComponent<NodeView>();
        if (view == null) { Destroy(nodeViewGO); return false; }
        
        // Initialize NodeView. It will handle its own _nodeData state based on IsSeed().
        view.Initialize(inventoryNodeData, geneDef, null); 

        NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: cellToUse);

        cellToUse.AssignNodeView(view, inventoryNodeData);
        
        if (logInventoryChanges) 
        {
            Debug.Log($"[Inventory] Added '{geneDef.displayName}' to inv cell {cellToUse.CellIndex}. IsSeed: {inventoryNodeData.IsSeed()}, StoredSeq: {(inventoryNodeData.storedSequence != null ? (inventoryNodeData.storedSequence.nodes != null ? inventoryNodeData.storedSequence.nodes.Count.ToString() + " nodes" : "graph exists") : "null")}");
        }
        return true;
    }

    if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add '{geneDef.displayName}', inventory full or no suitable cell found.");
    return false;
}
    
    public InventoryBarItem GetItemAtIndex(int index)
    {
        if (index >= 0 && index < inventoryCells.Count)
        {
            var cell = inventoryCells[index];
            if (cell.HasNode())
            {
                var toolDef = cell.GetToolDefinition();
                if (toolDef != null)
                {
                    // For tools, NodeView might be null if a dedicated ToolView prefab was used and that GO is passed.
                    // Or it might be the NodeView if it was converted.
                    GameObject viewObj = cell.GetNodeView()?.gameObject; 
                    if (viewObj == null) // Check if it was a ToolView directly on the cell's child
                    {
                        ToolView tv = cell.transform.GetComponentInChildren<ToolView>();
                        if (tv != null) viewObj = tv.gameObject;
                    }
                    return InventoryBarItem.FromTool(toolDef, viewObj);
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

        geneDataToReturn.orderIndex = -1;
        geneDataToReturn.canBeDeleted = false; // Typically false for items returned to inventory

        // If it's a seed being returned, its storedSequence should be preserved.
        // EnsureSeedSequenceInitialized will create it if it was somehow nulled and it IS a seed.
        geneDataToReturn.EnsureSeedSequenceInitialized();

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
        inventoryCell.ClearNodeReference(); // This will make the cell available again
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
            if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Pre-condition fail. Resetting.");
            draggedDraggable?.ResetPosition(); return;
        }

        NodeView draggedNodeView = draggedDraggable.GetComponent<NodeView>();
        ToolView draggedToolView = draggedDraggable.GetComponent<ToolView>();
        NodeData draggedData = null;
        NodeDefinition draggedNodeDef = null; // For nodes from sequence editor
        ToolDefinition draggedToolDef = null;   // For tools

        if (draggedNodeView != null)
        {
            draggedData = draggedNodeView.GetNodeData();
            draggedNodeDef = draggedNodeView.GetNodeDefinition(); // This is the definition if it came from inventory/sequence
        }
        else if (draggedToolView != null)
        {
            draggedData = draggedToolView.GetNodeData(); // This is the NodeData wrapper for the tool
            draggedToolDef = draggedToolView.GetToolDefinition();
        }

        if (draggedData == null || (draggedNodeDef == null && draggedToolDef == null && !(originalCell.IsInventoryCell && draggedNodeView != null))) {
             Debug.LogError($"[InventoryGridController] HandleDrop: Dragged object missing critical data. Resetting.", draggedDraggable.gameObject);
             draggedDraggable.ResetPosition(); return;
        }


        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Attempting drop. Original Cell - IsInv: {originalCell.IsInventoryCell}, IsSeedSlot: {originalCell.IsSeedSlot}. Target Cell: {targetInventoryCell.CellIndex} (HasNode: {targetInventoryCell.HasNode()})");

        // --- Case 1: Node dragged FROM SEED SLOT TO INVENTORY ---
        if (!originalCell.IsInventoryCell && originalCell.IsSeedSlot)
        {
            if (draggedNodeView == null || !draggedData.IsSeed()) { // Must be a NodeView and a Seed
                 draggedDraggable.ResetPosition(); return;
            }
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: SeedSlot -> Inventory. Seed: '{draggedData.nodeDisplayName}'");

            NodeCell actualTargetInvCell = targetInventoryCell;
            if (actualTargetInvCell.HasNode()) 
            {
                if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target Inv Cell {actualTargetInvCell.CellIndex} is occupied. Finding empty slot.");
                actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            }

            if (actualTargetInvCell == null) { 
                if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Inventory Full. Resetting drag to original seed slot.");
                draggedDraggable.ResetPosition(); 
                return;
            }
            
            NodeEditorGridController.Instance.UnloadSeedFromSlot(); // Clears sequence editor
            originalCell.ClearNodeReference(); // Clear the seed slot cell

            // draggedData is the seed. Ensure its sequence is initialized if it's a seed.
            draggedData.EnsureSeedSequenceInitialized(); 

            actualTargetInvCell.AssignNodeView(draggedNodeView, draggedData); 
            draggedDraggable.SnapToCell(actualTargetInvCell); 

            if (logInventoryChanges) Debug.Log($"[Inventory] Returned Seed '{draggedData.nodeDisplayName}' from Seed Slot to inv cell {actualTargetInvCell.CellIndex}. StoredSeqCount: {draggedData.storedSequence?.nodes?.Count ?? 0}");
        }
        // --- Case 2: Node dragged FROM SEQUENCE EDITOR TO INVENTORY ---
        else if (!originalCell.IsInventoryCell && !originalCell.IsSeedSlot) // From sequence editor
        {
            if (draggedNodeView == null || draggedNodeDef == null) { // Must be a NodeView with a Definition
                 draggedDraggable.ResetPosition(); return;
            }
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: SequenceEditor -> Inventory. Gene: '{draggedNodeDef.displayName}'");
            
            NodeCell actualTargetInvCell = targetInventoryCell;
            if (actualTargetInvCell.HasNode()) {
                if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target InvCell {actualTargetInvCell.CellIndex} is occupied. Finding empty slot.");
                actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            }
            
            if (actualTargetInvCell == null) {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Inventory Full. Resetting drag.");
                draggedDraggable.ResetPosition();
                return;
            }
            
            NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
            NodeEditorGridController.Instance?.RefreshGraphAndUpdateSeed();

            // Add a *new* instance of this gene to inventory using its definition.
            // The draggedDraggable represents the UI element from the sequence, which will be destroyed.
            AddGeneToInventoryFromDefinition(draggedNodeDef, actualTargetInvCell); 
            Destroy(draggedDraggable.gameObject); // Destroy the original view from sequence editor

            if (logInventoryChanges) Debug.Log($"[Inventory] Moved '{draggedNodeDef.displayName}' from seq cell {originalCell.CellIndex} to inv cell {actualTargetInvCell.CellIndex}.");
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
                
                // Get what's in the target cell
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                NodeView viewInTargetCell = targetInventoryCell.GetNodeView();
                ToolView toolViewInTargetCell = null;
                ToolDefinition toolDefInTargetCell = targetInventoryCell.GetToolDefinition();

                if (viewInTargetCell == null && toolDefInTargetCell != null) { // It's a tool in target
                    toolViewInTargetCell = targetInventoryCell.transform.GetComponentInChildren<ToolView>();
                }

                originalCell.ClearNodeReference(); // Clear original before assigning target's item

                if (toolViewInTargetCell != null && toolDefInTargetCell != null) { // Moving tool from target to original
                    originalCell.AssignToolView(toolViewInTargetCell.gameObject, dataInTargetCell, toolDefInTargetCell);
                    toolViewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                } else if (viewInTargetCell != null) { // Moving node from target to original
                    originalCell.AssignNodeView(viewInTargetCell, dataInTargetCell);
                    viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                }
                targetInventoryCell.ClearNodeReference(); 
            } else { 
                if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Moving to empty target cell {targetInventoryCell.CellIndex}.");
                originalCell.ClearNodeReference(); 
            }
            
            // Assign the dragged item to the target cell
            if (draggedToolView != null && draggedToolDef != null) { // Dragged item is a tool
                targetInventoryCell.AssignToolView(draggedToolView.gameObject, draggedData, draggedToolDef);
            } else if (draggedNodeView != null) { // Dragged item is a node
                // Ensure seed data's sequence is initialized if it's a seed
                draggedData.EnsureSeedSequenceInitialized();
                targetInventoryCell.AssignNodeView(draggedNodeView, draggedData);
            }
            draggedDraggable.SnapToCell(targetInventoryCell);
            if (logInventoryChanges) Debug.Log($"[Inventory] Moved inv item '{draggedData.nodeDisplayName}' from cell {originalCell.CellIndex} to cell {targetInventoryCell.CellIndex}.");
        }
        else
        {
            Debug.LogWarning($"[InventoryGridController HandleDrop] Unhandled drop scenario. Resetting.", gameObject);
            draggedDraggable.ResetPosition();
        }
    }

    public NodeCell GetInventoryCellAtIndex(int index)
    {
        if (index >= 0 && index < inventoryCells.Count) return inventoryCells[index];
        Debug.LogWarning($"[InventoryGridController] GetInventoryCellAtIndex: Index {index} out of bounds (0-{inventoryCells.Count-1})");
        return null;
    }
}
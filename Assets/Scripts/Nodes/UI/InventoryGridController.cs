// FILE: Assets/Scripts/Nodes/UI/InventoryGridController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; private set; }

    [Header("Grid Layout & Appearance")]
    [SerializeField][Min(1)] private int inventoryRows = 2;
    [SerializeField][Min(1)] private int inventoryColumns = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Cell Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;

    [Header("Node Visuals (Shared or Specific)")]
    [SerializeField] private GameObject nodeViewPrefab;
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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (cellContainer == null) Debug.LogError("[InventoryGridController] Cell Container not assigned!", gameObject);
        if (nodeViewPrefab == null) Debug.LogError("[InventoryGridController] Node View Prefab not assigned!", gameObject);
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
        // ... (no changes)
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
        for (int i = 0; i < totalCells; i++)
        {
            GameObject cellGO = new GameObject($"InventoryCell_{i}", typeof(RectTransform)); // Ensure RectTransform
            cellGO.transform.SetParent(cellContainer, false);
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage); 
            inventoryCells.Add(cellLogic);
        }
    }

    private void PopulateInitialGenesFromLibrary()
    {
        // ... (no changes)
        if (NodeEditorGridController.Instance == null || NodeEditorGridController.Instance.DefinitionLibrary == null) {
            Debug.LogWarning("[InventoryGridController] Cannot populate: NodeEditorGridController or DefinitionLibrary not ready.");
            return;
        }
        NodeDefinitionLibrary lib = NodeEditorGridController.Instance.DefinitionLibrary;
        if (lib.definitions != null) {
            var definitionsToAdd = lib.definitions.Where(d => d != null).Take(inventoryRows * inventoryColumns).ToList();
            foreach (var def in definitionsToAdd) AddGeneToInventoryFromDefinition(def); 
            if (definitionsToAdd.Count == 0 && lib.definitions.Count > 0)
                 Debug.LogWarning("[InventoryGridController] No valid definitions in library for initial population.");
        }
    }

    public bool AddGeneToInventoryFromDefinition(NodeDefinition geneDef)
    {
        // ... (ensure NodeData.storedSequence is new for all, especially seeds)
        if (geneDef == null) return false;
        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
        if (emptyCell != null)
        {
            NodeData inventoryNodeData = new NodeData() {
                nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
                nodeDisplayName = geneDef.displayName,
                effects = geneDef.CloneEffects(),
                orderIndex = -1,
                canBeDeleted = false,
                storedSequence = new NodeGraph() // All items (including seeds) in inventory start with a fresh/empty stored sequence.
                                                // If you want seeds to have default sequences, populate here based on geneDef.
            };

            GameObject nodeViewGO = Instantiate(nodeViewPrefab, emptyCell.transform);
            NodeView view = nodeViewGO.GetComponent<NodeView>();
            if (view == null) { Destroy(nodeViewGO); return false; }
            
            view.Initialize(inventoryNodeData, geneDef, null);

            NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
            draggable.Initialize(invCtrl: this, startingCell: emptyCell);

            emptyCell.AssignNodeView(view, inventoryNodeData);
            if (logInventoryChanges) Debug.Log($"[Inventory] Added '{geneDef.displayName}' to inv cell {emptyCell.CellIndex}. IsSeed: {inventoryNodeData.IsSeed()}");
            return true;
        }
        if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add '{geneDef.displayName}', inventory full.");
        return false;
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
        geneDataToReturn.canBeDeleted = false; // Items in inventory are generally not deletable this way.

        emptyCell.AssignNodeView(geneViewToReturn, geneDataToReturn); // Assigns the existing NodeView and NodeData

        NodeDraggable draggable = geneViewToReturn.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = geneViewToReturn.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: emptyCell);
        draggable.SnapToCell(emptyCell);

        if (logInventoryChanges) Debug.Log($"[Inventory] Returned '{geneDataToReturn.nodeDisplayName}' to inv cell {emptyCell.CellIndex}. IsSeed: {geneDataToReturn.IsSeed()}, StoredSeqCount: {geneDataToReturn.storedSequence?.nodes?.Count ?? 0}");
    }

    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        // ... (no changes)
        if (inventoryCell == null || !inventoryCell.HasNode() || !inventoryCell.IsInventoryCell) return;
        NodeData removedData = inventoryCell.GetNodeData();
        if (removedData != null) {
            if (logInventoryChanges) Debug.Log($"[Inventory] Dragged out '{removedData.nodeDisplayName}' from inv cell {inventoryCell.CellIndex}");
        }
        inventoryCell.ClearNodeReference();
    }

    public NodeCell FindInventoryCellAtScreenPosition(Vector2 screenPosition)
    {
        // ... (no changes)
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
        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeData draggedData = draggedView?.GetNodeData();
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition();

        if (draggedView == null || draggedData == null || draggedDef == null) {
            Debug.LogError($"[InventoryGridController] HandleDrop: Dragged object missing View/Data/Definition! DraggedView: {draggedView}, DraggedData: {draggedData}, DraggedDef: {draggedDef}. Resetting.", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition(); return;
        }

        if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Attempting drop. Original Cell - IsInv: {originalCell.IsInventoryCell}, IsSeedSlot: {originalCell.IsSeedSlot}. Target Cell: {targetInventoryCell.CellIndex}");

        // --- Case 1: Node dragged FROM SEED SLOT TO INVENTORY ---
        if (!originalCell.IsInventoryCell && originalCell.IsSeedSlot)
        {
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: SeedSlot -> Inventory. Seed: '{draggedData.nodeDisplayName}'");

            NodeCell actualTargetInvCell = targetInventoryCell;
            // If target inventory cell is occupied AND it's not the same as the original cell (which it won't be if original is seed slot)
            // try to find an empty one.
            if (actualTargetInvCell.HasNode()) 
            {
                if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target Inv Cell {actualTargetInvCell.CellIndex} is occupied. Finding empty slot.");
                actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            }

            if (actualTargetInvCell == null) { // No empty inventory slot found
                if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Inventory Full. Cannot return Seed '{draggedData.nodeDisplayName}'. Resetting drag to original seed slot.");
                draggedDraggable.ResetPosition(); // Reset to original cell (the seed slot)
                return;
            }
            
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Target inventory cell for seed is: {actualTargetInvCell.CellIndex}. Unloading from Node Editor.");

            // Unload from seed slot (this clears the editor UI, doesn't touch seed slot's content yet)
            NodeEditorGridController.Instance.UnloadSeedFromSlot();
            
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Clearing original seed slot cell ({originalCell.CellIndex}, IsSeedSlot: {originalCell.IsSeedSlot}).");
            originalCell.ClearNodeReference(); // Original seed slot cell is now visually empty and its NodeData/View refs are null

            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Assigning dragged seed '{draggedData.nodeDisplayName}' (View: {draggedView.gameObject.name}) to inventory cell {actualTargetInvCell.CellIndex}.");
            actualTargetInvCell.AssignNodeView(draggedView, draggedData); // Assigns the existing NodeView and NodeData
            
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Snapping '{draggedData.nodeDisplayName}' to inventory cell {actualTargetInvCell.CellIndex}.");
            draggedDraggable.SnapToCell(actualTargetInvCell); // This reparents and positions the draggedView

            if (logInventoryChanges) Debug.Log($"[Inventory] Returned Seed '{draggedData.nodeDisplayName}' from Seed Slot to inv cell {actualTargetInvCell.CellIndex}. StoredSeqCount: {draggedData.storedSequence?.nodes?.Count ?? 0}");
        }
        // --- Case 2: Node dragged FROM SEQUENCE EDITOR TO INVENTORY ---
        else if (!originalCell.IsInventoryCell && !originalCell.IsSeedSlot)
        {
            if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Path: SequenceEditor -> Inventory. Gene: '{draggedDef.displayName}'");
            NodeCell actualTargetInvCell = targetInventoryCell;
            if (actualTargetInvCell.HasNode()) actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            
            if (actualTargetInvCell == null) {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory HandleDrop] Inventory Full. Cannot move '{draggedDef.displayName}' from sequence. Resetting drag.");
                draggedDraggable.ResetPosition();
                return;
            }
            
            NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
            NodeEditorGridController.Instance?.RefreshGraphAndUpdateSeed();

            AddGeneToInventoryFromDefinition(draggedDef); 
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
                NodeView viewInTargetCell = targetInventoryCell.GetNodeView();
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                
                originalCell.ClearNodeReference(); 
                originalCell.AssignNodeView(viewInTargetCell, dataInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                
                targetInventoryCell.ClearNodeReference(); 
            } else { 
                if (logInventoryChanges) Debug.Log($"[Inventory HandleDrop] Moving to empty target cell {targetInventoryCell.CellIndex}.");
                originalCell.ClearNodeReference(); 
            }
            targetInventoryCell.AssignNodeView(draggedView, draggedData);
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
        // ... (no changes)
        if (index >= 0 && index < inventoryCells.Count) return inventoryCells[index];
        return null;
    }
}
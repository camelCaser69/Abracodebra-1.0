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
    [Tooltip("Prefab for displaying nodes in the inventory. Can be the same as NodeEditorGridController's NodeViewPrefab.")]
    [SerializeField] private GameObject nodeViewPrefab;
    [Tooltip("Uniform scale factor for the gene/item image (thumbnail) within NodeViews. Default is 1.")]
    [SerializeField] private float nodeGlobalImageScale = 1f;
    [Tooltip("Padding to shrink the raycast area of node images. Negative values shrink. Default 0.")]
    [SerializeField] private float nodeImageRaycastPadding = 0f;

    [Header("UI References")]
    [Tooltip("The Transform for inventory cell GameObjects. Should have a GridLayoutGroup.")]
    [SerializeField] private Transform cellContainer;
    [Tooltip("Assign the main UICanvas from your scene.")]
    [SerializeField] private Canvas _rootCanvas;

    [Header("Debugging")]
    [SerializeField] private bool logInventoryChanges = true;

    private List<NodeCell> inventoryCells = new List<NodeCell>();
    // private List<NodeDefinition> availableGenes = new List<NodeDefinition>(); // Less relevant now, inventory holds NodeData

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
            PopulateInitialGenesFromLibrary(); // Renamed from PopulateInitialTestGenes
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
        for (int i = 0; i < totalCells; i++)
        {
            GameObject cellGO = new GameObject($"InventoryCell_{i}");
            cellGO.transform.SetParent(cellContainer, false);
            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;
            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            cellLogic.Init(i, this, cellImage); // Pass this InventoryGridController
            inventoryCells.Add(cellLogic);
        }
    }

    private void PopulateInitialGenesFromLibrary()
    {
        if (NodeEditorGridController.Instance == null || NodeEditorGridController.Instance.DefinitionLibrary == null) {
            Debug.LogWarning("[InventoryGridController] Cannot populate: NodeEditorGridController or DefinitionLibrary not ready.");
            return;
        }
        NodeDefinitionLibrary lib = NodeEditorGridController.Instance.DefinitionLibrary;
        if (lib.definitions != null) {
            var definitionsToAdd = lib.definitions.Where(d => d != null).Take(inventoryRows * inventoryColumns).ToList();
            foreach (var def in definitionsToAdd) AddGeneToInventoryFromDefinition(def); // Use new method
            if (definitionsToAdd.Count == 0 && lib.definitions.Count > 0)
                 Debug.LogWarning("[InventoryGridController] No valid definitions in library for initial population.");
        }
    }

    // <<< MODIFIED: To create NodeData, potentially initializing storedSequence for seeds >>>
    public bool AddGeneToInventoryFromDefinition(NodeDefinition geneDef)
    {
        if (geneDef == null) return false;
        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
        if (emptyCell != null)
        {
            // Create NodeData based on the definition
            NodeData inventoryNodeData = new NodeData() {
                nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
                nodeDisplayName = geneDef.displayName,
                effects = geneDef.CloneEffects(), // Clone effects from definition
                orderIndex = -1, // Not relevant for inventory items directly
                canBeDeleted = false // Inventory items typically aren't deleted this way
            };

            // If this definition is a seed, its NodeData.storedSequence is already new NodeGraph() by default.
            // If seeds should start with a default internal sequence, this is where you'd populate:
            // if (inventoryNodeData.IsSeed() && geneDef.HasDefaultSeedSequence()) {
            //     inventoryNodeData.storedSequence = geneDef.GetClonedDefaultSeedSequence();
            // }

            GameObject nodeViewGO = Instantiate(nodeViewPrefab, emptyCell.transform);
            NodeView view = nodeViewGO.GetComponent<NodeView>();
            if (view == null) { Destroy(nodeViewGO); return false; }
            
            view.Initialize(inventoryNodeData, geneDef, null); // Controller is null for inventory views

            NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
            draggable.Initialize(invCtrl: this, startingCell: emptyCell);

            emptyCell.AssignNodeView(view, inventoryNodeData);
            // availableGenes.Add(geneDef); // No longer directly tracking availableGenes list
            if (logInventoryChanges) Debug.Log($"[Inventory] Added '{geneDef.displayName}' to inv cell {emptyCell.CellIndex}");
            return true;
        }
        if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add '{geneDef.displayName}', inventory full.");
        return false;
    }

    // <<< NEW: To return an existing NodeView (and its NodeData) to inventory, e.g., a seed from the slot >>>
    public void ReturnGeneToInventory(NodeView geneViewToReturn, NodeData geneDataToReturn)
    {
        if (geneViewToReturn == null || geneDataToReturn == null)
        {
            Debug.LogError("[InventoryGridController] ReturnGeneToInventory: geneView or geneData is null.");
            if (geneViewToReturn != null) Destroy(geneViewToReturn.gameObject); // Clean up orphaned view
            return;
        }

        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
        if (emptyCell == null)
        {
            if (logInventoryChanges) Debug.LogWarning($"[Inventory] Full. Cannot return '{geneDataToReturn.nodeDisplayName}'. Destroying item.");
            Destroy(geneViewToReturn.gameObject); // No space, destroy the item
            return;
        }

        // We are returning an existing NodeView with its associated NodeData
        geneDataToReturn.orderIndex = -1; // Reset order index
        geneDataToReturn.canBeDeleted = false; // Inventory items usually not deletable

        emptyCell.AssignNodeView(geneViewToReturn, geneDataToReturn); // Assign the existing view and data

        NodeDraggable draggable = geneViewToReturn.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = geneViewToReturn.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: emptyCell); // Re-initialize draggable for inventory context
        draggable.SnapToCell(emptyCell); // Ensure it's properly parented and positioned

        if (logInventoryChanges) Debug.Log($"[Inventory] Returned '{geneDataToReturn.nodeDisplayName}' to inv cell {emptyCell.CellIndex}. IsSeed: {geneDataToReturn.IsSeed()}, StoredSeqCount: {geneDataToReturn.storedSequence.nodes.Count}");
    }

    // For when a gene is dragged OUT of inventory
    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        if (inventoryCell == null || !inventoryCell.HasNode() || !inventoryCell.IsInventoryCell) return;
        NodeData removedData = inventoryCell.GetNodeData();
        if (removedData != null) {
            if (logInventoryChanges) Debug.Log($"[Inventory] Dragged out '{removedData.nodeDisplayName}' from inv cell {inventoryCell.CellIndex}");
        }
        inventoryCell.ClearNodeReference(); // This just nulls out refs, doesn't destroy the view yet (drag handler does)
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
            draggedDraggable?.ResetPosition(); return;
        }
        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeData draggedData = draggedView?.GetNodeData(); // The NodeData being dragged
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition(); // The definition (template)

        if (draggedView == null || draggedData == null || draggedDef == null) {
            Debug.LogError("[InventoryGridController] HandleDrop: Dragged object missing View/Data/Definition!", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition(); return;
        }

        // --- Case 1: Node dragged FROM SEQUENCE EDITOR (or other non-inventory, non-seed-slot) TO INVENTORY ---
        if (!originalCell.IsInventoryCell && !originalCell.IsSeedSlot)
        {
            // Node from sequence editor is being returned.
            // The NodeData from the sequence editor (draggedData) is effectively discarded.
            // A new representation of the NodeDefinition is created in inventory.
            NodeCell actualTargetInvCell = targetInventoryCell;
            if (actualTargetInvCell.HasNode()) actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            
            if (actualTargetInvCell == null) {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory] Full. Cannot move '{draggedDef.displayName}' from sequence. Resetting drag.");
                draggedDraggable.ResetPosition(); // No space, reset original drag
                return;
            }
            
            // Remove from sequence editor
            NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
            NodeEditorGridController.Instance?.RefreshGraph(); // Update the loaded seed's internal graph

            // Add a new instance of this gene type back to inventory
            // This destroys the draggedDraggable.gameObject as part of creating a new inventory item
            AddGeneToInventoryFromDefinition(draggedDef); 
            Destroy(draggedDraggable.gameObject); // Ensure the dragged view is destroyed

            if (logInventoryChanges) Debug.Log($"[Inventory] Moved '{draggedDef.displayName}' from seq cell {originalCell.CellIndex} to inv cell {actualTargetInvCell.CellIndex}.");
        }
        // --- Case 2: Node dragged FROM SEED SLOT TO INVENTORY ---
        else if (originalCell.IsSeedSlot)
        {
            NodeCell actualTargetInvCell = targetInventoryCell;
            // If target inventory cell is occupied, try to find an empty one
            if (actualTargetInvCell.HasNode() && actualTargetInvCell != originalCell) // Don't swap with self
            {
                 actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            }

            if (actualTargetInvCell == null) { // No empty slot found
                if (logInventoryChanges) Debug.LogWarning($"[Inventory] Full. Cannot return Seed '{draggedData.nodeDisplayName}' to inventory. Resetting drag.");
                draggedDraggable.ResetPosition(); // Reset to seed slot
                return;
            }
            
            // Unload from seed slot
            NodeEditorGridController.Instance.UnloadSeedFromSlot();
            originalCell.ClearNodeReference(); // Original seed slot is now empty

            // Assign the dragged seed (NodeView and its NodeData) to the target inventory cell
            actualTargetInvCell.AssignNodeView(draggedView, draggedData);
            draggedDraggable.SnapToCell(actualTargetInvCell);

            if (logInventoryChanges) Debug.Log($"[Inventory] Returned Seed '{draggedData.nodeDisplayName}' from Seed Slot to inv cell {actualTargetInvCell.CellIndex}. StoredSeqCount: {draggedData.storedSequence.nodes.Count}");
        }
        // --- Case 3: From INVENTORY to INVENTORY (Swapping or Moving) ---
        else if (originalCell.IsInventoryCell)
        {
            if (targetInventoryCell == originalCell) { draggedDraggable.ResetPosition(); return; }

            if (targetInventoryCell.HasNode()) { // Swap
                NodeView viewInTargetCell = targetInventoryCell.GetNodeView();
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                
                originalCell.ClearNodeReference(); // Original cell is about to receive new item
                originalCell.AssignNodeView(viewInTargetCell, dataInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                
                targetInventoryCell.ClearNodeReference(); // Target cell is about to receive dragged item
            } else { // Move to empty
                originalCell.ClearNodeReference(); // Original cell becomes empty
            }
            targetInventoryCell.AssignNodeView(draggedView, draggedData);
            draggedDraggable.SnapToCell(targetInventoryCell);
            if (logInventoryChanges) Debug.Log($"[Inventory] Moved inv item '{draggedData.nodeDisplayName}' from cell {originalCell.CellIndex} to cell {targetInventoryCell.CellIndex}.");
        }
        else // Should not happen
        {
            Debug.LogWarning($"[InventoryGridController] Unhandled drop scenario. Original: Inv({originalCell.IsInventoryCell})/SeedSlot({originalCell.IsSeedSlot}), Target: Inv({targetInventoryCell.IsInventoryCell}). Resetting drag.");
            draggedDraggable.ResetPosition();
        }
    }

    public NodeCell GetInventoryCellAtIndex(int index)
    {
        if (index >= 0 && index < inventoryCells.Count) return inventoryCells[index];
        return null;
    }
}
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
    // [SerializeField] private Vector3 emptyCellScale = Vector3.one; // Note: GridLayoutGroup typically overrides this

    [Header("Node Visuals (Shared or Specific)")]
    [Tooltip("Prefab for displaying nodes in the inventory. Can be the same as NodeEditorGridController's NodeViewPrefab.")]
    [SerializeField] private GameObject nodeViewPrefab;
    [Tooltip("Scale for the image inside the NodeView. Should match NodeEditorGridController if using shared prefabs/styles.")]
    [SerializeField] private Vector3 nodeImageScale = Vector3.one;


    [Header("UI References")]
    [Tooltip("The Transform within the UI Panel where inventory cell GameObjects should be created. Should have a GridLayoutGroup.")]
    [SerializeField] private Transform cellContainer;
    [Tooltip("Assign the main UICanvas from your scene.")]
    [SerializeField] private Canvas _rootCanvas;

    [Header("Debugging")]
    [SerializeField] private bool logInventoryChanges = true;

    private List<NodeCell> inventoryCells = new List<NodeCell>();
    private List<NodeDefinition> availableGenes = new List<NodeDefinition>(); // For tracking actual gene definitions if needed beyond view

    // Public accessors for NodeView or other systems if needed
    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Vector3 NodeImageScale => nodeImageScale;
    public Color EmptyCellColor => emptyCellColor;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
            PopulateInitialTestGenes(); // Example population
        }
    }

    private void CreateInventoryCells()
    {
        // Clear existing cells first
        foreach (Transform child in cellContainer)
        {
            Destroy(child.gameObject);
        }
        inventoryCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("[InventoryGridController] Cell Container MUST have a GridLayoutGroup component for proper layout.", cellContainer.gameObject);
            // Optionally, add one dynamically, but it's better to set up in Inspector
            // gridLayout = cellContainer.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        if (gridLayout != null) // Proceed if GridLayoutGroup exists
        {
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = new Vector2(cellMargin, cellMargin);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = inventoryColumns;
            // Adjust other GridLayoutGroup settings as needed (e.g., childAlignment, padding)
        }


        int totalCells = inventoryRows * inventoryColumns;
        for (int i = 0; i < totalCells; i++)
        {
            GameObject cellGO = new GameObject($"InventoryCell_{i}");
            // RectTransform rt = cellGO.AddComponent<RectTransform>(); // GridLayoutGroup handles size
            cellGO.transform.SetParent(cellContainer, false);
            // rt.localScale = Vector3.one; // Default scale

            Image cellImage = cellGO.AddComponent<Image>();
            cellImage.sprite = emptyCellSprite;
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;

            NodeCell cellLogic = cellGO.AddComponent<NodeCell>();
            // Explicitly use the InventoryGridController overload for NodeCell.Init
            cellLogic.Init(i, this, cellImage);
            inventoryCells.Add(cellLogic);
        }
    }

    private void PopulateInitialTestGenes()
    {
        // Ensure NodeEditorGridController.Instance is available if relying on its library
        if (NodeEditorGridController.Instance == null || NodeEditorGridController.Instance.DefinitionLibrary == null)
        {
            Debug.LogWarning("[InventoryGridController] Cannot populate initial test genes: NodeEditorGridController or its DefinitionLibrary not ready.");
            return;
        }

        NodeDefinitionLibrary lib = NodeEditorGridController.Instance.DefinitionLibrary;
        if (lib.definitions != null)
        {
            // Example: Add first few unique definitions found. Null checks added.
            var definitionsToAdd = lib.definitions.Where(d => d != null).Take(inventoryRows * inventoryColumns).ToList();
            foreach (var def in definitionsToAdd)
            {
                AddGeneToInventory(def);
            }
            if (definitionsToAdd.Count == 0 && lib.definitions.Count > 0)
            {
                 Debug.LogWarning("[InventoryGridController] No valid (non-null) definitions found in library to populate test genes.");
            }
        }
    }

    public bool AddGeneToInventory(NodeDefinition geneDef)
    {
        if (geneDef == null) return false;

        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
        if (emptyCell != null)
        {
            NodeData inventoryNodeData = new NodeData()
            {
                nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
                nodeDisplayName = geneDef.displayName,
                effects = new List<NodeEffectData>(), // Inventory view might not show effects
                orderIndex = -1, // Not part of the sequence
                canBeDeleted = false // Inventory items are "moved" not "deleted" by DEL key
            };

            GameObject nodeViewGO = Instantiate(nodeViewPrefab, emptyCell.transform);
            NodeView view = nodeViewGO.GetComponent<NodeView>();
            if (view == null) {
                Debug.LogError($"[InventoryGridController] NodeViewPrefab '{nodeViewPrefab.name}' is missing NodeView component.", nodeViewPrefab);
                Destroy(nodeViewGO);
                return false;
            }
            // Initialize NodeView with null for NodeEditorGridController, as it's an inventory item
            view.Initialize(inventoryNodeData, geneDef, null);

            NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
            // Use the specific Initialize overload for InventoryGridController
            draggable.Initialize(invCtrl: this, startingCell: emptyCell);

            emptyCell.AssignNodeView(view, inventoryNodeData);
            availableGenes.Add(geneDef); // Track the actual definition
            if (logInventoryChanges) Debug.Log($"[Inventory] Added '{geneDef.displayName}' to inventory cell {emptyCell.CellIndex}");
            return true;
        }
        if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add '{geneDef.displayName}', inventory is full.");
        return false;
    }

    // Called when a node is dragged FROM the main sequence INTO an inventory cell's area
    public void ReturnGeneToInventory(NodeDefinition geneDef, NodeCell targetInventoryCellHint)
    {
        if (geneDef == null) return;

        NodeCell actualTargetCell = targetInventoryCellHint;

        // If the hint cell is null (e.g. dropped outside) or occupied, find the first available empty cell
        if (actualTargetCell == null || !actualTargetCell.IsInventoryCell || actualTargetCell.HasNode())
        {
            actualTargetCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
            if (actualTargetCell == null) // No empty cell found
            {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory] Cannot return '{geneDef.displayName}', inventory is full.");
                // Caller (NodeEditorGridController) should handle re-adding to sequence if this happens.
                return; // Indicate failure to place in inventory
            }
        }
        
        // Create and place the node view in the chosen inventory cell
        NodeData inventoryNodeData = new NodeData() {
            nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
            nodeDisplayName = geneDef.displayName,
            canBeDeleted = false // Inventory items aren't deleted by DEL key
        };
        GameObject nodeViewGO = Instantiate(nodeViewPrefab, actualTargetCell.transform);
        NodeView view = nodeViewGO.GetComponent<NodeView>();
         if (view == null) {
            Debug.LogError($"[InventoryGridController] NodeViewPrefab '{nodeViewPrefab.name}' is missing NodeView component for ReturnGeneToInventory.", nodeViewPrefab);
            Destroy(nodeViewGO);
            return;
        }
        view.Initialize(inventoryNodeData, geneDef, null); // Null for NodeEditorGridController

        NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: actualTargetCell); // Specific overload

        actualTargetCell.AssignNodeView(view, inventoryNodeData);
        availableGenes.Add(geneDef); // Add back to available pool
        if (logInventoryChanges) Debug.Log($"[Inventory] Returned '{geneDef.displayName}' to inventory cell {actualTargetCell.CellIndex}");
    }

    // Called when a node is dragged FROM an inventory cell (and successfully dropped elsewhere)
    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        if (inventoryCell == null || !inventoryCell.HasNode() || !inventoryCell.IsInventoryCell) return;

        NodeDefinition geneDef = inventoryCell.GetNodeView()?.GetNodeDefinition(); // NodeView should exist
        if (geneDef != null)
        {
            availableGenes.Remove(geneDef); // Or decrement count if tracking quantities
            if (logInventoryChanges) Debug.Log($"[Inventory] Dragged out '{geneDef.displayName}' from inventory cell {inventoryCell.CellIndex}");
        }
        inventoryCell.RemoveNode(); // Clears the view and data from the cell, makes it empty
    }

    public NodeCell FindInventoryCellAtScreenPosition(Vector2 screenPosition)
    {
        if (_rootCanvas == null) return null;
        foreach (NodeCell cell in inventoryCells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition, _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera))
            {
                return cell;
            }
        }
        return null;
    }

    public void HandleDropOnInventoryCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetInventoryCell)
    {
        if (draggedDraggable == null || originalCell == null || targetInventoryCell == null || !targetInventoryCell.IsInventoryCell)
        {
            draggedDraggable?.ResetPosition();
            return;
        }

        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition();

        if (draggedView == null || draggedDef == null)
        {
            Debug.LogError("[InventoryGridController] HandleDropOnInventoryCell: Dragged object missing NodeView or Definition!", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition();
            return;
        }

        // --- Scenario 1: Dragged from SEQUENCE to INVENTORY ---
        if (!originalCell.IsInventoryCell)
        {
            // A. First, try to place in inventory. Determine actual target cell (could be targetInventoryCell or first empty).
            NodeCell actualTargetInvCell = targetInventoryCell;
            if (actualTargetInvCell.HasNode()) // If the hint target is occupied
            {
                actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode()); // Find any empty cell
            }

            if (actualTargetInvCell == null) // Inventory is truly full
            {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory] Inventory full. Cannot move '{draggedDef.displayName}' from sequence. Resetting drag.");
                draggedDraggable.ResetPosition(); // Put it back to its original sequence slot
                // No change to sequence graph or inventory needed.
                return;
            }

            // B. Inventory has space. Remove from sequence FIRST.
            NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
            NodeEditorGridController.Instance?.RefreshGraph();

            // C. Add to inventory. This creates a NEW NodeView instance in the inventory.
            ReturnGeneToInventory(draggedDef, actualTargetInvCell);

            // D. The original GameObject (draggedDraggable.gameObject) that was part of the sequence is no longer needed.
            Destroy(draggedDraggable.gameObject);
             if (logInventoryChanges) Debug.Log($"[Inventory] Moved '{draggedDef.displayName}' from sequence cell {originalCell.CellIndex} to inventory cell {actualTargetInvCell.CellIndex}.");
        }
        // --- Scenario 2: Dragged from INVENTORY to INVENTORY (Re-ordering) ---
        else // originalCell.IsInventoryCell must be true
        {
            if (targetInventoryCell == originalCell)
            {
                draggedDraggable.ResetPosition(); // Dropped on itself, no change
                return;
            }

            // Get the "dummy" NodeData from the item being dragged
            NodeData draggedInventoryItemData = draggedView.GetNodeData();

            if (targetInventoryCell.HasNode()) // Target inventory cell is OCCUPIED, so SWAP
            {
                NodeView viewInTargetCell = targetInventoryCell.GetNodeView();
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData(); // Dummy data of item in target

                // 1. Clear original cell (where drag started), makes it ready for the item from target
                originalCell.ClearNodeReference(); // availableGenes count is not changed by reordering

                // 2. Move item from target cell TO original cell
                originalCell.AssignNodeView(viewInTargetCell, dataInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell); // Update draggable's cell

                // 3. Clear target cell (where drag ended), makes it ready for dragged item
                targetInventoryCell.ClearNodeReference();
            }
            else // Target inventory cell is EMPTY
            {
                // Just clear the original cell as the item is moving out
                originalCell.ClearNodeReference();
            }

            // Place the dragged item (which was originally from inventory) into the target inventory cell
            targetInventoryCell.AssignNodeView(draggedView, draggedInventoryItemData);
            draggedDraggable.SnapToCell(targetInventoryCell); // Update draggable's cell

            if (logInventoryChanges) Debug.Log($"[Inventory] Moved inventory item '{draggedDef.displayName}' from cell {originalCell.CellIndex} to cell {targetInventoryCell.CellIndex}.");
        }
    }

    public NodeCell GetInventoryCellAtIndex(int index)
    {
        if (index >= 0 && index < inventoryCells.Count)
        {
            return inventoryCells[index];
        }
        return null;
    }
}
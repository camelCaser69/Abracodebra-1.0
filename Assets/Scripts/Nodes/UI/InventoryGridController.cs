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

    // --- MODIFIED: Scale is now a single float for uniform scaling ---
    [Tooltip("Uniform scale factor for the gene/item image (thumbnail) within NodeViews. Default is 1 (no change). Applies to both X and Y axes.")]
    [SerializeField] private float nodeGlobalImageScale = 1f; // Default to no scaling
    // --- END MODIFIED ---

    // --- NEW FIELD FOR RAYCAST PADDING ---
    [Tooltip("Padding to shrink the raycast area of node images (thumbnails and backgrounds) to prevent overlap with adjacent cells when scaled up. Negative values shrink. Default 0.")]
    [SerializeField] private float nodeImageRaycastPadding = 0f; // Default to no padding (shrinking)
    // --- END NEW FIELD ---

    [Header("UI References")]
    [Tooltip("The Transform within the UI Panel where inventory cell GameObjects should be created. Should have a GridLayoutGroup.")]
    [SerializeField] private Transform cellContainer;
    [Tooltip("Assign the main UICanvas from your scene.")]
    [SerializeField] private Canvas _rootCanvas;

    [Header("Debugging")]
    [SerializeField] private bool logInventoryChanges = true;

    private List<NodeCell> inventoryCells = new List<NodeCell>();
    private List<NodeDefinition> availableGenes = new List<NodeDefinition>();

    // Public accessors
    public GameObject NodeViewPrefab => nodeViewPrefab;
    public Color EmptyCellColor => emptyCellColor;
    public float NodeGlobalImageScale => nodeGlobalImageScale; // --- MODIFIED PUBLIC ACCESSOR ---
    public float NodeImageRaycastPadding => nodeImageRaycastPadding; // --- NEW PUBLIC ACCESSOR ---

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
            PopulateInitialTestGenes();
        }
    }

    private void CreateInventoryCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        inventoryCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("[InventoryGridController] Cell Container MUST have a GridLayoutGroup component.", cellContainer.gameObject);
            return; // Stop if no layout group
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
            cellLogic.Init(i, this, cellImage);
            inventoryCells.Add(cellLogic);
        }
    }

    private void PopulateInitialTestGenes()
    {
        if (NodeEditorGridController.Instance == null || NodeEditorGridController.Instance.DefinitionLibrary == null)
        {
            Debug.LogWarning("[InventoryGridController] Cannot populate initial test genes: NodeEditorGridController or its DefinitionLibrary not ready.");
            return;
        }
        NodeDefinitionLibrary lib = NodeEditorGridController.Instance.DefinitionLibrary;
        if (lib.definitions != null)
        {
            var definitionsToAdd = lib.definitions.Where(d => d != null).Take(inventoryRows * inventoryColumns).ToList();
            foreach (var def in definitionsToAdd) AddGeneToInventory(def);
            if (definitionsToAdd.Count == 0 && lib.definitions.Count > 0)
                 Debug.LogWarning("[InventoryGridController] No valid (non-null) definitions found in library to populate test genes.");
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
                effects = new List<NodeEffectData>(),
                orderIndex = -1,
                canBeDeleted = false
            };

            GameObject nodeViewGO = Instantiate(nodeViewPrefab, emptyCell.transform);
            NodeView view = nodeViewGO.GetComponent<NodeView>();
            if (view == null) { /* ... error handling ... */ Destroy(nodeViewGO); return false; }
            
            // Initialize NodeView: controller is null as it's an inventory item.
            // NodeView's Initialize method will handle fetching the correct image scale.
            view.Initialize(inventoryNodeData, geneDef, null); 

            NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
            draggable.Initialize(invCtrl: this, startingCell: emptyCell);

            emptyCell.AssignNodeView(view, inventoryNodeData);
            availableGenes.Add(geneDef);
            if (logInventoryChanges) Debug.Log($"[Inventory] Added '{geneDef.displayName}' to inventory cell {emptyCell.CellIndex}");
            return true;
        }
        if (logInventoryChanges) Debug.LogWarning($"[Inventory] Could not add '{geneDef.displayName}', inventory is full.");
        return false;
    }

    public void ReturnGeneToInventory(NodeDefinition geneDef, NodeCell targetInventoryCellHint)
    {
        if (geneDef == null) return;
        NodeCell actualTargetCell = targetInventoryCellHint;
        if (actualTargetCell == null || !actualTargetCell.IsInventoryCell || actualTargetCell.HasNode())
        {
            actualTargetCell = inventoryCells.FirstOrDefault(cell => !cell.HasNode());
            if (actualTargetCell == null) {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory] Cannot return '{geneDef.displayName}', inventory is full.");
                return;
            }
        }
        
        NodeData inventoryNodeData = new NodeData() { /* ... */ };
        inventoryNodeData.nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString();
        inventoryNodeData.nodeDisplayName = geneDef.displayName;
        inventoryNodeData.canBeDeleted = false;

        GameObject nodeViewGO = Instantiate(nodeViewPrefab, actualTargetCell.transform);
        NodeView view = nodeViewGO.GetComponent<NodeView>();
        if (view == null) { /* ... error handling ... */ Destroy(nodeViewGO); return; }

        view.Initialize(inventoryNodeData, geneDef, null); // Controller is null

        NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(invCtrl: this, startingCell: actualTargetCell);

        actualTargetCell.AssignNodeView(view, inventoryNodeData);
        availableGenes.Add(geneDef);
        if (logInventoryChanges) Debug.Log($"[Inventory] Returned '{geneDef.displayName}' to inventory cell {actualTargetCell.CellIndex}");
    }

    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        if (inventoryCell == null || !inventoryCell.HasNode() || !inventoryCell.IsInventoryCell) return;
        NodeDefinition geneDef = inventoryCell.GetNodeView()?.GetNodeDefinition();
        if (geneDef != null)
        {
            availableGenes.Remove(geneDef);
            if (logInventoryChanges) Debug.Log($"[Inventory] Dragged out '{geneDef.displayName}' from inventory cell {inventoryCell.CellIndex}");
        }
        inventoryCell.RemoveNode();
    }

    public NodeCell FindInventoryCellAtScreenPosition(Vector2 screenPosition)
    {
        if (_rootCanvas == null) return null;
        foreach (NodeCell cell in inventoryCells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(cellRect, screenPosition, _rootCanvas.worldCamera))
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
            draggedDraggable?.ResetPosition(); return;
        }
        NodeView draggedView = draggedDraggable.GetComponent<NodeView>();
        NodeDefinition draggedDef = draggedView?.GetNodeDefinition();
        if (draggedView == null || draggedDef == null)
        {
            Debug.LogError("[InventoryGridController] HandleDrop: Dragged object missing View/Definition!", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition(); return;
        }

        if (!originalCell.IsInventoryCell) // From SEQUENCE to INVENTORY
        {
            NodeCell actualTargetInvCell = targetInventoryCell;
            if (actualTargetInvCell.HasNode()) actualTargetInvCell = inventoryCells.FirstOrDefault(c => !c.HasNode());
            if (actualTargetInvCell == null) {
                if (logInventoryChanges) Debug.LogWarning($"[Inventory] Full. Cannot move '{draggedDef.displayName}' from sequence. Resetting drag.");
                draggedDraggable.ResetPosition(); return;
            }
            NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
            NodeEditorGridController.Instance?.RefreshGraph();
            ReturnGeneToInventory(draggedDef, actualTargetInvCell);
            Destroy(draggedDraggable.gameObject);
            if (logInventoryChanges) Debug.Log($"[Inventory] Moved '{draggedDef.displayName}' from seq cell {originalCell.CellIndex} to inv cell {actualTargetInvCell.CellIndex}.");
        }
        else // From INVENTORY to INVENTORY
        {
            if (targetInventoryCell == originalCell) { draggedDraggable.ResetPosition(); return; }
            NodeData draggedInventoryItemData = draggedView.GetNodeData();
            if (targetInventoryCell.HasNode()) {
                NodeView viewInTargetCell = targetInventoryCell.GetNodeView();
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                originalCell.ClearNodeReference();
                originalCell.AssignNodeView(viewInTargetCell, dataInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                targetInventoryCell.ClearNodeReference();
            } else {
                originalCell.ClearNodeReference();
            }
            targetInventoryCell.AssignNodeView(draggedView, draggedInventoryItemData);
            draggedDraggable.SnapToCell(targetInventoryCell);
            if (logInventoryChanges) Debug.Log($"[Inventory] Moved inv item '{draggedDef.displayName}' from cell {originalCell.CellIndex} to cell {targetInventoryCell.CellIndex}.");
        }
    }

    public NodeCell GetInventoryCellAtIndex(int index)
    {
        if (index >= 0 && index < inventoryCells.Count) return inventoryCells[index];
        return null;
    }
}
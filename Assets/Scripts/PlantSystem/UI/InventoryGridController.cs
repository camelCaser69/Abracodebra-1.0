using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; private set; }

    [Header("Grid Layout")]
    [SerializeField][Min(1)] public int inventoryRows = 2;
    [SerializeField][Min(1)] public int inventoryColumns = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private float cellMargin = 10f;

    [Header("Cell & Item Visuals")]
    [SerializeField] private Sprite emptyCellSprite;
    [SerializeField] private Color emptyCellColor = Color.white;
    [Tooltip("The single, unified prefab with an ItemView component, used for both Nodes and Tools.")]
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private float nodeGlobalImageScale = 1f;
    [SerializeField] private float nodeImageRaycastPadding = 0f;

    [Header("Content")]
    [SerializeField] private ToolDefinition[] availableTools;

    [Header("Required References")]
    [SerializeField] private Transform cellContainer;
    [SerializeField] private Canvas _rootCanvas;

    [Header("Settings")]
    [SerializeField] private bool logInventoryChanges = true;

    private readonly List<NodeCell> inventoryCells = new List<NodeCell>();

    public GameObject InventoryItemPrefab => inventoryItemPrefab;
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
        if (inventoryItemPrefab == null) Debug.LogError("[InventoryGridController] Inventory Item Prefab not assigned!", gameObject);
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

    void CreateInventoryCells()
    {
        foreach (Transform child in cellContainer) Destroy(child.gameObject);
        inventoryCells.Clear();

        GridLayoutGroup gridLayout = cellContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("[InventoryGridController] Cell Container MUST have a GridLayoutGroup.", cellContainer.gameObject);
            return;
        }

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(cellMargin, cellMargin);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = inventoryColumns;

        int totalCells = inventoryRows * inventoryColumns;
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
    }

    void PopulateInitialGenesFromLibrary()
    {
        if (NodeEditorGridController.Instance?.DefinitionLibrary == null)
        {
            Debug.LogWarning("[InventoryGridController] Cannot populate: NodeEditorGridController or DefinitionLibrary not ready.");
            return;
        }

        if (availableTools != null)
        {
            foreach (var tool in availableTools)
            {
                if (tool != null && tool.autoAddToInventory)
                {
                    AddToolToInventory(tool);
                }
            }
        }

        NodeDefinitionLibrary lib = NodeEditorGridController.Instance.DefinitionLibrary;
        if (lib.definitions != null)
        {
            int remainingSlots = TotalSlots - GetUsedSlotCount();
            var definitionsToAdd = lib.definitions.Where(d => d != null).Take(remainingSlots).ToList();
            foreach (var def in definitionsToAdd)
            {
                AddGeneToInventoryFromDefinition(def, null);
            }
        }
    }

    private bool AddToolToInventory(ToolDefinition tool)
    {
        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasItem());
        if (emptyCell == null) return false;

        NodeData toolNodeData = new NodeData
        {
            nodeId = tool.name + "_tool_" + System.Guid.NewGuid().ToString(),
            nodeDisplayName = tool.displayName,
            effects = new List<NodeEffectData>(),
            orderIndex = -1,
            canBeDeleted = false,
            storedSequence = null
        };

        GameObject itemViewGO = Instantiate(inventoryItemPrefab, emptyCell.transform);
        ItemView itemView = itemViewGO.GetComponent<ItemView>();

        if (itemView != null)
        {
            itemView.Initialize(toolNodeData, tool);

            NodeDraggable draggable = itemViewGO.GetComponent<NodeDraggable>() ?? itemViewGO.AddComponent<NodeDraggable>();
            draggable.Initialize(this, emptyCell);
            emptyCell.AssignItemView(itemView, toolNodeData, tool);
            return true;
        }
        
        Destroy(itemViewGO);
        return false;
    }

    public bool AddGeneToInventoryFromDefinition(NodeDefinition geneDef, NodeCell targetCellHint)
    {
        if (geneDef == null) return false;

        NodeCell cellToUse = (targetCellHint != null && targetCellHint.IsInventoryCell && !targetCellHint.HasItem())
            ? targetCellHint
            : inventoryCells.FirstOrDefault(cell => !cell.HasItem());

        if (cellToUse != null)
        {
            NodeData inventoryNodeData = new NodeData
            {
                nodeId = geneDef.name + "_inventory_" + System.Guid.NewGuid().ToString(),
                nodeDisplayName = geneDef.displayName,
                effects = geneDef.CloneEffects(),
                orderIndex = -1,
                canBeDeleted = false,
            };
            inventoryNodeData.EnsureSeedSequenceInitialized();
            inventoryNodeData.CleanForSerialization(0, "InvGridAddGene");

            GameObject itemViewGO = Instantiate(inventoryItemPrefab, cellToUse.transform);
            ItemView view = itemViewGO.GetComponent<ItemView>();
            if (view == null) { Destroy(itemViewGO); return false; }

            view.Initialize(inventoryNodeData, geneDef, null);
            NodeDraggable draggable = view.GetComponent<NodeDraggable>() ?? view.gameObject.AddComponent<NodeDraggable>();
            draggable.Initialize(this, cellToUse);
            cellToUse.AssignItemView(view, inventoryNodeData, null);
            return true;
        }
        return false;
    }

    public void ReturnGeneToInventory(ItemView itemViewToReturn, NodeData geneDataToReturn)
    {
        if (itemViewToReturn == null || geneDataToReturn == null)
        {
            if (itemViewToReturn != null) Destroy(itemViewToReturn.gameObject);
            return;
        }

        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasItem());
        if (emptyCell == null)
        {
            Destroy(itemViewToReturn.gameObject);
            return;
        }

        geneDataToReturn.orderIndex = -1;
        geneDataToReturn.canBeDeleted = false;
        geneDataToReturn.EnsureSeedSequenceInitialized();

        emptyCell.AssignItemView(itemViewToReturn, geneDataToReturn, null);

        NodeDraggable draggable = itemViewToReturn.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = itemViewToReturn.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(this, emptyCell);
        draggable.SnapToCell(emptyCell);
    }

    public void HandleDropOnInventoryCell(NodeDraggable draggedDraggable, NodeCell originalCell, NodeCell targetInventoryCell)
    {
        if (draggedDraggable == null || originalCell == null || targetInventoryCell == null || !targetInventoryCell.IsInventoryCell)
        {
            draggedDraggable?.ResetPosition();
            return;
        }

        ItemView draggedItemView = draggedDraggable.GetComponent<ItemView>();
        if (draggedItemView == null)
        {
            draggedDraggable.ResetPosition();
            return;
        }

        NodeData draggedData = draggedItemView.GetNodeData();
        NodeDefinition draggedNodeDef = draggedItemView.GetNodeDefinition();
        ToolDefinition draggedToolDef = draggedItemView.GetToolDefinition();

        // Case 1: Moving an item within the inventory grid
        if (originalCell.IsInventoryCell)
        {
            if (targetInventoryCell == originalCell) { draggedDraggable.ResetPosition(); return; }

            if (targetInventoryCell.HasItem()) // Swap
            {
                ItemView viewInTargetCell = targetInventoryCell.GetItemView();
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                ToolDefinition toolInTargetCell = targetInventoryCell.GetToolDefinition();

                originalCell.ClearNodeReference();
                originalCell.AssignItemView(viewInTargetCell, dataInTargetCell, toolInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                targetInventoryCell.ClearNodeReference();
            }
            else // Move to empty
            {
                originalCell.ClearNodeReference();
            }

            targetInventoryCell.AssignItemView(draggedItemView, draggedData, draggedToolDef);
            draggedDraggable.SnapToCell(targetInventoryCell);
        }
        // Case 2: Moving an item from the editor (sequence or seed slot) to inventory
        else
        {
            NodeCell actualTargetInvCell = targetInventoryCell.HasItem()
                ? inventoryCells.FirstOrDefault(c => !c.HasItem())
                : targetInventoryCell;

            if (actualTargetInvCell == null) { draggedDraggable.ResetPosition(); return; } // Inventory full

            // From seed slot
            if (originalCell.IsSeedSlot)
            {
                 NodeEditorGridController.Instance.UnloadSeedFromSlot();
                 originalCell.ClearNodeReference();
                 actualTargetInvCell.AssignItemView(draggedItemView, draggedData, null);
                 draggedDraggable.SnapToCell(actualTargetInvCell);
            }
            // From sequence editor cell
            else
            {
                NodeEditorGridController.Instance?.GetCellAtIndex(originalCell.CellIndex)?.RemoveNode();
                NodeEditorGridController.Instance?.RefreshGraphAndUpdateSeed();
                // Create a new item in inventory from the definition
                AddGeneToInventoryFromDefinition(draggedNodeDef, actualTargetInvCell);
                // The dragged item was a temporary copy, so destroy it
                Destroy(draggedDraggable.gameObject);
            }
        }
    }

    public int GetUsedSlotCount() => inventoryCells.Count(cell => cell.HasItem());
    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        if (inventoryCell != null && inventoryCell.HasItem() && inventoryCell.IsInventoryCell)
        {
            inventoryCell.ClearNodeReference();
        }
    }
    public NodeCell GetInventoryCellAtIndex(int index) => (index >= 0 && index < inventoryCells.Count) ? inventoryCells[index] : null;
    public NodeCell FindInventoryCellAtScreenPosition(Vector2 screenPosition)
    {
        if (_rootCanvas == null) return null;
        foreach (NodeCell cell in inventoryCells)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(cell.GetComponent<RectTransform>(), screenPosition, _rootCanvas.worldCamera))
            {
                return cell;
            }
        }
        return null;
    }
    public InventoryBarItem GetItemAtIndex(int index)
    {
        if (index < 0 || index >= inventoryCells.Count) return null;
        
        var cell = inventoryCells[index];
        if (cell.HasItem())
        {
            var itemView = cell.GetItemView();
            var toolDef = cell.GetToolDefinition();
            var nodeDef = cell.GetNodeDefinition();
            var nodeData = cell.GetNodeData();

            if (toolDef != null)
            {
                return InventoryBarItem.FromTool(toolDef, itemView?.gameObject);
            }
            if (nodeDef != null && nodeData != null)
            {
                return InventoryBarItem.FromNode(nodeData, nodeDef, itemView?.gameObject);
            }
        }
        return null;
    }
}
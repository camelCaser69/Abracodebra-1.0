// Assets/Scripts/PlantSystem/UI/InventoryGridController.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class InventoryGridController : MonoBehaviour
{
    public static InventoryGridController Instance { get; set; }

    [Header("Grid Layout")]
    [SerializeField][Min(1)] public int inventoryRows = 2;
    [SerializeField][Min(1)] public int inventoryColumns = 8;
    [SerializeField] Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] float cellMargin = 10f;

    [Header("Visuals & Prefabs")]
    [SerializeField] Sprite emptyCellSprite;
    [SerializeField] Color emptyCellColor = Color.white;
    [SerializeField] GameObject inventoryItemPrefab;
    [SerializeField] float nodeGlobalImageScale = 1f;
    [SerializeField] float nodeImageRaycastPadding = 0f;

    [Header("Initial Items")]
    [SerializeField] ToolDefinition[] availableTools;

    [Header("References")]
    [SerializeField] Transform cellContainer;
    [SerializeField] Canvas _rootCanvas;

    [Header("Debugging")]
    [SerializeField] bool logInventoryChanges = true;

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

    bool AddToolToInventory(ToolDefinition tool)
    {
        NodeCell emptyCell = inventoryCells.FirstOrDefault(cell => !cell.HasItem());
        if (emptyCell == null) return false;

        NodeData toolNodeData = new NodeData
        {
            nodeId = tool.name + "_tool_" + System.Guid.NewGuid().ToString(),
            definitionName = tool.name,
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
            if (logInventoryChanges) Debug.Log($"[Inventory] Added tool '{tool.displayName}' to cell {emptyCell.CellIndex}.");
            return true;
        }

        Destroy(itemViewGO);
        return false;
    }

    public bool AddGeneToInventoryFromDefinition(NodeDefinition geneDef, NodeCell targetCellHint = null)
    {
        if (geneDef == null) return false;

        NodeCell cellToUse = (targetCellHint != null && targetCellHint.IsInventoryCell && !targetCellHint.HasItem())
            ? targetCellHint
            : inventoryCells.FirstOrDefault(cell => !cell.HasItem());

        if (cellToUse == null) return false;

        NodeData inventoryNode = new NodeData
        {
            nodeId = geneDef.name + "_inventory_" + Guid.NewGuid().ToString(),
            definitionName = geneDef.name,
            nodeDisplayName = geneDef.displayName,
            effects = geneDef.CloneEffects(),
            orderIndex = -1,
            canBeDeleted = false
        };

        if (inventoryNode.IsSeed())
        {
            inventoryNode.EnsureSeedSequenceInitialized();
        }

        GameObject itemViewGO = Instantiate(inventoryItemPrefab, cellToUse.transform);
        ItemView view = itemViewGO.GetComponent<ItemView>();
        if (view == null)
        {
            Destroy(itemViewGO);
            return false;
        }

        view.Initialize(inventoryNode, geneDef, null);

        NodeDraggable draggable = view.GetComponent<NodeDraggable>();
        if (draggable == null)
        {
            draggable = view.gameObject.AddComponent<NodeDraggable>();
        }
        draggable.Initialize(this, cellToUse);

        cellToUse.AssignItemView(view, inventoryNode, null);

        if (logInventoryChanges)
        {
            Debug.Log($"[Inventory] Added gene '{geneDef.displayName}' (seed: {inventoryNode.IsSeed()}) to cell {cellToUse.CellIndex}");
        }

        return true;
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
            if (logInventoryChanges)
            {
                Debug.LogWarning($"[Inventory] No empty cell to return '{geneDataToReturn.nodeDisplayName}' to. Item destroyed.");
            }
            Destroy(itemViewToReturn.gameObject);
            return;
        }

        geneDataToReturn.orderIndex = -1;
        geneDataToReturn.canBeDeleted = false;

        if (geneDataToReturn.IsSeed())
        {
            geneDataToReturn.EnsureSeedSequenceInitialized();
        }

        emptyCell.AssignItemView(itemViewToReturn, geneDataToReturn, null);

        NodeDraggable draggable = itemViewToReturn.GetComponent<NodeDraggable>();
        if (draggable == null)
        {
            draggable = itemViewToReturn.gameObject.AddComponent<NodeDraggable>();
        }
        draggable.Initialize(this, emptyCell);
        draggable.SnapToCell(emptyCell);

        if (logInventoryChanges)
        {
            Debug.Log($"[Inventory] Returned gene '{geneDataToReturn.nodeDisplayName}' to cell {emptyCell.CellIndex}");
        }
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
        ToolDefinition draggedToolDef = draggedItemView.GetToolDefinition();

        if (originalCell.IsInventoryCell)
        {
            if (targetInventoryCell == originalCell) { draggedDraggable.ResetPosition(); return; }

            if (targetInventoryCell.HasItem())
            {
                ItemView viewInTargetCell = targetInventoryCell.GetItemView();
                NodeData dataInTargetCell = targetInventoryCell.GetNodeData();
                ToolDefinition toolInTargetCell = targetInventoryCell.GetToolDefinition();

                originalCell.ClearNodeReference();
                originalCell.AssignItemView(viewInTargetCell, dataInTargetCell, toolInTargetCell);
                viewInTargetCell.GetComponent<NodeDraggable>()?.SnapToCell(originalCell);
                targetInventoryCell.ClearNodeReference();
            }
            else
            {
                originalCell.ClearNodeReference();
            }

            targetInventoryCell.AssignItemView(draggedItemView, draggedData, draggedToolDef);
            draggedDraggable.SnapToCell(targetInventoryCell);
        }
        else
        {
            // --- FIX STARTS HERE ---
            if (targetInventoryCell.HasItem())
            {
                // Swapping item from editor with item in inventory
                NodeDefinition definitionInInventory = targetInventoryCell.GetNodeDefinition();
                ToolDefinition toolInInventory = targetInventoryCell.GetToolDefinition();

                if ((definitionInInventory != null && definitionInInventory.effects.Any(e => e.effectType == NodeEffectType.SeedSpawn)) || toolInInventory != null)
                {
                    draggedDraggable.ResetPosition();
                    return;
                }
                
                NodeDefinition definitionFromSequence = draggedItemView.GetNodeDefinition();
                
                originalCell.RemoveNode();
                targetInventoryCell.RemoveNode();
                
                AddGeneToInventoryFromDefinition(definitionFromSequence, targetInventoryCell);
                originalCell.AssignNode(definitionInInventory);
            }
            else 
            {
                // Moving from sequence/seed slot to an EMPTY inventory cell
                if (originalCell.IsSeedSlot)
                {
                    // Important: Refresh to save the sequence data before moving the seed
                    NodeEditorGridController.Instance.RefreshGraphAndUpdateSeed();
                    NodeEditorGridController.Instance.UnloadSeedFromSlot();
                }

                // Detach the dragged view from its original cell without destroying it
                originalCell.ClearNodeReference();

                // Assign the existing ItemView and its data to the new empty inventory cell
                targetInventoryCell.AssignItemView(draggedItemView, draggedData, draggedToolDef);
                
                // Re-initialize the draggable component for its new controller
                draggedDraggable.Initialize(this, targetInventoryCell);
                draggedDraggable.SnapToCell(targetInventoryCell);
            }
            // --- FIX ENDS HERE ---

            NodeEditorGridController.Instance?.RefreshGraphAndUpdateSeed();
        }
    }

    public int GetUsedSlotCount() => inventoryCells.Count(cell => cell.HasItem());

    public void RemoveGeneFromInventory(NodeCell inventoryCell)
    {
        if (inventoryCell != null && inventoryCell.HasItem() && inventoryCell.IsInventoryCell)
        {
            if (logInventoryChanges) Debug.Log($"[Inventory] Removing item '{inventoryCell.GetNodeData()?.nodeDisplayName ?? "Unknown"}' from cell {inventoryCell.CellIndex}.");
            inventoryCell.RemoveNode();
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
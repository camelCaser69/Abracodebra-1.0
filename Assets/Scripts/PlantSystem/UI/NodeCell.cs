using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static NodeCell CurrentlySelectedCell { get; private set; }

    public int CellIndex { get; private set; }
    public bool IsInventoryCell { get; private set; }
    public bool IsSeedSlot { get; private set; }

    private NodeEditorGridController _sequenceController;
    private InventoryGridController _inventoryController;

    private ItemView _itemView;
    private NodeData _nodeData;
    private NodeDefinition _nodeDefinition;
    private ToolDefinition _toolDefinition;

    private Image _backgroundImage;
    private GameObject _displayObject; // For inventory bar display-only items

    public void Init(int index, NodeEditorGridController sequenceController, InventoryGridController inventoryController, Image bgImage)
    {
        CellIndex = index;
        _sequenceController = sequenceController;
        _inventoryController = inventoryController;
        _backgroundImage = bgImage;
        IsInventoryCell = (_inventoryController != null);
        IsSeedSlot = false;

        if (_backgroundImage != null)
        {
            Color emptyColor = Color.gray;
            if (IsInventoryCell && _inventoryController != null) emptyColor = _inventoryController.EmptyCellColor;
            else if (!IsInventoryCell && _sequenceController != null) emptyColor = _sequenceController.EmptyCellColor;
            _backgroundImage.color = emptyColor;
        }
    }
    public void Init(int index, NodeEditorGridController sequenceController, Image bgImage) => Init(index, sequenceController, null, bgImage);
    public void Init(int index, InventoryGridController inventoryController, Image bgImage) => Init(index, null, inventoryController, bgImage);
    public void InitAsSeedSlot(NodeEditorGridController sequenceController, Image bgImage)
    {
        CellIndex = -1;
        _sequenceController = sequenceController;
        _inventoryController = null;
        _backgroundImage = bgImage;
        IsInventoryCell = false;
        IsSeedSlot = true;
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _sequenceController != null ? _sequenceController.EmptyCellColor : Color.magenta;
        }
    }
    
    public void UpdateCellBackgroundColor()
    {
        if (_backgroundImage != null && HasItem() && InventoryColorManager.Instance != null)
        {
            Color cellColor = InventoryColorManager.Instance.GetCellColorForItem(_nodeData, _nodeDefinition, _toolDefinition);
            _backgroundImage.color = cellColor;
        }
    }

    public bool HasItem() => _itemView != null || _displayObject != null;
    public NodeData GetNodeData() => _nodeData;
    public NodeDefinition GetNodeDefinition() => _nodeDefinition;
    public ToolDefinition GetToolDefinition() => _toolDefinition;
    public ItemView GetItemView() => _itemView;

    public void AssignItemView(ItemView view, NodeData data, ToolDefinition toolDef)
    {
        RemoveNode();

        _itemView = view;
        _nodeData = data;
        _toolDefinition = toolDef;
        _nodeDefinition = view?.GetNodeDefinition();

        if (_itemView != null)
        {
            _itemView.transform.SetParent(transform, false);
            _itemView.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            if (_nodeData != null && !IsInventoryCell && !IsSeedSlot && _sequenceController != null)
            {
                _nodeData.orderIndex = this.CellIndex;
            }

            if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
        }
    }

    public void AssignNode(NodeDefinition def)
    {
        if (def == null || IsInventoryCell || IsSeedSlot || _sequenceController == null) return;
        if (def.effects.Any(e => e != null && e.effectType == NodeEffectType.SeedSpawn)) return;

        RemoveNode();

        _nodeData = new NodeData
        {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(),
            orderIndex = this.CellIndex,
            canBeDeleted = true
        };
        _nodeData.ClearStoredSequence();
        _nodeDefinition = def;
        _toolDefinition = null;

        // --- FIXED: Access the prefab from the correct controller ---
        GameObject prefabToInstantiate = _sequenceController.InventoryItemPrefab;
        if (prefabToInstantiate == null)
        {
            Debug.LogError($"[NodeCell {CellIndex}] Sequence controller is missing its InventoryItemPrefab!", gameObject);
            return;
        }
        
        GameObject itemViewGO = Instantiate(prefabToInstantiate, transform);
        _itemView = itemViewGO.GetComponent<ItemView>();
        
        if (_itemView == null) { Destroy(itemViewGO); return; }

        _itemView.Initialize(_nodeData, def, _sequenceController);
        
        NodeDraggable draggable = _itemView.GetComponent<NodeDraggable>() ?? _itemView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_sequenceController, this);
        
        if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
    }

    public void RemoveNode()
    {
        if (CurrentlySelectedCell == this) NodeCell.ClearSelection();
        if (_itemView != null) Destroy(_itemView.gameObject);
        if (_displayObject != null) Destroy(_displayObject);
        
        _itemView = null;
        _nodeData = null;
        _nodeDefinition = null;
        _toolDefinition = null;
        _displayObject = null;

        if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
    }

    public void ClearNodeReference()
    {
        _itemView = null;
        _nodeData = null;
        _nodeDefinition = null;
        _toolDefinition = null;
        if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
    }

    public static void SelectCell(NodeCell cellToSelect)
    {
        if (cellToSelect == null || !cellToSelect.HasItem() || cellToSelect.IsInventoryCell || cellToSelect.IsSeedSlot)
        {
            ClearSelection();
            return;
        }
        if (CurrentlySelectedCell == cellToSelect) return;

        ClearSelection();
        CurrentlySelectedCell = cellToSelect;
        CurrentlySelectedCell?.GetItemView()?.Highlight();
    }

    public static void ClearSelection()
    {
        CurrentlySelectedCell?.GetItemView()?.Unhighlight();
        CurrentlySelectedCell = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!HasItem() && !IsInventoryCell && !IsSeedSlot && _sequenceController != null)
            {
                ClearSelection();
                _sequenceController.OnEmptyCellRightClicked(this, eventData);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (HasItem() && !IsInventoryCell && !IsSeedSlot) SelectCell(this);
            else if (!HasItem()) ClearSelection();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        NodeDraggable draggedDraggable = draggedObject.GetComponent<NodeDraggable>();
        if (draggedDraggable == null) return;

        NodeCell originalCell = draggedDraggable.OriginalCell;
        if (originalCell == null || draggedDraggable.GetComponent<ItemView>() == null)
        {
            draggedDraggable.ResetPosition();
            return;
        }

        if (this.IsSeedSlot && _sequenceController != null)
            _sequenceController.HandleDropOnSeedSlot(draggedDraggable, originalCell, this);
        else if (!this.IsInventoryCell && _sequenceController != null)
            _sequenceController.HandleDropOnSequenceCell(draggedDraggable, originalCell, this);
        else if (this.IsInventoryCell && _inventoryController != null)
            _inventoryController.HandleDropOnInventoryCell(draggedDraggable, originalCell, this);
        else
            draggedDraggable.ResetPosition();
    }

    public void AssignDisplayOnly(GameObject displayObject, NodeData data, ToolDefinition toolDef)
    {
        RemoveNode();
        _nodeData = data;
        _toolDefinition = toolDef;
        _displayObject = displayObject;

        if (_displayObject != null)
        {
            _displayObject.transform.SetParent(transform, false);
            _displayObject.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
        }
    }
}
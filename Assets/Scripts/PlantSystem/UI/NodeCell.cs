using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static NodeCell CurrentlySelectedCell { get; private set; }

    public int  CellIndex       { get; private set; }
    public bool IsInventoryCell { get; private set; }
    public bool IsSeedSlot      { get; private set; }

    private NodeEditorGridController _sequenceController;
    private InventoryGridController  _inventoryController;

    private NodeData   _nodeData;
    private NodeView   _nodeView;
    private ToolDefinition _toolDefinition;

    private Image _backgroundImage;
    
    private GameObject _displayObject;   // holds the “display-only” icon used in the inventory bar

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
            _backgroundImage.enabled = true;
            _backgroundImage.raycastTarget = true;
        }
        else Debug.LogWarning($"[NodeCell {CellIndex}] Init: Background Image component is not assigned.", gameObject);
    }

    public void Init(int index, NodeEditorGridController sequenceController, Image bgImage)
    {
        Init(index, sequenceController, null, bgImage);
    }
    public void Init(int index, InventoryGridController inventoryController, Image bgImage)
    {
        Init(index, null, inventoryController, bgImage);
    }

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
            _backgroundImage.enabled = true;
            _backgroundImage.raycastTarget = true;
        }
        else Debug.LogWarning($"[NodeCell SeedSlot] Init: Background Image component is not assigned.", gameObject);
    }

    public bool HasNode()
    {
        // Bar-copy cells: _nodeData != null, _nodeView == null, _toolDefinition == null
        // Tool cells     : _toolDefinition != null
        return _nodeData != null || _toolDefinition != null;
    }

    public NodeData GetNodeData()
    {
        return _nodeData;
    }

    public NodeView GetNodeView()
    {
        return _nodeView;
    }

    public void AssignNode(NodeDefinition def)
    {
        if (def == null)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNode called with a null NodeDefinition.", gameObject);
            return;
        }
        if (IsInventoryCell)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNode (for new sequence nodes) called on an inventory cell. Use InventoryGridController.AddGeneToInventory.", gameObject);
            return;
        }
        if (IsSeedSlot)
        {
            Debug.LogError($"[NodeCell SeedSlot] AssignNode (for new sequence nodes) called on the Seed Slot. Seeds should be dragged here.", gameObject);
            return;
        }
        if (_sequenceController == null)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNode called, but _sequenceController is null.", gameObject);
            return;
        }

        // CRITICAL CHECK: Prevent creating seeds inside sequences
        bool defIsSeed = def.effects != null && def.effects.Any(e => e != null && e.effectType == NodeEffectType.SeedSpawn && e.isPassive);
        if (defIsSeed)
        {
            Debug.LogError($"[NodeCell {CellIndex}] Cannot place a Seed definition '{def.displayName}' inside a sequence! Seeds can only exist in inventory or the seed slot.", gameObject);
            return;
        }

        RemoveNode();

        _nodeData = new NodeData() {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(),
            orderIndex = this.CellIndex,
            canBeDeleted = true
            // storedSequence will be null by default (from NodeData constructor)
        };
        // Ensure that a node freshly created from a definition and placed into a sequence
        // does NOT have a stored sequence, even if the definition itself was for a seed.
        _nodeData.ClearStoredSequence();


        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _sequenceController.NodeViewPrefab;
        if (prefabToInstantiate == null) {
            Debug.LogError($"[NodeCell {CellIndex}] No NodeView prefab for '{def.displayName}'.", gameObject);
            _nodeData = null;
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
            return;
        }

        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform);
        _nodeView = nodeViewGO.GetComponent<NodeView>();

        if (_nodeView == null) {
            Debug.LogError($"[NodeCell {CellIndex}] Prefab '{prefabToInstantiate.name}' missing NodeView. Destroying.", gameObject);
            Destroy(nodeViewGO);
            _nodeData = null;
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
            return;
        }

        // Initialize NodeView. It will also ensure its _nodeData.storedSequence is null if not a seed.
        _nodeView.Initialize(_nodeData, def, _sequenceController);

        RectTransform viewRectTransform = _nodeView.GetComponent<RectTransform>();
        if (viewRectTransform != null) viewRectTransform.anchoredPosition = Vector2.zero;

        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>() ?? _nodeView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_sequenceController, null, this);

        if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
    }
    
    public ToolDefinition GetToolDefinition()
    {
        return _toolDefinition;
    }
    
    public void AssignToolView(GameObject toolView, NodeData data, ToolDefinition toolDef)
    {
        RemoveNode();
    
        _nodeView = toolView?.GetComponent<NodeView>(); 
        _nodeData = data;
        _toolDefinition = toolDef;

        // For tools, _nodeData.storedSequence should remain null.
        // _nodeData.EnsureSeedSequenceInitialized(); // Tools are not seeds.
    
        if (toolView != null)
        {
            toolView.transform.SetParent(transform, false);
            RectTransform viewRectTransform = toolView.GetComponent<RectTransform>();
            if (viewRectTransform != null) viewRectTransform.anchoredPosition = Vector2.zero;
        
            if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
        }
        else if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
        
        Debug.Log($"[NodeCell {CellIndex}] AssignToolView: Tool='{toolDef?.displayName}', HasView={toolView != null}, HasData={data != null}");
    }

    public void AssignNodeView(NodeView view, NodeData data)
    {
         RemoveNode();

         _nodeView = view;
         _nodeData = data;
         _toolDefinition = null; 

         // If data represents a seed, its storedSequence should be preserved (or initialized if null).
         // If it's not a seed, its storedSequence should be null.
         // The NodeData instance (data) coming in should already have this handled correctly.
         // For example, if it's a seed from inventory, EnsureSeedSequenceInitialized was called there.
         // If it's a node being moved within the sequence editor, its storedSequence should already be null.

         if (_nodeView != null) {
            _nodeView.transform.SetParent(transform, false);
            RectTransform viewRectTransform = _nodeView.GetComponent<RectTransform>();
            if (viewRectTransform != null) viewRectTransform.anchoredPosition = Vector2.zero;

            if (_nodeData != null && !IsInventoryCell && !IsSeedSlot && _sequenceController != null)
            {
                _nodeData.orderIndex = this.CellIndex;
            }

            if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
         }
         else if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
         
         Debug.Log($"[NodeCell {CellIndex}] AssignNodeView: Node='{data?.nodeDisplayName}', HasView={view != null}");
    }

    public void RemoveNode()
    {
        bool wasSelected = (CurrentlySelectedCell == this && !IsInventoryCell && !IsSeedSlot);

        /* 1. Destroy the interactive NodeView (sequence / inventory) */
        if (_nodeView != null)
        {
            if (wasSelected) _nodeView.Unhighlight();
            Destroy(_nodeView.gameObject);
        }

        /* 2. Destroy any display-only object that AssignDisplayOnly() created */
        if (_displayObject != null)
        {
            Destroy(_displayObject);
            _displayObject = null;
        }

        /* 3. Reset state */
        _nodeView        = null;
        _nodeData        = null;
        _toolDefinition  = null;
        if (wasSelected) NodeCell.ClearSelection();

        if (_backgroundImage != null) _backgroundImage.raycastTarget = true;

        Debug.Log($"[NodeCell {CellIndex}] RemoveNode: Cleared all references");
    }

    public void AssignDisplayOnly(GameObject displayObject, NodeData data, ToolDefinition toolDef)
    {
        RemoveNode();                    // clears any prior content

        _nodeView       = null;
        _nodeData       = data;
        _toolDefinition = toolDef;
        _displayObject  = displayObject; // remember so RemoveNode can clean it up

        if (_displayObject != null)
        {
            _displayObject.transform.SetParent(transform, false);
            RectTransform rect = _displayObject.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = Vector2.zero;

            if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
        }
        else if (_backgroundImage != null) _backgroundImage.raycastTarget = true;

        Debug.Log($"[NodeCell {CellIndex}] AssignDisplayOnly: Item='{data?.nodeDisplayName ?? toolDef?.displayName}', IsDisplayOnly=true");
    }

    public void ClearNodeReference()
    {
        _nodeView = null;
        _nodeData = null;
        _toolDefinition = null;
        if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
        
        Debug.Log($"[NodeCell {CellIndex}] ClearNodeReference: References cleared but GameObject not destroyed");
    }

    public static void SelectCell(NodeCell cellToSelect)
    {
        if (cellToSelect == null || !cellToSelect.HasNode() || cellToSelect.IsInventoryCell || cellToSelect.IsSeedSlot)
        {
            ClearSelection();
            return;
        }
        if (CurrentlySelectedCell == cellToSelect) return;
        
        ClearSelection();

        CurrentlySelectedCell = cellToSelect;
        if (CurrentlySelectedCell?.GetNodeView() != null) {
            CurrentlySelectedCell.GetNodeView().Highlight();
        }
    }

    public static void ClearSelection()
    {
        if (CurrentlySelectedCell?.GetNodeView() != null) {
            CurrentlySelectedCell.GetNodeView().Unhighlight();
        }
        CurrentlySelectedCell = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!HasNode() && !IsInventoryCell && !IsSeedSlot && _sequenceController != null)
            {
                ClearSelection();
                _sequenceController.OnEmptyCellRightClicked(this, eventData);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (HasNode() && !IsInventoryCell && !IsSeedSlot) SelectCell(this);
            else if (!HasNode()) ClearSelection();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        NodeDraggable draggedDraggable = draggedObject.GetComponent<NodeDraggable>();
        if (draggedDraggable == null) return;

        NodeCell originalCell = draggedDraggable.OriginalCell;
        if (originalCell == null) {
            Debug.LogError($"[NodeCell {CellIndex} OnDrop] Dragged node '{draggedDraggable.name}' missing originalCell. Resetting.", draggedDraggable.gameObject);
            draggedDraggable.ResetPosition();
            return;
        }

        NodeView draggedNodeView = draggedDraggable.GetComponent<NodeView>();
        ToolView draggedToolView = draggedDraggable.GetComponent<ToolView>();
        NodeData draggedData = null;

        if (draggedNodeView != null)
        {
            draggedData = draggedNodeView.GetNodeData();
        }
        else if (draggedToolView != null)
        {
            draggedData = draggedToolView.GetNodeData();
        }

        if (draggedData == null) {
             Debug.LogError($"[NodeCell {CellIndex} OnDrop] Dragged object '{draggedDraggable.name}' missing NodeView/ToolView or NodeData. Resetting.", draggedDraggable.gameObject);
             draggedDraggable.ResetPosition();
             return;
        }

        if (this.IsSeedSlot && _sequenceController != null)
        {
            _sequenceController.HandleDropOnSeedSlot(draggedDraggable, originalCell, this);
        }
        else if (!this.IsInventoryCell && _sequenceController != null)
        {
            _sequenceController.HandleDropOnSequenceCell(draggedDraggable, originalCell, this);
        }
        else if (this.IsInventoryCell && _inventoryController != null)
        {
            _inventoryController.HandleDropOnInventoryCell(draggedDraggable, originalCell, this);
        }
        else
        {
            Debug.LogWarning($"[NodeCell {CellIndex} OnDrop] (IsInventory: {IsInventoryCell}, IsSeedSlot: {IsSeedSlot}) Target cell type unhandled or controller missing. Resetting.", gameObject);
            draggedDraggable.ResetPosition();
        }
    }
    
    public void AssignNodeFromData(NodeData nodeData, NodeDefinition def)
    {
        if (nodeData == null || def == null)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNodeFromData called with null data or definition.", gameObject);
            return;
        }
    
        // Ensure this node data doesn't have a stored sequence if it's for a sequence cell
        if (!IsInventoryCell && !IsSeedSlot)
        {
            nodeData.storedSequence = null;
            nodeData.ClearStoredSequence();
        }
    
        RemoveNode();
    
        _nodeData = nodeData;
    
        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _sequenceController.NodeViewPrefab;
        if (prefabToInstantiate == null) 
        {
            Debug.LogError($"[NodeCell {CellIndex}] No NodeView prefab for '{def.displayName}'.", gameObject);
            _nodeData = null;
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
            return;
        }

        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform);
        _nodeView = nodeViewGO.GetComponent<NodeView>();

        if (_nodeView == null) 
        {
            Debug.LogError($"[NodeCell {CellIndex}] Prefab '{prefabToInstantiate.name}' missing NodeView. Destroying.", gameObject);
            Destroy(nodeViewGO);
            _nodeData = null;
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
            return;
        }

        _nodeView.Initialize(_nodeData, def, _sequenceController);

        RectTransform viewRectTransform = _nodeView.GetComponent<RectTransform>();
        if (viewRectTransform != null) viewRectTransform.anchoredPosition = Vector2.zero;

        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>() ?? _nodeView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_sequenceController, null, this);

        if (_backgroundImage != null) _backgroundImage.raycastTarget = false;
    }
}
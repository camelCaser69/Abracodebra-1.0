// FILE: Assets/Scripts/Nodes/UI/NodeCell.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static NodeCell CurrentlySelectedCell { get; private set; }
    public int CellIndex { get; private set; }
    public bool IsInventoryCell { get; private set; }

    private NodeEditorGridController _sequenceController;
    private InventoryGridController _inventoryController;

    private NodeData _nodeData;
    private NodeView _nodeView;

    private Image _backgroundImage; // This is the NodeCell's own background

    public void Init(int index, NodeEditorGridController sequenceController, InventoryGridController inventoryController, Image bgImage)
    {
        CellIndex = index;
        _sequenceController = sequenceController;
        _inventoryController = inventoryController;
        _backgroundImage = bgImage;
        IsInventoryCell = (_inventoryController != null);

        if (_backgroundImage != null)
        {
            Color emptyColor = Color.gray; // Default fallback
            if (IsInventoryCell && _inventoryController != null)
            {
                emptyColor = _inventoryController.EmptyCellColor;
            }
            else if (!IsInventoryCell && _sequenceController != null)
            {
                emptyColor = _sequenceController.EmptyCellColor;
            }
            _backgroundImage.color = emptyColor;
            _backgroundImage.enabled = true;
            _backgroundImage.raycastTarget = true; // Empty cell is a raycast target
        }
        else
        {
            Debug.LogWarning($"[NodeCell {CellIndex}] Init: Background Image component is not assigned.", gameObject);
        }
    }

    public void Init(int index, NodeEditorGridController sequenceController, Image bgImage)
    {
        Init(index, sequenceController, null, bgImage);
    }

    public void Init(int index, InventoryGridController inventoryController, Image bgImage)
    {
        Init(index, null, inventoryController, bgImage);
    }

    public bool HasNode()
    {
        return _nodeData != null && _nodeView != null;
    }

    public NodeData GetNodeData()
    {
        return _nodeData;
    }

    public NodeView GetNodeView()
    {
        return _nodeView;
    }

    // FULLY WRITTEN METHOD
    public void AssignNode(NodeDefinition def)
    {
        if (def == null)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNode called with a null NodeDefinition.", gameObject);
            return;
        }
        if (IsInventoryCell)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNode (for new sequence nodes) called on an inventory cell. This is likely an error. Use InventoryGridController.AddGeneToInventory instead.", gameObject);
            return;
        }
        if (_sequenceController == null)
        {
            Debug.LogError($"[NodeCell {CellIndex}] AssignNode called, but _sequenceController is null. Cannot proceed.", gameObject);
            return;
        }

        RemoveNode(); // Clear any existing content first, sets background raycastTarget = true

        // Create new NodeData for the sequence
        _nodeData = new NodeData() {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(), // Crucial to clone for runtime instances
            orderIndex = this.CellIndex,
            canBeDeleted = true // Default for nodes added from library; initial nodes might override this
        };

        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _sequenceController.NodeViewPrefab;
        if (prefabToInstantiate == null) {
            Debug.LogError($"[NodeCell {CellIndex}] No NodeView prefab available (def specific or controller default) for '{def.displayName}'.", gameObject);
            _nodeData = null; // Abort assignment
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true; // Ensure it's targetable if assign fails
            return;
        }

        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform); // Instantiate as child of this cell
        _nodeView = nodeViewGO.GetComponent<NodeView>();

        if (_nodeView == null) {
            Debug.LogError($"[NodeCell {CellIndex}] NodeView prefab '{prefabToInstantiate.name}' is missing NodeView component. Destroying instance.", gameObject);
            Destroy(nodeViewGO);
            _nodeData = null;
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true; // Ensure it's targetable if assign fails
            return;
        }

        _nodeView.Initialize(_nodeData, def, _sequenceController); // Initialize the NodeView

        RectTransform viewRectTransform = _nodeView.GetComponent<RectTransform>();
        if (viewRectTransform != null)
        {
            viewRectTransform.anchoredPosition = Vector2.zero; // Center it
        }

        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>() ?? _nodeView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_sequenceController, null, this); // Initialize draggable for sequence cell

        if (_backgroundImage != null)
        {
            _backgroundImage.raycastTarget = false; // Cell background no longer primary raycast target
        }
    }

    // FULLY WRITTEN METHOD
    public void AssignNodeView(NodeView view, NodeData data)
    {
         RemoveNode(); // Clear current content first, sets background raycastTarget = true

         _nodeView = view;
         _nodeData = data;

         if (_nodeView != null) {
            _nodeView.transform.SetParent(transform, false);
            RectTransform viewRectTransform = _nodeView.GetComponent<RectTransform>();
            if (viewRectTransform != null) viewRectTransform.anchoredPosition = Vector2.zero;

            if (_nodeData != null && !IsInventoryCell && _sequenceController != null)
            {
                _nodeData.orderIndex = this.CellIndex;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.raycastTarget = false; // Cell background no longer primary raycast target
            }
         }
         else // Fallback if view somehow becomes null after assignment (should not happen)
         {
            if (_backgroundImage != null) _backgroundImage.raycastTarget = true;
         }
    }

    // FULLY WRITTEN METHOD
    public void RemoveNode()
    {
        bool wasSelected = (CurrentlySelectedCell == this && !IsInventoryCell);
        if (_nodeView != null) {
            if (wasSelected) _nodeView.Unhighlight();
            Destroy(_nodeView.gameObject);
        }
        _nodeView = null;
        _nodeData = null;
        if (wasSelected) NodeCell.ClearSelection();

        if (_backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true; // Cell is empty, make its background targetable
        }
    }

    // FULLY WRITTEN METHOD
    public void ClearNodeReference() // Called when a node is dragged OUT
    {
        _nodeView = null;
        _nodeData = null;
        if (_backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true; // Cell is empty, make its background targetable
        }
    }

    // FULLY WRITTEN METHOD
    public static void SelectCell(NodeCell cellToSelect)
    {
        if (cellToSelect == null || !cellToSelect.HasNode() || cellToSelect.IsInventoryCell)
        {
            ClearSelection();
            return;
        }
        if (CurrentlySelectedCell == cellToSelect)
        {
            return; // Already selected
        }
        
        ClearSelection(); // Unhighlight previous one

        CurrentlySelectedCell = cellToSelect;
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null) { // Defensive check
            CurrentlySelectedCell.GetNodeView().Highlight();
        }
    }

    // FULLY WRITTEN METHOD
    public static void ClearSelection()
    {
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null) {
            CurrentlySelectedCell.GetNodeView().Unhighlight();
        }
        CurrentlySelectedCell = null;
    }

    // FULLY WRITTEN METHOD
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // If this cell is empty, a sequence cell, and its controller is available
            if (!HasNode() && !IsInventoryCell && _sequenceController != null)
            {
                ClearSelection(); // Deselect any other node
                _sequenceController.OnEmptyCellRightClicked(this, eventData); // Show add-node dropdown
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // If this cell has a node and is a sequence cell
            if (HasNode() && !IsInventoryCell)
            {
                SelectCell(this); // Select this cell's node
            }
            // If this cell is empty (any type: inventory or sequence)
            else if (!HasNode())
            {
                ClearSelection(); // Clear current selection
            }
            // Left-clicking an occupied inventory cell does nothing in terms of "selection"
        }
    }

    // FULLY WRITTEN METHOD
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        NodeDraggable draggedNode = draggedObject.GetComponent<NodeDraggable>();
        if (draggedNode == null) return;

        NodeCell originalCell = draggedNode.OriginalCell;
        if (originalCell == null) {
            Debug.LogError($"[NodeCell {CellIndex} OnDrop] Dragged node '{draggedNode.name}' is missing its originalCell reference! Attempting to reset draggable.", draggedNode.gameObject);
            draggedNode.ResetPosition();
            return;
        }

        // Delegate drop handling to the appropriate controller based on THIS cell's type (the drop target)
        if (!this.IsInventoryCell && _sequenceController != null) // Dropped onto a SEQUENCE cell
        {
            _sequenceController.HandleDropOnSequenceCell(draggedNode, originalCell, this);
        }
        else if (this.IsInventoryCell && _inventoryController != null) // Dropped onto an INVENTORY cell
        {
            _inventoryController.HandleDropOnInventoryCell(draggedNode, originalCell, this);
        }
        else
        {
            // This cell is not properly configured or its controller is missing.
            Debug.LogWarning($"[NodeCell {CellIndex} OnDrop] (IsInventory: {IsInventoryCell}) is not a valid drop target type or its controller is missing. Resetting dragged item.", gameObject);
            draggedNode.ResetPosition();
        }
    }
}
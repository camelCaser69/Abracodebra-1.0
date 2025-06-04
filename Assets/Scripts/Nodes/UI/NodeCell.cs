// FILE: Assets\Scripts\Nodes\UI\NodeCell.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static NodeCell CurrentlySelectedCell { get; private set; } // For the main sequence
    public int CellIndex { get; private set; }
    public bool IsInventoryCell { get; private set; } // NEW: Flag to identify cell type

    private NodeEditorGridController _sequenceController; // For sequence cells
    private InventoryGridController _inventoryController; // For inventory cells
    private NodeData _nodeData;
    private NodeView _nodeView;
    private Image _backgroundImage;

    // MODIFIED Init for flexibility
    public void Init(int index, NodeEditorGridController sequenceController, InventoryGridController inventoryController, Image bgImage)
    {
        CellIndex = index;
        _sequenceController = sequenceController;
        _inventoryController = inventoryController;
        _backgroundImage = bgImage;
        IsInventoryCell = (_inventoryController != null); // Determine if it's an inventory cell

        if (_backgroundImage != null)
        {
            Color emptyColor = IsInventoryCell ?
                               _inventoryController.EmptyCellColor :
                               _sequenceController.EmptyCellColor;
            _backgroundImage.color = emptyColor;
            _backgroundImage.enabled = true;
        }
    }
    // Overload for original NodeEditorGridController usage (or it can call the new one with null for inventoryController)
    public void Init(int index, NodeEditorGridController sequenceController, Image bgImage)
    {
        Init(index, sequenceController, null, bgImage);
    }
    // Overload for InventoryGridController usage
    public void Init(int index, InventoryGridController inventoryController, Image bgImage)
    {
        Init(index, null, inventoryController, bgImage);
    }


    public bool HasNode() => _nodeData != null && _nodeView != null;
    public NodeData GetNodeData() => _nodeData;
    public NodeView GetNodeView() => _nodeView;

    // AssignNode for sequence cells
    public void AssignNode(NodeDefinition def)
    {
        if (def == null || _sequenceController == null) return; // Only for sequence
        RemoveNode();

        _nodeData = new NodeData() {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(),
            orderIndex = this.CellIndex
        };

        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _sequenceController.NodeViewPrefab;
        if (prefabToInstantiate == null) { _nodeData = null; return; }

        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform);
        _nodeView = nodeViewGO.GetComponent<NodeView>();
        if (_nodeView == null) { Destroy(nodeViewGO); _nodeData = null; return; }

        _nodeView.Initialize(_nodeData, def, _sequenceController);

        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = _nodeView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_sequenceController, this); // Sequence draggable inits with sequence controller

        if(_backgroundImage != null) _backgroundImage.enabled = true; // Should be false if occupied
    }

    // AssignNodeView (generic for both inventory and sequence when dragging)
    public void AssignNodeView(NodeView view, NodeData data)
    {
         RemoveNode(); // Clear current content
         _nodeView = view;
         _nodeData = data; // This data could be "real" (sequence) or "dummy" (inventory)
         if (_nodeView != null) {
             _nodeView.transform.SetParent(transform, false);
             _nodeView.transform.localPosition = Vector2.zero; // Ensure it's centered
             if (_nodeData != null && !IsInventoryCell && _sequenceController != null) // Only update orderIndex for sequence
             {
                 _nodeData.orderIndex = this.CellIndex;
             }
             // Parent cell ref on NodeView and NodeDraggable will be updated by SnapToCell
         }
         if(_backgroundImage != null) _backgroundImage.enabled = true; // Should be false if occupied
    }


    public void RemoveNode()
    {
        bool wasSelected = (CurrentlySelectedCell == this && !IsInventoryCell);
        if (_nodeView != null) {
            if (wasSelected) {
                 _nodeView.Unhighlight();
                 CurrentlySelectedCell = null;
            }
            Destroy(_nodeView.gameObject);
        }
        _nodeView = null; _nodeData = null;
        if (wasSelected && CurrentlySelectedCell == this) CurrentlySelectedCell = null; // Redundant?
        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }
    public void ClearNodeReference() // Used when a node is dragged *out* of this cell
    {
        _nodeView = null; _nodeData = null;
        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }


    public static void SelectCell(NodeCell cellToSelect)
    {
        if (cellToSelect == null || !cellToSelect.HasNode() || cellToSelect.IsInventoryCell) // Don't select inventory items
        {
            ClearSelection(); return;
        }
        if (CurrentlySelectedCell == cellToSelect) return;
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null) {
            CurrentlySelectedCell.GetNodeView().Unhighlight();
        }
        CurrentlySelectedCell = cellToSelect;
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null) {
            CurrentlySelectedCell.GetNodeView().Highlight();
        }
    }
    public static void ClearSelection()
    {
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null) {
            CurrentlySelectedCell.GetNodeView().Unhighlight();
        }
        CurrentlySelectedCell = null;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!HasNode() && !IsInventoryCell && _sequenceController != null) // Only open add for empty SEQUENCE cells
            {
                ClearSelection();
                _sequenceController.OnEmptyCellRightClicked(this, eventData);
            }
            // Right click on inventory or occupied sequence cells does nothing here.
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (HasNode() && !IsInventoryCell) // Left click selects sequence nodes
            {
                SelectCell(this);
            }
            else if (!HasNode()) // Left click on any empty cell (inventory or sequence)
            {
                ClearSelection();
            }
        }
    }

    // MODIFIED OnDrop to handle inter-grid logic
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        NodeDraggable draggedNode = draggedObject.GetComponent<NodeDraggable>();
        if (draggedNode == null) return;

        NodeCell originalCell = draggedNode.OriginalCell;
        if (originalCell == null) return; // Should not happen

        NodeEditorGridController mainSeqController = NodeEditorGridController.Instance;
        InventoryGridController invController = InventoryGridController.Instance;

        // Case 1: Dropping onto a SEQUENCE cell (this cell)
        if (!this.IsInventoryCell && mainSeqController != null)
        {
            mainSeqController.HandleDropOnSequenceCell(draggedNode, originalCell, this);
        }
        // Case 2: Dropping onto an INVENTORY cell (this cell)
        else if (this.IsInventoryCell && invController != null)
        {
            invController.HandleDropOnInventoryCell(draggedNode, originalCell, this);
        }
    }
}
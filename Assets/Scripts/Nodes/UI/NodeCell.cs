using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
// Remove IPointerClickHandler interface from here
// Keep IDropHandler for dropping onto the cell background
public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler // Keep IPointerClickHandler for RIGHT click on empty
{
    public static NodeCell CurrentlySelectedCell { get; private set; }
    public int CellIndex { get; private set; }

    private NodeEditorGridController _controller;
    private NodeData _nodeData;
    private NodeView _nodeView;
    private Image _backgroundImage;

    // --- Init remains the same ---
    public void Init(int index, NodeEditorGridController gridController, Image bgImage)
    {
        CellIndex = index;
        _controller = gridController;
        _backgroundImage = bgImage;

        if (_backgroundImage != null && _controller != null)
        {
            _backgroundImage.color = _controller.EmptyCellColor;
            _backgroundImage.enabled = true;
        }
    }

    // --- HasNode, Getters remain the same ---
    public bool HasNode() => _nodeData != null && _nodeView != null;
    public NodeData GetNodeData() => _nodeData;
    public NodeView GetNodeView() => _nodeView;

    // --- AssignNode, AssignNodeView remain the same ---
     public void AssignNode(NodeDefinition def)
    {
        if (def == null || _controller == null) return;
        RemoveNode();

        _nodeData = new NodeData() { /* ... data setup ... */
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(),
            orderIndex = this.CellIndex
        };

        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _controller.NodeViewPrefab;
        if (prefabToInstantiate == null) { /* ... error handling ... */ Debug.LogError($"..."); _nodeData = null; return; }

        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform);
        _nodeView = nodeViewGO.GetComponent<NodeView>();
        if (_nodeView == null) { /* ... error handling ... */ Debug.LogError($"..."); Destroy(nodeViewGO); _nodeData = null; return; }

        _nodeView.Initialize(_nodeData, def, _controller); // Init NodeView

        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = _nodeView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_controller, this); // Init Draggable

        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }
     public void AssignNodeView(NodeView view, NodeData data)
    {
         RemoveNode();
         _nodeView = view;
         _nodeData = data;
         if (_nodeView != null) {
             _nodeView.transform.SetParent(transform, false);
             if (_nodeData != null) _nodeData.orderIndex = this.CellIndex;
             // Parent ref will be updated by NodeDraggable.SnapToCell which calls NodeView.UpdateParent...
         }
         if(_backgroundImage != null) _backgroundImage.enabled = true;
    }


    // --- RemoveNode, ClearNodeReference remain the same ---
    public void RemoveNode()
    {
        bool wasSelected = (CurrentlySelectedCell == this);
        if (_nodeView != null) {
            if (wasSelected) {
                 _nodeView.Unhighlight();
                 CurrentlySelectedCell = null;
            }
            Destroy(_nodeView.gameObject);
        }
        _nodeView = null; _nodeData = null;
        if (wasSelected && CurrentlySelectedCell == this) CurrentlySelectedCell = null;
        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }
     public void ClearNodeReference()
    {
        _nodeView = null; _nodeData = null;
        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }

    // --- Selection Handling (SelectCell, ClearSelection) remains the same ---
     public static void SelectCell(NodeCell cellToSelect)
    {
        if (cellToSelect == null || !cellToSelect.HasNode()) { ClearSelection(); return; }
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

    // --- Event Handlers UPDATED ---

    /// <summary>
    /// Handles clicks on the CELL BACKGROUND image.
    /// Left Click: Does nothing (NodeView handles selection).
    /// Right Click: Opens add node menu ONLY if the cell is currently empty.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // This click is on the NodeCell's background image

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right Click: Only allow opening the dropdown if the cell is currently EMPTY.
            if (!HasNode())
            {
                // Debug.Log($"NodeCell Background Clicked (Right) on Empty Cell {CellIndex}. Opening dropdown.");
                ClearSelection(); // Clear selection before showing add menu
                _controller?.OnEmptyCellRightClicked(this, eventData);
            }
            // If right-clicking the background of an occupied cell, do nothing.
        }
        // Left click on the background does nothing regarding selection now.
    }


    // --- OnDrop remains the same ---
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null) {
            NodeDraggable draggedNode = draggedObject.GetComponent<NodeDraggable>();
            if (draggedNode != null && _controller != null) {
                 _controller.HandleNodeDrop(draggedNode, draggedNode.OriginalCell, eventData.position);
            }
        }
    }
}
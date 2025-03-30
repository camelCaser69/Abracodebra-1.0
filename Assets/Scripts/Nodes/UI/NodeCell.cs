using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
// Removed IPointerClickHandler here as NodeView handles left clicks now
public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static NodeCell CurrentlySelectedCell { get; private set; }
    public int CellIndex { get; private set; }

    private NodeEditorGridController _controller;
    private NodeData _nodeData;
    private NodeView _nodeView;
    private Image _backgroundImage; // The cell's own background (the slot)

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

    public bool HasNode() => _nodeData != null && _nodeView != null;
    public NodeData GetNodeData() => _nodeData;
    public NodeView GetNodeView() => _nodeView;

    public void AssignNode(NodeDefinition def)
    {
        if (def == null || _controller == null) return;
        RemoveNode();

        _nodeData = new NodeData() {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(),
            orderIndex = this.CellIndex
        };

        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _controller.NodeViewPrefab;
        if (prefabToInstantiate == null) {
            Debug.LogError($"[NodeCell] No NodeView prefab for '{def.displayName}' or default.");
             _nodeData = null; return;
        }

        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform);
        _nodeView = nodeViewGO.GetComponent<NodeView>();
        if (_nodeView == null) {
            Debug.LogError($"[NodeCell] Prefab '{prefabToInstantiate.name}' missing NodeView.");
             Destroy(nodeViewGO); _nodeData = null; return;
        }

        // Initialize NodeView *after* getting the component
        _nodeView.Initialize(_nodeData, def, _controller);

        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>();
        if (draggable == null) draggable = _nodeView.gameObject.AddComponent<NodeDraggable>();
        draggable.Initialize(_controller, this);

        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }

    public void AssignNodeView(NodeView view, NodeData data)
    {
         RemoveNode();
         _nodeView = view;
         _nodeData = data;

         if (_nodeView != null)
         {
             _nodeView.transform.SetParent(transform, false);
             if (_nodeData != null) _nodeData.orderIndex = this.CellIndex;
             // Ensure the NodeView's internal parent reference is updated if needed
             // _nodeView.Initialize(...) might need recalling if controller/parent changed significantly, but likely ok here.
         }
         if(_backgroundImage != null) _backgroundImage.enabled = true;
    }

    public void RemoveNode()
    {
        bool wasSelected = (CurrentlySelectedCell == this);

        if (_nodeView != null)
        {
            // Unhighlight must happen *before* destroy
            if (wasSelected)
            {
                 // Call Unhighlight directly on the view we are about to destroy
                 _nodeView.Unhighlight();
                 CurrentlySelectedCell = null; // Clear static reference *before* destroy
            }
            Destroy(_nodeView.gameObject);
        }
        _nodeView = null;
        _nodeData = null;

        // If this cell was selected, ensure static ref is null
        if (wasSelected && CurrentlySelectedCell == this) {
             CurrentlySelectedCell = null;
        }

        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }

    public void ClearNodeReference()
    {
        _nodeView = null;
        _nodeData = null;
        if(_backgroundImage != null) _backgroundImage.enabled = true;
    }

    // --- Selection Handling ---

    public static void SelectCell(NodeCell cellToSelect)
    {
        if (cellToSelect == null || !cellToSelect.HasNode()) {
             ClearSelection();
             return;
        }
        if (CurrentlySelectedCell == cellToSelect) return;

        // Unhighlight previous node
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null)
        {
             // Debug.Log($"SelectCell: Unhighlighting previous cell {CurrentlySelectedCell.CellIndex}");
            CurrentlySelectedCell.GetNodeView().Unhighlight();
        } else if (CurrentlySelectedCell != null) {
            // Debug.LogWarning($"SelectCell: Previous cell {CurrentlySelectedCell.CellIndex} had no NodeView to unhighlight.");
        }

        // Select new cell and highlight its node
        CurrentlySelectedCell = cellToSelect;
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null)
        {
            // Debug.Log($"SelectCell: Highlighting new cell {CurrentlySelectedCell.CellIndex}");
            CurrentlySelectedCell.GetNodeView().Highlight();
        } else if (CurrentlySelectedCell != null) {
             // Debug.LogWarning($"SelectCell: New cell {CurrentlySelectedCell.CellIndex} has no NodeView to highlight.");
        }
    }

    public static void ClearSelection()
    {
        // Unhighlight previous node
        if (CurrentlySelectedCell != null && CurrentlySelectedCell.GetNodeView() != null)
        {
             // Debug.Log($"ClearSelection: Unhighlighting cell {CurrentlySelectedCell.CellIndex}");
            CurrentlySelectedCell.GetNodeView().Unhighlight();
        }
        CurrentlySelectedCell = null;
    }

    // --- Event Handlers ---

    /// <summary>
    /// Handles clicks on the CELL BACKGROUND (the slot).
    /// Left Click: Clears selection (clicking empty space).
    /// Right Click: Opens add node menu IF EMPTY.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // This click is on the NodeCell's background image, not the NodeView itself

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // If the user clicked the empty background area of a cell (even if it contains a node), clear selection.
            // Selection now only happens when clicking the NodeView directly.
            // Debug.Log($"NodeCell Background Clicked (Left). Clearing selection.");
            ClearSelection();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right Click: Only allow opening the dropdown if the cell is currently EMPTY.
            if (!HasNode())
            {
                // Debug.Log($"NodeCell Background Clicked (Right) on Empty Cell. Opening dropdown.");
                ClearSelection(); // Clear selection before showing add menu
                _controller?.OnEmptyCellRightClicked(this, eventData);
            }
            // Right-clicking the background of an occupied cell does nothing.
        }
    }


    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null)
        {
            NodeDraggable draggedNode = draggedObject.GetComponent<NodeDraggable>();
            if (draggedNode != null && _controller != null)
            {
                 _controller.HandleNodeDrop(draggedNode, draggedNode.OriginalCell, eventData.position);
            }
        }
    }
}
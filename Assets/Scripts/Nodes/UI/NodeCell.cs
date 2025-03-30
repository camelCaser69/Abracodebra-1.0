using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Requires a RectTransform, which is added by the controller
[RequireComponent(typeof(RectTransform))]
public class NodeCell : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    /// <summary> Static reference to the currently selected cell. </summary>
    public static NodeCell CurrentlySelectedCell { get; private set; }

    /// <summary> The zero-based index of this cell in the grid. </summary>
    public int CellIndex { get; private set; }

    private NodeEditorGridController _controller;
    private NodeData _nodeData;   // The logical data of the node in this cell (null if empty)
    private NodeView _nodeView;   // The visual representation of the node (null if empty)
    private Image _backgroundImage; // Reference to the cell's background image

    /// <summary>
    /// Initializes the cell with its index, controller reference, and background image.
    /// </summary>
    public void Init(int index, NodeEditorGridController gridController, Image bgImage)
    {
        CellIndex = index;
        _controller = gridController;
        _backgroundImage = bgImage;
        UpdateVisualState(false); // Set initial visual state (not selected)
    }

    /// <summary> Returns true if this cell currently contains a node. </summary>
    public bool HasNode() => _nodeData != null && _nodeView != null;

    /// <summary> Returns the NodeData associated with this cell, or null if empty. </summary>
    public NodeData GetNodeData() => _nodeData;

    /// <summary> Returns the NodeView component currently in this cell, or null if empty. </summary>
    public NodeView GetNodeView() => _nodeView;

    /// <summary>
    /// Creates and assigns a new node to this cell based on a NodeDefinition.
    /// Called when selecting from the dropdown.
    /// </summary>
    public void AssignNode(NodeDefinition def)
    {
        if (def == null || _controller == null) return;

        // Clear any existing node first (handles deselection if needed)
        RemoveNode();

        // 1. Create NodeData
        _nodeData = new NodeData()
        {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeDisplayName = def.displayName,
            effects = def.CloneEffects(), // Use the cloning method
            orderIndex = this.CellIndex  // Set order index based on cell
        };

        // 2. Determine which NodeView prefab to use
        GameObject prefabToInstantiate = def.nodeViewPrefab != null ? def.nodeViewPrefab : _controller.NodeViewPrefab;

        if (prefabToInstantiate == null)
        {
            Debug.LogError($"[NodeCell] No NodeView prefab found for '{def.displayName}' and no default is set in the controller.");
            _nodeData = null; // Failed to create visual, reset data
            return;
        }

        // 3. Instantiate the NodeView prefab as a child of this cell's transform
        GameObject nodeViewGO = Instantiate(prefabToInstantiate, transform); // Parent is this cell
        _nodeView = nodeViewGO.GetComponent<NodeView>();

        if (_nodeView == null)
        {
             Debug.LogError($"[NodeCell] Instantiated prefab '{prefabToInstantiate.name}' is missing NodeView component.");
             Destroy(nodeViewGO);
             _nodeData = null; // Failed to get component, reset data
             return;
        }

        // 4. Initialize the NodeView
        _nodeView.Initialize(_nodeData, def, _controller); // Pass definition and controller

        // 5. Add Draggable Component if not already present
        NodeDraggable draggable = _nodeView.GetComponent<NodeDraggable>();
        if (draggable == null)
        {
             draggable = _nodeView.gameObject.AddComponent<NodeDraggable>();
             // Debug.LogWarning($"[NodeCell] NodeView prefab '{_nodeView.name}' was missing NodeDraggable component. Added automatically.");
        }
        // Ensure the draggable knows its controller and starting cell
        draggable.Initialize(_controller, this);

        // NodeView GO is now a child of this cell, positioned at (0,0) locally, appearing on top of the background
        UpdateVisualState(CurrentlySelectedCell == this); // Update selection visual if this cell happens to be selected already
    }

    /// <summary>
    /// Assigns an *existing* NodeView and its data to this cell.
    /// Used during drag-and-drop swaps. Ensures correct parenting and updates data.
    /// </summary>
    public void AssignNodeView(NodeView view, NodeData data)
    {
         // Clear existing first (should be done by caller, but safety check)
         RemoveNode();

         _nodeView = view;
         _nodeData = data;

         if (_nodeView != null)
         {
             // Ensure the view is parented correctly and data is updated
             _nodeView.transform.SetParent(transform, false); // Set parent without changing world position initially
             // Associated NodeDraggable's SnapToCell will handle the final position reset (anchoredPosition = Vector2.zero)
             if (_nodeData != null) _nodeData.orderIndex = this.CellIndex; // Update order index
         }
         UpdateVisualState(CurrentlySelectedCell == this); // Update visuals based on current selection state
    }


    /// <summary>
    /// Removes the node (data and view) from this cell. Also handles deselection if this cell was selected.
    /// </summary>
    public void RemoveNode()
    {
        bool wasSelected = (CurrentlySelectedCell == this);

        if (_nodeView != null)
        {
            Destroy(_nodeView.gameObject); // Destroy the visual representation
        }
        _nodeView = null;
        _nodeData = null;

        if (wasSelected) {
            CurrentlySelectedCell = null; // Clear static reference if this was the selected one
        }
        // Visually update this cell (will apply default color since it's now empty and potentially deselects)
        UpdateVisualState(false);
    }

    /// <summary>
    /// Clears the node references (_nodeView, _nodeData) without destroying the NodeView GameObject.
    /// Used during drag-and-drop before potentially reassigning the view elsewhere.
    /// Updates the visual state but does not change the global selection.
    /// </summary>
    public void ClearNodeReference()
    {
        _nodeView = null;
        _nodeData = null;
        // Don't change selection state here, UpdateVisualState handles background based on current global selection
        UpdateVisualState(CurrentlySelectedCell == this);
    }

    /// <summary>
    /// Updates the background color based on selection state.
    /// The background image itself is now always enabled to act as the slot background.
    /// </summary>
    private void UpdateVisualState(bool isSelected)
    {
        if (_backgroundImage != null && _controller != null)
        {
            // **Ensure background is always visible for the slot appearance**
            _backgroundImage.enabled = true;
            // Set color based on whether this cell is the currently selected one
            _backgroundImage.color = isSelected ? _controller.SelectedCellColor : _controller.DefaultCellColor;
        }
    }

    // --- Selection Handling ---

    /// <summary>
    /// Sets this cell's background to the selected color. (Internal use)
    /// </summary>
    private void Select() // Changed to private, use SelectCell static method externally
    {
        if (_controller == null) return;
        // Debug.Log($"Selecting Cell: {CellIndex}"); // Optional log
        UpdateVisualState(true); // Update visual to selected color
    }

    /// <summary>
    /// Sets this cell's background to the default color. (Internal use)
    /// </summary>
    private void Deselect() // Changed to private, use ClearSelection or SelectCell static methods externally
    {
         if (_controller == null) return;
         // Debug.Log($"Deselecting Cell: {CellIndex}"); // Optional log
         UpdateVisualState(false); // Update visual to default color
    }

    /// <summary>
    /// Static method to handle selecting a specific cell. Only selects if the cell contains a node.
    /// </summary>
    public static void SelectCell(NodeCell cellToSelect)
    {
        // Can only select cells that actually contain a node
        if (cellToSelect == null || !cellToSelect.HasNode()) {
             // Clear selection if trying to select an invalid/empty cell
             ClearSelection();
             return;
        }

        // Do nothing if already selected
        if (CurrentlySelectedCell == cellToSelect) return;

        // Deselect previous cell if there was one
        if (CurrentlySelectedCell != null)
        {
            CurrentlySelectedCell.Deselect();
        }

        // Select the new cell
        CurrentlySelectedCell = cellToSelect;
        CurrentlySelectedCell.Select(); // Call private Select method
    }

    /// <summary>
    /// Static method to clear the current selection, deselecting the cell visually.
    /// </summary>
    public static void ClearSelection()
    {
        if (CurrentlySelectedCell != null)
        {
            CurrentlySelectedCell.Deselect(); // Call private Deselect method
            CurrentlySelectedCell = null;
        }
    }


    // --- Event Handlers ---

    /// <summary>
    /// Handles pointer clicks on the cell's background image.
    /// Left Click: Selects the cell if it has a node, otherwise clears selection.
    /// Right Click: Opens the add node dropdown ONLY if the cell is empty.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left Click: Select this cell IF it has a node. Otherwise, clear any existing selection.
            if (HasNode()) {
                 SelectCell(this);
            } else {
                 ClearSelection();
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right Click: Only allow opening the dropdown if the cell is currently EMPTY.
            if (!HasNode())
            {
                // Clear selection *before* showing dropdown for adding
                ClearSelection();
                _controller?.OnEmptyCellRightClicked(this, eventData);
            }
             // If right-clicking an occupied cell, currently do nothing.
        }
    }

    /// <summary>
    /// Handles the drop event when a NodeDraggable is released onto this cell.
    /// Defers the logic to the NodeEditorGridController.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject != null)
        {
            NodeDraggable draggedNode = draggedObject.GetComponent<NodeDraggable>();
            if (draggedNode != null && _controller != null)
            {
                 // Controller handles all drop logic, including updating selection state
                 _controller.HandleNodeDrop(draggedNode, draggedNode.OriginalCell, eventData.position);
                 // No need to call ResetPosition or SnapToCell here; the controller manages it based on success/failure.
            }
        }
    }
}
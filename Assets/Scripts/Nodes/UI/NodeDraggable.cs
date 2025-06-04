// FILE: Assets\Scripts\Nodes\UI\NodeDraggable.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NodeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalAnchoredPosition;
    private Transform _originalParent;
    private NodeCell _originalCell;
    private NodeEditorGridController _gridController; // Null if from inventory
    private InventoryGridController _inventoryController; // Null if from sequence
    private Canvas _rootCanvas;

    public NodeCell OriginalCell => _originalCell;
    public bool IsFromInventoryGrid => _inventoryController != null; // NEW: Helper property

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
    }

    // MODIFIED: Initialize to handle both grid types
    public void Initialize(NodeEditorGridController gridCtrl, InventoryGridController invCtrl, NodeCell startingCell)
    {
        _gridController = gridCtrl;
        _inventoryController = invCtrl;
        _originalCell = startingCell;
        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas; // Safe navigation
        if (_rootCanvas == null) _rootCanvas = FindObjectOfType<Canvas>(); // Fallback
        if (_rootCanvas == null) Debug.LogError("[NodeDraggable] Could not find root Canvas!");

        if (_canvasGroup != null) {
             _canvasGroup.blocksRaycasts = true;
             _canvasGroup.alpha = 1f;
        }
    }
    // Overload for original NodeEditorGridController usage
    public void Initialize(NodeEditorGridController gridCtrl, NodeCell startingCell)
    {
        Initialize(gridCtrl, null, startingCell);
    }
    // Overload for InventoryGridController usage
    public void Initialize(InventoryGridController invCtrl, NodeCell startingCell)
    {
        Initialize(null, invCtrl, startingCell);
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        // Check if it's draggable (e.g. initial seed nodes might not be)
        NodeView view = GetComponent<NodeView>();
        if (view != null && view.GetNodeData() != null && !view.GetNodeData().canBeDeleted && OriginalCell != null && !OriginalCell.IsInventoryCell)
        {
            // Example: Non-deletable sequence nodes might also be non-draggable from their initial spot
            // For inventory, canBeDeleted is false, but they should be draggable.
            // This logic might need refinement based on exact canMove/canDelete rules.
            // For now, assume inventory items are always draggable FROM inventory.
            // And sequence items are draggable if not explicitly locked.
            if (!IsFromInventoryGrid) // If from sequence and marked as not deletable (e.g. initial seed)
            {
                 // Check canMove on the NodeDefinition's InitialNodeConfig if this is an initial node
                 // This requires more complex tracking or flags. For now, just prevent drag if from sequence & !canBeDeleted (simple proxy for non-movable initial nodes)
                if (view.GetNodeData().canBeDeleted == false) // A simple check, refine if needed
                {
                    Debug.Log($"Node '{view.GetNodeData().nodeDisplayName}' is not draggable from sequence.");
                    eventData.pointerDrag = null; // Cancel drag
                    return;
                }
            }
        }


        if (_rootCanvas == null) return;

        _originalParent = transform.parent;
        // _originalCell is already set by Initialize and updated by SnapToCell
        _originalAnchoredPosition = _rectTransform.anchoredPosition;

        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;

        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _canvasGroup == null || _canvasGroup.blocksRaycasts) return;
        if (_rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
             _rootCanvas.transform as RectTransform, eventData.position,
             _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, out Vector2 currentLocalPoint);
        _rectTransform.localPosition = currentLocalPoint; // Simpler movement for top-level canvas drag
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null) {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            ResetPosition(); // Reset if not a left-button drag end
            return;
        }

        // Determine which controller should handle the drop
        // The actual drop logic is now in NodeCell.OnDrop, which then calls the appropriate grid controller.
        // Here, we just need to ensure the NodeDraggable is reset if no valid drop target is found by the EventSystem.
        // The EventSystem calls OnDrop on the NodeCell *under* the mouse pointer.
        // If eventData.pointerEnter is null or not a NodeCell, it means it was dropped outside.
        
        GameObject dropTarget = eventData.pointerEnter;
        NodeCell targetCell = dropTarget?.GetComponent<NodeCell>();

        if (targetCell == null) // Dropped outside any cell
        {
            if (IsFromInventoryGrid && _inventoryController != null)
            {
                // If dragged from inventory and dropped outside, return it to an inventory slot
                // _inventoryController.ReturnGeneToInventory(GetComponent<NodeView>().GetNodeDefinition(), _originalCell); // This logic is complex, simpler to reset
                ResetPosition(); // Simplest: reset to original inventory slot
            }
            else if (!IsFromInventoryGrid && _gridController != null)
            {
                // If dragged from sequence and dropped outside, reset to original sequence slot
                ResetPosition();
            }
            else // Fallback
            {
                ResetPosition();
            }
        }
        // If targetCell is not null, NodeCell.OnDrop will handle it.
        // NodeDraggable doesn't need to do more here.
    }

    public void ResetPosition()
    {
        if (_originalParent == null) {
            Debug.LogWarning($"[NodeDraggable ResetPosition] {gameObject.name} has no original parent to reset to. Destroying or hiding might be an option if it's orphaned.", gameObject);
            // Fallback: try to reparent to root canvas and hide, or destroy
            if(_rootCanvas != null) transform.SetParent(_rootCanvas.transform, false);
            gameObject.SetActive(false); // Hide it
            return;
        }
        transform.SetParent(_originalParent, false);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
        _originalCell = _originalParent.GetComponent<NodeCell>();

        if (_canvasGroup != null) {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
        GetComponent<NodeView>()?.UpdateParentCellReference();
    }

    public void SnapToCell(NodeCell targetCell)
    {
        if (targetCell == null) { ResetPosition(); return; }

        transform.SetParent(targetCell.transform, false);
        _rectTransform.anchoredPosition = Vector2.zero;

        _originalParent = targetCell.transform;
        _originalCell = targetCell; // Update original cell to the new one
        _originalAnchoredPosition = Vector2.zero;

        if (_canvasGroup != null) {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
        GetComponent<NodeView>()?.UpdateParentCellReference();

        // Update which controller this draggable "belongs" to based on the target cell
        _inventoryController = targetCell.IsInventoryCell ? InventoryGridController.Instance : null;
        _gridController = !targetCell.IsInventoryCell ? NodeEditorGridController.Instance : null;
    }

    void OnDisable()
    {
        if (_canvasGroup != null && !_canvasGroup.blocksRaycasts)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Might need for CanvasGroup or Image

// Requires RectTransform and CanvasGroup (added automatically if needed)
[RequireComponent(typeof(RectTransform))]
public class NodeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalAnchoredPosition;
    private Transform _originalParent;
    private NodeCell _originalCell; // Store the cell it started in
    private NodeEditorGridController _gridController;
    private Canvas _rootCanvas; // To ensure dragging happens on top

    // Public property to access the original cell
    public NodeCell OriginalCell => _originalCell;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
         // We expect Initialize to be called by NodeCell after instantiation
    }

    /// <summary>
    /// Initializes the draggable component with necessary references.
    /// Should be called by NodeCell when the NodeView is created or assigned.
    /// </summary>
    public void Initialize(NodeEditorGridController controller, NodeCell startingCell)
    {
        _gridController = controller;
        _originalCell = startingCell;
        _originalParent = transform.parent; // Initial parent is the cell
        _originalAnchoredPosition = _rectTransform.anchoredPosition; // Should be (0,0) if placed correctly

        // Find the root canvas for proper drag behavior
        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null)
        {
             Debug.LogError("[NodeDraggable] Could not find root Canvas!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_gridController == null || _rootCanvas == null) return; // Not initialized

        // Store current state
        _originalParent = transform.parent;
        _originalCell = _originalParent?.GetComponent<NodeCell>(); // Re-confirm original cell
        _originalAnchoredPosition = _rectTransform.anchoredPosition;

        // Make it draggable visually
        _canvasGroup.alpha = 0.6f; // Make semi-transparent
        _canvasGroup.blocksRaycasts = false; // Allow raycasts to pass through to cells

        // Reparent to root canvas temporarily to ensure it renders on top
        transform.SetParent(_rootCanvas.transform, true); // Keep world position
        transform.SetAsLastSibling(); // Render on top
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_gridController == null || _rootCanvas == null) return;

        // Move the object with the mouse pointer
        // Convert delta screen space movement to the local space of the root canvas
         RectTransformUtility.ScreenPointToLocalPointInRectangle(
             _rootCanvas.transform as RectTransform,
             eventData.position,
             _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
             out Vector2 currentLocalPoint);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position - eventData.delta, // Previous position
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
            out Vector2 previousLocalPoint);

        Vector2 localDelta = currentLocalPoint - previousLocalPoint;
        _rectTransform.localPosition += (Vector3)localDelta; // Apply movement in canvas local space

        // Alternative simpler movement (might have slight offset depending on canvas setup):
        // _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_gridController == null) return;

        // Restore visual state
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        // Let the controller handle the drop logic
        bool droppedSuccessfully = _gridController.HandleNodeDrop(this, _originalCell, eventData.position);

        // If the drop was *not* handled successfully by the controller (e.g., dropped outside),
        // the controller's HandleNodeDrop should call ResetPosition or SnapToCell on the draggable.
        // If HandleNodeDrop returns true, it means the controller has already placed this draggable
        // in its new cell via SnapToCell.
        // If HandleNodeDrop returns false, it means it called ResetPosition.

        // No explicit ResetPosition call needed here anymore, as the controller manages it.
    }

    /// <summary>
    /// Snaps the node view back to its original parent cell and position.
    /// Called by the controller if a drop is invalid.
    /// </summary>
    public void ResetPosition()
    {
        transform.SetParent(_originalParent, false); // Set parent back
        _rectTransform.anchoredPosition = _originalAnchoredPosition; // Reset local position
         // Update internal reference in case it's needed again
        _originalCell = _originalParent?.GetComponent<NodeCell>();
    }

    /// <summary>
    /// Snaps the node view to a target cell, updating its parent and resetting position.
    /// Called by the controller after a successful drop or swap.
    /// </summary>
    public void SnapToCell(NodeCell targetCell)
    {
        if (targetCell == null)
        {
            ResetPosition(); // Fallback if target cell is somehow null
            return;
        }
        transform.SetParent(targetCell.transform, false); // Set new parent
        _rectTransform.anchoredPosition = Vector2.zero; // Center in the new cell

         // Update internal references for future drags
         _originalParent = targetCell.transform;
         _originalCell = targetCell;
         _originalAnchoredPosition = Vector2.zero;
    }
}
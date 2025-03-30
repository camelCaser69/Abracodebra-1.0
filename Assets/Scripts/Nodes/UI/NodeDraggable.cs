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
    private NodeEditorGridController _gridController;
    private Canvas _rootCanvas;

    // No _isDragging flag needed if managed carefully by events

    public NodeCell OriginalCell => _originalCell;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        // CRITICAL: Default state MUST allow interactions (clicks, hovers)
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f; // Ensure fully visible
    }

    public void Initialize(NodeEditorGridController controller, NodeCell startingCell)
    {
        _gridController = controller;
        _originalCell = startingCell;
        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null) Debug.LogError("[NodeDraggable] Could not find root Canvas!");

        // Ensure initial state after initialization
        if (_canvasGroup != null) {
             _canvasGroup.blocksRaycasts = true;
             _canvasGroup.alpha = 1f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_gridController == null || _rootCanvas == null) return;

        _originalParent = transform.parent;
        _originalCell = _originalParent?.GetComponent<NodeCell>();
        _originalAnchoredPosition = _rectTransform.anchoredPosition;

        // Become transparent and STOP blocking raycasts so underlying cells can be detected
        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;

        // Reparent for top rendering
        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();
         // Debug.Log($"[NodeDraggable OnBeginDrag] Started drag. blocksRaycasts: {_canvasGroup.blocksRaycasts}", gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Only process drag if it's the left button and dragging is conceptually active (raycasts blocked)
        if (eventData.button != PointerEventData.InputButton.Left || _canvasGroup == null || _canvasGroup.blocksRaycasts) return;
        if (_gridController == null || _rootCanvas == null) return;

        // Move logic (unchanged)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
             _rootCanvas.transform as RectTransform, eventData.position,
             _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, out Vector2 currentLocalPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform, eventData.position - eventData.delta,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, out Vector2 previousLocalPoint);
        Vector2 localDelta = currentLocalPoint - previousLocalPoint;
        _rectTransform.localPosition += (Vector3)localDelta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
         // Debug.Log($"[NodeDraggable OnEndDrag] Drag ended. Button: {eventData.button}", gameObject);

        // Ensure visuals and raycast blocking are reset REGARDLESS of button, BEFORE handling drop
        if (_canvasGroup != null) {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true; // CRITICAL: Re-enable raycasts immediately
             // Debug.Log($"--> Reset alpha and blocksRaycasts to: {_canvasGroup.blocksRaycasts}");
        }

        // Only handle the drop logic if the drag was initiated by the left button
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_gridController != null) {
                _gridController.HandleNodeDrop(this, _originalCell, eventData.position);
            } else {
                 // No controller, attempt reset
                 ResetPosition();
            }
        }
        else
        {
             // If drag ended via another button (unlikely but possible), ensure reset
             ResetPosition();
        }
    }

    public void ResetPosition()
    {
        // Debug.Log($"[NodeDraggable ResetPosition] Resetting {gameObject.name}", gameObject);
        // Reset parent and position
        transform.SetParent(_originalParent, false);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
        _originalCell = _originalParent?.GetComponent<NodeCell>(); // Update cell ref

        // Ensure visuals and raycasts are correct after reset
        if (_canvasGroup != null) {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        // Update parent cell reference on the NodeView
        NodeView view = GetComponent<NodeView>();
        view?.UpdateParentCellReference();
    }

    public void SnapToCell(NodeCell targetCell)
    {
        // Debug.Log($"[NodeDraggable SnapToCell] Snapping {gameObject.name} to Cell {targetCell?.CellIndex}", gameObject);
        if (targetCell == null) { ResetPosition(); return; }

        // Set Parent and Position
        transform.SetParent(targetCell.transform, false);
        _rectTransform.anchoredPosition = Vector2.zero;

        // Update Internal Draggable References
        _originalParent = targetCell.transform;
        _originalCell = targetCell;
        _originalAnchoredPosition = Vector2.zero;

        // Ensure visuals and raycasts are correct after snap
        if (_canvasGroup != null) {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        // Update the NodeView's parent reference
        NodeView view = GetComponent<NodeView>();
        view?.UpdateParentCellReference();
    }

    // OnDisable might not be strictly needed now but can be kept as safety
    void OnDisable()
    {
        // If it was disabled mid-drag (blocksRaycasts == false), reset state
        if (_canvasGroup != null && !_canvasGroup.blocksRaycasts)
        {
             Debug.LogWarning($"[NodeDraggable] Disabled while dragging {gameObject.name}. Resetting CanvasGroup.", gameObject);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            // Resetting position might be desired but complex here, ensure visuals are ok.
        }
    }
}
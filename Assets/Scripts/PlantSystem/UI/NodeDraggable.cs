using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ItemView))] // Ensures an ItemView is always present
public class NodeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalAnchoredPosition;
    private Transform _originalParent;
    private NodeCell _originalCell;
    private NodeEditorGridController _gridController;
    private InventoryGridController _inventoryController;
    private Canvas _rootCanvas;
    private ItemView _itemView; // Reference to the unified view

    public NodeCell OriginalCell => _originalCell;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _itemView = GetComponent<ItemView>(); // Get the unified ItemView component
    }

    // A single Initialize method is now cleaner
    public void Initialize(NodeEditorGridController gridCtrl, NodeCell startingCell) => Initialize(gridCtrl, null, startingCell);
    public void Initialize(InventoryGridController invCtrl, NodeCell startingCell) => Initialize(null, invCtrl, startingCell);

    private void Initialize(NodeEditorGridController gridCtrl, InventoryGridController invCtrl, NodeCell startingCell)
    {
        _gridController = gridCtrl;
        _inventoryController = invCtrl;
        _originalCell = startingCell;

        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas ?? FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeDraggable] Could not find root Canvas!", gameObject);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_originalCell == null || _itemView == null) return;
        
        NodeData data = _itemView.GetNodeData();
        if (data == null) return;

        // Check if the node is allowed to be dragged
        bool isInSequenceEditorCell = _gridController != null && !_originalCell.IsInventoryCell && !_originalCell.IsSeedSlot;
        if (isInSequenceEditorCell && !data.canBeDeleted)
        {
            eventData.pointerDrag = null; // Prevent drag from starting
            return;
        }

        if (_rootCanvas == null) return;

        // Store original state
        _originalParent = transform.parent;
        _originalAnchoredPosition = _rectTransform.anchoredPosition;

        // Prepare for dragging
        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;

        // Reparent to the root canvas to render on top of everything
        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // Move to mouse position
        Vector2 mouseLocalPosInCanvas;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootCanvas.transform as RectTransform, eventData.position, _rootCanvas.worldCamera, out mouseLocalPosInCanvas);
        _rectTransform.localPosition = mouseLocalPosInCanvas;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _canvasGroup.blocksRaycasts) return;
        if (_rootCanvas == null || _rectTransform == null) return;

        Vector2 mouseLocalPosInCanvas;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootCanvas.transform as RectTransform, eventData.position, _rootCanvas.worldCamera, out mouseLocalPosInCanvas))
        {
            _rectTransform.localPosition = mouseLocalPosInCanvas;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            if (transform.parent == _rootCanvas.transform) ResetPosition();
            return;
        }

        // If not dropped on a valid target (IDropHandler), ResetPosition will be called by the target cell.
        // If the pointer isn't over anything, or the thing it's over doesn't have an IDropHandler,
        // we need to reset its position here.
        if (eventData.pointerEnter == null || eventData.pointerEnter.GetComponent<IDropHandler>() == null)
        {
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        if (_originalParent == null || _originalCell == null)
        {
            // This can happen if the original cell was destroyed during the drag.
            // In this case, we destroy the dragged object to prevent it from being orphaned.
            Destroy(gameObject);
            return;
        }

        transform.SetParent(_originalParent, false);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
    }

    public void SnapToCell(NodeCell targetCell)
    {
        if (targetCell == null)
        {
            ResetPosition();
            return;
        }

        transform.SetParent(targetCell.transform, false);
        _rectTransform.anchoredPosition = Vector2.zero;

        // Update the 'original' state to the new state
        _originalParent = targetCell.transform;
        _originalCell = targetCell;
        _originalAnchoredPosition = Vector2.zero;

        _itemView?.UpdateParentCellReference();

        // Update controller references based on the new cell type
        if (targetCell.IsInventoryCell)
        {
            _inventoryController = InventoryGridController.Instance;
            _gridController = null;
        }
        else // Sequence or Seed Slot
        {
            _inventoryController = null;
            _gridController = NodeEditorGridController.Instance;
        }
    }

    void OnDisable()
    {
        // Ensure raycasts are re-enabled if the object is disabled mid-drag
        if (_canvasGroup != null && !_canvasGroup.blocksRaycasts)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}
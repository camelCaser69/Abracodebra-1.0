// FILE: Assets/Scripts/Nodes/UI/NodeDraggable.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NodeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalAnchoredPosition; // Anchored position within its original cell
    private Transform _originalParent;         // The NodeCell's transform
    private NodeCell _originalCell;
    private NodeEditorGridController _gridController;
    private InventoryGridController _inventoryController;
    private Canvas _rootCanvas;
    // REMOVED: private Vector2 _dragOffset; // No longer needed for center-snap behavior

    public NodeCell OriginalCell => _originalCell;
    public bool IsFromInventoryGrid => _inventoryController != null;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
    }

    public void Initialize(NodeEditorGridController gridCtrl, InventoryGridController invCtrl, NodeCell startingCell)
    {
        _gridController = gridCtrl;
        _inventoryController = invCtrl;
        _originalCell = startingCell;

        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null) _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) Debug.LogError("[NodeDraggable] Could not find root Canvas!", gameObject);

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }
    }

    public void Initialize(NodeEditorGridController gridCtrl, NodeCell startingCell)
    {
        Initialize(gridCtrl, null, startingCell);
    }

    public void Initialize(InventoryGridController invCtrl, NodeCell startingCell)
    {
        Initialize(null, invCtrl, startingCell);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        NodeView view = GetComponent<NodeView>();
        if (view != null && view.GetNodeData() != null)
        {
            if (!IsFromInventoryGrid && view.GetNodeData().canBeDeleted == false && _gridController != null)
            {
                Debug.Log($"Node '{view.GetNodeData().nodeDisplayName}' is configured as non-draggable/non-deletable from sequence.", gameObject);
                eventData.pointerDrag = null;
                return;
            }
        }

        if (_rootCanvas == null)
        {
            Debug.LogError("[NodeDraggable] OnBeginDrag: Root Canvas is null! Cannot start drag.", gameObject);
            eventData.pointerDrag = null;
            return;
        }

        _originalParent = transform.parent;
        _originalAnchoredPosition = _rectTransform.anchoredPosition; // Should be (0,0) if snapped in cell

        _canvasGroup.alpha = 0.6f; // Ghost effect
        _canvasGroup.blocksRaycasts = false;

        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // --- MODIFICATION: Directly position the item's pivot under the mouse ---
        Vector2 mouseLocalPosInCanvas;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position,
            _rootCanvas.worldCamera,
            out mouseLocalPosInCanvas
        );
        _rectTransform.localPosition = mouseLocalPosInCanvas; // Item's pivot jumps to mouse
        // --- END MODIFICATION ---
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _canvasGroup == null || _canvasGroup.blocksRaycasts) return;
        if (_rootCanvas == null || _rectTransform == null) return;

        // --- MODIFICATION: Keep the item's pivot directly under the mouse ---
        Vector2 mouseLocalPosInCanvas;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position,
            _rootCanvas.worldCamera,
            out mouseLocalPosInCanvas))
        {
            _rectTransform.localPosition = mouseLocalPosInCanvas;
        }
        // --- END MODIFICATION ---
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            ResetPosition();
            return;
        }
        
        GameObject dropTargetGO = eventData.pointerEnter;
        NodeCell targetCell = dropTargetGO?.GetComponent<NodeCell>();

        if (targetCell == null) // Dropped outside a valid cell
        {
            ResetPosition();
        }
        // If targetCell is not null, NodeCell.OnDrop will handle the rest.
    }

    public void ResetPosition()
    {
        if (_originalParent == null || _originalCell == null)
        {
            Debug.LogWarning($"[NodeDraggable ResetPosition] {gameObject.name} missing original parent/cell. Disabling.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        transform.SetParent(_originalParent, false);
        _rectTransform.anchoredPosition = _originalAnchoredPosition; // This should be (0,0) for a snapped cell

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    public void SnapToCell(NodeCell targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning($"[NodeDraggable SnapToCell] TargetCell is null for {gameObject.name}. Attempting Reset.", gameObject);
            ResetPosition();
            return;
        }

        transform.SetParent(targetCell.transform, false);
        _rectTransform.anchoredPosition = Vector2.zero; // Snap to center (pivot) of the cell

        _originalParent = targetCell.transform;
        _originalCell = targetCell;
        _originalAnchoredPosition = Vector2.zero;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
        
        GetComponent<NodeView>()?.UpdateParentCellReference();

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
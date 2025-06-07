// FILE: Assets/Scripts/Nodes/UI/NodeDraggable.cs
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
    private NodeEditorGridController _gridController; // Controller if in sequence or seed slot
    private InventoryGridController _inventoryController; // Controller if in inventory
    private Canvas _rootCanvas;
    
    private Transform _temporaryParentStorage;

    public NodeCell OriginalCell => _originalCell;
    public bool IsFromInventoryGrid => _inventoryController != null && _gridController == null; // More specific check

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    // Call this after the NodeView is placed in a cell
    public void Initialize(NodeEditorGridController gridCtrl, InventoryGridController invCtrl, NodeCell startingCell)
    {
        _gridController = gridCtrl;
        _inventoryController = invCtrl;
        _originalCell = startingCell;

        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null) _rootCanvas = FindFirstObjectByType<Canvas>(); // Fallback
        if (_rootCanvas == null) Debug.LogError("[NodeDraggable] Could not find root Canvas!", gameObject);

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }
    }
    // Convenience initializers
    public void Initialize(NodeEditorGridController gridCtrl, NodeCell startingCell) => Initialize(gridCtrl, null, startingCell);
    public void Initialize(InventoryGridController invCtrl, NodeCell startingCell) => Initialize(null, invCtrl, startingCell);


    public void OnBeginDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;

    if (_originalCell == null)
    {
        Debug.LogError($"[NodeDraggable OnBeginDrag] Critical error: _originalCell is null for {gameObject.name}. Drag aborted.", gameObject);
        eventData.pointerDrag = null;
        return;
    }

    // FIXED: Handle both NodeView and ToolView components
    NodeView nodeView = GetComponent<NodeView>();
    ToolView toolView = GetComponent<ToolView>();
    NodeData data = null;

    if (nodeView != null)
    {
        data = nodeView.GetNodeData();
    }
    else if (toolView != null)
    {
        data = toolView.GetNodeData();
    }

    if (data == null)
    {
        Debug.LogError($"[NodeDraggable OnBeginDrag] Missing NodeView/ToolView or NodeData on {gameObject.name}. Drag aborted.", gameObject);
        eventData.pointerDrag = null;
        return;
    }

    // --- MODIFIED LOGIC TO ALLOW DRAGGING FROM SEED SLOT ---
    // Prevent dragging if:
    // 1. It's in a SEQUENCE EDITOR cell (not inventory, not seed slot)
    // AND
    // 2. Its NodeData.canBeDeleted is false (which we use as a proxy for non-movable in sequence)
    bool isInSequenceEditorCell = _gridController != null && _originalCell != null && !_originalCell.IsInventoryCell && !_originalCell.IsSeedSlot;

    if (isInSequenceEditorCell && !data.canBeDeleted)
    {
        Debug.Log($"Node '{data.nodeDisplayName}' in sequence editor cell {_originalCell.CellIndex} is configured as non-draggable/non-deletable.", gameObject);
        eventData.pointerDrag = null; // This prevents the drag from starting
        return;
    }

    if (_rootCanvas == null)
    {
        Debug.LogError("[NodeDraggable] OnBeginDrag: Root Canvas is null! Cannot start drag.", gameObject);
        eventData.pointerDrag = null;
        return;
    }

    _originalParent = transform.parent;
    _originalAnchoredPosition = _rectTransform.anchoredPosition;

    _canvasGroup.alpha = 0.6f;
    _canvasGroup.blocksRaycasts = false;

    transform.SetParent(_rootCanvas.transform, true);
    transform.SetAsLastSibling();

    Vector2 mouseLocalPosInCanvas;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _rootCanvas.transform as RectTransform,
        eventData.position,
        _rootCanvas.worldCamera,
        out mouseLocalPosInCanvas
    );
    _rectTransform.localPosition = mouseLocalPosInCanvas;
}

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _canvasGroup == null || _canvasGroup.blocksRaycasts) return;
        if (_rootCanvas == null || _rectTransform == null) return;

        Vector2 mouseLocalPosInCanvas;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position,
            _rootCanvas.worldCamera,
            out mouseLocalPosInCanvas))
        {
            _rectTransform.localPosition = mouseLocalPosInCanvas;
        }
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
            // If it wasn't a left-drag, or if drag was cancelled by setting pointerDrag = null
            if (transform.parent == _rootCanvas.transform) // Check if it was actually reparented for dragging
            {
                ResetPosition();
            }
            return;
        }
        
        GameObject dropTargetGO = eventData.pointerEnter;
        NodeCell targetCell = dropTargetGO?.GetComponent<NodeCell>();

        // If not dropped on a NodeCell, or if the drag was not processed by a NodeCell's OnDrop
        // (e.g. because targetCell was null or OnDrop did nothing and ResetPosition wasn't called by it),
        // then reset it here.
        // A successful drop via NodeCell.OnDrop would have called SnapToCell, changing the parent.
        if (targetCell == null || transform.parent == _rootCanvas.transform)
        {
            ResetPosition();
        }
        // If dropped on a cell, NodeCell.OnDrop handles the logic, including calling SnapToCell or ResetPosition.
    }
    
    public void SetTemporaryParent(Transform tempParent)
    {
        _temporaryParentStorage = tempParent;
    }
    
    public void RestoreToTemporaryParent()
    {
        if (_temporaryParentStorage != null)
        {
            transform.SetParent(_temporaryParentStorage, false);
            _temporaryParentStorage = null;
        }
    }

    public void ResetPosition()
    {
        if (_originalParent == null || _originalCell == null)
        {
            // This can happen if the original cell/parent was destroyed during drag
            Debug.LogWarning($"[NodeDraggable ResetPosition] {gameObject.name} missing original parent/cell. Object might be orphaned or destroyed.", gameObject);
            // Consider destroying the object if it's truly orphaned to prevent issues
            // if (Application.isPlaying) Destroy(gameObject);
            return;
        }

        transform.SetParent(_originalParent, false);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;

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
        _rectTransform.anchoredPosition = Vector2.zero;

        // Update original state to the new cell
        _originalParent = targetCell.transform;
        _originalCell = targetCell;
        _originalAnchoredPosition = Vector2.zero; // It's now centered in the new cell

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
        
        GetComponent<NodeView>()?.UpdateParentCellReference();

        // Update controller references based on the target cell's type
        if (targetCell.IsInventoryCell)
        {
            _inventoryController = InventoryGridController.Instance;
            _gridController = null;
        }
        else if (targetCell.IsSeedSlot)
        {
            _inventoryController = null;
            _gridController = NodeEditorGridController.Instance; // Seed slot is managed by NodeEditorGridController
        }
        else // It's a sequence editor cell
        {
            _inventoryController = null;
            _gridController = NodeEditorGridController.Instance;
        }
    }

    void OnDisable()
    {
        // If the draggable is disabled mid-drag (e.g. parent panel hidden), ensure raycasts are re-enabled.
        if (_canvasGroup != null && _canvasGroup.blocksRaycasts == false)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            // Potentially reset position too, if that's desired behavior for an interrupted drag.
            // if (transform.parent == _rootCanvas?.transform) ResetPosition();
        }
    }
}
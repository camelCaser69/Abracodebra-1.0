using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drags a node in its parent's local space, correctly handling parent scaling & panning.
/// Attach this to each node (NodeView prefab).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class NodeDraggable : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private RectTransform nodeRect;      // The node's own RectTransform
    private RectTransform parentRect;    // The parent panel's RectTransform
    private Vector2 pointerOffset;       // Offset between pointer & node's anchoredPosition

    private void Awake()
    {
        nodeRect = GetComponent<RectTransform>();
        parentRect = nodeRect.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogError("[NodeDraggable] No parent RectTransform found! Dragging won't work.");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Convert the pointer's screen position to the parent's local coordinates.
        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out localPointerPos))
        {
            // The offset is (nodePosition - pointerPosition) in parent's local space.
            pointerOffset = nodeRect.anchoredPosition - localPointerPos;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out localPointerPos))
        {
            // On drag, update the node's anchoredPosition so it follows the pointer + offset.
            nodeRect.anchoredPosition = localPointerPos + pointerOffset;
        }
    }
}
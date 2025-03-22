using UnityEngine;
using UnityEngine.EventSystems;

public class NodeHexDraggable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform nodeRect;
    private RectTransform parentRect;
    private Vector2 pointerOffset;
    private NodeView nodeView;
    private float hexSize;

    private void Awake()
    {
        nodeRect = GetComponent<RectTransform>();
        parentRect = nodeRect.parent as RectTransform;
        nodeView = GetComponent<NodeView>();
        hexSize = (HexGridManager.Instance != null) ? HexGridManager.Instance.hexSize : 50f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            pointerOffset = nodeRect.anchoredPosition - localPoint;
        }
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            nodeRect.anchoredPosition = localPoint + pointerOffset;
        }
        eventData.Use();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Vector2 currentPos = nodeRect.anchoredPosition;
        float hexSizeValue = hexSize;
        HexCoords coords = HexCoords.WorldToHex(currentPos, hexSizeValue);
        Vector2 snappedPos = coords.HexToWorld(hexSizeValue);
        nodeRect.anchoredPosition = snappedPos;
        if (nodeView != null)
        {
            nodeView.GetNodeData().coords = coords;
            nodeView.GetNodeData().editorPosition = snappedPos;
        }
        eventData.Use();
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private NodeEditorGridController gridController;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        gridController = GetComponentInParent<NodeEditorGridController>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        if (gridController != null)
        {
            bool dropped = gridController.HandleNodeDrop(this, eventData.position);
            if (!dropped)
            {
                rectTransform.anchoredPosition = originalPosition;
                transform.SetParent(originalParent);
            }
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
            transform.SetParent(originalParent);
        }
    }

    public void SetParent(Transform newParent)
    {
        transform.SetParent(newParent, false);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
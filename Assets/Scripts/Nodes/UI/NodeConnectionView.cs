using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class NodeConnectionView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform startRect;
    [SerializeField] private RectTransform endRect;

    public PinView sourcePin;
    public PinView targetPin;

    private UICubicBezier bezier;
    private RectTransform ownRect;

    private bool isPreviewing = false;

    private void Awake()
    {
        ownRect = GetComponent<RectTransform>();
        bezier  = GetComponent<UICubicBezier>();
    }

    private void Update()
    {
        if (bezier == null || startRect == null) return;

        Vector2 localStart;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ownRect, startRect.position, null, out localStart);

        Vector2 localEnd;
        if (!isPreviewing && endRect != null)
        {
            // final connection
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ownRect, endRect.position, null, out localEnd);
        }
        else
        {
            // preview => use the mouse position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ownRect, Input.mousePosition, null, out localEnd);
        }

        bezier.UpdateCurve(localStart, localEnd);
    }

    public void StartPreview(RectTransform source)
    {
        startRect    = source;
        endRect      = null;
        isPreviewing = true;
    }

    public void FinalizeConnection(RectTransform end)
    {
        endRect      = end;
        isPreviewing = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only called if user right-clicks near the line (due to custom raycast)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // remove adjacency logic, etc.

            Debug.Log("[NodeConnectionView] Deleting line");
            Destroy(gameObject);
        }
    }
}
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
    private bool isFinalized = false;

    private void Awake()
    {
        ownRect = GetComponent<RectTransform>();
        bezier  = GetComponent<UICubicBezier>();
    }

    private void Update()
    {
        // If finalized and either pin is missing, destroy self.
        if (isFinalized && (sourcePin == null || targetPin == null))
        {
            Destroy(gameObject);
            return;
        }

        if (bezier == null || startRect == null)
            return;

        Vector2 localStart;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ownRect, startRect.position, null, out localStart);

        Vector2 localEnd;
        if (!isPreviewing && endRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ownRect, endRect.position, null, out localEnd);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ownRect, Input.mousePosition, null, out localEnd);
        }

        bezier.UpdateCurve(localStart, localEnd);
    }

    public void StartPreview(RectTransform source)
    {
        startRect = source;
        endRect = null;
        isPreviewing = true;
    }

    public void FinalizeConnection(RectTransform end)
    {
        endRect = end;
        isPreviewing = false;
        isFinalized = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only delete if right-clicked near the curve (custom Raycast in UICubicBezier).
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("[NodeConnectionView] Deleting line");
            Destroy(gameObject);
        }
    }
}

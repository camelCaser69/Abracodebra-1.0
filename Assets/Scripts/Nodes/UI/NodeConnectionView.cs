/* Assets/Scripts/Nodes/UI/NodeConnectionView.cs
   Draws a line (UI-based) between two node ports in the editor. */

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class NodeConnectionView : MonoBehaviour
{
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform endPoint;

    private RectTransform lineRect;
    private Image lineImage;

    private void Awake()
    {
        lineRect = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (startPoint != null && endPoint != null)
            UpdateLine();
    }

    private void UpdateLine()
    {
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        // Position at the midpoint.
        lineRect.position = startPos + direction * 0.5f;
        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetConnectionPoints(RectTransform start, RectTransform end)
    {
        startPoint = start;
        endPoint = end;
    }

    // For dynamic updating by PinView dragging.
    public void UpdateEndPoint(Vector2 localEndPoint)
    {
        if (startPoint != null)
        {
            // Convert screen space to local space inside the canvas
            Vector2 worldStart = startPoint.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                startPoint.parent as RectTransform, worldStart, null, out Vector2 localStart);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                startPoint.parent as RectTransform, localEndPoint, null, out Vector2 localEnd);

            // Update line's endpoints
            SetConnectionPoints(startPoint, startPoint); // Keep source fixed
            endPoint.anchoredPosition = localEnd;  // Move the end dynamically
        }
    }
}

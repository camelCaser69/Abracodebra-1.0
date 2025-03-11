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
        {
            UpdateLine();
        }
    }

    private void UpdateLine()
    {
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        // Move line to midpoint
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
}
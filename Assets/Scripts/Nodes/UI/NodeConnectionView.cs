// Assets/Scripts/Nodes/UI/NodeConnectionView.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class NodeConnectionView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform startRect;
    [SerializeField] private RectTransform endRect;

    // Keep track of the source and target pins for easy disconnection.
    public PinView sourcePin;
    public PinView targetPin;

    private RectTransform lineRect;
    private Image lineImage;

    private void Awake()
    {
        lineRect = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (startRect && endRect)
        {
            UpdateLine();
        }
    }

    public void SetStartRect(RectTransform start)
    {
        startRect = start;
    }

    public void SetEndRect(RectTransform end)
    {
        endRect = end;
    }

    public void SetEndPosition(Vector2 localPos, RectTransform parentCanvas)
    {
        if (!endRect)
        {
            GameObject endObj = new GameObject("TempEndRect", typeof(RectTransform));
            endObj.transform.SetParent(parentCanvas);
            endRect = endObj.GetComponent<RectTransform>();
            endRect.sizeDelta = Vector2.zero;
        }
        endRect.anchoredPosition = localPos;
    }

    private void UpdateLine()
    {
        Vector3 startPos = startRect.position;
        Vector3 endPos = endRect.position;
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        lineRect.position = startPos + direction * 0.5f;
        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ============= Right-Click to Delete the Connection =============
    public void OnPointerClick(PointerEventData eventData)
    {
        // If right-clicked
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Remove references from source/target
            if (sourcePin != null && targetPin != null)
            {
                sourcePin.port.connectedPortIds.Remove(targetPin.port.portId);
                // If you store the reverse connection, remove that too
            }

            // Destroy this connection line
            Destroy(gameObject);
        }
    }
}

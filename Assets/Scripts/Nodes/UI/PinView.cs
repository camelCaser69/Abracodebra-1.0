using UnityEngine;
using UnityEngine.EventSystems;

public class PinView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public NodePort port;
    public bool isInput; // True for input pins, false for output pins.
    private RectTransform rectTransform;
    private NodeConnectionView currentConnection;
    private NodeEditorController nodeEditor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(NodePort nodePort, bool isInputPin, NodeEditorController editor)
    {
        port = nodePort;
        isInput = isInputPin;
        nodeEditor = editor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Start connection only from output pins.
        if (!isInput)
            currentConnection = nodeEditor.StartConnectionFromPin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentConnection != null)
        {
            // Update the connection line's end position.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                nodeEditor.EditorCanvas, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            currentConnection.UpdateEndPoint(localPoint);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentConnection != null)
        {
            GameObject pointerObj = eventData.pointerCurrentRaycast.gameObject;
            if (pointerObj != null)
            {
                PinView targetPin = pointerObj.GetComponent<PinView>();
                if (targetPin != null && targetPin.isInput)
                {
                    nodeEditor.CompleteConnection(this, targetPin, currentConnection);
                }
                else
                {
                    nodeEditor.CancelConnection(currentConnection);
                }
            }
            else
            {
                nodeEditor.CancelConnection(currentConnection);
            }
            currentConnection = null;
        }
    }
}

// Assets/Scripts/Nodes/UI/PinView.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class PinView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public NodePort port;
    public bool isInput;
    private NodeEditorController nodeEditor;

    public void Initialize(NodePort nodePort, bool isInputPin, NodeEditorController editor)
    {
        port = nodePort;
        isInput = isInputPin;
        nodeEditor = editor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[PinView] Clicked on {(isInput ? "Input" : "Output")} port of node: {port.portName}");

        if (!isInput && nodeEditor != null)
        {
            Debug.Log("[PinView] Starting connection drag...");
            nodeEditor.StartConnectionDrag(this, eventData);
        }
        else
        {
            Debug.LogWarning("[PinView] Attempted to start a connection from an input pin.");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (nodeEditor != null)
        {
            nodeEditor.UpdateConnectionDrag(this, eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (nodeEditor != null)
        {
            nodeEditor.EndConnectionDrag(this, eventData);
        }
    }
}
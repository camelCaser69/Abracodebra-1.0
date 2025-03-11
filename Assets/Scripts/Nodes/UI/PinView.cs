// Assets/Scripts/Nodes/UI/PinView.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class PinView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public NodePort port;
    public bool isInput; 
    private NodeEditorController nodeEditor;
    
    // We do NOT store the connection line here; the controller manages it.
    // We'll just let the controller know which pin the user is dragging from/to.

    public void Initialize(NodePort nodePort, bool isInputPin, NodeEditorController editor)
    {
        port = nodePort;
        isInput = isInputPin;
        nodeEditor = editor;
    }

    // Called on mouse down.
    public void OnPointerDown(PointerEventData eventData)
    {
        // Typically, we only start a new connection from an output pin.
        // But if you want to allow “reverse” connection from input to output, remove the check.
        if (!isInput)
        {
            nodeEditor.StartConnectionDrag(this, eventData);
        }
    }

    // Called continuously while dragging.
    public void OnDrag(PointerEventData eventData)
    {
        nodeEditor.UpdateConnectionDrag(this, eventData);
    }

    // Called once on mouse up.
    public void OnPointerUp(PointerEventData eventData)
    {
        nodeEditor.EndConnectionDrag(this, eventData);
    }
}
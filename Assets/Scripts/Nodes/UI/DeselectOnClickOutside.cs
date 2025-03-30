using UnityEngine;
using UnityEngine.EventSystems; // Required for IPointerClickHandler

// Add this script to an invisible, fullscreen background UI Image
// Make sure it's the first child of the Canvas to render behind everything else
public class DeselectOnClickOutside : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if the click was with the left mouse button
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // If the background is clicked, clear the current node selection
            // Debug.Log("Background Clicked. Clearing Node Selection.");
            NodeCell.ClearSelection();
        }
    }
}
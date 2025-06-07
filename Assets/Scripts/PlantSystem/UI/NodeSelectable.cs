using UnityEngine;
using UnityEngine.EventSystems;

public class NodeSelectable : MonoBehaviour, IPointerClickHandler
{
    public static GameObject CurrentSelected { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select(gameObject);
    }

    public static void Select(GameObject node)
    {
        if (CurrentSelected != null && CurrentSelected != node)
        {
            // (Optional) Remove highlight from previously selected node.
        }
        CurrentSelected = node;
        // (Optional) Add highlight effect here.
    }
}
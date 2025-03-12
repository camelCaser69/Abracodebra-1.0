using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NodeSelectable : MonoBehaviour, IPointerClickHandler
{
    private static GameObject currentSelected;

    // Reference to an Outline component added to the NodeView prefab.
    [SerializeField] private Outline outline;

    public void OnPointerClick(PointerEventData eventData)
    {
        Select(gameObject);
    }

    public static void Select(GameObject node)
    {
        if (currentSelected != null && currentSelected != node)
        {
            // Remove highlight from previously selected node.
            var prevOutline = currentSelected.GetComponent<Outline>();
            if (prevOutline != null)
                prevOutline.enabled = false;
        }
        currentSelected = node;
        var outlineComp = node.GetComponent<Outline>();
        if (outlineComp != null)
            outlineComp.enabled = true;
    }
}
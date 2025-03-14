using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NodeSelectable : MonoBehaviour, IPointerClickHandler
{
    // Public static property to hold the currently selected node.
    public static GameObject CurrentSelected { get; set; }

    [SerializeField] private Outline outline;

    private void Awake()
    {
        if (outline != null)
            outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select(gameObject);
    }

    public static void Select(GameObject node)
    {
        if (CurrentSelected != null && CurrentSelected != node)
        {
            // Disable outline on previously selected node.
            var prevOutline = CurrentSelected.GetComponent<Outline>();
            if (prevOutline != null)
                prevOutline.enabled = false;
        }
        CurrentSelected = node;
        var outlineComp = node.GetComponent<Outline>();
        if (outlineComp != null)
            outlineComp.enabled = true;
    }
}
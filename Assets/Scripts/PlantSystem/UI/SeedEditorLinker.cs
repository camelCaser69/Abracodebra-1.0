using UnityEngine;
using UnityEngine.EventSystems;
using Abracodabra.UI.Genes;

[RequireComponent(typeof(InventoryGridController))]
public class SeedEditorLinker : MonoBehaviour
{
    [Tooltip("The Gene Sequence UI that will display the contents of the selected seed.")]
    [SerializeField]
    private GeneSequenceUI geneSequenceUI;

    private InventoryGridController inventoryGridController;

    void Awake()
    {
        inventoryGridController = GetComponent<InventoryGridController>();
        if (geneSequenceUI == null)
        {
            // Try to find it if not assigned
            geneSequenceUI = FindObjectOfType<GeneSequenceUI>();
        }
        
        if (geneSequenceUI == null)
        {
            Debug.LogError("[SeedEditorLinker] GeneSequenceUI reference is missing! The seed editor will not open.", this);
            enabled = false;
        }
    }

    void Update()
    {
        // Check for a left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Use the EventSystem to find what UI element was clicked on
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            // Go through the results to find a GeneSlotUI
            foreach (var result in results)
            {
                GeneSlotUI clickedSlot = result.gameObject.GetComponentInParent<GeneSlotUI>();
                if (clickedSlot != null && clickedSlot.CurrentItem != null)
                {
                    // Check if the slot belongs to our inventory grid
                    if (clickedSlot.transform.IsChildOf(inventoryGridController.transform))
                    {
                        // If the clicked item is a seed, load it into the editor
                        if (clickedSlot.CurrentItem.Type == InventoryBarItem.ItemType.Seed)
                        {
                            Debug.Log($"Loading seed '{clickedSlot.CurrentItem.GetDisplayName()}' into editor.");
                            geneSequenceUI.LoadRuntimeState(clickedSlot.CurrentItem.SeedRuntimeState);
                            // We found our target, no need to check further
                            return; 
                        }
                    }
                }
            }
        }
    }
}
// REWORKED FILE: Assets/Scripts/UI/Genes/GeneSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Genes
{
    // This now represents a generic inventory/sequence slot, not just for genes.
    public class GeneSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public GeneCategory acceptedCategory; // Still useful for sequence slots
        public int slotIndex;
        public bool isLocked = false;
        
        [Header("Visuals")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private GameObject emptyIndicator;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject invalidDropOverlay;
        [SerializeField] private ItemView itemView; // The view that shows the item's icon

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.gray;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color invalidColor = Color.red;

        public InventoryBarItem CurrentItem { get; private set; }

        private void Awake()
        {
            // The ItemView should be a child of this slot object.
            if (itemView == null)
            {
                itemView = GetComponentInChildren<ItemView>();
            }
            if (itemView == null)
            {
                Debug.LogError($"GeneSlotUI on {gameObject.name} is missing its ItemView child component.", this);
            }
        }

        public void SetItem(InventoryBarItem item)
        {
            CurrentItem = item;
            UpdateVisuals();
        }

        public void ClearSlot()
        {
            SetItem(null);
        }

        private void UpdateVisuals()
        {
            bool isEmpty = CurrentItem == null || !CurrentItem.IsValid();

            if (emptyIndicator != null) emptyIndicator.SetActive(isEmpty);
            if (itemView != null) itemView.gameObject.SetActive(!isEmpty);

            if (!isEmpty)
            {
                switch(CurrentItem.Type)
                {
                    case InventoryBarItem.ItemType.Gene:
                        itemView.InitializeAsGene(CurrentItem.GeneInstance);
                        break;
                    case InventoryBarItem.ItemType.Seed:
                        itemView.InitializeAsSeed(CurrentItem.SeedTemplate);
                        break;

                    case InventoryBarItem.ItemType.Tool:
                        itemView.InitializeAsTool(CurrentItem.ToolDefinition);
                        break;
                }
            }
            else
            {
                 if (slotBackground != null) slotBackground.color = normalColor;
            }

            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (isLocked) return;

            GeneSlotUI sourceSlot = eventData.pointerDrag?.GetComponent<GeneSlotUI>();
            if (sourceSlot == null || sourceSlot == this || sourceSlot.CurrentItem == null) return;
            
            // For now, just a simple swap. More complex validation could be added.
            var itemFromSource = sourceSlot.CurrentItem;
            var itemFromThisSlot = this.CurrentItem;
            
            this.SetItem(itemFromSource);
            sourceSlot.SetItem(itemFromThisSlot);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Simplified for brevity
            if (isLocked || slotBackground == null || eventData.pointerDrag == null) return;
            slotBackground.color = highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
             if (slotBackground != null) slotBackground.color = normalColor;
        }
    }
}
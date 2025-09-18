// File: Assets/Scripts/UI/Genes/SequenceRowUI.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Genes
{
    public class SequenceRowUI : MonoBehaviour
    {
        public GeneSlotUI modifierSlot;
        public GeneSlotUI activeSlot;
        public GeneSlotUI payloadSlot;

        private int rowIndex;
        private GeneSequenceUI parentSequence;

        public void Initialize(int index, GeneSequenceUI parent)
        {
            rowIndex = index;
            parentSequence = parent;

            if (modifierSlot != null)
            {
                modifierSlot.acceptedCategory = GeneCategory.Modifier;
                modifierSlot.slotIndex = index;
            }
            if (activeSlot != null)
            {
                activeSlot.acceptedCategory = GeneCategory.Active;
                activeSlot.slotIndex = index;
            }
            if (payloadSlot != null)
            {
                payloadSlot.acceptedCategory = GeneCategory.Payload;
                payloadSlot.slotIndex = index;
            }
        }

        public void LoadSlot(RuntimeSequenceSlot slotData)
        {
            activeSlot?.SetItem(slotData.activeInstance != null ? InventoryBarItem.FromGene(slotData.activeInstance) : null);
            
            var modInstance = slotData.modifierInstances.Count > 0 ? slotData.modifierInstances[0] : null;
            modifierSlot?.SetItem(modInstance != null ? InventoryBarItem.FromGene(modInstance) : null);
            
            var payloadInstance = slotData.payloadInstances.Count > 0 ? slotData.payloadInstances[0] : null;
            payloadSlot?.SetItem(payloadInstance != null ? InventoryBarItem.FromGene(payloadInstance) : null);

            UpdateAttachmentSlots(GetActiveGene());
        }

        public void ClearRow() 
        {
            modifierSlot?.ClearSlot();
            activeSlot?.ClearSlot();
            payloadSlot?.ClearSlot();
            UpdateAttachmentSlots(null);
        }

        public ActiveGene GetActiveGene()
        {
            // Get the item, check if it's a gene, then get the gene and cast it
            return activeSlot?.CurrentItem?.GeneInstance?.GetGene<ActiveGene>();
        }

        public void UpdateAttachmentSlots(ActiveGene activeGene)
        {
            bool hasActive = activeGene != null;
            if (modifierSlot != null)
            {
                modifierSlot.isLocked = !hasActive;
                modifierSlot.gameObject.SetActive(hasActive && activeGene.slotConfig.modifierSlots > 0);
            }
            if (payloadSlot != null)
            {
                payloadSlot.isLocked = !hasActive;
                payloadSlot.gameObject.SetActive(hasActive && activeGene.slotConfig.payloadSlots > 0);
            }
        }
    }
}
// File: Assets/Scripts/UI/Genes/SequenceRowUI.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Genes
{
    /// <summary>
    /// Manages a single row in the active sequence UI, containing modifier, active, and payload slots.
    /// </summary>
    public class SequenceRowUI : MonoBehaviour
    {
        [Header("Slot References")]
        public GeneSlotUI modifierSlot;
        public GeneSlotUI activeSlot;
        public GeneSlotUI payloadSlot;

        private int rowIndex;
        private GeneSequenceUI parentSequence;

        public void Initialize(int index, GeneSequenceUI parent)
        {
            rowIndex = index;
            parentSequence = parent;

            // Configure slots
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
            activeSlot?.SetGeneInstance(slotData.activeInstance);
            
            // For simplicity in this example, we assume one modifier/payload slot.
            // A real implementation would handle lists.
            modifierSlot?.SetGeneInstance(slotData.modifierInstances.Count > 0 ? slotData.modifierInstances[0] : null);
            payloadSlot?.SetGeneInstance(slotData.payloadInstances.Count > 0 ? slotData.payloadInstances[0] : null);

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
            return activeSlot?.GetGeneInstance()?.GetGene<ActiveGene>();
        }

        public void UpdateAttachmentSlots(ActiveGene activeGene)
        {
            // Lock/unlock modifier and payload slots based on the active gene
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
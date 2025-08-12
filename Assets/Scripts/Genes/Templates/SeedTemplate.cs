using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Templates
{
    [CreateAssetMenu(fileName = "NewSeedTemplate", menuName = "Abracodabra/Genes/Seed Template")]
    public class SeedTemplate : ScriptableObject
    {
        public string templateName;
        public string description;
        public Sprite icon;

        [Header("Gene Slot Configuration")]
        [Range(1, 8)] public int passiveSlotCount = 3;
        [Range(1, 8)] public int activeSequenceLength = 3;

        [Header("Default Gene Loadout")]
        public List<GeneTemplateEntry> passiveGenes = new List<GeneTemplateEntry>();
        public List<SequenceSlotTemplate> activeSequence = new List<SequenceSlotTemplate>();

        [Header("Plant Base Stats")]
        public int baseRechargeTime = 3;
        public float energyRegenRate = 10f;
        public float maxEnergy = 100f;

        [Header("Unlocking")]
        public bool isUnlocked = true;
        public List<string> unlockRequirements = new List<string>();

        // REWRITTEN and CORRECTED IsValid() method
        public bool IsValid()
        {
            bool hasAtLeastOneActiveGene = false;

            // Go through each slot in the sequence
            foreach (var slot in activeSequence)
            {
                // If the slot has no active gene, it's just an empty slot. This is perfectly valid, so we skip it.
                if (slot.activeGene == null)
                {
                    continue;
                }

                // If we find a slot that IS configured, we mark that the template has an active gene.
                hasAtLeastOneActiveGene = true;

                // Now, for this configured slot, we must ensure its own configuration is valid (e.g., not too many modifiers).
                if (!slot.Validate())
                {
                    // If a configured slot is invalid, the entire template is invalid.
                    return false;
                }
            }

            // The template is only valid if we found at least one configured active gene.
            // A template with zero active genes is not plantable.
            return hasAtLeastOneActiveGene;
        }

        public PlantGeneRuntimeState CreateRuntimeState()
        {
            var state = new PlantGeneRuntimeState();
            state.template = this;
            state.InitializeFromTemplate();
            return state;
        }

        private void OnValidate()
        {
            while (passiveGenes.Count < passiveSlotCount)
            {
                passiveGenes.Add(new GeneTemplateEntry());
            }
            while (passiveGenes.Count > passiveSlotCount)
            {
                passiveGenes.RemoveAt(passiveGenes.Count - 1);
            }

            while (activeSequence.Count < activeSequenceLength)
            {
                activeSequence.Add(new SequenceSlotTemplate());
            }
            while (activeSequence.Count > activeSequenceLength)
            {
                activeSequence.RemoveAt(activeSequence.Count - 1);
            }
        }
    }

    [System.Serializable]
    public class GeneTemplateEntry
    {
        public GeneBase gene;
        [Range(0f, 5f)]
        public float powerMultiplier = 1f;
    }

    [System.Serializable]
    public class SequenceSlotTemplate
    {
        public ActiveGene activeGene;
        public List<GeneTemplateEntry> modifiers = new List<GeneTemplateEntry>();
        public List<GeneTemplateEntry> payloads = new List<GeneTemplateEntry>();

        public bool Validate()
        {
            if (activeGene == null) return true; // An empty slot is inherently valid.
            if (modifiers.Count > activeGene.slotConfig.modifierSlots) return false;
            if (payloads.Count > activeGene.slotConfig.payloadSlots) return false;

            var modifierGenes = modifiers.Select(m => m.gene as ModifierGene).ToList();
            var payloadGenes = payloads.Select(p => p.gene as PayloadGene).ToList();

            return activeGene.IsValidConfiguration(modifierGenes, payloadGenes);
        }
    }
}
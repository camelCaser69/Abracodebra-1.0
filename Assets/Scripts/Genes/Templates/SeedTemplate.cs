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
        // FIX: Added these two fields. They are required by GeneSequenceUI.
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

        public bool IsValid()
        {
            if (activeSequence.Count == 0) return false;
            foreach (var slot in activeSequence)
            {
                if (slot.activeGene == null) return false;
                if (!slot.Validate()) return false;
            }
            return true;
        }

        public PlantGeneRuntimeState CreateRuntimeState()
        {
            var state = new PlantGeneRuntimeState();
            state.template = this;
            state.InitializeFromTemplate();
            return state;
        }

        // This method is useful for ensuring the data structure matches the counts.
        private void OnValidate()
        {
            // Ensure passive gene list matches the slot count
            while (passiveGenes.Count < passiveSlotCount)
            {
                passiveGenes.Add(new GeneTemplateEntry());
            }
            while (passiveGenes.Count > passiveSlotCount)
            {
                passiveGenes.RemoveAt(passiveGenes.Count - 1);
            }

            // Ensure active sequence list matches the length
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
            if (activeGene == null) return false;
            if (modifiers.Count > activeGene.slotConfig.modifierSlots) return false;
            if (payloads.Count > activeGene.slotConfig.payloadSlots) return false;

            var modifierGenes = modifiers.Select(m => m.gene as ModifierGene).ToList();
            var payloadGenes = payloads.Select(p => p.gene as PayloadGene).ToList();

            return activeGene.IsValidConfiguration(modifierGenes, payloadGenes);
        }
    }
}
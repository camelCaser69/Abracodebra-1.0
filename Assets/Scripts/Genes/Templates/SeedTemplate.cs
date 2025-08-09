using UnityEngine;
using System.Collections.Generic;
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

        [Header("Gene Configuration")]
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
        // FIX: Use GeneTemplateEntry to store power multipliers
        public List<GeneTemplateEntry> modifiers = new List<GeneTemplateEntry>();
        public List<GeneTemplateEntry> payloads = new List<GeneTemplateEntry>();

        public bool Validate()
        {
            if (activeGene == null) return false;

            if (modifiers.Count > activeGene.slotConfig.modifierSlots) return false;
            if (payloads.Count > activeGene.slotConfig.payloadSlots) return false;

            // Note: This validation might need to be updated to extract the GeneBase from the entry
            var modifierGenes = modifiers.ConvertAll(m => m.gene as ModifierGene);
            var payloadGenes = payloads.ConvertAll(p => p.gene as PayloadGene);

            return activeGene.IsValidConfiguration(modifierGenes, payloadGenes);
        }
    }
}
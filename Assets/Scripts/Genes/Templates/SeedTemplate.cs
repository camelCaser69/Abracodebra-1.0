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

        [Header("Slot Configuration")]
        [Range(1, 8)] public int passiveSlotCount = 3;
        [Range(1, 8)] public int activeSequenceLength = 3;

        public List<GeneTemplateEntry> passiveGenes = new List<GeneTemplateEntry>();
        public List<SequenceSlotTemplate> activeSequence = new List<SequenceSlotTemplate>();

        [Header("Growth Parameters")]
        [Range(0f, 1f)] public float baseGrowthChance = 0.1f;
        public int minHeight = 3;
        public int maxHeight = 5;
        public int leafDensity = 2;
        public int leafGap = 1;

        [Header("Energy & Sequence")]
        public int baseRechargeTime = 3;
        public float energyRegenRate = 10f;
        public float maxEnergy = 100f;
        public float startingEnergy = 0f; // <-- NEW FIELD

        [Header("Unlocking")]
        public bool isUnlocked = true;
        public List<string> unlockRequirements = new List<string>();

        public bool IsValid()
        {
            bool hasAtLeastOneActiveGene = false;
            foreach (var slot in activeSequence)
            {
                if (slot.activeGene == null) continue;
                hasAtLeastOneActiveGene = true;
                if (!slot.Validate()) return false;
            }
            return hasAtLeastOneActiveGene;
        }

        public PlantGeneRuntimeState CreateRuntimeState()
        {
            var state = new PlantGeneRuntimeState();
            state.template = this;
            state.InitializeFromTemplate();
            return state;
        }

        void OnValidate()
        {
            while (passiveGenes.Count < passiveSlotCount) passiveGenes.Add(new GeneTemplateEntry());
            while (passiveGenes.Count > passiveSlotCount) passiveGenes.RemoveAt(passiveGenes.Count - 1);

            while (activeSequence.Count < activeSequenceLength) activeSequence.Add(new SequenceSlotTemplate());
            while (activeSequence.Count > activeSequenceLength) activeSequence.RemoveAt(activeSequence.Count - 1);
        }
    }

    [System.Serializable]
    public class GeneTemplateEntry
    {
        public GeneBase gene;
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
            if (activeGene == null) return true; // Empty slot is valid

            if (modifiers.Count > activeGene.slotConfig.modifierSlots) return false;
            if (payloads.Count > activeGene.slotConfig.payloadSlots) return false;

            var modifierGenes = modifiers.Select(m => m.gene as ModifierGene).ToList();
            var payloadGenes = payloads.Select(p => p.gene as PayloadGene).ToList();

            return activeGene.IsValidConfiguration(modifierGenes, payloadGenes);
        }
    }
}
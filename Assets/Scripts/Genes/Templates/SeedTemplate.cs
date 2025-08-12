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

        // NEW: The section for defining seed-specific growth properties.
        [Header("Base Growth Parameters")]
        [Tooltip("The base chance (0 to 1) for the plant to attempt a growth step each tick.")]
        [Range(0f, 1f)] public float baseGrowthChance = 0.1f;
        [Tooltip("The minimum number of stem segments the plant will grow.")]
        public int minHeight = 3;
        [Tooltip("The maximum number of stem segments the plant will grow.")]
        public int maxHeight = 5;
        [Tooltip("The number of leaves that will attempt to grow at each leaf-spawning step.")]
        public int leafDensity = 2;
        [Tooltip("How many stem segments to grow before spawning new leaves (1 = every segment, 2 = every other).")]
        public int leafGap = 1;

        [Header("Plant Base Stats")]
        public int baseRechargeTime = 3;
        public float energyRegenRate = 10f;
        public float maxEnergy = 100f;

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

        private void OnValidate()
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
            if (activeGene == null) return true;
            if (modifiers.Count > activeGene.slotConfig.modifierSlots) return false;
            if (payloads.Count > activeGene.slotConfig.payloadSlots) return false;
            var modifierGenes = modifiers.Select(m => m.gene as ModifierGene).ToList();
            var payloadGenes = payloads.Select(p => p.gene as PayloadGene).ToList();
            return activeGene.IsValidConfiguration(modifierGenes, payloadGenes);
        }
    }
}
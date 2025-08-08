// File: Assets/Scripts/Genes/Templates/SeedTemplate.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Templates
{
    [CreateAssetMenu(fileName = "SeedTemplate", menuName = "Abracodabra/Seed Template")]
    public class SeedTemplate : ScriptableObject
    {
        [Header("Template Info")]
        public string templateName;
        [TextArea]
        public string description;
        public Sprite icon;

        [Header("Gene Configuration")]
        public List<GeneTemplateEntry> passiveGenes = new List<GeneTemplateEntry>();
        public List<SequenceSlotTemplate> activeSequence = new List<SequenceSlotTemplate>();

        [Header("Base Settings")]
        public int baseRechargeTime = 3;
        public float energyRegenRate = 10f;
        public float maxEnergy = 100f;

        [Header("Unlock Requirements")]
        public bool isUnlocked = true;
        public List<string> unlockRequirements = new List<string>();

        /// <summary>
        /// Validates the entire template in the editor.
        /// </summary>
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

        /// <summary>
        /// Creates a new runtime state instance from this template blueprint.
        /// </summary>
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
        public float powerMultiplier = 1f;
        // public Dictionary<string, float> initialValues = new Dictionary<string, float>(); // Note: Dictionaries don't serialize well in inspector by default
    }

    [System.Serializable]
    public class SequenceSlotTemplate
    {
        public ActiveGene activeGene;
        public List<ModifierGene> modifiers = new List<ModifierGene>();
        public List<PayloadGene> payloads = new List<PayloadGene>();

        public bool Validate()
        {
            if (activeGene == null) return false;

            // Check slot limits
            if (modifiers.Count > activeGene.slotConfig.modifierSlots) return false;
            if (payloads.Count > activeGene.slotConfig.payloadSlots) return false;

            // Validate configuration
            return activeGene.IsValidConfiguration(modifiers, payloads);
        }
    }
}
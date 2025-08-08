// File: Assets/Scripts/Genes/Core/ActiveGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Services;

namespace Abracodabra.Genes.Core
{
    [System.Serializable]
    public class ActiveGeneSlotConfig
    {
        public int modifierSlots = 1;
        public int payloadSlots = 2;

        public bool CanAcceptMoreModifiers(int current) => current < modifierSlots;
        public bool CanAcceptMorePayloads(int current) => current < payloadSlots;
    }

    /// <summary>
    /// Base class for genes that are the main executable actions in a sequence.
    /// They consume energy and can be altered by Modifiers and enhanced by Payloads.
    /// </summary>
    public abstract class ActiveGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Active;

        [Header("Active Settings")]
        public float baseEnergyCost = 20f;
        public ActiveGeneSlotConfig slotConfig;
        public bool canExecuteEmpty = false;

        [Header("Execution")]
        public float executionDelay = 0f;
        public bool requiresTarget = false;

        /// <summary>
        /// Validates if the attached modifiers and payloads are a valid combination at configuration time.
        /// </summary>
        public virtual bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            if (!canExecuteEmpty && payloads.Count == 0)
                return false;
            return true;
        }

        /// <summary>
        /// Validates if the gene can be executed at runtime (e.g., sufficient energy).
        /// </summary>
        public virtual bool CanExecuteNow(PlantGrowth plant, float availableEnergy)
        {
            return availableEnergy >= baseEnergyCost;
        }

        /// <summary>
        /// Calculates the final energy cost after applying all modifier effects.
        /// </summary>
        public float GetFinalEnergyCost(List<RuntimeGeneInstance> modifiers)
        {
            float cost = baseEnergyCost;
            foreach (var modInstance in modifiers)
            {
                var modifier = modInstance.GetGene<ModifierGene>();
                if (modifier != null)
                    cost = modifier.ModifyEnergyCost(cost, modInstance);
            }
            return Mathf.Max(0, cost);
        }

        /// <summary>
        /// The main execution method for the gene's action.
        /// </summary>
        public abstract void Execute(ActiveGeneContext context);
    }

    /// <summary>
    /// A context object containing all necessary information for an ActiveGene's execution.
    /// </summary>
    public class ActiveGeneContext
    {
        public PlantGrowth plant;
        public RuntimeGeneInstance activeInstance;
        public List<RuntimeGeneInstance> modifiers;
        public List<RuntimeGeneInstance> payloads;
        public int sequencePosition;
        public PlantSequenceExecutor executor;
        public IDeterministicRandom random;
    }
}
// File: Assets/Scripts/Genes/Core/ActiveGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// The base class for all genes that perform an action when their
    /// position in a sequence is reached.
    /// </summary>
    public abstract class ActiveGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Active;

        [Header("Active Gene Settings")]
        public float baseEnergyCost = 20f;
        public ActiveGeneSlotConfig slotConfig = new ActiveGeneSlotConfig();
        
        [Tooltip("If true, this gene can execute even with no payloads attached.")]
        public bool canExecuteEmpty = false;

        [Tooltip("Delay in seconds before the effect visually happens (not implemented).")]
        public float executionDelay = 0f;
        
        [Tooltip("Does this gene require a specific target to execute? (not implemented)")]
        public bool requiresTarget = false;

        /// <summary>
        /// Checks if the current combination of modifiers and payloads is valid for this gene.
        /// </summary>
        public virtual bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            if (!canExecuteEmpty && payloads.Count == 0)
                return false;
            
            return true;
        }

        /// <summary>
        /// Checks if the plant has enough energy to execute this gene right now.
        /// </summary>
        public virtual bool CanExecuteNow(PlantGrowth plant, float availableEnergy)
        {
            return availableEnergy >= baseEnergyCost;
        }

        /// <summary>
        /// Calculates the final energy cost of this gene after all attached modifiers are applied.
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
        /// The main execution logic for this active gene.
        /// </summary>
        /// <param name="context">All contextual data needed for execution.</param>
        public abstract void Execute(ActiveGeneContext context);
    }
}
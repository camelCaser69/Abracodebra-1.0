using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    public abstract class ActiveGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Active;

        public float baseEnergyCost = 20f;
        public ActiveGeneSlotConfig slotConfig = new ActiveGeneSlotConfig();

        public bool canExecuteEmpty = false;

        // Replaced real-time delay with a tick-based delay
        public int executionDelayTicks = 0;

        public bool requiresTarget = false;

        public virtual bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            if (!canExecuteEmpty && payloads.Count == 0)
                return false;

            return true;
        }

        public virtual bool CanExecuteNow(PlantGrowth plant, float availableEnergy)
        {
            return availableEnergy >= baseEnergyCost;
        }

        public float GetFinalEnergyCost(List<RuntimeGeneInstance> modifiers)
        {
            float cost = baseEnergyCost;
            foreach (var modInstance in modifiers)
            {
                if (modInstance == null) continue;

                var modifier = modInstance.GetGene<ModifierGene>();
                if (modifier != null)
                    cost = modifier.ModifyEnergyCost(cost, modInstance);
            }
            return Mathf.Max(0, cost);
        }

        public abstract void Execute(ActiveGeneContext context);
    }
}
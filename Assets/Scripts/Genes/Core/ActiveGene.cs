// File: Assets/Scripts/Genes/Core/ActiveGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    public abstract class ActiveGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Active;

        public float baseEnergyCost = 20f;
        public ActiveGeneSlotConfig slotConfig = new ActiveGeneSlotConfig();

        public bool canExecuteEmpty = false;

        public int executionDelayTicks = 0;

        [Header("Targeting")]
        [Tooltip("If true, the executor checks for a target before spending energy. No target = skip slot.")]
        public bool requiresTarget = false;

        [Tooltip("Range in tiles for target detection. Only used when requiresTarget is true.")]
        public float targetRange = 3f;

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

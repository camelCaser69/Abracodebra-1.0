// FILE: Assets/Scripts/Genes/Implementations/Passive/ThornedLeavesGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations {
    /// <summary>
    /// Passive gene that damages pests when they consume a leaf from this plant.
    /// Damage is applied AFTER the pest finishes eating the leaf.
    /// Stacks additively: ×1 = 5 dmg, ×2 = 10 dmg, ×3 = 15 dmg.
    /// A plant with ThornedLeaves ×3 can kill a small pest (30 HP) after 2 leaves.
    /// </summary>
    public class ThornedLeavesGene : PassiveGene {
        public ThornedLeavesGene() {
            statToModify = PassiveStatType.ThornDamage;
            baseValue = 5f;            // 5 damage per leaf consumed per stack
            stacksAdditively = true;   // ×2 = 10 damage, ×3 = 15 damage
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance) {
            // Stat application handled by PlantGrowthLogic.CalculateAndApplyPassiveStats()
            // Damage application in PlantGrowth.HandleBeingEaten()
        }

        public override string GetStatModificationText() {
            return $"+{baseValue:F0} Thorn Damage";
        }

        public override string GetTooltip(GeneTooltipContext context) {
            float finalDamage = baseValue;
            if (context.instance != null) {
                finalDamage = baseValue * context.instance.GetValue("power_multiplier", 1f);
            }

            return $"{description}\n\n" +
                   $"<b>Effect:</b> Pests take {finalDamage:F0} damage per leaf consumed\n" +
                   $"Stacks: +{baseValue:F0} damage per additional copy.";
        }
    }
}
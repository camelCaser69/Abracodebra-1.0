// File: Assets/Scripts/Genes/Implementations/Passive/ThickBarkGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    /// <summary>
    /// Passive gene that increases damage resistance by 30%.
    /// Works through the existing PassiveStatType.Defense multiplier.
    /// PlantGrowth.TakeDamage() reads defenseMultiplier to reduce incoming damage.
    /// </summary>
    [CreateAssetMenu(fileName = "ThickBarkGene", menuName = "Abracodabra/Genes/Passive/ThickBark")]
    public class ThickBarkGene : PassiveGene
    {
        public ThickBarkGene()
        {
            statToModify = PassiveStatType.Defense;
            baseValue = 1.3f; // +30% defense (defenseMultiplier 1.3 → 30% damage reduction)
            stacksAdditively = true;
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
            // Stat application handled generically by PlantGrowthLogic.CalculateAndApplyPassiveStats()
        }

        public override string GetStatModificationText()
        {
            float percentage = (baseValue - 1f) * 100f;
            return percentage >= 0
                ? $"+{percentage:F0}% Defense"
                : $"{percentage:F0}% Defense";
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float finalMultiplier = baseValue;
            if (context.instance != null)
            {
                finalMultiplier = baseValue * context.instance.GetValue("power_multiplier", 1f);
            }
            float finalPercentage = (finalMultiplier - 1f) * 100f;

            string effectText = finalPercentage >= 0
                ? $"+{finalPercentage:F0}% Damage Resistance"
                : $"{finalPercentage:F0}% Damage Resistance";

            return $"{description}\n\n" +
                $"<b>Effect:</b> {effectText}\n" +
                "Reduces damage taken from pests and environmental hazards.";
        }
    }
}

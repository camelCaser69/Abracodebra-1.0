using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    public class GrowthSpeedGene : PassiveGene
    {
        // The ApplyToPlant logic is now centralized in PlantGrowthLogic.
        // This gene now just needs to provide its data correctly.
        public GrowthSpeedGene()
        {
            statToModify = PassiveStatType.GrowthSpeed;
            baseValue = 1.5f; // This now represents a 50% increase
            stacksAdditively = true;
        }
        
        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
            // This method is now handled by the centralized logic in PlantGrowthLogic.
            // It can be left empty or used for unique, non-standard effects.
        }

        public override string GetStatModificationText()
        {
            float percentage = (baseValue - 1f) * 100f;
            return percentage >= 0
                ? $"+{percentage:F0}% Growth Speed"
                : $"{percentage:F0}% Growth Speed";
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
                ? $"+{finalPercentage:F0}% Growth Speed"
                : $"{finalPercentage:F0}% Growth Speed";

            return $"{description}\n\n" +
                   $"<b>Effect:</b> {effectText}\n" +
                   "Reduces the time required for the plant to reach maturity.";
        }
    }
}
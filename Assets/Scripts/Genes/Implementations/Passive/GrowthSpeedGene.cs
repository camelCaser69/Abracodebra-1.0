// FILE: Assets/Scripts/Genes/Implementations/Passive/GrowthSpeedGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Passive/Growth Speed", fileName = "Gene_Passive_GrowthSpeed")]
    public class GrowthSpeedGene : PassiveGene
    {
        public GrowthSpeedGene()
        {
            statToModify = PassiveStatType.GrowthSpeed;
            baseValue = 1.5f; // This now represents a 50% increase
            stacksAdditively = true;
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
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
// File: Assets/Scripts/Genes/Implementations/Passive/EnergyRootsGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    /// <summary>
    /// Passive gene that increases energy generation per leaf by 25%.
    /// Works through the existing PassiveStatType.EnergyGeneration multiplier.
    /// </summary>
    [CreateAssetMenu(fileName = "EnergyRootsGene", menuName = "Abracodabra/Genes/Passive/EnergyRoots")]
    public class EnergyRootsGene : PassiveGene
    {
        public EnergyRootsGene()
        {
            statToModify = PassiveStatType.EnergyGeneration;
            baseValue = 1.25f; // +25% energy generation
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
                ? $"+{percentage:F0}% Energy Generation"
                : $"{percentage:F0}% Energy Generation";
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
                ? $"+{finalPercentage:F0}% Energy Generation"
                : $"{finalPercentage:F0}% Energy Generation";

            return $"{description}\n\n" +
                $"<b>Effect:</b> {effectText}\n" +
                "Increases the energy generated per leaf each tick.";
        }
    }
}

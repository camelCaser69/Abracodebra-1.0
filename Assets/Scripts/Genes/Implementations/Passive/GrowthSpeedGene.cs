// File: Assets/Scripts/Genes/Implementations/Passive/GrowthSpeedGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "GrowthSpeedGene", menuName = "Abracodabra/Genes/Passive/Growth Speed")]
    public class GrowthSpeedGene : PassiveGene
    {
        [Header("Growth Settings")]
        [Range(0.5f, 3f)]
        public float growthMultiplier = 1.5f;

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
            if (plant == null) return;
            
            // Note: This assumes PlantGrowth will be reworked to have these properties.
            // We are pre-emptively coding against the future state of PlantGrowth.
            float finalMultiplier = growthMultiplier * instance.GetValue("power_multiplier", 1f);

            // Example of modifying a hypothetical growth property
            // plant.GrowthSpeedModifier *= finalMultiplier; 
            
            Debug.Log($"Applied Growth Speed Gene: {finalMultiplier}x modifier to {plant.name}");
        }

        public override string GetStatModificationText()
        {
            float percentage = (growthMultiplier - 1f) * 100f;
            return percentage >= 0
                ? $"+{percentage:F0}% Growth Speed"
                : $"{percentage:F0}% Growth Speed";
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float finalMultiplier = growthMultiplier;
            if (context.instance != null)
            {
                finalMultiplier *= context.instance.GetValue("power_multiplier", 1f);
            }
                
            return $"{description}\n\n" +
                   $"<b>Effect:</b> {GetStatModificationText()}\n" +
                   "Reduces the time required for the plant to reach maturity.";
        }
    }
}
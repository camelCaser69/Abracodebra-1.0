// File: Assets/Scripts/Genes/Implementations/Modifier/CostReductionGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "CostReductionGene", menuName = "Abracodabra/Genes/Modifier/Cost Reduction")]
    public class CostReductionGene : ModifierGene
    {
        [Header("Cost Settings")]
        [Range(0.1f, 0.9f)]
        public float costMultiplier = 0.75f; // i.e., 25% reduction

        public CostReductionGene()
        {
            modifierType = ModifierType.Cost;
        }

        public override float ModifyEnergyCost(float baseCost, RuntimeGeneInstance instance)
        {
            float finalMultiplier = costMultiplier * instance.GetValue("efficiency", 1f);
            return baseCost * finalMultiplier;
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float reduction = (1f - costMultiplier) * 100f;
            return $"{description}\n\n" +
                   $"Reduces the attached Active Gene's energy cost by <b>{reduction:F0}%</b>.";
        }
    }
}
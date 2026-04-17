// FILE: Assets/Scripts/Genes/Implementations/Passive/ThickBarkGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Passive/Thick Bark", fileName = "Gene_Passive_ThickBark")]
    public class ThickBarkGene : PassiveGene
    {
        public ThickBarkGene()
        {
            statToModify = PassiveStatType.Defense;  // Defense enum drives leafDurabilityMultiplier via PlantGrowthLogic
            baseValue = 2.0f;  // v6: ×2 leaf durability (pests take twice as long per leaf)
            stacksAdditively = true;  // ×1 = 2.0, ×2 = 3.0 (1 + 1.0 + 1.0)
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
        }

        public override string GetStatModificationText()
        {
            return $"Leaf Durability ×{baseValue:F1}";
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float finalMultiplier = baseValue;
            if (context.instance != null)
            {
                finalMultiplier = baseValue * context.instance.GetValue("power_multiplier", 1f);
            }

            return $"{description}\n\n" +
                   $"<b>Effect:</b> Leaf Durability ×{finalMultiplier:F1}\n" +
                   "Pests take longer to consume each leaf. Stacks additively.";
        }
    }
}
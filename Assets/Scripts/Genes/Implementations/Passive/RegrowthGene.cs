// FILE: Assets/Scripts/Genes/Implementations/Passive/RegrowthGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Passive/Regrowth", fileName = "Gene_Passive_Regrowth")]
    public class RegrowthGene : PassiveGene
    {
        public RegrowthGene()
        {
            statToModify = PassiveStatType.LeafRegrowth;
            baseValue = 5f;            // Regrow 1 leaf every 5 ticks
            stacksAdditively = false;  // Custom stacking handled in PlantGrowthLogic
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
        }

        public override string GetStatModificationText()
        {
            return $"Regrow 1 leaf / {baseValue:F0} ticks";
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float rate = baseValue;
            if (context.instance != null)
            {
                rate = baseValue * context.instance.GetValue("power_multiplier", 1f);
            }

            return $"{description}\n\n" +
                   $"<b>Effect:</b> Regrows 1 lost leaf every {rate:F0} ticks\n" +
                   "Additional stacks reduce the interval by 1 tick (min 2).\n" +
                   "Works during Withering — can save a dying plant!";
        }
    }
}
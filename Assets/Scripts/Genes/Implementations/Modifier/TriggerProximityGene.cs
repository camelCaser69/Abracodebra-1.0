// FILE: Assets/Scripts/Genes/Implementations/Modifier/TriggerProximityGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Modifier/Trigger Proximity", fileName = "Gene_Modifier_TriggerProximity")]
    public class TriggerProximityGene : ModifierGene
    {
        [Header("Proximity Trigger Settings")]
        [Tooltip("Detection range in tiles.")]
        public int detectionRange = 3;

        public TriggerProximityGene()
        {
            modifierType = ModifierType.Trigger;
        }

        public override bool CheckTriggerCondition(ActiveGeneContext context)
        {
            if (context.plant == null) return false;

            return TargetFinder.HasCreatureInRange(context.plant.transform.position, detectionRange);
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                   $"Detection Range: <b>{detectionRange}</b> tiles.\n" +
                   "The attached Active gene only fires when creatures are within range.\n" +
                   "Saves energy when no threats are present.";
        }
    }
}
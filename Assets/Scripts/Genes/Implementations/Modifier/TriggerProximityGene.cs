// File: Assets/Scripts/Genes/Implementations/Modifier/TriggerProximityGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    /// <summary>
    /// Trigger modifier: the Active gene only fires when a creature is within detection range.
    /// If no creature nearby, the slot is skipped (no energy spent).
    /// </summary>
    [CreateAssetMenu(fileName = "TriggerProximityGene", menuName = "Abracodabra/Genes/Modifier/TriggerProximity")]
    public class TriggerProximityGene : ModifierGene
    {
        [Header("Proximity Trigger Settings")]
        [Tooltip("Detection range in tiles.")]
        public int detectionRange = 3;

        public TriggerProximityGene()
        {
            modifierType = ModifierType.Trigger;
        }

        /// <summary>
        /// Called by PlantSequenceExecutor before energy is spent.
        /// Returns false to skip the slot (save energy).
        /// </summary>
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

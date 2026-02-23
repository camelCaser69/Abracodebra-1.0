// File: Assets/Scripts/Genes/Implementations/Modifier/OverchargeGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    /// <summary>
    /// Modifier gene that increases effect power by 40% but costs 50% more energy.
    /// The power multiplier is stored on the active instance via PreExecution,
    /// then read by payloads and actives via context.activeInstance's "effect_multiplier" value.
    /// </summary>
    [CreateAssetMenu(fileName = "OverchargeGene", menuName = "Abracodabra/Genes/Modifier/Overcharge")]
    public class OverchargeGene : ModifierGene
    {
        [Header("Overcharge Settings")]
        [Tooltip("Energy cost multiplier. 1.5 = 50% more expensive.")]
        public float costMultiplier = 1.5f;

        [Tooltip("Effect power multiplier. 1.4 = 40% more powerful.")]
        public float powerMultiplier = 1.4f;

        public OverchargeGene()
        {
            modifierType = ModifierType.Behavior;
        }

        public override float ModifyEnergyCost(float baseCost, RuntimeGeneInstance instance)
        {
            return baseCost * costMultiplier;
        }

        public override void PreExecution(ActiveGeneContext context)
        {
            // Stack the effect multiplier onto the active instance
            // This way Execute() and payloads can read it
            if (context.activeInstance != null)
            {
                float currentMultiplier = context.activeInstance.GetValue("effect_multiplier", 1f);
                context.activeInstance.SetValue("effect_multiplier", currentMultiplier * powerMultiplier);
            }
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float costIncrease = (costMultiplier - 1f) * 100f;
            float powerIncrease = (powerMultiplier - 1f) * 100f;

            return $"{description}\n\n" +
                $"<color=#FF6666>Energy Cost: <b>+{costIncrease:F0}%</b></color>\n" +
                $"<color=#66FF66>Effect Power: <b>+{powerIncrease:F0}%</b></color>\n" +
                "Makes the attached Active gene more powerful but more expensive.";
        }
    }
}

// FILE: Assets/Scripts/Genes/Implementations/Modifier/OverchargeGene.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Modifier/Overcharge", fileName = "Gene_Modifier_Overcharge")]
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
// FILE: Assets/Scripts/Genes/Implementations/Payload/HealingPayload.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Payload/Healing", fileName = "Gene_Payload_Healing")]
    public class HealingPayload : PayloadGene
    {
        [Header("Healing Configuration")]
        [Tooltip("HP healed per application on creatures.")]
        public float baseHealAmount = 2f;

        [Tooltip("Chance per tick to regrow a leaf on a plant in range (0-1). Used by Cloud/Aura effects.")]
        [Range(0f, 1f)]
        public float plantRegrowChance = 0.5f;

        public HealingPayload()
        {
            payloadType = PayloadType.Healing;
            geneColor = new Color(0.3f, 1f, 0.5f); // Soft green — healing
        }

        public override bool IsPlantHealingPayload => true;

        public override void ApplyPayload(PayloadContext context)
        {
            if (context.target == null) return;

            var animalController = context.target.GetComponent<AnimalController>();
            if (animalController != null)
            {
                float potency = GetFinalPotency(context.payloadInstance);
                float finalHeal = baseHealAmount * potency * context.effectMultiplier;
                animalController.Heal(finalHeal);

                if (context.source != null)
                {
                    Debug.Log($"[HealingPayload] Plant '{context.source.name}' healed '{animalController.SpeciesName}' for {finalHeal:F1} HP");
                }
                return;
            }

            var effectable = context.target.GetComponent<IStatusEffectable>();
            if (effectable != null)
            {
                float potency = GetFinalPotency(context.payloadInstance);
                float finalHeal = baseHealAmount * potency * context.effectMultiplier;
                effectable.Heal(finalHeal);

                Debug.Log($"[HealingPayload] Healed '{effectable.GetDisplayName()}' for {finalHeal:F1} HP");
            }

            // NOTE: Plant leaf regrowth is NOT handled here — it's handled by
            // CloudWorldEffect and AuraWorldEffect using plantRegrowChance
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            var nutrition = fruit.gameObject.GetComponent<NutritionComponent>() ??
                fruit.gameObject.AddComponent<NutritionComponent>();
            float potency = GetFinalPotency(instance);
            nutrition.healAmount = baseHealAmount * potency;

            fruit.AddVisualEffect(geneColor);

            if (fruit.DynamicProperties == null)
                fruit.DynamicProperties = new System.Collections.Generic.Dictionary<string, float>();

            fruit.DynamicProperties["is_healing"] = 1f;
            fruit.DynamicProperties["heal_amount"] = baseHealAmount * potency;
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance)
        {
            var effectable = target.GetComponent<IStatusEffectable>();
            if (effectable != null)
            {
                float potency = GetFinalPotency(instance);
                effectable.Heal(baseHealAmount * potency);
            }
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float potency = 1f;
            if (context?.instance != null)
            {
                potency = GetFinalPotency(context.instance);
            }

            return $"{description}\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(geneColor)}><b>Effect: Healing</b></color>\n" +
                $"On creatures: heals <b>{baseHealAmount * potency:F0} HP</b> per application.\n" +
                $"On plants (via Cloud/Aura): <b>{plantRegrowChance * 100:F0}%</b> chance to regrow a leaf per tick.\n" +
                $"A withering plant can be saved by a nearby healing source.";
        }
    }
}
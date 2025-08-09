// File: Assets/Scripts/Genes/Implementations/Payload/NutritiousPayload.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Implementations
{
    public class NutritiousPayload : PayloadGene
    {
        public float nutritionValue = 10f;
        public float healAmount = 5f;

        public NutritiousPayload()
        {
            payloadType = PayloadType.Nutrition;
        }

        public override void ApplyPayload(PayloadContext context)
        {
            ApplyToTarget(context.target, context.payloadInstance);
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            var nutrition = fruit.gameObject.GetComponent<NutritionComponent>() ?? fruit.gameObject.AddComponent<NutritionComponent>();
            nutrition.nutritionValue = nutritionValue * GetFinalPotency(instance);
            nutrition.healAmount = healAmount;

            fruit.AddVisualEffect(geneColor);
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance)
        {
            var creature = target.GetComponent<Creature>();
            if (creature != null)
            {
                float finalNutrition = nutritionValue * GetFinalPotency(instance);
                creature.Feed(finalNutrition);
                creature.Heal(healAmount);
            }
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float potency = 1f;
            if (context.instance != null)
            {
                potency = GetFinalPotency(context.instance);
            }

            return $"{description}\n\n" +
                   $"Adds <b>{nutritionValue * potency:F0}</b> nutrition to the fruit.\n" +
                   $"Heals for <b>{healAmount:F0}</b> HP when consumed.";
        }
    }
}
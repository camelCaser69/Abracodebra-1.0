// FILE: Assets/Scripts/Genes/Implementations/Payload/NutritiousPayload.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Payload/Nutritious", fileName = "Gene_Payload_Nutritious")]
    public class NutritiousPayload : PayloadGene
    {
        [Tooltip("Healing is disabled for Nutritious payload in v5.")]
        public float healAmount = 0f;

        public float nutritionValue = 10f;

        public NutritiousPayload()
        {
            payloadType = PayloadType.Nutrition;
        }

        public override void ApplyPayload(PayloadContext context)
        {
            if (context.target != null)
            {
                Debug.Log($"[NutritiousPayload] Applied to {context.target.name} (Food Scent / Lure effect)");
            }
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            var nutrition = fruit.gameObject.GetComponent<NutritionComponent>() ??
                            fruit.gameObject.AddComponent<NutritionComponent>();
            nutrition.nutritionValue = nutritionValue * GetFinalPotency(instance);

            fruit.AddVisualEffect(geneColor);
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance)
        {
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
                   $"<color=#AAAAAA>(Heal removed in v5)</color>";
        }
    }
}
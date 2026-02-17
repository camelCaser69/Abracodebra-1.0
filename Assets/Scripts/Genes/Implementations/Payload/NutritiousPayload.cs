using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Implementations {
    
    [CreateAssetMenu(fileName = "Gene_Payload_Nutrition", menuName = "Abracodabra/Genes/Payload/Nutrition")]
    public class NutritiousPayload : PayloadGene {
        // v5 Design Update: healAmount is deprecated for NutritiousPayload. 
        // It provides nutrition only. Healing is for a dedicated Healing Payload.
        [Tooltip("Healing is disabled for Nutritious payload in v5.")]
        public float healAmount = 0f; 
        
        public float nutritionValue = 10f;

        public NutritiousPayload() {
            payloadType = PayloadType.Nutrition;
        }

        public override void ApplyPayload(PayloadContext context) {
            // In v5, "Nutritious" primarily modifies the fruit's item data via ConfigureFruit.
            // If applied directly to a target (e.g. via Projectile or Cloud), it acts as "Food Scent" (lure).
            // Actual feeding/hunger reduction happens via the FeedingSystem consuming the item.
            
            // For now, we can play a visual effect or log.
            // Do NOT call Feed() here to avoid double-dipping with FeedingSystem.
            if(context.target != null) {
                Debug.Log($"[NutritiousPayload] Applied to {context.target.name} (Food Scent / Lure effect)");
            }
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance) {
            var nutrition = fruit.gameObject.GetComponent<NutritionComponent>() ?? fruit.gameObject.AddComponent<NutritionComponent>();
            nutrition.nutritionValue = nutritionValue * GetFinalPotency(instance);
            
            // Visual feedback
            fruit.AddVisualEffect(geneColor);
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance) {
            // Deprecated logic removed per v5 design. 
            // Nutrition is handled by item consumption stats.
        }

        public override string GetTooltip(GeneTooltipContext context) {
            float potency = 1f;
            if (context.instance != null) {
                potency = GetFinalPotency(context.instance);
            }

            return $"{description}\n\n" +
                   $"Adds <b>{nutritionValue * potency:F0}</b> nutrition to the fruit.\n" +
                   $"<color=#AAAAAA>(Heal removed in v5)</color>";
        }
    }
}
// File: Assets/Scripts/Genes/Implementations/Payload/NutritiousPayload.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    // A placeholder component for things that can be eaten
    public class Creature : MonoBehaviour
    {
        public void Feed(float amount) { Debug.Log($"{name} was fed for {amount} nutrition."); }
        public void Heal(float amount) { Debug.Log($"{name} was healed for {amount} HP."); }
    }
    
    // A placeholder component to attach payload data to a fruit
    public class NutritionComponent : MonoBehaviour
    {
        public float nutritionValue;
        public float healAmount;
    }


    [CreateAssetMenu(fileName = "NutritiousPayload", menuName = "Abracodabra/Genes/Payload/Nutritious")]
    public class NutritiousPayload : PayloadGene
    {
        [Header("Nutrition Settings")]
        public float nutritionValue = 10f;
        public float healAmount = 5f;

        public NutritiousPayload()
        {
            payloadType = PayloadType.Nutrition;
        }
        
        public override void ApplyPayload(PayloadContext context)
        {
            // This is called when the effect is directly applied to a target
            ApplyToTarget(context.target, context.payloadInstance);
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            // This is called to add the payload's properties to a fruit
            var nutrition = fruit.gameObject.GetComponent<NutritionComponent>() ?? fruit.gameObject.AddComponent<NutritionComponent>();
            nutrition.nutritionValue = nutritionValue * GetFinalPotency(instance);
            nutrition.healAmount = healAmount;

            // Add a visual indicator
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
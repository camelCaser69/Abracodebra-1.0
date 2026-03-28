// FILE: Assets/Scripts/Genes/Implementations/Payload/FearPayload.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "FearPayload", menuName = "Abracodabra/Genes/Payload/Fear")]
    public class FearPayload : PayloadGene
    {
        [Header("Fear Configuration")]
        [Tooltip("How many ticks the creature flees after being hit.")]
        public int baseFearDurationTicks = 4;

        [Tooltip("Assign the Fear StatusEffect asset here (for visual tint/icon).")]
        public StatusEffect fearStatusEffect;

        public FearPayload()
        {
            payloadType = PayloadType.Substance;
            geneColor = new Color(0.8f, 0.3f, 0.9f); // Purple — fear/psychic
        }

        public override void ApplyPayload(PayloadContext context)
        {
            if (context.target == null) return;

            var animalController = context.target.GetComponent<AnimalController>();
            if (animalController == null) return;

            // Check fear immunity (large creatures configurable per AnimalDefinition)
            if (animalController.Definition != null && animalController.Definition.immuneToFear)
            {
                Debug.Log($"[FearPayload] '{animalController.SpeciesName}' is immune to Fear.");
                return;
            }

            // Determine the source position for flee direction
            Vector3 fearSourcePos = context.source != null
                ? context.source.transform.position
                : context.target.transform.position;

            // Apply fear to the animal controller
            float potency = GetFinalPotency(context.payloadInstance);
            int finalDuration = Mathf.CeilToInt(baseFearDurationTicks * potency);

            animalController.ApplyFear(fearSourcePos, finalDuration);

            // Also apply the status effect SO for visual feedback (tint, icon)
            if (fearStatusEffect != null)
            {
                var statusManager = animalController.StatusManager;
                if (statusManager != null)
                {
                    statusManager.ApplyStatusEffect(fearStatusEffect);
                }
            }

            if (context.source != null)
            {
                Debug.Log($"[FearPayload] Plant '{context.source.name}' feared '{animalController.SpeciesName}' for {finalDuration} ticks");
            }
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            fruit.AddVisualEffect(geneColor);

            if (fruit.DynamicProperties == null)
                fruit.DynamicProperties = new System.Collections.Generic.Dictionary<string, float>();

            fruit.DynamicProperties["is_fearsome"] = 1f;
            fruit.DynamicProperties["fear_duration"] = baseFearDurationTicks;
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance)
        {
            // Used when fruit is eaten — apply fear from the fruit's position
            var animalController = target.GetComponent<AnimalController>();
            if (animalController == null) return;

            if (animalController.Definition != null && animalController.Definition.immuneToFear)
                return;

            animalController.ApplyFear(target.transform.position, baseFearDurationTicks);
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(geneColor)}><b>Effect: Fear</b></color>\n" +
                $"Causes creatures to flee for <b>{baseFearDurationTicks}</b> ticks.\n" +
                $"Large creatures may be immune.\n" +
                $"Refreshes duration on re-application (does not stack).";
        }
    }
}
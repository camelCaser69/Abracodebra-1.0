// FILE: Assets/Scripts/Genes/Implementations/Payload/FreezePayload.cs
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "FreezePayload", menuName = "Abracodabra/Genes/Payload/Freeze")]
    public class FreezePayload : PayloadGene
    {
        [Header("Freeze Configuration")]
        [Tooltip("Number of freeze stacks applied per hit.")]
        public int baseStacksPerHit = 1;

        [Tooltip("Assign the Freeze StatusEffect asset here. Must have canStack=true, isFreezeType=true.")]
        public StatusEffect freezeStatusEffect;

        public FreezePayload()
        {
            payloadType = PayloadType.Substance;
            geneColor = new Color(0.5f, 0.85f, 1f); // Light ice blue
        }

        public override void ApplyPayload(PayloadContext context)
        {
            if (context.target == null || freezeStatusEffect == null) return;

            var statusManager = context.target.GetComponent<StatusEffectManager>();

            if (statusManager == null)
            {
                var effectable = context.target.GetComponent<IStatusEffectable>();
                if (effectable != null)
                {
                    statusManager = effectable.StatusManager;
                }
            }

            if (statusManager == null) return;

            // Apply N stacks (each ApplyStatusEffect call adds 1 stack)
            float potency = GetFinalPotency(context.payloadInstance);
            int stacks = Mathf.CeilToInt(baseStacksPerHit * potency);

            for (int i = 0; i < stacks; i++)
            {
                statusManager.ApplyStatusEffect(freezeStatusEffect);
            }

            int currentStacks = statusManager.GetStackCount(freezeStatusEffect.effectID);

            if (context.source != null)
            {
                Debug.Log($"[FreezePayload] Plant '{context.source.name}' applied {stacks} freeze stack(s) to '{context.target.name}' (total: {currentStacks}/{freezeStatusEffect.maxStacks})");
            }
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance)
        {
            fruit.AddVisualEffect(geneColor);

            if (fruit.DynamicProperties == null)
                fruit.DynamicProperties = new System.Collections.Generic.Dictionary<string, float>();

            fruit.DynamicProperties["is_freezing"] = 1f;
            fruit.DynamicProperties["freeze_stacks"] = baseStacksPerHit;
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance)
        {
            // Used when fruit is eaten — apply freeze stacks
            if (target == null || freezeStatusEffect == null) return;

            var statusManager = target.GetComponent<StatusEffectManager>();
            if (statusManager == null)
            {
                var effectable = target.GetComponent<IStatusEffectable>();
                if (effectable != null) statusManager = effectable.StatusManager;
            }

            if (statusManager != null)
            {
                for (int i = 0; i < baseStacksPerHit; i++)
                {
                    statusManager.ApplyStatusEffect(freezeStatusEffect);
                }
            }
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            if (freezeStatusEffect == null) return "Missing StatusEffect Asset";

            return $"{description}\n\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(geneColor)}><b>Effect: Freeze</b></color>\n" +
                $"Applies <b>{baseStacksPerHit}</b> freeze stack(s) per hit.\n" +
                $"Max stacks: <b>{freezeStatusEffect.maxStacks}</b>.\n" +
                $"At max stacks: <b>FROZEN</b> for {freezeStatusEffect.frozenDurationTicks} ticks.\n" +
                $"Stacks decay: -1 every {freezeStatusEffect.stackDecayIntervalTicks} ticks.";
        }
    }
}
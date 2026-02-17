using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;
using WegoSystem;

namespace Abracodabra.Genes.Implementations {
    
    [CreateAssetMenu(fileName = "Gene_Payload_Slow", menuName = "Abracodabra/Genes/Payload/Slow")]
    public class SlowPayload : PayloadGene {
        [Header("Slow Configuration")]
        [Tooltip("Assign the Slow StatusEffect asset here.")]
        public StatusEffect slowStatusEffect;

        public SlowPayload() {
            payloadType = PayloadType.Substance;
            geneColor = new Color(0.3f, 0.6f, 1f); // Ice Blue
        }

        public override void ApplyPayload(PayloadContext context) {
            if (context.target == null || slowStatusEffect == null) return;

            var statusManager = context.target.GetComponent<StatusEffectManager>();
            if (statusManager == null) {
                var effectable = context.target.GetComponent<IStatusEffectable>();
                if (effectable != null) {
                    statusManager = effectable.StatusManager;
                }
            }

            if (statusManager != null) {
                statusManager.ApplyStatusEffect(slowStatusEffect);
                
                if (context.source != null) {
                    Debug.Log($"[SlowPayload] Plant '{context.source.name}' slowed '{context.target.name}'");
                }
            }
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance) {
            // Visual feedback: Blue tint
            fruit.AddVisualEffect(geneColor);

            if (fruit.DynamicProperties == null) fruit.DynamicProperties = new System.Collections.Generic.Dictionary<string, float>();
            fruit.DynamicProperties["is_slowing"] = 1f;
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance) {
            // Deprecated direct call, logic is in ApplyPayload
        }

        public override string GetTooltip(GeneTooltipContext context) {
            if (slowStatusEffect == null) return "Missing StatusEffect Asset";

            return $"{description}\n\n" +
                   $"<color=#{ColorUtility.ToHtmlStringRGB(geneColor)}><b>Effect: {slowStatusEffect.displayName}</b></color>\n" +
                   $"Increases movement cost by <b>{slowStatusEffect.additionalMoveTicks}</b> tick(s).\n" +
                   $"Duration: <b>{slowStatusEffect.durationTicks}</b> ticks.";
        }
    }
}
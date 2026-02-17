using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;
using WegoSystem; // For StatusEffect references

namespace Abracodabra.Genes.Implementations {
    
    [CreateAssetMenu(fileName = "Gene_Payload_Poison", menuName = "Abracodabra/Genes/Payload/Poison")]
    public class PoisonPayload : PayloadGene {
        [Header("Poison Configuration")]
        [Tooltip("Assign the Poison StatusEffect asset here.")]
        public StatusEffect poisonStatusEffect;

        public PoisonPayload() {
            payloadType = PayloadType.Substance;
            geneColor = new Color(0.4f, 0.8f, 0.2f); // Toxic Green
        }

        public override void ApplyPayload(PayloadContext context) {
            if (context.target == null || poisonStatusEffect == null) return;

            // Try to find StatusEffectManager on the target (Animal, Player, etc.)
            var statusManager = context.target.GetComponent<StatusEffectManager>();
            
            // Fallback: Check if it implements IStatusEffectable
            if (statusManager == null) {
                var effectable = context.target.GetComponent<IStatusEffectable>();
                if (effectable != null) {
                    statusManager = effectable.StatusManager;
                }
            }

            if (statusManager != null) {
                statusManager.ApplyStatusEffect(poisonStatusEffect);
                
                if (context.source != null) {
                    Debug.Log($"[PoisonPayload] Plant '{context.source.name}' poisoned '{context.target.name}'");
                }
            }
        }

        public override void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance) {
            // Visual feedback: Green tint
            fruit.AddVisualEffect(geneColor);

            // Logic tag for UI/Tooltip
            if (fruit.DynamicProperties == null) fruit.DynamicProperties = new System.Collections.Generic.Dictionary<string, float>();
            fruit.DynamicProperties["is_poisonous"] = 1f;
        }

        public override void ApplyToTarget(GameObject target, RuntimeGeneInstance instance) {
            // Deprecated direct call, logic is in ApplyPayload
        }

        public override string GetTooltip(GeneTooltipContext context) {
            if (poisonStatusEffect == null) return "Missing StatusEffect Asset";

            return $"{description}\n\n" +
                   $"<color=#{ColorUtility.ToHtmlStringRGB(geneColor)}><b>Effect: {poisonStatusEffect.displayName}</b></color>\n" +
                   $"Deals <b>{poisonStatusEffect.damageAmount}</b> damage per tick.\n" +
                   $"Duration: <b>{poisonStatusEffect.durationTicks}</b> ticks.";
        }
    }
}
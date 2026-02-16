using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Components {
    /// <summary>
    /// Static utility to handle the application of gene payloads when a fruit or food item is consumed.
    /// </summary>
    public static class FruitConsumptionHandler {
        
        /// <summary>
        /// Processes a list of payloads against a consumer. 
        /// Used when consuming inventory items (where the Fruit GO is long dead).
        /// </summary>
        public static void Consume(List<RuntimeGeneInstance> payloads, GameObject consumer, PlantGrowth sourcePlant = null) {
            if (payloads == null || payloads.Count == 0) return;
            if (consumer == null) return;

            Debug.Log($"[FruitConsumptionHandler] Applying {payloads.Count} payloads to {consumer.name}");

            foreach (var instance in payloads) {
                if (instance == null) continue;

                var payloadGene = instance.GetGene<PayloadGene>();
                if (payloadGene != null) {
                    var context = new PayloadContext {
                        target = consumer,
                        source = sourcePlant, // Might be null if from inventory, payloads must handle null source gracefully
                        payloadInstance = instance,
                        effectMultiplier = 1f, // Default, modifiers might need to be serialized into ItemInstance later for full accuracy
                        parentGene = null // Lost context when itemized
                    };

                    try {
                        payloadGene.ApplyPayload(context);
                        Debug.Log($"[FruitConsumptionHandler] Applied {payloadGene.geneName} to {consumer.name}");
                    }
                    catch (System.Exception e) {
                        Debug.LogError($"[FruitConsumptionHandler] Error applying payload {payloadGene.geneName}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Processes a Fruit MonoBehaviour directly (e.g. eating straight from the vine).
        /// </summary>
        public static void Consume(Fruit fruit, GameObject consumer) {
            if (fruit == null || consumer == null) return;

            Consume(fruit.PayloadGeneInstances, consumer, fruit.SourcePlant);
        }
    }
}
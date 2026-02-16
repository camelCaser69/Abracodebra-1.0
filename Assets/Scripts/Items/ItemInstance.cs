using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime; // Required for RuntimeGeneInstance

public class ItemInstance {
    public ItemDefinition definition;
    public Dictionary<string, float> dynamicProperties;
    public int stackCount = 1;

    // ✅ ADDED: Carry payloads from the source fruit
    public List<RuntimeGeneInstance> payloads = new List<RuntimeGeneInstance>();

    public ItemInstance(ItemDefinition def, Dictionary<string, float> props = null, List<RuntimeGeneInstance> payloadInstances = null) {
        definition = def;
        dynamicProperties = props ?? new Dictionary<string, float>();
        stackCount = 1;
        
        if (payloadInstances != null) {
            payloads = new List<RuntimeGeneInstance>(payloadInstances);
        }
    }

    public float GetNutrition() {
        if (definition == null) return 0f;

        float finalNutrition = definition.baseNutrition;
        if (dynamicProperties.TryGetValue("nutrition_multiplier", out float multiplier)) {
            finalNutrition *= multiplier;
        }
        if (dynamicProperties.TryGetValue("nutrition_add", out float additive)) {
            finalNutrition += additive;
        }
        return finalNutrition;
    }

    public float GetHealAmount() {
        if (definition == null) return 0f;
        return definition.baseHealing;
    }
}
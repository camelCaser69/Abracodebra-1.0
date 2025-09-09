using System;
using System.Collections.Generic;

[System.Serializable]
public class ItemInstance
{
    public ItemDefinition definition;
    public Dictionary<string, float> dynamicProperties;
    public int stackCount = 1;

    public ItemInstance(ItemDefinition def, Dictionary<string, float> props = null)
    {
        definition = def;
        dynamicProperties = props ?? new Dictionary<string, float>();
        stackCount = 1;
    }

    // Example of how to calculate a final value using dynamic properties
    public float GetNutrition()
    {
        if (definition == null) return 0f;

        float finalNutrition = definition.baseNutrition;
        if (dynamicProperties.TryGetValue("nutrition_multiplier", out float multiplier))
        {
            finalNutrition *= multiplier;
        }
        if (dynamicProperties.TryGetValue("nutrition_add", out float additive))
        {
            finalNutrition += additive;
        }
        return finalNutrition;
    }

    public float GetHealAmount()
    {
        if (definition == null) return 0f;
        // This could also be expanded with dynamic properties in the future
        return definition.baseHealing;
    }
}
using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Components;

// Reworked File: Assets/Scripts/WorldInteraction/Player/HarvestedItem.cs
public class HarvestedItem
{
    public RuntimeGeneInstance HarvestedGeneInstance { get; set; }

    // This class is a wrapper for data from a harvested item,
    // primarily gene-based fruits for now.
    public HarvestedItem(RuntimeGeneInstance instance)
    {
        HarvestedGeneInstance = instance;
    }

    public float GetNutritionValue()
    {
        if (HarvestedGeneInstance == null) return 0f;

        // This logic assumes nutrition comes from a PAYLOAD gene.
        // If the ActiveGene itself defined nutrition, you'd check that here.
        // This is a placeholder for a more complex system where you might query
        // all attached payloads for different effects.

        if (HarvestedGeneInstance.GetGene() is NutritiousPayload nutritiousGene)
        {
            return nutritiousGene.nutritionValue * HarvestedGeneInstance.GetValue("potency_multiplier", 1f);
        }
        
        // This could be expanded to check for other payload types
        // that might have a nutrition value.

        return 0f;
    }

    public bool IsConsumable()
    {
        return GetNutritionValue() > 0;
    }
}
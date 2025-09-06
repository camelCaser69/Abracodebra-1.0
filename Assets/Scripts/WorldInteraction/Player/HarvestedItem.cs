using UnityEngine;
using System.Collections.Generic; // Added for List
using System.Linq; // Added for Linq
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Core;

public class HarvestedItem
{
    // MODIFIED: Now holds a list of gene instances.
    public List<RuntimeGeneInstance> HarvestedGeneInstances { get; set; }

    public HarvestedItem(List<RuntimeGeneInstance> instances)
    {
        HarvestedGeneInstances = instances ?? new List<RuntimeGeneInstance>();
    }

    public float GetNutritionValue()
    {
        if (HarvestedGeneInstances == null || HarvestedGeneInstances.Count == 0) return 0f;

        float totalNutrition = 0f;

        foreach (var instance in HarvestedGeneInstances)
        {
            if (instance.GetGene() is NutritiousPayload nutritiousGene)
            {
                totalNutrition += nutritiousGene.nutritionValue * instance.GetValue("potency_multiplier", 1f);
            }
        }
        
        return totalNutrition;
    }

    public bool IsConsumable()
    {
        return GetNutritionValue() > 0;
    }

    // A helper to get the primary gene for UI purposes (e.g., icon, name)
    public RuntimeGeneInstance GetPrimaryGeneInstance()
    {
        return HarvestedGeneInstances.FirstOrDefault();
    }
}
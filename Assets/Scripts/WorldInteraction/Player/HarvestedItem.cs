// Reworked File: Assets/Scripts/WorldInteraction/Player/HarvestedItem.cs
using System.Linq;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations; // For NutritionComponent

/// <summary>
/// Represents an item harvested from a plant, now wrapping a RuntimeGeneInstance.
/// </summary>
public class HarvestedItem
{
    public RuntimeGeneInstance HarvestedGeneInstance { get; set; }

    public HarvestedItem(RuntimeGeneInstance instance)
    {
        HarvestedGeneInstance = instance;
    }

    public float GetNutritionValue()
    {
        if (HarvestedGeneInstance == null) return 0f;

        // In the new system, nutrition value is a property of a payload.
        // A proper implementation would check for a "NutritiousPayload" or similar.
        // For now, let's assume a conventional value or look for a specific component.
        if (HarvestedGeneInstance.GetGene() is NutritiousPayload nutritiousGene)
        {
            return nutritiousGene.nutritionValue;
        }

        return 0f; // Default if not a nutritious gene
    }

    public bool IsConsumable()
    {
        return GetNutritionValue() > 0;
    }
}
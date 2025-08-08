// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantGrowthLogic.cs
using UnityEngine;
using System.Linq;
using WegoSystem;
using Abracodabra.Genes.Core;

public class PlantGrowthLogic
{
    private readonly PlantGrowth plant;

    // --- Passively Calculated Stats ---
    public int TargetStemLength { get; set; }
    public int GrowthTicksPerStage { get; set; }
    public float PhotosynthesisEfficiencyPerLeaf { get; set; }
    
    // ... other passive stats can be added here as needed

    public PlantGrowthLogic(PlantGrowth plant)
    {
        this.plant = plant;
    }

    /// <summary>
    /// Calculates and applies the initial passive stats from the plant's gene loadout.
    /// This is called once when the plant is initialized.
    /// </summary>
    public void CalculateAndApplyPassiveStats()
    {
        if (plant.geneRuntimeState == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null geneRuntimeState!");
            return;
        }

        // Apply passive genes to set the initial stats
        foreach (var instance in plant.geneRuntimeState.passiveInstances)
        {
            var passiveGene = instance.GetGene<PassiveGene>();
            if (passiveGene != null)
            {
                passiveGene.ApplyToPlant(plant, instance);
            }
        }
    }
}
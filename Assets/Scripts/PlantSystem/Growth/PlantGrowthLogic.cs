// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantGrowthLogic.cs

using Abracodabra.Genes;
using UnityEngine;
using WegoSystem;
using Abracodabra.Genes.Core;

public class PlantGrowthLogic
{
    readonly PlantGrowth plant;

    public int TargetStemLength { get; set; }
    public int GrowthTicksPerStage { get; set; }
    public float PhotosynthesisEfficiencyPerLeaf { get; set; }

    public PlantGrowthLogic(PlantGrowth plant)
    {
        this.plant = plant;
    }

    public void CalculateAndApplyPassiveStats()
    {
        if (plant.geneRuntimeState == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null geneRuntimeState!");
            return;
        }

        // Apply passive genes - they will modify the plant's stat multipliers
        foreach (var instance in plant.geneRuntimeState.passiveInstances)
        {
            var passiveGene = instance.GetGene<PassiveGene>();
            if (passiveGene != null)
            {
                passiveGene.ApplyToPlant(plant, instance);
            }
        }
        
        // After all passives are applied, update the energy system's base rate
        if (plant.EnergySystem != null)
        {
            plant.EnergySystem.BaseEnergyPerLeaf = PhotosynthesisEfficiencyPerLeaf;
        }
        
        Debug.Log($"[{plant.gameObject.name}] Final stats after passives: " +
                  $"GrowthSpeed={plant.growthSpeedMultiplier}x, " +
                  $"EnergyGen={plant.energyGenerationMultiplier}x, " +
                  $"Height={plant.minHeight}-{plant.maxHeight}");
    }
}
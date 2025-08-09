using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes;
using Abracodabra.Genes.Core;

public class PlantGrowthLogic
{
    private readonly PlantGrowth plant;

    public int TargetStemLength { get; set; }
    public int GrowthTicksPerStage { get; set; }
    public float PhotosynthesisEfficiencyPerLeaf { get; set; }

    public PlantGrowthLogic(PlantGrowth plant)
    {
        this.plant = plant;
    }

    /// <summary>
    /// Calculates and applies all passive gene stats to the plant.
    /// This method now correctly handles additive and multiplicative stacking.
    /// </summary>
    public void CalculateAndApplyPassiveStats()
    {
        if (plant.geneRuntimeState == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null geneRuntimeState!");
            return;
        }

        // 1. Reset all plant multipliers to their base values before recalculating
        plant.growthSpeedMultiplier = 1f;
        plant.energyGenerationMultiplier = 1f;
        plant.energyStorageMultiplier = 1f;
        plant.fruitYieldMultiplier = 1f;

        // 2. Prepare dictionaries to aggregate stats
        var additiveBonuses = new Dictionary<PassiveStatType, float>();
        var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

        // 3. Iterate through all passive genes and aggregate their effects
        foreach (var instance in plant.geneRuntimeState.passiveInstances)
        {
            var passiveGene = instance.GetGene<PassiveGene>();
            if (passiveGene == null) continue;

            float value = passiveGene.baseValue * instance.GetValue("power_multiplier", 1f);

            if (passiveGene.stacksAdditively)
            {
                if (!additiveBonuses.ContainsKey(passiveGene.statToModify))
                    additiveBonuses[passiveGene.statToModify] = 0f;
                // For additive stats, we sum the *bonuses* (e.g., a 1.2x multiplier is a 0.2 bonus)
                additiveBonuses[passiveGene.statToModify] += (value - 1f);
            }
            else
            {
                if (!multiplicativeBonuses.ContainsKey(passiveGene.statToModify))
                    multiplicativeBonuses[passiveGene.statToModify] = 1f;
                multiplicativeBonuses[passiveGene.statToModify] *= value;
            }
        }

        // 4. Apply the aggregated stats to the plant
        foreach (var kvp in additiveBonuses)
        {
            ApplyStat(kvp.Key, 1f + kvp.Value); // Apply the summed bonuses
        }
        foreach (var kvp in multiplicativeBonuses)
        {
            ApplyStat(kvp.Key, kvp.Value); // Apply the compounded multipliers
        }

        // 5. Finalize any dependent calculations
        if (plant.EnergySystem != null)
        {
            plant.EnergySystem.BaseEnergyPerLeaf = PhotosynthesisEfficiencyPerLeaf;
        }

        Debug.Log($"[{plant.gameObject.name}] Final stats after passives: " +
                  $"GrowthSpeed={plant.growthSpeedMultiplier:F2}x, " +
                  $"EnergyGen={plant.energyGenerationMultiplier:F2}x, " +
                  $"EnergyStore={plant.energyStorageMultiplier:F2}x, " +
                  $"FruitYield={plant.fruitYieldMultiplier:F2}x");
    }

    private void ApplyStat(PassiveStatType stat, float value)
    {
        switch (stat)
        {
            case PassiveStatType.GrowthSpeed:
                plant.growthSpeedMultiplier *= value;
                break;
            case PassiveStatType.EnergyGeneration:
                plant.energyGenerationMultiplier *= value;
                break;
            case PassiveStatType.EnergyStorage:
                plant.energyStorageMultiplier *= value;
                break;
            case PassiveStatType.FruitYield:
                plant.fruitYieldMultiplier *= value;
                break;
        }
    }
}
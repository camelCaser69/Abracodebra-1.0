using UnityEngine;
using Abracodabra.Genes;
using Abracodabra.Genes.Core;
using System.Collections.Generic;

public class PlantGrowthLogic {
    readonly PlantGrowth plant;

    public int TargetStemLength { get; private set; }
    public int GrowthTicksPerStage { get; private set; }
    public float PhotosynthesisEfficiencyPerLeaf { get; private set; }

    public PlantGrowthLogic(PlantGrowth plant) {
        this.plant = plant;
    }

    public void CalculateAndApplyPassiveStats() {
        if (plant.geneRuntimeState == null) {
            Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null geneRuntimeState!");
            return;
        }

        // Reset all multipliers to 1
        plant.growthSpeedMultiplier = 1f;
        plant.energyGenerationMultiplier = 1f;
        plant.energyStorageMultiplier = 1f;
        plant.fruitYieldMultiplier = 1f;

        var additiveBonuses = new Dictionary<PassiveStatType, float>();
        var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

        foreach (var instance in plant.geneRuntimeState.passiveInstances) {
            var passiveGene = instance.GetGene<PassiveGene>();
            if (passiveGene == null) continue;
            
            // Skip genes that don't modify stats (like TerrainAffinityGene)
            if (passiveGene.statToModify == PassiveStatType.None) {
                Debug.Log($"[{plant.gameObject.name}] Passive gene '{passiveGene.geneName}' has statToModify=None, skipping stat application.");
                continue;
            }

            float value = passiveGene.baseValue * instance.GetValue("power_multiplier", 1f);

            if (passiveGene.stacksAdditively) {
                if (!additiveBonuses.ContainsKey(passiveGene.statToModify))
                    additiveBonuses[passiveGene.statToModify] = 0f;
                additiveBonuses[passiveGene.statToModify] += (value - 1f);
            }
            else {
                if (!multiplicativeBonuses.ContainsKey(passiveGene.statToModify))
                    multiplicativeBonuses[passiveGene.statToModify] = 1f;
                multiplicativeBonuses[passiveGene.statToModify] *= value;
            }
        }

        // Apply additive bonuses
        foreach (var kvp in additiveBonuses) {
            ApplyStat(kvp.Key, 1f + kvp.Value);
        }
        // Apply multiplicative bonuses
        foreach (var kvp in multiplicativeBonuses) {
            ApplyStat(kvp.Key, kvp.Value);
        }

        if (plant.EnergySystem != null) {
            plant.EnergySystem.BaseEnergyPerLeaf = PhotosynthesisEfficiencyPerLeaf;
        }

        Debug.Log($"[{plant.gameObject.name}] Final stats after passives: " +
            $"GrowthSpeed={plant.growthSpeedMultiplier:F2}x, " +
            $"EnergyGen={plant.energyGenerationMultiplier:F2}x, " +
            $"EnergyStore={plant.energyStorageMultiplier:F2}x, " +
            $"FruitYield={plant.fruitYieldMultiplier:F2}x");
    }

    void ApplyStat(PassiveStatType stat, float value) {
        switch (stat) {
            case PassiveStatType.None:
                // Do nothing - this gene doesn't modify stats
                break;
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
            case PassiveStatType.Defense:
                // Defense stat if plant has it
                break;
        }
    }
}
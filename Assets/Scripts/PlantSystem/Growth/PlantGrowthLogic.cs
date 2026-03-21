// FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowthLogic.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes {
    public class PlantGrowthLogic {
        readonly PlantGrowth plant;

        public int TargetStemLength { get; set; }
        public int GrowthTicksPerStage { get; set; }
        public float PhotosynthesisEfficiencyPerLeaf { get; set; }

        public PlantGrowthLogic(PlantGrowth plant) {
            this.plant = plant;
        }

        public void CalculateAndApplyPassiveStats() {
            if (plant.geneRuntimeState == null) {
                Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null geneRuntimeState!");
                return;
            }

            plant.growthSpeedMultiplier = 1f;
            plant.energyGenerationMultiplier = 1f;
            plant.energyStorageMultiplier = 1f;
            plant.fruitYieldMultiplier = 1f;
            plant.leafDurabilityMultiplier = 1f;
            plant.leafRegrowthRate = 0f;
            plant.thornDamage = 0f;

            var additiveBonuses = new Dictionary<PassiveStatType, float>();
            var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

            float thornDamageAccumulator = 0f;
            int regrowthStackCount = 0;
            float regrowthBaseValue = 0f;

            foreach (var instance in plant.geneRuntimeState.passiveInstances) {
                var passiveGene = instance?.GetGene<PassiveGene>();
                if (passiveGene == null) continue;

                if (passiveGene.statToModify == PassiveStatType.None) {
                    Debug.Log($"[{plant.gameObject.name}] Passive gene '{passiveGene.geneName}' has statToModify=None, skipping stat application.");
                    continue;
                }

                float value = passiveGene.baseValue * instance.GetValue("power_multiplier", 1f);

                if (passiveGene.statToModify == PassiveStatType.ThornDamage) {
                    thornDamageAccumulator += value;
                    continue;
                }

                if (passiveGene.statToModify == PassiveStatType.LeafRegrowth) {
                    regrowthStackCount++;
                    regrowthBaseValue = value; // all stacks have same base
                    continue;
                }

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

            foreach (var kvp in additiveBonuses) {
                ApplyStat(kvp.Key, 1f + kvp.Value);
            }
            foreach (var kvp in multiplicativeBonuses) {
                ApplyStat(kvp.Key, kvp.Value);
            }

            plant.thornDamage = thornDamageAccumulator;

            if (regrowthStackCount > 0) {
                plant.leafRegrowthRate = Mathf.Max(2f, regrowthBaseValue - (regrowthStackCount - 1));
            }

            if (plant.EnergySystem != null) {
                plant.EnergySystem.BaseEnergyPerLeaf = PhotosynthesisEfficiencyPerLeaf;
            }

            Debug.Log($"[{plant.gameObject.name}] Final stats after passives: " +
                $"GrowthSpeed={plant.growthSpeedMultiplier:F2}x, " +
                $"EnergyGen={plant.energyGenerationMultiplier:F2}x, " +
                $"EnergyStore={plant.energyStorageMultiplier:F2}x, " +
                $"FruitYield={plant.fruitYieldMultiplier:F2}x, " +
                $"LeafDurability={plant.leafDurabilityMultiplier:F2}x, " +
                $"LeafRegrowth={plant.leafRegrowthRate:F1}t, " +
                $"ThornDmg={plant.thornDamage:F1}");
        }

        private void ApplyStat(PassiveStatType stat, float value) {
            switch (stat) {
                case PassiveStatType.None:
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
                    plant.leafDurabilityMultiplier *= value;
                    break;
                case PassiveStatType.LeafRegrowth:
                case PassiveStatType.ThornDamage:
                    // Handled separately in CalculateAndApplyPassiveStats
                    break;
            }
        }
    }
}
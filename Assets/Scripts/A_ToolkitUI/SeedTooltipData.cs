// FILE: Assets/Scripts/A_ToolkitUI/SeedTooltipData.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;

namespace Abracodabra.UI.Tooltips {
    public class SeedTooltipData {

        public string seedName;
        public PlantType plantType;
        public int generation;
        public SeedRarity rarity;

        public float growthSpeedMultiplier = 1f;
        public float energyStorageMultiplier = 1f;
        public float energyGenerationMultiplier = 1f;
        public float fruitYieldMultiplier = 1f;
        public float leafDurabilityMultiplier = 1f;  // v6: replaces defenseMultiplier

        // Keep for backward compatibility during migration — mirrors leafDurabilityMultiplier
        [System.Obsolete("Use leafDurabilityMultiplier instead.")]
        public float defenseMultiplier {
            get => leafDurabilityMultiplier;
            set => leafDurabilityMultiplier = value;
        }

        public List<PassiveGeneInfo> passiveGenes = new List<PassiveGeneInfo>();
        public List<SequenceSlotInfo> sequenceSlots = new List<SequenceSlotInfo>();
        public float totalBaseEnergyCost;
        public float totalModifiedEnergyCost;
        public int totalCycleTime;

        public List<string> synergies = new List<string>();
        public List<string> warnings = new List<string>();

        public float estimatedMaturityTicks;
        public float energySurplusPerCycle;
        public string primaryYieldSummary;
        public SeedQualityCalculator.QualityTier qualityTier;

        public class PassiveGeneInfo {
            public string geneName;
            public PassiveStatType statType;
            public string effectText;
        }

        public class SequenceSlotInfo {
            public int position;
            public string actionName;
            public float baseCost;
            public float modifiedCost;
            public List<string> modifiers = new List<string>();
            public List<string> payloads = new List<string>();
        }

        public static SeedTooltipData CreateFromSeed(SeedTemplate template, PlantGeneRuntimeState runtimeState) {
            if (template == null || runtimeState == null) return null;

            var data = new SeedTooltipData();

            data.seedName = template.templateName;
            data.plantType = template.plantType;
            data.generation = template.generation;
            data.rarity = template.rarity;

            data.ProcessPassives(runtimeState);
            data.ProcessSequence(runtimeState, template);
            data.CalculateHighLevelMetrics(template);
            data.DetectSynergiesAndWarnings();

            data.qualityTier = SeedQualityCalculator.CalculateQuality(data);

            return data;
        }

        void ProcessPassives(PlantGeneRuntimeState state) {
            var statBonuses = new Dictionary<PassiveStatType, float>();
            var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

            foreach (var instance in state.passiveInstances) {
                var gene = instance?.GetGene<PassiveGene>();
                if (gene == null) continue;

                var info = new PassiveGeneInfo {
                    geneName = gene.geneName,
                    statType = gene.statToModify,
                    effectText = gene.GetStatModificationText()
                };
                passiveGenes.Add(info);

                float value = gene.baseValue * instance.GetValue("power_multiplier", 1f);

                if (gene.stacksAdditively) {
                    if (!statBonuses.ContainsKey(gene.statToModify)) statBonuses[gene.statToModify] = 0f;
                    statBonuses[gene.statToModify] += (value - 1f);
                }
                else {
                    if (!multiplicativeBonuses.ContainsKey(gene.statToModify)) multiplicativeBonuses[gene.statToModify] = 1f;
                    multiplicativeBonuses[gene.statToModify] *= value;
                }
            }

            growthSpeedMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.GrowthSpeed)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.GrowthSpeed, 1f);
            energyGenerationMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.EnergyGeneration)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.EnergyGeneration, 1f);
            energyStorageMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.EnergyStorage)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.EnergyStorage, 1f);
            fruitYieldMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.FruitYield)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.FruitYield, 1f);
            leafDurabilityMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.Defense)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.Defense, 1f);
        }

        void ProcessSequence(PlantGeneRuntimeState state, SeedTemplate template) {
            totalBaseEnergyCost = 0;
            totalModifiedEnergyCost = 0;
            int activeSlots = 0;

            for (int i = 0; i < state.activeSequence.Count; i++) {
                var slot = state.activeSequence[i];
                if (!slot.HasContent) continue;

                activeSlots++;
                var activeGene = slot.activeInstance.GetGene<ActiveGene>();

                var info = new SequenceSlotInfo {
                    position = i + 1,
                    actionName = activeGene?.geneName ?? "Unknown",
                    baseCost = activeGene?.baseEnergyCost ?? 0,
                    modifiedCost = slot.GetEnergyCost(),
                    modifiers = slot.modifierInstances.Select(m => m?.GetGene()?.geneName ?? "?").ToList(),
                    payloads = slot.payloadInstances.Select(p => p?.GetGene()?.geneName ?? "?").ToList()
                };
                sequenceSlots.Add(info);

                totalBaseEnergyCost += info.baseCost;
                totalModifiedEnergyCost += info.modifiedCost;
            }

            totalCycleTime = template.baseRechargeTime + activeSlots;
        }

        void CalculateHighLevelMetrics(SeedTemplate template) {
            float avgHeight = (template.minHeight + template.maxHeight) / 2f;
            float effectiveGrowthRate = template.baseGrowthChance * growthSpeedMultiplier;
            estimatedMaturityTicks = effectiveGrowthRate > 0 ? (avgHeight / effectiveGrowthRate) : float.PositiveInfinity;

            float energyGeneratedPerCycle = template.energyRegenRate * energyGenerationMultiplier * totalCycleTime;
            energySurplusPerCycle = energyGeneratedPerCycle - totalModifiedEnergyCost;

            var yieldCounts = new Dictionary<string, int>();
            foreach (var slot in sequenceSlots) {
                var fruitGene = passiveGenes.FirstOrDefault(g => g.geneName == slot.actionName);
                if (slot.actionName.ToLower().Contains("fruit")) {
                    string itemName = "Fruit";
                    if (!yieldCounts.ContainsKey(itemName)) yieldCounts[itemName] = 0;
                    yieldCounts[itemName]++;
                }
            }
            primaryYieldSummary = yieldCounts.Any()
                ? string.Join(", ", yieldCounts.Select(kvp => $"{kvp.Key} (x{kvp.Value})"))
                : "No yield";
        }

        void DetectSynergiesAndWarnings() {
            if (growthSpeedMultiplier > 1.25f && energyGenerationMultiplier > 1.25f) {
                synergies.Add("Rapid Growth Engine: High growth speed is fueled by increased energy generation.");
            }
            if (energySurplusPerCycle < 0) {
                warnings.Add("Energy Deficit: This plant consumes more energy per cycle than it generates.");
            }
            if (leafDurabilityMultiplier < 1f) {
                warnings.Add("Fragile Leaves: Low leaf durability makes this plant vulnerable to pests.");
            }
        }
    }
}
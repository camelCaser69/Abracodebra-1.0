using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;

namespace Abracodabra.UI.Tooltips
{
    /// <summary>
    /// Processes and holds all calculated data for seed tooltips.
    /// </summary>
    [Serializable]
    public class SeedTooltipData
    {
        #region Fields

        // Basic Information
        public string seedName;
        public PlantType plantType;
        public int generation;
        public SeedRarity rarity;

        // Multipliers (from passives)
        public float growthSpeedMultiplier = 1f;
        public float energyStorageMultiplier = 1f;
        public float energyGenerationMultiplier = 1f;
        public float fruitYieldMultiplier = 1f;
        public float defenseMultiplier = 1f;

        // Gene Information
        public List<PassiveGeneInfo> passiveGenes = new List<PassiveGeneInfo>();
        public List<SequenceSlotInfo> sequenceSlots = new List<SequenceSlotInfo>();
        public float totalBaseEnergyCost;
        public float totalModifiedEnergyCost;
        public int totalCycleTime;

        // Synergies and Warnings
        public List<string> synergies = new List<string>();
        public List<string> warnings = new List<string>();

        // High-Level Calculated Metrics
        public float estimatedMaturityTicks;
        public float energySurplusPerCycle;
        public string primaryYieldSummary;
        public SeedQualityCalculator.QualityTier qualityTier;

        #endregion

        #region Helper Subclasses
        [Serializable]
        public class PassiveGeneInfo
        {
            public string geneName;
            public PassiveStatType statType;
            public string effectText;
        }

        [Serializable]
        public class SequenceSlotInfo
        {
            public int position;
            public string actionName;
            public float baseCost;
            public float modifiedCost;
            public List<string> modifiers = new List<string>();
            public List<string> payloads = new List<string>();
        }
        #endregion

        public static SeedTooltipData CreateFromSeed(SeedTemplate template, PlantGeneRuntimeState runtimeState)
        {
            if (template == null || runtimeState == null) return null;

            var data = new SeedTooltipData();

            // Basic info
            data.seedName = template.templateName;
            data.plantType = template.plantType;
            data.generation = template.generation;
            data.rarity = template.rarity;

            // Process data from runtime state
            data.ProcessPassives(runtimeState);
            data.ProcessSequence(runtimeState, template);
            data.CalculateHighLevelMetrics(template);
            data.DetectSynergiesAndWarnings();
            
            data.qualityTier = SeedQualityCalculator.CalculateQuality(data);

            return data;
        }

        private void ProcessPassives(PlantGeneRuntimeState state)
        {
            var statBonuses = new Dictionary<PassiveStatType, float>();
            var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

            foreach (var instance in state.passiveInstances)
            {
                var gene = instance?.GetGene<PassiveGene>();
                if (gene == null) continue;

                var info = new PassiveGeneInfo
                {
                    geneName = gene.geneName,
                    statType = gene.statToModify,
                    effectText = gene.GetStatModificationText()
                };
                passiveGenes.Add(info);
                
                float value = gene.baseValue * instance.GetValue("power_multiplier", 1f);

                if (gene.stacksAdditively)
                {
                    if (!statBonuses.ContainsKey(gene.statToModify)) statBonuses[gene.statToModify] = 0f;
                    statBonuses[gene.statToModify] += (value - 1f);
                }
                else
                {
                    if (!multiplicativeBonuses.ContainsKey(gene.statToModify)) multiplicativeBonuses[gene.statToModify] = 1f;
                    multiplicativeBonuses[gene.statToModify] *= value;
                }
            }
            
            // Apply final multipliers
            growthSpeedMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.GrowthSpeed)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.GrowthSpeed, 1f);
            energyGenerationMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.EnergyGeneration)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.EnergyGeneration, 1f);
            energyStorageMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.EnergyStorage)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.EnergyStorage, 1f);
            fruitYieldMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.FruitYield)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.FruitYield, 1f);
            defenseMultiplier = (1f + statBonuses.GetValueOrDefault(PassiveStatType.Defense)) * multiplicativeBonuses.GetValueOrDefault(PassiveStatType.Defense, 1f);
        }

        private void ProcessSequence(PlantGeneRuntimeState state, SeedTemplate template)
        {
            totalBaseEnergyCost = 0;
            totalModifiedEnergyCost = 0;
            int activeSlots = 0;

            for (int i = 0; i < state.activeSequence.Count; i++)
            {
                var slot = state.activeSequence[i];
                if (!slot.HasContent) continue;
                
                activeSlots++;
                var activeGene = slot.activeInstance.GetGene<ActiveGene>();

                var info = new SequenceSlotInfo
                {
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

        private void CalculateHighLevelMetrics(SeedTemplate template)
        {
            // Maturity time estimation (ticks to full height)
            float avgHeight = (template.minHeight + template.maxHeight) / 2f;
            float effectiveGrowthRate = template.baseGrowthChance * growthSpeedMultiplier;
            estimatedMaturityTicks = effectiveGrowthRate > 0 ? (avgHeight / effectiveGrowthRate) : float.PositiveInfinity;
            
            // Energy Surplus/Deficit
            float energyGeneratedPerCycle = template.energyRegenRate * energyGenerationMultiplier * totalCycleTime;
            energySurplusPerCycle = energyGeneratedPerCycle - totalModifiedEnergyCost;
            
            // Primary Yield Summary
            var yieldCounts = new Dictionary<string, int>();
            foreach(var slot in sequenceSlots)
            {
                // This is a placeholder. You need to get the actual item from the gene.
                // Assuming BasicFruitGene has a public `ItemDefinition harvestedItemDefinition`.
                var fruitGene = passiveGenes.FirstOrDefault(g => g.geneName == slot.actionName); // Simplistic lookup
                // In a real scenario, you'd need to access the RuntimeGeneInstance and its underlying ActiveGene
                // For now, we'll simulate.
                if(slot.actionName.ToLower().Contains("fruit"))
                {
                    string itemName = "Fruit"; // Placeholder
                    if (!yieldCounts.ContainsKey(itemName)) yieldCounts[itemName] = 0;
                    yieldCounts[itemName]++;
                }
            }
            primaryYieldSummary = yieldCounts.Any() 
                ? string.Join(", ", yieldCounts.Select(kvp => $"{kvp.Key} (x{kvp.Value})"))
                : "No yield";
        }

        private void DetectSynergiesAndWarnings()
        {
            // Synergy: High growth and energy generation
            if (growthSpeedMultiplier > 1.25f && energyGenerationMultiplier > 1.25f)
            {
                synergies.Add("Rapid Growth Engine: High growth speed is fueled by increased energy generation.");
            }
            // Warning: Energy deficit
            if (energySurplusPerCycle < 0)
            {
                warnings.Add("Energy Deficit: This plant consumes more energy per cycle than it generates.");
            }
            // Warning: Low defense
            if (defenseMultiplier < 0.8f)
            {
                warnings.Add("Vulnerable: Low defense makes this plant an easy target for herbivores.");
            }
        }
    }
}
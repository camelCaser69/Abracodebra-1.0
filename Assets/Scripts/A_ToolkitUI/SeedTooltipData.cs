// ============================================================
// FILE: Assets/Scripts/A_ToolkitUI/SeedTooltipData.cs
// ============================================================
// Task 8.2: Added leaf balance fields + self-damage/regrowth detection
// ============================================================

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
        public float leafDurabilityMultiplier = 1f;

        // v6 leaf vitality stats
        public float thornDamageTotal = 0f;
        public float leafRegrowthTickRate = 0f;       // ticks between regrowths (0 = no regrowth)
        public float selfDamageLeafLossRate = 0f;     // estimated leaves lost per tick from self-damage (0 = none)
        public string leafBalanceSummary = "";          // human-readable: "Sustainable" or "Net: -1 leaf/7.5 ticks"
        public bool hasThornedLeaves = false;
        public bool hasRegrowth = false;

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
            data.CalculateLeafBalance(template);
            data.DetectSynergiesAndWarnings();

            data.qualityTier = SeedQualityCalculator.CalculateQuality(data);

            return data;
        }

        private void ProcessPassives(PlantGeneRuntimeState state) {
            var statBonuses = new Dictionary<PassiveStatType, float>();
            var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

            float thornDamageAccumulator = 0f;
            int regrowthStackCount = 0;
            float regrowthBaseValue = 0f;

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

                if (gene.statToModify == PassiveStatType.ThornDamage) {
                    thornDamageAccumulator += value;
                    continue;
                }

                if (gene.statToModify == PassiveStatType.LeafRegrowth) {
                    regrowthStackCount++;
                    regrowthBaseValue = value;
                    continue;
                }

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

            // v6 leaf vitality passives
            thornDamageTotal = thornDamageAccumulator;
            hasThornedLeaves = thornDamageAccumulator > 0f;

            if (regrowthStackCount > 0) {
                leafRegrowthTickRate = Mathf.Max(2f, regrowthBaseValue - (regrowthStackCount - 1));
                hasRegrowth = true;
            }
        }

        private void ProcessSequence(PlantGeneRuntimeState state, SeedTemplate template) {
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

        private void CalculateHighLevelMetrics(SeedTemplate template) {
            float avgHeight = (template.minHeight + template.maxHeight) / 2f;
            float effectiveGrowthRate = template.baseGrowthChance * growthSpeedMultiplier;
            estimatedMaturityTicks = effectiveGrowthRate > 0 ? (avgHeight / effectiveGrowthRate) : float.PositiveInfinity;

            float energyGeneratedPerCycle = template.energyRegenRate * energyGenerationMultiplier * totalCycleTime;
            energySurplusPerCycle = energyGeneratedPerCycle - totalModifiedEnergyCost;

            var yieldCounts = new Dictionary<string, int>();
            foreach (var slot in sequenceSlots) {
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

        /// <summary>
        /// Calculates the leaf balance for self-damaging builds.
        /// Estimates self-damage rate from gene names that imply leaf destruction,
        /// compares with regrowth rate to produce a sustainability summary.
        /// </summary>
        private void CalculateLeafBalance(SeedTemplate template) {
            if (totalCycleTime <= 0) return;

            // Estimate self-damage from active genes and modifiers
            float selfDamageLeavesPerCycle = 0f;

            foreach (var slot in sequenceSlots) {
                string actionLower = slot.actionName.ToLower();

                // Explosive-type actives: destroy ~1 leaf per firing
                if (actionLower.Contains("explosive") || actionLower.Contains("aura")) {
                    // Check if it has explosive-like payloads
                    bool hasExplosivePayload = slot.payloads.Any(p =>
                        p.ToLower().Contains("explosive"));
                    if (hasExplosivePayload || actionLower.Contains("explosive")) {
                        selfDamageLeavesPerCycle += 1f;
                    }
                }

                // Pruning active: sacrifices 1 leaf per execution
                if (actionLower.Contains("pruning")) {
                    selfDamageLeavesPerCycle += 1f;
                }

                // Volatile modifier: ~15% chance per active execution to destroy 1 leaf
                bool hasVolatile = slot.modifiers.Any(m =>
                    m.ToLower().Contains("volatile"));
                if (hasVolatile) {
                    selfDamageLeavesPerCycle += 0.15f;
                }
            }

            selfDamageLeafLossRate = totalCycleTime > 0
                ? selfDamageLeavesPerCycle / totalCycleTime
                : 0f;

            // Build the summary
            if (selfDamageLeavesPerCycle <= 0f) {
                leafBalanceSummary = "";
                return;
            }

            float selfDamageTicksPerLeaf = selfDamageLeavesPerCycle > 0
                ? totalCycleTime / selfDamageLeavesPerCycle
                : float.PositiveInfinity;

            if (hasRegrowth && leafRegrowthTickRate > 0f) {
                // Compare: regrowth ticks per leaf vs self-damage ticks per leaf
                // Lower ticks per leaf = faster rate
                if (leafRegrowthTickRate <= selfDamageTicksPerLeaf) {
                    leafBalanceSummary = "Sustainable (regrowth offsets self-damage)";
                }
                else {
                    // Net loss: combined rate
                    // Leaves lost per tick from self-damage: 1/selfDamageTicksPerLeaf
                    // Leaves gained per tick from regrowth: 1/leafRegrowthTickRate
                    float netLossPerTick = (1f / selfDamageTicksPerLeaf) - (1f / leafRegrowthTickRate);
                    float ticksPerNetLeafLoss = netLossPerTick > 0 ? 1f / netLossPerTick : float.PositiveInfinity;

                    // Estimate max leaves from template
                    int estimatedMaxLeaves = Mathf.Max(1, template.maxHeight * template.leafDensity);
                    float estimatedLifespan = estimatedMaxLeaves * ticksPerNetLeafLoss;

                    leafBalanceSummary = $"Net loss: ~1 leaf per {ticksPerNetLeafLoss:F0} ticks. " +
                        $"Lifespan: ~{estimatedLifespan:F0} ticks with {estimatedMaxLeaves} leaves.";
                }
            }
            else {
                // No regrowth at all — pure self-damage
                int estimatedMaxLeaves = Mathf.Max(1, template.maxHeight * template.leafDensity);
                float estimatedLifespan = estimatedMaxLeaves * selfDamageTicksPerLeaf;

                leafBalanceSummary = $"Self-damage: ~1 leaf per {selfDamageTicksPerLeaf:F0} ticks. " +
                    $"No regrowth. Lifespan: ~{estimatedLifespan:F0} ticks with {estimatedMaxLeaves} leaves.";
            }
        }

        private void DetectSynergiesAndWarnings() {
            if (growthSpeedMultiplier > 1.25f && energyGenerationMultiplier > 1.25f) {
                synergies.Add("Rapid Growth Engine: High growth speed is fueled by increased energy generation.");
            }
            if (hasThornedLeaves && leafDurabilityMultiplier >= 2f) {
                synergies.Add("Armored Thorns: High leaf durability forces pests to eat slowly while taking thorn damage each leaf.");
            }
            if (hasRegrowth && selfDamageLeafLossRate > 0f && leafBalanceSummary.Contains("Sustainable")) {
                synergies.Add("Self-Sustaining Cycle: Regrowth offsets self-damage for indefinite operation.");
            }
            if (hasThornedLeaves) {
                synergies.Add($"Thorned Leaves: Pests take {thornDamageTotal:F0} damage per leaf eaten.");
            }
            if (hasRegrowth) {
                synergies.Add($"Leaf Regrowth: Regrows 1 leaf every {leafRegrowthTickRate:F0} ticks.");
            }

            if (energySurplusPerCycle < 0) {
                warnings.Add("Energy Deficit: This plant consumes more energy per cycle than it generates.");
            }
            if (leafDurabilityMultiplier < 1f) {
                warnings.Add("Fragile Leaves: Low leaf durability makes this plant vulnerable to pests.");
            }
            if (selfDamageLeafLossRate > 0f && !string.IsNullOrEmpty(leafBalanceSummary)) {
                if (!leafBalanceSummary.Contains("Sustainable")) {
                    warnings.Add($"Self-Damage: {leafBalanceSummary}");
                }
            }
            if (selfDamageLeafLossRate > 0f && !hasRegrowth) {
                warnings.Add("No Regrowth: This build damages its own leaves with no way to recover. Add a Regrowth passive to sustain.");
            }
        }
    }
}
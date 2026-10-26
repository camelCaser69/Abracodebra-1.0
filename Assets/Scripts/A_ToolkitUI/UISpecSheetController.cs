// ============================================================
// FILE: Assets/Scripts/A_ToolkitUI/UISpecSheetController.cs
// ============================================================
// Task 8.2: Added leaf vitality info display (thorn, regrowth, leaf balance)
// ============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Tooltips;

namespace Abracodabra.UI.Toolkit {
    public class UISpecSheetController {
        private Image seedIcon;
        private Label seedNameText, qualityText, descriptionText;
        private Label maturityTimeText, energyBalanceText, yieldText, cycleTimeText;
        private VisualElement attributeContainer, sequenceContainer, synergiesContainer, warningsContainer;
        private Label readsAsHeader, readsAsText;

        private const int THUMBNAIL_SIZE = 64;

        public void Initialize(VisualElement specSheetPanel) {
            seedIcon = specSheetPanel.Q<Image>("seed-icon");
            seedNameText = specSheetPanel.Q<Label>("seed-name-text");
            qualityText = specSheetPanel.Q<Label>("quality-text");
            descriptionText = specSheetPanel.Q<Label>("description-text");
            maturityTimeText = specSheetPanel.Q<Label>("maturity-time-text");
            energyBalanceText = specSheetPanel.Q<Label>("energy-balance-text");
            yieldText = specSheetPanel.Q<Label>("yield-text");
            cycleTimeText = specSheetPanel.Q<Label>("cycle-time-text");
            attributeContainer = specSheetPanel.Q<VisualElement>("attribute-breakdown-container");
            sequenceContainer = specSheetPanel.Q<VisualElement>("sequence-breakdown-container");
            synergiesContainer = specSheetPanel.Q<VisualElement>("synergies-container");
            warningsContainer = specSheetPanel.Q<VisualElement>("warnings-container");

            if (seedIcon != null) {
                seedIcon.style.width = THUMBNAIL_SIZE;
                seedIcon.style.height = THUMBNAIL_SIZE;
                seedIcon.style.minWidth = THUMBNAIL_SIZE;
                seedIcon.style.minHeight = THUMBNAIL_SIZE;
                seedIcon.style.maxWidth = THUMBNAIL_SIZE;
                seedIcon.style.maxHeight = THUMBNAIL_SIZE;
                seedIcon.scaleMode = ScaleMode.ScaleToFit;
            }

            // B7: "Reads as" line, code-built right under the description (no uxml change).
            if (descriptionText != null && descriptionText.parent != null) {
                readsAsHeader = new Label("READS AS");
                readsAsHeader.AddToClassList("section-header");
                readsAsText = CreateReadsAsLabel("spec-reads-as-text");
                readsAsHeader.style.display = DisplayStyle.None;
                readsAsText.style.display = DisplayStyle.None;
                var parent = descriptionText.parent;
                int at = parent.IndexOf(descriptionText) + 1;
                parent.Insert(at, readsAsHeader);
                parent.Insert(at + 1, readsAsText);
            }
        }

        public void DisplayItem(UIInventoryItem item) {
            if (item == null) {
                Clear();
                return;
            }

            if (item.OriginalData is SeedTemplate seedTemplate) {
                DisplaySeed(item, seedTemplate);
            }
            else if (item.OriginalData is GeneBase gene) {
                DisplayGene(gene);
            }
            else if (item.OriginalData is ToolDefinition tool) {
                DisplayTool(tool);
            }
            else {
                Clear();
            }
        }

        private void DisplaySeed(UIInventoryItem item, SeedTemplate seedTemplate) {
            var data = SeedTooltipData.CreateFromSeed(seedTemplate, item.SeedRuntimeState);
            if (data == null) {
                Clear();
                return;
            }

            seedIcon.sprite = item.Icon;
            ApplyIconSizing();
            SetReadsAs(item.SeedRuntimeState);

            seedNameText.text = item.GetDisplayName();
            qualityText.text = SeedQualityCalculator.GetQualityDescription(data.qualityTier);
            qualityText.style.color = SeedQualityCalculator.GetQualityColor(data.qualityTier);
            descriptionText.text = seedTemplate.description;

            maturityTimeText.text = $"Maturity: {data.estimatedMaturityTicks:F0} ticks";
            energyBalanceText.text = $"Energy Balance: {data.energySurplusPerCycle:F1} E/cycle";
            yieldText.text = $"Primary Yield: {data.primaryYieldSummary}";

            attributeContainer.Clear();
            CreateAttributeDisplay("Growth", data.growthSpeedMultiplier);
            CreateAttributeDisplay("Storage", data.energyStorageMultiplier);
            CreateAttributeDisplay("Generation", data.energyGenerationMultiplier);
            CreateAttributeDisplay("Yield", data.fruitYieldMultiplier);
            CreateAttributeDisplay("Leaf Durability", data.leafDurabilityMultiplier);

            // v6 leaf vitality attributes
            if (data.hasThornedLeaves) {
                var thornLabel = new Label($"Thorn Damage: {data.thornDamageTotal:F0} per leaf eaten");
                thornLabel.style.fontSize = 13;
                thornLabel.style.color = new StyleColor(new Color(0.6f, 1f, 0.6f));
                attributeContainer.Add(thornLabel);
            }
            if (data.hasRegrowth) {
                var regrowthLabel = new Label($"Leaf Regrowth: 1 leaf every {data.leafRegrowthTickRate:F0} ticks");
                regrowthLabel.style.fontSize = 13;
                regrowthLabel.style.color = new StyleColor(new Color(0.5f, 1f, 0.8f));
                attributeContainer.Add(regrowthLabel);
            }

            cycleTimeText.text = $"Cycle Time: {data.totalCycleTime} ticks";
            sequenceContainer.Clear();
            foreach (var slot in data.sequenceSlots) {
                var label = new Label($"A{slot.position}: {slot.actionName} ({slot.modifiedCost:F0}E)");
                sequenceContainer.Add(label);
            }

            synergiesContainer.Clear();
            warningsContainer.Clear();
            foreach (var synergy in data.synergies) {
                var label = new Label($"\u2713 {synergy}");
                label.style.color = new StyleColor(new Color(0.5f, 1f, 0.5f));
                synergiesContainer.Add(label);
            }
            foreach (var warning in data.warnings) {
                var label = new Label($"\u26A0 {warning}");
                label.style.color = new StyleColor(new Color(1f, 0.8f, 0.5f));
                warningsContainer.Add(label);
            }

            // Leaf balance summary (only for self-damaging builds)
            if (!string.IsNullOrEmpty(data.leafBalanceSummary)) {
                bool isSustainable = data.leafBalanceSummary.Contains("Sustainable");
                var balanceLabel = new Label(isSustainable
                    ? $"\u2705 {data.leafBalanceSummary}"
                    : $"\U0001F342 {data.leafBalanceSummary}");
                balanceLabel.style.fontSize = 12;
                balanceLabel.style.color = new StyleColor(isSustainable
                    ? new Color(0.4f, 1f, 0.4f)
                    : new Color(1f, 0.65f, 0.3f));
                balanceLabel.style.whiteSpace = WhiteSpace.Normal;
                balanceLabel.style.marginTop = 4;
                warningsContainer.Add(balanceLabel);
            }
        }

        public void DisplayGene(GeneBase gene) {
            seedIcon.sprite = gene.icon;
            ApplyIconSizing();
            SetReadsAs(null);

            seedNameText.text = gene.geneName;
            qualityText.text = $"Tier {gene.tier} {gene.Category}";
            qualityText.style.color = Color.cyan;
            descriptionText.text = gene.description;

            maturityTimeText.text = "";
            energyBalanceText.text = "";
            yieldText.text = "";
            cycleTimeText.text = "";

            attributeContainer.Clear();
            sequenceContainer.Clear();
            synergiesContainer.Clear();
            warningsContainer.Clear();

            var categoryLabel = new Label($"Category: {gene.Category}");
            attributeContainer.Add(categoryLabel);

            var tierLabel = new Label($"Tier: {gene.tier}");
            attributeContainer.Add(tierLabel);
        }

        private void DisplayTool(ToolDefinition tool) {
            seedIcon.sprite = tool.icon;
            ApplyIconSizing();
            SetReadsAs(null);

            seedNameText.text = tool.displayName;
            qualityText.text = "Tool";
            qualityText.style.color = Color.white;
            descriptionText.text = tool.GetTooltipDescription();

            maturityTimeText.text = "";
            energyBalanceText.text = "";
            yieldText.text = "";
            cycleTimeText.text = "";

            attributeContainer.Clear();
            sequenceContainer.Clear();
            synergiesContainer.Clear();
            warningsContainer.Clear();

            var typeLabel = new Label($"Tool Type: {tool.toolType}");
            attributeContainer.Add(typeLabel);
        }

        public void Clear() {
            SetReadsAs(null);
            seedNameText.text = "Select an Item";
            qualityText.text = "Awaiting Selection...";
            qualityText.style.color = Color.gray;
            descriptionText.text = "Select an item from the inventory to see its details.";
            seedIcon.sprite = null;
            maturityTimeText.text = "";
            energyBalanceText.text = "";
            yieldText.text = "";
            cycleTimeText.text = "";
            attributeContainer.Clear();
            sequenceContainer.Clear();
            synergiesContainer.Clear();
            warningsContainer.Clear();
        }

        // ------------------------------------------------------------
        // B7: "Reads as" - the strand in one plain sentence
        // ------------------------------------------------------------

        /// <summary>Label for a "reads as" sentence in the moss-and-sage palette (shared by the spec sheet and the seed editor).</summary>
        public static Label CreateReadsAsLabel(string elementName) {
            var label = new Label();
            label.name = elementName;
            label.style.fontSize = 13;
            label.style.color = new StyleColor(new Color(0.74f, 0.82f, 0.70f));
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 4;
            label.style.marginBottom = 8;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 4;
            label.style.paddingBottom = 4;
            label.style.borderLeftWidth = 3;
            label.style.borderLeftColor = new StyleColor(new Color(0.55f, 0.68f, 0.52f));
            label.style.backgroundColor = new StyleColor(new Color(0.55f, 0.68f, 0.52f, 0.10f));
            return label;
        }

        /// <summary>The sentence shown in the spec sheet ("" when hidden). For checks.</summary>
        public string ReadsAsText => (readsAsText != null && readsAsText.style.display != DisplayStyle.None) ? readsAsText.text : "";

        private void SetReadsAs(PlantGeneRuntimeState state) {
            if (readsAsText == null) return;
            if (state == null) {
                readsAsHeader.style.display = DisplayStyle.None;
                readsAsText.style.display = DisplayStyle.None;
                return;
            }
            readsAsText.text = BuildReadsAs(state);
            readsAsHeader.style.display = DisplayStyle.Flex;
            readsAsText.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Builds one plain sentence from the strand, e.g.
        /// "Each cycle: a cheap poison cloud (6 E), then a nutritious fruit (20 E). Waits at a step until the plant has the energy, rests 3 ticks after the last."
        /// The strand is the slot list (PlantGeneRuntimeState.activeSequence); its order is the order the executor runs.
        /// </summary>
        public static string BuildReadsAs(PlantGeneRuntimeState state) {
            if (state == null) return "";

            var steps = new List<string>();
            var reactions = new List<string>();

            if (state.activeSequence != null) {
                foreach (var slot in state.activeSequence) {
                    if (slot == null || !slot.HasContent) continue;
                    var active = slot.activeInstance.GetGene<ActiveGene>();
                    if (active == null) continue;

                    if (active.isTriggerType) reactions.Add(DescribeReaction(active, slot));
                    else steps.Add(DescribeStep(active, slot));
                }
            }

            if (steps.Count == 0 && reactions.Count == 0) {
                return "Nothing yet: put an active gene into the sequence.";
            }

            var parts = new List<string>();
            if (steps.Count > 0) {
                string text = "Each cycle: " + string.Join(", then ", steps) + ".";
                int rest = state.template != null ? state.template.baseRechargeTime : 0;
                text += rest > 0
                    ? $" Waits at a step until the plant has the energy, rests {rest} ticks after the last."
                    : " Waits at a step until the plant has the energy.";
                parts.Add(text);
            }
            else {
                parts.Add("No step on the cycle.");
            }
            if (reactions.Count > 0) {
                parts.Add("Also: " + string.Join(", and ", reactions) + ".");
            }
            return string.Join(" ", parts);
        }

        private static string DescribeStep(ActiveGene active, RuntimeSequenceSlot slot) {
            var prefixes = new List<string>();
            var suffixes = new List<string>();

            foreach (var modInstance in slot.modifierInstances) {
                var mod = modInstance?.GetGene<ModifierGene>();
                if (mod == null) continue;
                if (mod is CostReductionGene) prefixes.Add("cheap");
                else if (mod is OverchargeGene) prefixes.Add("overcharged");
                else if (mod is TriggerProximityGene prox) suffixes.Add($"when a pest is within {prox.detectionRange} tiles");
                else prefixes.Add(mod.geneName.ToLower());
            }
            prefixes.AddRange(PayloadAdjectives(slot));

            if (active.requiresTarget && suffixes.Count == 0) suffixes.Add("when a pest is in range");

            string noun = StepNoun(active);
            string phrase = prefixes.Count > 0 ? string.Join(" ", prefixes) + " " + noun : noun;
            phrase = Article(phrase) + " " + phrase;

            if (active is AuraGene aura) {
                phrase += $" ({aura.energyDrainPerTick.ToString("0.#", CultureInfo.InvariantCulture)} E per tick while it lasts)";
            }
            else {
                float cost = slot.GetEnergyCost();
                if (cost > 0f) phrase += $" ({cost.ToString("0.#", CultureInfo.InvariantCulture)} E)";
            }

            if (suffixes.Count > 0) phrase += " " + string.Join(" and ", suffixes);
            return phrase;
        }

        private static string DescribeReaction(ActiveGene active, RuntimeSequenceSlot slot) {
            var adjectives = PayloadAdjectives(slot);
            if (active is ReactiveBurstGene burst) {
                string noun = adjectives.Count > 0 ? string.Join(" ", adjectives) + " burst" : "burst";
                return $"{Article(noun)} {noun} whenever a pest bites a leaf ({burst.burstEnergyCost.ToString("0.#", CultureInfo.InvariantCulture)} E, at most once per {burst.cooldownTicks} ticks, outside the cycle)";
            }
            return $"{active.geneName.ToLower()} on its own, outside the cycle";
        }

        private static List<string> PayloadAdjectives(RuntimeSequenceSlot slot) {
            var result = new List<string>();
            foreach (var payloadInstance in slot.payloadInstances) {
                var payload = payloadInstance?.GetGene<PayloadGene>();
                if (payload == null) continue;

                if (payload is NutritiousPayload) result.Add("nutritious");
                else if (payload is PoisonPayload) result.Add("poison");
                else if (payload is SlowPayload) result.Add("slowing");
                else if (payload is FreezePayload) result.Add("freezing");
                else if (payload is FearPayload) result.Add("frightening");
                else if (payload is ExplosivePayload) result.Add("explosive");
                else if (payload is HealingPayload) result.Add("healing");
                else {
                    string name = payload.geneName ?? "?";
                    if (name.StartsWith("Payload ")) name = name.Substring("Payload ".Length);
                    result.Add(name.ToLower());
                }
            }
            return result;
        }

        private static string StepNoun(ActiveGene active) {
            if (active is BasicFruitGene) return "fruit";
            if (active is CloudGene) return "cloud";
            if (active is AuraGene) return "aura";
            if (active is ProjectileGene) return "shot";
            if (active is PruningGene) return "leaf pruned for energy";
            if (active is TrapGene) return "root trap";
            return active.geneName.ToLower();
        }

        private static string Article(string phrase) {
            return !string.IsNullOrEmpty(phrase) && "aeiou".IndexOf(char.ToLower(phrase[0])) >= 0 ? "an" : "a";
        }

        private void ApplyIconSizing() {
            if (seedIcon != null) {
                seedIcon.style.width = THUMBNAIL_SIZE;
                seedIcon.style.height = THUMBNAIL_SIZE;
                seedIcon.style.minWidth = THUMBNAIL_SIZE;
                seedIcon.style.minHeight = THUMBNAIL_SIZE;
                seedIcon.style.maxWidth = THUMBNAIL_SIZE;
                seedIcon.style.maxHeight = THUMBNAIL_SIZE;
                seedIcon.scaleMode = ScaleMode.ScaleToFit;
            }
        }

        private void CreateAttributeDisplay(string label, float value) {
            var labelElement = new Label($"{label}: \u00D7{value:F1}");
            labelElement.style.fontSize = 13;
            attributeContainer.Add(labelElement);
        }
    }
}
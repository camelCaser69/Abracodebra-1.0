// ============================================================
// FILE: Assets/Scripts/A_ToolkitUI/UISpecSheetController.cs
// ============================================================
// Task 8.2: Added leaf vitality info display (thorn, regrowth, leaf balance)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.UI.Tooltips;

namespace Abracodabra.UI.Toolkit {
    public class UISpecSheetController {
        private Image seedIcon;
        private Label seedNameText, qualityText, descriptionText;
        private Label maturityTimeText, energyBalanceText, yieldText, cycleTimeText;
        private VisualElement attributeContainer, sequenceContainer, synergiesContainer, warningsContainer;

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
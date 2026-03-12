// FILE: Assets/Scripts/A_ToolkitUI/UISpecSheetController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.UI.Tooltips;

namespace Abracodabra.UI.Toolkit {
    public class UISpecSheetController {
        Image seedIcon;
        Label seedNameText, qualityText, descriptionText;
        Label maturityTimeText, energyBalanceText, yieldText, cycleTimeText;
        VisualElement attributeContainer, sequenceContainer, synergiesContainer, warningsContainer;

        const int THUMBNAIL_SIZE = 64;

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

        void DisplaySeed(UIInventoryItem item, SeedTemplate seedTemplate) {
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

            cycleTimeText.text = $"Cycle Time: {data.totalCycleTime} ticks";
            sequenceContainer.Clear();
            foreach (var slot in data.sequenceSlots) {
                var label = new Label($"A{slot.position}: {slot.actionName} ({slot.modifiedCost:F0}E)");
                sequenceContainer.Add(label);
            }

            synergiesContainer.Clear();
            warningsContainer.Clear();
            foreach (var synergy in data.synergies) {
                var label = new Label($"✓ {synergy}");
                label.style.color = new StyleColor(new Color(0.5f, 1f, 0.5f));
                synergiesContainer.Add(label);
            }
            foreach (var warning in data.warnings) {
                var label = new Label($"⚠ {warning}");
                label.style.color = new StyleColor(new Color(1f, 0.8f, 0.5f));
                warningsContainer.Add(label);
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

        void DisplayTool(ToolDefinition tool) {
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

        void ApplyIconSizing() {
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

        void CreateAttributeDisplay(string label, float value) {
            var labelElement = new Label($"{label}: ×{value:F1}");
            labelElement.style.fontSize = 13;
            attributeContainer.Add(labelElement);
        }
    }
}
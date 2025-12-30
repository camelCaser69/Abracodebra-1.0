using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Manages the seed editor panel (gene slot display and editing)
    /// </summary>
    public class UISeedEditorController
    {
        // Events
        public event Action<GeneBase, VisualElement> OnGeneSlotPointerDown;
        public event Action<GeneBase> OnGeneSlotHoverEnter;
        public event Action OnGeneSlotHoverExit;
        public event Action<Color> OnSeedColorChanged; // NEW: Color picker event
        
        // References
        private VisualElement seedDropSlotContainer;
        private VisualElement passiveGenesContainer;
        private VisualElement activeSequenceContainer;
        private VisualTreeAsset geneSlotTemplate;
        
        // Color picker
        private VisualElement colorPickerContainer;
        private UIInventoryItem currentSeedItem; // Track current seed for color updates

        /// <summary>
        /// Initialize the seed editor controller
        /// </summary>
        public void Initialize(
            VisualElement seedContainer, 
            VisualElement passiveContainer, 
            VisualElement activeContainer,
            VisualTreeAsset slotTemplate)
        {
            seedDropSlotContainer = seedContainer;
            passiveGenesContainer = passiveContainer;
            activeSequenceContainer = activeContainer;
            geneSlotTemplate = slotTemplate;
            
            // Create color picker container (will be added when seed is displayed)
            CreateColorPicker();
        }

        /// <summary>
        /// Display a seed in the editor with all its genes
        /// </summary>
        public void DisplaySeed(UIInventoryItem seedItem)
        {
            if (seedItem == null || seedItem.OriginalData is not SeedTemplate template)
            {
                Clear();
                return;
            }

            currentSeedItem = seedItem; // Store reference for color updates
            
            seedDropSlotContainer.Clear();
            passiveGenesContainer.Clear();
            activeSequenceContainer.Clear();

            // Create seed slot (larger, special)
            var seedSlot = geneSlotTemplate.Instantiate();
            BindGeneSlot(seedSlot, seedItem.OriginalData);
            seedSlot.name = "seed-drop-slot";
            
            // Apply current background color if set
            if (seedItem.HasCustomColor())
            {
                var background = seedSlot.Q("background");
                if (background != null)
                {
                    background.style.backgroundColor = seedItem.BackgroundColor;
                }
            }
            
            seedDropSlotContainer.Add(seedSlot);
            
            // Add color picker below seed slot
            if (colorPickerContainer != null)
            {
                seedDropSlotContainer.Add(colorPickerContainer);
                UpdateColorPickerSelection(seedItem.BackgroundColor);
            }

            var runtimeState = seedItem.SeedRuntimeState;

            // Create passive gene slots with labels below
            for (int i = 0; i < template.passiveSlotCount; i++)
            {
                var geneInstance = (i < runtimeState.passiveInstances.Count) ? runtimeState.passiveInstances[i] : null;
                var gene = geneInstance?.GetGene();
                
                string labelText = gene != null ? $"T{gene.tier}" : $"P{i+1}";
                var wrappedSlot = CreateGeneSlotWithLabel(gene, labelText);
                wrappedSlot.userData = "passive";
                passiveGenesContainer.Add(wrappedSlot);
            }
            
            // Create column headers for active sequence
            var headerRow = new VisualElement();
            headerRow.AddToClassList("active-sequence-header");
            
            var activeHeader = new Label("Active");
            activeHeader.style.width = 64;
            activeHeader.style.marginLeft = 2;
            activeHeader.style.marginRight = 2;
            
            var modifierHeader = new Label("Modifier");
            modifierHeader.style.width = 64;
            modifierHeader.style.marginLeft = 2;
            modifierHeader.style.marginRight = 2;
            
            var payloadHeader = new Label("Payload");
            payloadHeader.style.width = 64;
            payloadHeader.style.marginLeft = 2;
            payloadHeader.style.marginRight = 2;
            
            headerRow.Add(activeHeader);
            headerRow.Add(modifierHeader);
            headerRow.Add(payloadHeader);
            activeSequenceContainer.Add(headerRow);
            
            // Create active sequence slots with labels below
            for (int i = 0; i < template.activeSequenceLength; i++)
            {
                var sequenceRow = new VisualElement();
                sequenceRow.AddToClassList("sequence-row");
                activeSequenceContainer.Add(sequenceRow);

                var sequenceData = (i < runtimeState.activeSequence.Count) ? runtimeState.activeSequence[i] : null;

                // Active gene
                var activeGene = sequenceData?.activeInstance?.GetGene();
                string activeLabel = activeGene != null ? $"T{activeGene.tier}" : $"A{i+1}";
                var activeWrapped = CreateGeneSlotWithLabel(activeGene, activeLabel);
                activeWrapped.userData = "active";
                sequenceRow.Add(activeWrapped);
                
                // Modifier gene
                var modifierGene = sequenceData?.modifierInstances.FirstOrDefault()?.GetGene();
                string modifierLabel = modifierGene != null ? $"T{modifierGene.tier}" : $"M{i+1}";
                var modifierWrapped = CreateGeneSlotWithLabel(modifierGene, modifierLabel);
                modifierWrapped.userData = "modifier";
                sequenceRow.Add(modifierWrapped);
                
                // Payload gene
                var payloadGene = sequenceData?.payloadInstances.FirstOrDefault()?.GetGene();
                string payloadLabel = payloadGene != null ? $"T{payloadGene.tier}" : $"P{i+1}";
                var payloadWrapped = CreateGeneSlotWithLabel(payloadGene, payloadLabel);
                payloadWrapped.userData = "payload";
                sequenceRow.Add(payloadWrapped);
            }
        }

        /// <summary>
        /// Clear the editor to empty state
        /// </summary>
        public void Clear()
        {
            currentSeedItem = null;
            
            seedDropSlotContainer.Clear();
            passiveGenesContainer.Clear();
            activeSequenceContainer.Clear();

            var emptySeedSlot = geneSlotTemplate.Instantiate();
            var label = emptySeedSlot.Q<Label>("tier-label");
            if(label != null)
            {
                label.text = "SEED";
                label.style.display = DisplayStyle.Flex;
            }
            emptySeedSlot.Q("icon").style.display = DisplayStyle.None;
            emptySeedSlot.AddToClassList("gene-slot--seed");
            emptySeedSlot.name = "seed-drop-slot";
            seedDropSlotContainer.Add(emptySeedSlot);
        }

        /// <summary>
        /// Update a specific gene slot's visual
        /// </summary>
        public void UpdateGeneSlot(VisualElement slotOrWrapper, GeneBase gene)
        {
            var slot = slotOrWrapper.ClassListContains("gene-slot") 
                ? slotOrWrapper 
                : slotOrWrapper.Q(className: "gene-slot");
                
            if (slot != null)
            {
                BindGeneSlot(slot, gene);
                
                if (slotOrWrapper != slot)
                {
                    var label = slotOrWrapper.Q<Label>();
                    if (label != null && gene != null)
                    {
                        label.text = $"T{gene.tier}";
                    }
                }
            }
        }

        /// <summary>
        /// Create the color picker UI with preset pastel colors
        /// </summary>
        private void CreateColorPicker()
        {
            colorPickerContainer = new VisualElement();
            colorPickerContainer.AddToClassList("color-picker-container");
            colorPickerContainer.style.flexDirection = FlexDirection.Row;
            colorPickerContainer.style.flexWrap = Wrap.Wrap;
            colorPickerContainer.style.marginTop = 10;
            colorPickerContainer.style.marginBottom = 10;
            colorPickerContainer.style.justifyContent = Justify.Center;
            colorPickerContainer.style.maxWidth = 200;
            
            // Define pastel colors for seed identification
            var colors = new[]
            {
                new Color(0, 0, 0, 0),           // Transparent (default/none)
                new Color(1.0f, 0.8f, 0.8f, 0.6f),   // Pastel Red
                new Color(1.0f, 0.9f, 0.7f, 0.6f),   // Pastel Orange
                new Color(1.0f, 1.0f, 0.8f, 0.6f),   // Pastel Yellow
                new Color(0.8f, 1.0f, 0.8f, 0.6f),   // Pastel Green
                new Color(0.8f, 0.9f, 1.0f, 0.6f),   // Pastel Blue
                new Color(0.9f, 0.8f, 1.0f, 0.6f),   // Pastel Purple
                new Color(1.0f, 0.8f, 0.9f, 0.6f),   // Pastel Pink
            };
            
            foreach (var color in colors)
            {
                var colorButton = new Button();
                colorButton.AddToClassList("color-picker-button");
                colorButton.style.width = 24;
                colorButton.style.height = 24;
                colorButton.style.marginTop = 2;
                colorButton.style.marginBottom = 2;
                colorButton.style.marginLeft = 2;
                colorButton.style.marginRight = 2;
                colorButton.style.borderTopLeftRadius = 4;
                colorButton.style.borderTopRightRadius = 4;
                colorButton.style.borderBottomLeftRadius = 4;
                colorButton.style.borderBottomRightRadius = 4;
                colorButton.style.borderLeftWidth = 2;
                colorButton.style.borderRightWidth = 2;
                colorButton.style.borderTopWidth = 2;
                colorButton.style.borderBottomWidth = 2;
                colorButton.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                colorButton.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                colorButton.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                colorButton.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                
                // Special styling for transparent option
                if (color.a < 0.01f)
                {
                    // Checkerboard pattern simulation (diagonal lines)
                    colorButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    colorButton.text = "✕"; // X mark for "none"
                    colorButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                    colorButton.style.fontSize = 16;
                }
                else
                {
                    colorButton.style.backgroundColor = color;
                }
                
                // Store color in userData for retrieval
                colorButton.userData = color;
                
                // Click handler
                colorButton.clicked += () =>
                {
                    var selectedColor = (Color)colorButton.userData;
                    if (currentSeedItem != null)
                    {
                        currentSeedItem.BackgroundColor = selectedColor;
                        
                        // Update the seed slot background immediately
                        var seedSlot = seedDropSlotContainer.Q("seed-drop-slot");
                        if (seedSlot != null)
                        {
                            var background = seedSlot.Q("background");
                            if (background != null)
                            {
                                background.style.backgroundColor = selectedColor;
                            }
                        }
                        
                        // Update selection highlighting
                        UpdateColorPickerSelection(selectedColor);
                        
                        // Notify manager to update inventory/hotbar
                        OnSeedColorChanged?.Invoke(selectedColor);
                    }
                };
                
                colorPickerContainer.Add(colorButton);
            }
            
            // Add label
            var label = new Label("Seed Color");
            label.style.fontSize = 10;
            label.style.color = new Color(0.6f, 0.6f, 0.6f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.width = Length.Percent(100);
            label.style.marginBottom = 3;
            colorPickerContainer.Insert(0, label);
        }
        
        /// <summary>
        /// Update which color button is shown as selected
        /// </summary>
        private void UpdateColorPickerSelection(Color currentColor)
        {
            if (colorPickerContainer == null) return;
            
            foreach (var child in colorPickerContainer.Children())
            {
                if (child is Button button && button.userData is Color buttonColor)
                {
                    // Check if this button's color matches current color
                    bool isSelected = Mathf.Approximately(buttonColor.r, currentColor.r) &&
                                    Mathf.Approximately(buttonColor.g, currentColor.g) &&
                                    Mathf.Approximately(buttonColor.b, currentColor.b) &&
                                    Mathf.Approximately(buttonColor.a, currentColor.a);
                    
                    if (isSelected)
                    {
                        button.style.borderLeftColor = new Color(1f, 1f, 0.3f); // Yellow border
                        button.style.borderRightColor = new Color(1f, 1f, 0.3f);
                        button.style.borderTopColor = new Color(1f, 1f, 0.3f);
                        button.style.borderBottomColor = new Color(1f, 1f, 0.3f);
                        button.style.borderLeftWidth = 3;
                        button.style.borderRightWidth = 3;
                        button.style.borderTopWidth = 3;
                        button.style.borderBottomWidth = 3;
                    }
                    else
                    {
                        button.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                        button.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                        button.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                        button.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                        button.style.borderLeftWidth = 2;
                        button.style.borderRightWidth = 2;
                        button.style.borderTopWidth = 2;
                        button.style.borderBottomWidth = 2;
                    }
                }
            }
        }

        /// <summary>
        /// Bind data to a gene slot visual element
        /// </summary>
        private void BindGeneSlot(VisualElement slot, object data)
        {
            var background = slot.Q("background");
            var icon = slot.Q<Image>("icon");
            var tierLabel = slot.Q<Label>("tier-label");

            background.ClearClassList();
            background.AddToClassList("gene-slot__background");

            if (tierLabel != null)
            {
                tierLabel.style.display = DisplayStyle.None;
            }

            if (data == null)
            {
                icon.style.display = DisplayStyle.None;
                return;
            }

            icon.style.display = DisplayStyle.Flex;
            icon.style.width = Length.Percent(100);
            icon.style.height = Length.Percent(100);
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.left = 0;
            icon.scaleMode = ScaleMode.ScaleToFit;

            if (data is GeneBase gene)
            {
                icon.sprite = gene.icon;
                background.AddToClassList($"gene-slot--{gene.Category.ToString().ToLower()}");
                
                slot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnGeneSlotPointerDown?.Invoke(gene, slot);
                });
                
                slot.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    OnGeneSlotHoverEnter?.Invoke(gene);
                });
                
                slot.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    OnGeneSlotHoverExit?.Invoke();
                });
            }
            else if (data is SeedTemplate seed)
            {
                icon.sprite = seed.icon;
                background.AddToClassList("gene-slot--seed");
            }
        }
        
        /// <summary>
        /// Create a wrapped gene slot with label below
        /// </summary>
        private VisualElement CreateGeneSlotWithLabel(object data, string labelText)
        {
            var wrapper = new VisualElement();
            wrapper.style.flexDirection = FlexDirection.Column;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.marginLeft = 2;
            wrapper.style.marginRight = 2;
            wrapper.style.marginBottom = 5;
            wrapper.style.width = 68;
            wrapper.style.maxWidth = 68;
            wrapper.style.minWidth = 68;
            wrapper.style.flexShrink = 0;
            wrapper.style.flexGrow = 0;
            
            var slot = geneSlotTemplate.Instantiate();
            BindGeneSlot(slot, data);
            wrapper.Add(slot);
            
            var label = new Label(labelText);
            label.style.fontSize = 9;
            label.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 2;
            label.style.maxWidth = 68;
            label.style.overflow = Overflow.Hidden;
            wrapper.Add(label);
            
            return wrapper;
        }

        // Getters
        public VisualElement GetSeedContainer() => seedDropSlotContainer;
        public VisualElement GetPassiveContainer() => passiveGenesContainer;
        public VisualElement GetActiveContainer() => activeSequenceContainer;
        
        /// <summary>
        /// Highlight only compatible slots when dragging a gene
        /// </summary>
        public void HighlightCompatibleSlots(GeneCategory? draggedCategory)
        {
            if (draggedCategory == null) return;
            
            foreach (var wrapper in passiveGenesContainer.Children())
            {
                var slot = wrapper.Q(className: "gene-slot");
                if (slot != null)
                {
                    if (draggedCategory == GeneCategory.Passive)
                    {
                        slot.RemoveFromClassList("gene-slot--incompatible");
                    }
                    else
                    {
                        slot.AddToClassList("gene-slot--incompatible");
                    }
                }
            }
            
            foreach (var row in activeSequenceContainer.Children())
            {
                if (row.ClassListContains("active-sequence-header")) continue;
                
                foreach (var wrapper in row.Children())
                {
                    var slot = wrapper.Q(className: "gene-slot");
                    if (slot != null)
                    {
                        var slotType = wrapper.userData as string;
                        bool compatible = slotType switch
                        {
                            "active" => draggedCategory == GeneCategory.Active,
                            "modifier" => draggedCategory == GeneCategory.Modifier,
                            "payload" => draggedCategory == GeneCategory.Payload,
                            _ => false
                        };
                        
                        if (compatible)
                        {
                            slot.RemoveFromClassList("gene-slot--incompatible");
                        }
                        else
                        {
                            slot.AddToClassList("gene-slot--incompatible");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Clear all highlighting when drag ends
        /// </summary>
        public void ClearSlotHighlighting()
        {
            foreach (var wrapper in passiveGenesContainer.Children())
            {
                var slot = wrapper.Q(className: "gene-slot");
                slot?.RemoveFromClassList("gene-slot--incompatible");
            }
            
            foreach (var row in activeSequenceContainer.Children())
            {
                if (row.ClassListContains("active-sequence-header")) continue;
                
                foreach (var wrapper in row.Children())
                {
                    var slot = wrapper.Q(className: "gene-slot");
                    slot?.RemoveFromClassList("gene-slot--incompatible");
                }
            }
        }
    }
}

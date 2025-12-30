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
        public event Action<GeneBase> OnGeneSlotHoverEnter; // FIX #4: Hover support
        public event Action OnGeneSlotHoverExit; // FIX #4: Clear on exit
        
        // References
        private VisualElement seedDropSlotContainer;
        private VisualElement passiveGenesContainer;
        private VisualElement activeSequenceContainer;
        private VisualTreeAsset geneSlotTemplate;

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

            seedDropSlotContainer.Clear();
            passiveGenesContainer.Clear();
            activeSequenceContainer.Clear();

            // Create seed slot (larger, special)
            var seedSlot = geneSlotTemplate.Instantiate();
            BindGeneSlot(seedSlot, seedItem.OriginalData);
            seedSlot.name = "seed-drop-slot";
            seedDropSlotContainer.Add(seedSlot);

            var runtimeState = seedItem.SeedRuntimeState;

            // FIX #2: Create passive gene slots with labels below
            for (int i = 0; i < template.passiveSlotCount; i++)
            {
                var geneInstance = (i < runtimeState.passiveInstances.Count) ? runtimeState.passiveInstances[i] : null;
                var gene = geneInstance?.GetGene();
                
                string labelText = gene != null ? $"T{gene.tier}" : $"P{i+1}";
                var wrappedSlot = CreateGeneSlotWithLabel(gene, labelText);
                wrappedSlot.userData = "passive"; // FIX #2: Mark slot type for drag feedback
                passiveGenesContainer.Add(wrappedSlot);
            }
            
            // FIX #3: Create column headers for active sequence
            var headerRow = new VisualElement();
            headerRow.AddToClassList("active-sequence-header");
            
            // FIX #1: Each header needs to match wrapper structure exactly
            // Wrapper is: 2px margin + 64px slot + 2px margin = 68px total
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
            
            // FIX #2: Create active sequence slots with labels below
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
                activeWrapped.userData = "active"; // FIX #2: Mark slot type
                sequenceRow.Add(activeWrapped);
                
                // Modifier gene
                var modifierGene = sequenceData?.modifierInstances.FirstOrDefault()?.GetGene();
                string modifierLabel = modifierGene != null ? $"T{modifierGene.tier}" : $"M{i+1}";
                var modifierWrapped = CreateGeneSlotWithLabel(modifierGene, modifierLabel);
                modifierWrapped.userData = "modifier"; // FIX #2: Mark slot type
                sequenceRow.Add(modifierWrapped);
                
                // Payload gene
                var payloadGene = sequenceData?.payloadInstances.FirstOrDefault()?.GetGene();
                string payloadLabel = payloadGene != null ? $"T{payloadGene.tier}" : $"P{i+1}";
                var payloadWrapped = CreateGeneSlotWithLabel(payloadGene, payloadLabel);
                payloadWrapped.userData = "payload"; // FIX #2: Mark slot type
                sequenceRow.Add(payloadWrapped);
            }
        }

        /// <summary>
        /// Clear the editor to empty state
        /// </summary>
        public void Clear()
        {
            seedDropSlotContainer.Clear();
            passiveGenesContainer.Clear();
            activeSequenceContainer.Clear();

            var emptySeedSlot = geneSlotTemplate.Instantiate();
            var label = emptySeedSlot.Q<Label>("tier-label");
            if(label != null)
            {
                label.text = "SEED";
                label.style.display = DisplayStyle.Flex; // Show for empty seed slot
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
            // The slot might be wrapped, so find the actual gene-slot element
            var slot = slotOrWrapper.ClassListContains("gene-slot") 
                ? slotOrWrapper 
                : slotOrWrapper.Q(className: "gene-slot");
                
            if (slot != null)
            {
                BindGeneSlot(slot, gene);
                
                // Also update the label if this is a wrapped slot
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
        /// Bind data to a gene slot visual element - creates wrapper with label below
        /// </summary>
        private void BindGeneSlot(VisualElement slot, object data)
        {
            var background = slot.Q("background");
            var icon = slot.Q<Image>("icon");
            var tierLabel = slot.Q<Label>("tier-label");

            background.ClearClassList();
            background.AddToClassList("gene-slot__background");

            // FIX #2: Hide the tier label inside the slot (we'll add it below instead)
            if (tierLabel != null)
            {
                tierLabel.style.display = DisplayStyle.None;
            }

            if (data == null)
            {
                icon.style.display = DisplayStyle.None;
                return;
            }

            // FIX #1: Ensure icon fills entire slot properly
            icon.style.display = DisplayStyle.Flex;
            icon.style.width = Length.Percent(100);
            icon.style.height = Length.Percent(100);
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.left = 0;
            icon.scaleMode = ScaleMode.ScaleToFit; // Ensure proper scaling

            if (data is GeneBase gene)
            {
                icon.sprite = gene.icon;
                background.AddToClassList($"gene-slot--{gene.Category.ToString().ToLower()}");
                
                // FIX #4: Register pointer down for dragging genes back to inventory
                slot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnGeneSlotPointerDown?.Invoke(gene, slot);
                });
                
                // FIX #4: Register hover events for tooltip
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
            // Create wrapper container
            var wrapper = new VisualElement();
            wrapper.style.flexDirection = FlexDirection.Column;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.marginLeft = 2;
            wrapper.style.marginRight = 2;
            wrapper.style.marginBottom = 5;
            // FIX: Constrain wrapper width to prevent expansion
            wrapper.style.width = 68; // 64px slot + 2px margin on each side
            wrapper.style.maxWidth = 68;
            wrapper.style.minWidth = 68;
            wrapper.style.flexShrink = 0; // Don't shrink
            wrapper.style.flexGrow = 0; // Don't grow
            
            // Create the gene slot
            var slot = geneSlotTemplate.Instantiate();
            BindGeneSlot(slot, data);
            wrapper.Add(slot);
            
            // FIX #2: Add label below the slot
            var label = new Label(labelText);
            label.style.fontSize = 9;
            label.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 2;
            label.style.maxWidth = 68; // FIX: Constrain label width too
            label.style.overflow = Overflow.Hidden; // FIX: Clip long text
            wrapper.Add(label);
            
            return wrapper;
        }

        // Getters for drag-drop system
        public VisualElement GetSeedContainer() => seedDropSlotContainer;
        public VisualElement GetPassiveContainer() => passiveGenesContainer;
        public VisualElement GetActiveContainer() => activeSequenceContainer;
        
        /// <summary>
        /// FIX #2: Highlight only compatible slots when dragging a gene
        /// </summary>
        public void HighlightCompatibleSlots(GeneCategory? draggedCategory)
        {
            if (draggedCategory == null) return; // Not dragging a gene
            
            // Gray out incompatible passive slots
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
            
            // Gray out incompatible active sequence slots
            foreach (var row in activeSequenceContainer.Children())
            {
                // Skip header row
                if (row.ClassListContains("active-sequence-header")) continue;
                
                int slotIndex = 0;
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
                    slotIndex++;
                }
            }
        }
        
        /// <summary>
        /// FIX #2: Clear all highlighting when drag ends
        /// </summary>
        public void ClearSlotHighlighting()
        {
            // Clear passive slots
            foreach (var wrapper in passiveGenesContainer.Children())
            {
                var slot = wrapper.Q(className: "gene-slot");
                slot?.RemoveFromClassList("gene-slot--incompatible");
            }
            
            // Clear active sequence slots
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

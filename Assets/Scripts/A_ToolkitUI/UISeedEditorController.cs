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

            // Create seed slot
            var seedSlot = geneSlotTemplate.Instantiate();
            BindGeneSlot(seedSlot, seedItem.OriginalData);
            seedSlot.name = "seed-drop-slot";
            seedDropSlotContainer.Add(seedSlot);

            var runtimeState = seedItem.SeedRuntimeState;

            // Create passive gene slots
            for (int i = 0; i < template.passiveSlotCount; i++)
            {
                var passiveSlot = geneSlotTemplate.Instantiate();
                var geneInstance = (i < runtimeState.passiveInstances.Count) ? runtimeState.passiveInstances[i] : null;
                BindGeneSlot(passiveSlot, geneInstance?.GetGene());
                passiveGenesContainer.Add(passiveSlot);
            }
            
            // Create active sequence slots
            for (int i = 0; i < template.activeSequenceLength; i++)
            {
                var sequenceRow = new VisualElement();
                sequenceRow.AddToClassList("sequence-row");
                activeSequenceContainer.Add(sequenceRow);

                var sequenceData = (i < runtimeState.activeSequence.Count) ? runtimeState.activeSequence[i] : null;

                var activeSlot = geneSlotTemplate.Instantiate();
                BindGeneSlot(activeSlot, sequenceData?.activeInstance?.GetGene());
                sequenceRow.Add(activeSlot);
                
                var modifierSlot = geneSlotTemplate.Instantiate();
                var payloadSlot = geneSlotTemplate.Instantiate();
                BindGeneSlot(modifierSlot, sequenceData?.modifierInstances.FirstOrDefault()?.GetGene());
                BindGeneSlot(payloadSlot, sequenceData?.payloadInstances.FirstOrDefault()?.GetGene());
                sequenceRow.Add(modifierSlot);
                sequenceRow.Add(payloadSlot);
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
            if(label != null) label.text = "SEED";
            emptySeedSlot.Q("icon").style.display = DisplayStyle.None;
            emptySeedSlot.AddToClassList("gene-slot--seed");
            emptySeedSlot.name = "seed-drop-slot";
            seedDropSlotContainer.Add(emptySeedSlot);
        }

        /// <summary>
        /// Update a specific gene slot's visual
        /// </summary>
        public void UpdateGeneSlot(VisualElement slot, GeneBase gene)
        {
            BindGeneSlot(slot, gene);
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

            if (data == null)
            {
                icon.style.display = DisplayStyle.None;
                tierLabel.text = "";
                return;
            }

            icon.style.display = DisplayStyle.Flex;

            if (data is GeneBase gene)
            {
                icon.sprite = gene.icon;
                tierLabel.text = $"T{gene.tier}";
                background.AddToClassList($"gene-slot--{gene.Category.ToString().ToLower()}");
                
                // FIX #4: Register pointer down for dragging genes back to inventory
                slot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnGeneSlotPointerDown?.Invoke(gene, slot);
                });
            }
            else if (data is SeedTemplate seed)
            {
                icon.sprite = seed.icon;
                tierLabel.text = "SEED";
                background.AddToClassList("gene-slot--seed");
            }
        }

        // Getters for drag-drop system
        public VisualElement GetSeedContainer() => seedDropSlotContainer;
        public VisualElement GetPassiveContainer() => passiveGenesContainer;
        public VisualElement GetActiveContainer() => activeSequenceContainer;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Toolkit
{
    public class UISeedEditorController
    {
        // Updated event signature to include slot metadata
        public event Action<GeneBase, VisualElement, int, string> OnGeneSlotPointerDown;
        public event Action<GeneBase> OnGeneSlotHoverEnter;
        public event Action OnGeneSlotHoverExit;
        public event Action<Color> OnSeedColorChanged;
        public event Action<string> OnSeedNameChanged;
        public event Action<GeneBase, int, string> OnGeneRemovedFromEditor;

        VisualElement seedDropSlotContainer;
        VisualElement passiveGenesContainer;
        VisualElement activeSequenceContainer;
        VisualTreeAsset geneSlotTemplate;

        TextField seedNameField;
        VisualElement nameEditorContainer;

        VisualElement colorPickerContainer;
        UIInventoryItem currentSeedItem;

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

            CreateNameEditor();
            CreateColorPicker();
        }

        public void DisplaySeed(UIInventoryItem seedItem)
        {
            if (seedItem == null || seedItem.OriginalData is not SeedTemplate template)
            {
                Clear();
                return;
            }

            currentSeedItem = seedItem;

            seedDropSlotContainer.Clear();
            passiveGenesContainer.Clear();
            activeSequenceContainer.Clear();

            var seedSlot = geneSlotTemplate.Instantiate();
            BindGeneSlot(seedSlot, seedItem.OriginalData, -1, "seed");
            seedSlot.name = "seed-drop-slot";

            if (seedItem.HasCustomColor())
            {
                var background = seedSlot.Q("background");
                if (background != null)
                {
                    background.style.backgroundColor = seedItem.BackgroundColor;
                }
            }

            seedDropSlotContainer.Add(seedSlot);

            if (nameEditorContainer != null)
            {
                seedDropSlotContainer.Add(nameEditorContainer);
                UpdateNameEditor(seedItem);
            }

            if (colorPickerContainer != null)
            {
                seedDropSlotContainer.Add(colorPickerContainer);
                UpdateColorPickerSelection(seedItem.BackgroundColor);
            }

            var runtimeState = seedItem.SeedRuntimeState;

            for (int i = 0; i < template.passiveSlotCount; i++)
            {
                var geneInstance = (i < runtimeState.passiveInstances.Count) ? runtimeState.passiveInstances[i] : null;
                var gene = geneInstance?.GetGene();

                string labelText = gene != null ? $"T{gene.tier}" : $"P{i + 1}";
                var wrappedSlot = CreateGeneSlotWithLabel(gene, labelText, i, "passive");
                passiveGenesContainer.Add(wrappedSlot);
            }

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

            for (int i = 0; i < template.activeSequenceLength; i++)
            {
                var sequenceRow = new VisualElement();
                sequenceRow.AddToClassList("sequence-row");
                activeSequenceContainer.Add(sequenceRow);

                var sequenceData = (i < runtimeState.activeSequence.Count) ? runtimeState.activeSequence[i] : null;

                var activeGene = sequenceData?.activeInstance?.GetGene();
                string activeLabel = activeGene != null ? $"T{activeGene.tier}" : $"A{i + 1}";
                var activeWrapped = CreateGeneSlotWithLabel(activeGene, activeLabel, i, "active");
                sequenceRow.Add(activeWrapped);

                var modifierGene = sequenceData?.modifierInstances.FirstOrDefault()?.GetGene();
                string modifierLabel = modifierGene != null ? $"T{modifierGene.tier}" : $"M{i + 1}";
                var modifierWrapped = CreateGeneSlotWithLabel(modifierGene, modifierLabel, i, "modifier");
                sequenceRow.Add(modifierWrapped);

                var payloadGene = sequenceData?.payloadInstances.FirstOrDefault()?.GetGene();
                string payloadLabel = payloadGene != null ? $"T{payloadGene.tier}" : $"Y{i + 1}";
                var payloadWrapped = CreateGeneSlotWithLabel(payloadGene, payloadLabel, i, "payload");
                sequenceRow.Add(payloadWrapped);
            }
        }

        public bool AddGeneToSlot(GeneBase gene, int slotIndex, string slotType)
        {
            if (currentSeedItem == null || gene == null) return false;

            var runtimeState = currentSeedItem.SeedRuntimeState;
            var template = currentSeedItem.OriginalData as SeedTemplate;
            if (runtimeState == null || template == null) return false;

            var instance = new RuntimeGeneInstance(gene);
            instance.SetValue("power_multiplier", 1f);

            bool success = false;

            if (slotType == "passive")
            {
                if (slotIndex < template.passiveSlotCount)
                {
                    while (runtimeState.passiveInstances.Count <= slotIndex)
                    {
                        runtimeState.passiveInstances.Add(null);
                    }

                    runtimeState.passiveInstances[slotIndex] = instance;
                    success = true;
                }
            }
            else if (slotType == "active" || slotType == "modifier" || slotType == "payload")
            {
                while (runtimeState.activeSequence.Count <= slotIndex)
                {
                    runtimeState.activeSequence.Add(new RuntimeSequenceSlot());
                }

                var slot = runtimeState.activeSequence[slotIndex];

                if (slotType == "active")
                {
                    slot.activeInstance = instance;
                    success = true;
                }
                else if (slotType == "modifier")
                {
                    slot.modifierInstances.Clear();
                    slot.modifierInstances.Add(instance);
                    success = true;
                }
                else if (slotType == "payload")
                {
                    slot.payloadInstances.Clear();
                    slot.payloadInstances.Add(instance);
                    success = true;
                }
            }

            if (success)
            {
                Debug.Log($"[SeedEditor] Added {gene.geneName} to {slotType} slot {slotIndex}");
                DisplaySeed(currentSeedItem);
            }

            return success;
        }

        public GeneBase RemoveGeneFromSlot(int slotIndex, string slotType)
        {
            if (currentSeedItem == null) return null;

            var runtimeState = currentSeedItem.SeedRuntimeState;
            if (runtimeState == null) return null;

            GeneBase removedGene = null;

            if (slotType == "passive")
            {
                if (slotIndex < runtimeState.passiveInstances.Count)
                {
                    var instance = runtimeState.passiveInstances[slotIndex];
                    removedGene = instance?.GetGene();
                    runtimeState.passiveInstances[slotIndex] = null;
                }
            }
            else if (slotType == "active" || slotType == "modifier" || slotType == "payload")
            {
                if (slotIndex < runtimeState.activeSequence.Count)
                {
                    var slot = runtimeState.activeSequence[slotIndex];

                    if (slotType == "active")
                    {
                        removedGene = slot.activeInstance?.GetGene();
                        slot.activeInstance = null;
                    }
                    else if (slotType == "modifier" && slot.modifierInstances.Count > 0)
                    {
                        removedGene = slot.modifierInstances[0]?.GetGene();
                        slot.modifierInstances.Clear();
                    }
                    else if (slotType == "payload" && slot.payloadInstances.Count > 0)
                    {
                        removedGene = slot.payloadInstances[0]?.GetGene();
                        slot.payloadInstances.Clear();
                    }
                }
            }

            if (removedGene != null)
            {
                Debug.Log($"[SeedEditor] Removed {removedGene.geneName} from {slotType} slot {slotIndex}");
                DisplaySeed(currentSeedItem);
            }

            return removedGene;
        }

        public void Clear()
        {
            currentSeedItem = null;

            seedDropSlotContainer.Clear();
            passiveGenesContainer.Clear();
            activeSequenceContainer.Clear();

            var emptySeedSlot = geneSlotTemplate.Instantiate();
            var label = emptySeedSlot.Q<Label>("tier-label");
            if (label != null)
            {
                label.text = "SEED";
                label.style.display = DisplayStyle.Flex;
            }
            emptySeedSlot.Q("icon").style.display = DisplayStyle.None;

            var background = emptySeedSlot.Q("background");
            if (background != null)
            {
                background.AddToClassList("gene-slot--empty");
            }
            emptySeedSlot.AddToClassList("gene-slot--seed");
            emptySeedSlot.name = "seed-drop-slot";
            seedDropSlotContainer.Add(emptySeedSlot);
        }

        public bool HasSeedLoaded() => currentSeedItem != null;

        void CreateNameEditor()
        {
            nameEditorContainer = new VisualElement();
            nameEditorContainer.style.flexDirection = FlexDirection.Row;
            nameEditorContainer.style.alignItems = Align.Center;
            nameEditorContainer.style.justifyContent = Justify.Center;
            nameEditorContainer.style.marginTop = 10;
            nameEditorContainer.style.marginBottom = 6;
            nameEditorContainer.style.width = Length.Percent(100);

            seedNameField = new TextField();
            seedNameField.AddToClassList("seed-name-field");

            seedNameField.style.width = 280;
            seedNameField.style.height = 36;

            seedNameField.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                seedNameField.schedule.Execute(() => StyleNameFieldInput()).StartingIn(10);
            });

            seedNameField.RegisterCallback<GeometryChangedEvent>(evt => StyleNameFieldInput());

            seedNameField.RegisterValueChangedCallback(evt =>
            {
                if (currentSeedItem != null)
                {
                    currentSeedItem.CustomName = evt.newValue;
                    OnSeedNameChanged?.Invoke(evt.newValue);
                }
            });

            nameEditorContainer.Add(seedNameField);
        }

        void StyleNameFieldInput()
        {
            if (seedNameField == null) return;

            var textInput = seedNameField.Q("unity-text-input");
            if (textInput != null)
            {
                textInput.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);

                textInput.style.color = Color.white;
                textInput.style.fontSize = 18;
                textInput.style.unityFontStyleAndWeight = FontStyle.Bold;
                textInput.style.unityTextAlign = TextAnchor.MiddleCenter;

                textInput.style.borderTopLeftRadius = 6;
                textInput.style.borderTopRightRadius = 6;
                textInput.style.borderBottomLeftRadius = 6;
                textInput.style.borderBottomRightRadius = 6;

                textInput.style.paddingLeft = 12;
                textInput.style.paddingRight = 12;
                textInput.style.paddingTop = 6;
                textInput.style.paddingBottom = 6;

                textInput.style.borderLeftWidth = 2;
                textInput.style.borderRightWidth = 2;
                textInput.style.borderTopWidth = 2;
                textInput.style.borderBottomWidth = 2;
                textInput.style.borderLeftColor = new Color(0.4f, 0.4f, 0.5f, 1f);
                textInput.style.borderRightColor = new Color(0.4f, 0.4f, 0.5f, 1f);
                textInput.style.borderTopColor = new Color(0.4f, 0.4f, 0.5f, 1f);
                textInput.style.borderBottomColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            }

            var textElement = seedNameField.Q<TextElement>();
            if (textElement != null)
            {
                textElement.style.color = Color.white;
            }
        }

        void UpdateNameEditor(UIInventoryItem seedItem)
        {
            if (seedNameField == null || seedItem == null) return;

            string displayName = !string.IsNullOrEmpty(seedItem.CustomName)
                ? seedItem.CustomName
                : (seedItem.OriginalData as SeedTemplate)?.templateName ?? "Unnamed Seed";

            seedNameField.SetValueWithoutNotify(displayName);
        }

        void CreateColorPicker()
        {
            colorPickerContainer = new VisualElement();
            colorPickerContainer.AddToClassList("color-picker-container");
            colorPickerContainer.style.flexDirection = FlexDirection.Row;
            colorPickerContainer.style.flexWrap = Wrap.Wrap;
            colorPickerContainer.style.marginTop = 6;
            colorPickerContainer.style.marginBottom = 10;
            colorPickerContainer.style.justifyContent = Justify.Center;
            colorPickerContainer.style.alignItems = Align.Center;
            colorPickerContainer.style.maxWidth = 400;
            colorPickerContainer.style.width = Length.Percent(100);

            var colors = new[]
            {
                new Color(0, 0, 0, 0),                      // Clear/None
                new Color(1.0f, 0.7f, 0.7f, 0.6f),          // Light Red
                new Color(1.0f, 0.5f, 0.5f, 0.6f),          // Red
                new Color(1.0f, 0.85f, 0.7f, 0.6f),         // Peach
                new Color(1.0f, 0.75f, 0.5f, 0.6f),         // Orange
                new Color(1.0f, 0.95f, 0.7f, 0.6f),         // Light Yellow
                new Color(1.0f, 1.0f, 0.5f, 0.6f),          // Yellow
                new Color(0.85f, 1.0f, 0.7f, 0.6f),         // Lime
                new Color(0.7f, 1.0f, 0.7f, 0.6f),          // Light Green
                new Color(0.5f, 1.0f, 0.5f, 0.6f),          // Green
                new Color(0.7f, 1.0f, 0.9f, 0.6f),          // Cyan
                new Color(0.7f, 0.85f, 1.0f, 0.6f),         // Light Blue
                new Color(0.6f, 0.7f, 1.0f, 0.6f),          // Blue
                new Color(0.85f, 0.7f, 1.0f, 0.6f),         // Purple
                new Color(1.0f, 0.7f, 0.9f, 0.6f),          // Pink
                new Color(0.8f, 0.8f, 0.8f, 0.5f),          // Gray
            };

            foreach (var color in colors)
            {
                var colorButton = new Button();
                colorButton.AddToClassList("color-picker-button");
                colorButton.style.width = 22;
                colorButton.style.height = 22;
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

                if (color.a < 0.01f)
                {
                    colorButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    colorButton.text = "✕";
                    colorButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                    colorButton.style.fontSize = 14;
                    colorButton.style.color = new Color(0.6f, 0.6f, 0.6f);
                }
                else
                {
                    colorButton.style.backgroundColor = color;
                }

                colorButton.userData = color;

                colorButton.clicked += () =>
                {
                    var selectedColor = (Color)colorButton.userData;
                    if (currentSeedItem != null)
                    {
                        currentSeedItem.BackgroundColor = selectedColor;

                        var seedSlot = seedDropSlotContainer.Q("seed-drop-slot");
                        if (seedSlot != null)
                        {
                            var background = seedSlot.Q("background");
                            if (background != null)
                            {
                                background.style.backgroundColor = selectedColor;
                            }
                        }

                        UpdateColorPickerSelection(selectedColor);
                        OnSeedColorChanged?.Invoke(selectedColor);
                    }
                };

                colorPickerContainer.Add(colorButton);
            }

            var label = new Label("Seed Color");
            label.style.fontSize = 10;
            label.style.color = new Color(0.6f, 0.6f, 0.6f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.width = Length.Percent(100);
            label.style.marginBottom = 3;
            colorPickerContainer.Insert(0, label);
        }

        void UpdateColorPickerSelection(Color currentColor)
        {
            if (colorPickerContainer == null) return;

            foreach (var child in colorPickerContainer.Children())
            {
                if (child is Button button && button.userData is Color buttonColor)
                {
                    bool isSelected = Mathf.Approximately(buttonColor.r, currentColor.r) &&
                                      Mathf.Approximately(buttonColor.g, currentColor.g) &&
                                      Mathf.Approximately(buttonColor.b, currentColor.b) &&
                                      Mathf.Approximately(buttonColor.a, currentColor.a);

                    if (isSelected)
                    {
                        button.style.borderLeftColor = new Color(1f, 1f, 0.3f);
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

        void BindGeneSlot(VisualElement slot, object data, int slotIndex, string slotType)
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
                background.AddToClassList("gene-slot--empty");
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
                    if (evt.button == 0) // Left click - start drag
                    {
                        OnGeneSlotPointerDown?.Invoke(gene, slot, slotIndex, slotType);
                    }
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

        VisualElement CreateGeneSlotWithLabel(object data, string labelText, int slotIndex, string slotType)
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

            wrapper.userData = new SlotMetadata { index = slotIndex, type = slotType };

            var slot = geneSlotTemplate.Instantiate();
            BindGeneSlot(slot, data, slotIndex, slotType);

            if (data is GeneBase gene)
            {
                slot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1) // Right click - remove
                    {
                        var removedGene = RemoveGeneFromSlot(slotIndex, slotType);
                        if (removedGene != null)
                        {
                            OnGeneRemovedFromEditor?.Invoke(removedGene, slotIndex, slotType);
                        }
                        evt.StopPropagation();
                    }
                });
            }

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

        public VisualElement GetSeedContainer() => seedDropSlotContainer;
        public VisualElement GetPassiveContainer() => passiveGenesContainer;
        public VisualElement GetActiveContainer() => activeSequenceContainer;

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
                        var metadata = wrapper.userData as SlotMetadata;
                        if (metadata == null) continue;

                        bool compatible = metadata.type switch
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

        class SlotMetadata
        {
            public int index;
            public string type;
        }
    }
}

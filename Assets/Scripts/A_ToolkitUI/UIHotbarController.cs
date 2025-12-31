using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.UI.Genes;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Manages the hotbar display with MANUAL container (no ListView).
    /// Uses HotbarSelectionService to communicate selection to game systems.
    /// </summary>
    public class UIHotbarController
    {
        // Events
        public event Action<int, UIInventoryItem> OnSlotSelected;

        // References
        private VisualElement hotbarContainer;
        private VisualElement slotsContainer;
        private VisualElement hotbarSelector;
        private VisualTreeAsset slotTemplate;
        private List<UIInventoryItem> hotbarItems;
        private List<VisualElement> slotElements = new List<VisualElement>();

        // State
        private int selectedHotbarIndex = 0;
        private int maxHotbarSlots = 8;

        /// <summary>
        /// Initialize the hotbar controller
        /// </summary>
        public void Initialize(ListView listView, VisualElement selector, VisualTreeAsset template)
        {
            // Get the parent container (the #Hotbar element)
            hotbarContainer = listView.parent;
            hotbarSelector = selector;
            slotTemplate = template;

            // Remove the ListView entirely and create our own container
            if (listView != null)
            {
                listView.RemoveFromHierarchy();
                Debug.Log("[UIHotbarController] Removed ListView, creating manual container");
            }

            // Create manual slots container
            slotsContainer = new VisualElement();
            slotsContainer.name = "hotbar-slots-container";
            slotsContainer.AddToClassList("hotbar-slots-container");
            slotsContainer.style.flexDirection = FlexDirection.Row;
            slotsContainer.style.flexWrap = Wrap.NoWrap;
            slotsContainer.style.overflow = Overflow.Hidden;
            slotsContainer.style.height = 74;

            hotbarContainer.Add(slotsContainer);

            Debug.Log("[UIHotbarController] Initialized with manual container");
        }

        /// <summary>
        /// Setup the hotbar with items (first row of inventory)
        /// </summary>
        public void SetupHotbar(List<UIInventoryItem> items)
        {
            if (slotsContainer == null) return;

            hotbarItems = items;
            maxHotbarSlots = items.Count;

            // Clear existing slots
            slotsContainer.Clear();
            slotElements.Clear();

            // Manually create each slot
            for (int i = 0; i < items.Count; i++)
            {
                int slotIndex = i; // Capture for closure

                var slotElement = slotTemplate.Instantiate();
                
                // CRITICAL FIX: Get the actual slot container from the template
                // The template instantiates a TemplateContainer, we need the slot inside
                var actualSlot = slotElement.Q(className: "slot");
                if (actualSlot == null)
                {
                    // If the template root is the slot itself
                    actualSlot = slotElement;
                    actualSlot.AddToClassList("slot");
                }

                // Bind the item data with PROPER icon sizing
                BindSlot(actualSlot, items[i]);

                // Add click handler for selection
                actualSlot.RegisterCallback<ClickEvent>(evt =>
                {
                    SelectSlot(slotIndex);
                });

                slotElements.Add(actualSlot);
                slotsContainer.Add(slotElement);
            }

            // Calculate total width and set it
            int totalWidth = items.Count * 74; // 64px slot + 10px margin
            slotsContainer.style.width = totalWidth;
            slotsContainer.style.minWidth = totalWidth;
            slotsContainer.style.maxWidth = totalWidth;

            // Set hotbar container width (with padding)
            hotbarContainer.style.width = totalWidth + 10;
            hotbarContainer.style.minWidth = totalWidth + 10;
            hotbarContainer.style.maxWidth = totalWidth + 10;

            Debug.Log($"[UIHotbarController] Created {items.Count} manual slots, total width: {totalWidth}px");

            // Initial selection
            SelectSlot(0);
        }

        /// <summary>
        /// Bind item data to a slot with PROPER icon sizing
        /// </summary>
        private void BindSlot(VisualElement slotElement, UIInventoryItem item)
        {
            var icon = slotElement.Q<Image>("icon");
            var stack = slotElement.Q<Label>("stack-size");

            if (icon != null)
            {
                // CRITICAL FIX: Ensure icon fills the slot properly
                icon.style.width = Length.Percent(100);
                icon.style.height = Length.Percent(100);
                icon.style.position = Position.Absolute;
                icon.style.top = 0;
                icon.style.left = 0;
                icon.scaleMode = ScaleMode.ScaleToFit;

                if (item != null && item.Icon != null)
                {
                    icon.sprite = item.Icon;
                    icon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    icon.sprite = null;
                    icon.style.display = DisplayStyle.None;
                }
            }

            if (stack != null)
            {
                // Position stack size label
                stack.style.position = Position.Absolute;
                stack.style.bottom = 2;
                stack.style.right = 4;

                if (item != null && item.StackSize > 1)
                {
                    stack.text = item.StackSize.ToString();
                    stack.style.display = DisplayStyle.Flex;
                }
                else
                {
                    stack.text = "";
                    stack.style.display = DisplayStyle.None;
                }
            }

            // Apply custom background color if set (for seeds)
            if (item != null && item.HasCustomColor())
            {
                slotElement.style.backgroundColor = item.BackgroundColor;
            }
            else
            {
                slotElement.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.4f));
            }
        }

        /// <summary>
        /// Refresh the hotbar to update visuals (e.g., after color change)
        /// </summary>
        public void RefreshHotbar()
        {
            if (slotElements == null || hotbarItems == null) return;

            for (int i = 0; i < slotElements.Count && i < hotbarItems.Count; i++)
            {
                BindSlot(slotElements[i], hotbarItems[i]);
            }

            // Refresh the selection to update the service with current item
            if (selectedHotbarIndex >= 0 && selectedHotbarIndex < hotbarItems.Count)
            {
                HotbarSelectionService.RefreshCurrentSelection(hotbarItems[selectedHotbarIndex]);
            }
        }

        /// <summary>
        /// Handle hotbar input - number keys 1-8 (top row of keyboard)
        /// </summary>
        public void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SelectSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SelectSlot(4);
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) SelectSlot(5);
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) SelectSlot(6);
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) SelectSlot(7);
        }

        /// <summary>
        /// Select a hotbar slot by index
        /// </summary>
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= maxHotbarSlots) return;
            if (hotbarItems == null || index >= hotbarItems.Count) return;

            selectedHotbarIndex = index;

            // Update visual selector position
            if (slotElements != null && index < slotElements.Count)
            {
                var selectedSlot = slotElements[index];
                if (selectedSlot != null && hotbarSelector != null)
                {
                    // Schedule the position update for after layout
                    selectedSlot.schedule.Execute(() =>
                    {
                        if (hotbarSelector != null && selectedSlot != null)
                        {
                            hotbarSelector.style.left = selectedSlot.layout.xMin;
                            hotbarSelector.style.display = DisplayStyle.Flex;
                        }
                    }).StartingIn(1);
                }
            }

            // Get the selected item
            var selectedItem = hotbarItems[index];

            // CRITICAL: Update the HotbarSelectionService (replaces old InventoryBarController integration)
            HotbarSelectionService.SelectItem(index, selectedItem);

            // Fire local event
            OnSlotSelected?.Invoke(index, selectedItem);

            // Also try to notify old InventoryBarController if it exists (backwards compatibility)
            if (InventoryBarController.Instance != null)
            {
                InventoryBarController.Instance.SelectSlotByIndex(index);
            }
        }

        /// <summary>
        /// Get the currently selected hotbar index
        /// </summary>
        public int GetSelectedIndex() => selectedHotbarIndex;

        /// <summary>
        /// Get the currently selected item
        /// </summary>
        public UIInventoryItem GetSelectedItem()
        {
            if (hotbarItems == null || selectedHotbarIndex < 0 || selectedHotbarIndex >= hotbarItems.Count)
                return null;
            return hotbarItems[selectedHotbarIndex];
        }
    }
}

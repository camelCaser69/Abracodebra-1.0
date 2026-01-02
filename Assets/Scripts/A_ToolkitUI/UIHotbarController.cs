using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.UI.Genes; // For HotbarSelectionService

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Manages the hotbar display with MANUAL container (no ListView) and integrates with HotbarSelectionService.
    /// IMPORTANT: This preserves empty slots - if inventory row 1 has nulls, hotbar shows empty slots.
    /// </summary>
    public class UIHotbarController
    {
        // References
        private VisualElement hotbarContainer; // The parent Hotbar element
        private VisualElement slotsContainer; // Manual container for slots
        private VisualElement hotbarSelector; // Legacy selector element (hidden, we use CSS)
        private VisualTreeAsset slotTemplate;
        private List<UIInventoryItem> hotbarItems;
        private List<VisualElement> slotElements = new List<VisualElement>();

        // State
        private int selectedHotbarIndex = 0;
        private int maxHotbarSlots = 8;

        // Constants for layout calculation
        private const int SLOT_WIDTH = 64;
        private const int SLOT_MARGIN = 5; // margin on each side
        private const int SLOT_TOTAL_WIDTH = SLOT_WIDTH + SLOT_MARGIN * 2; // 74px total per slot

        /// <summary>
        /// Initialize the hotbar controller - NOTE: We use the parent element, not ListView
        /// </summary>
        public void Initialize(ListView listView, VisualElement selector, VisualTreeAsset template)
        {
            // Get the parent container (the #Hotbar element)
            hotbarContainer = listView.parent;
            hotbarSelector = selector;
            slotTemplate = template;

            // HIDE the legacy selector element - we use CSS class instead
            if (hotbarSelector != null)
            {
                hotbarSelector.style.display = DisplayStyle.None;
            }

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
            slotsContainer.style.alignItems = Align.Center;

            hotbarContainer.Add(slotsContainer);

            Debug.Log("[UIHotbarController] Initialized with manual container");
        }

        /// <summary>
        /// Setup the hotbar with items (first row of inventory).
        /// IMPORTANT: This preserves NULL items as empty slots - does NOT filter them out.
        /// </summary>
        public void SetupHotbar(List<UIInventoryItem> items)
        {
            if (slotsContainer == null) return;

            // Store reference to items - INCLUDING nulls
            hotbarItems = items;
            maxHotbarSlots = items.Count;

            // Clear existing slots
            slotsContainer.Clear();
            slotElements.Clear();

            // Manually create each slot - INCLUDING empty slots for null items
            for (int i = 0; i < items.Count; i++)
            {
                int slotIndex = i; // Capture for closure
                var item = items[i]; // May be null!

                var slotElement = slotTemplate.Instantiate();

                // Get the actual slot container from the template
                var slotContainer = slotElement.Q<VisualElement>("slot-container");
                if (slotContainer == null)
                {
                    // If no slot-container, use the first child or the element itself
                    slotContainer = slotElement.childCount > 0 ? slotElement[0] : slotElement;
                }

                slotContainer.AddToClassList("slot");

                // Set explicit size and margins
                slotContainer.style.width = SLOT_WIDTH;
                slotContainer.style.height = SLOT_WIDTH;
                slotContainer.style.marginLeft = SLOT_MARGIN;
                slotContainer.style.marginRight = SLOT_MARGIN;
                slotContainer.style.flexShrink = 0;
                slotContainer.style.flexGrow = 0;

                // Bind the item data (handles null for empty slots)
                BindSlot(slotContainer, item);

                // Add click handler for selection
                slotContainer.RegisterCallback<ClickEvent>(evt =>
                {
                    SelectSlot(slotIndex);
                });

                slotElements.Add(slotContainer);
                slotsContainer.Add(slotContainer);
            }

            // Calculate total width and set it
            int totalWidth = items.Count * SLOT_TOTAL_WIDTH;
            slotsContainer.style.width = totalWidth;
            slotsContainer.style.minWidth = totalWidth;
            slotsContainer.style.maxWidth = totalWidth;

            // Set hotbar container width (with padding)
            hotbarContainer.style.width = totalWidth + 10; // 5px padding on each side
            hotbarContainer.style.minWidth = totalWidth + 10;
            hotbarContainer.style.maxWidth = totalWidth + 10;

            Debug.Log($"[UIHotbarController] Created {items.Count} slots (including empty), total width: {totalWidth}px");

            // Initial selection
            SelectSlot(0);
        }

        /// <summary>
        /// Bind item data to a slot. Handles NULL items as empty slots.
        /// </summary>
        private void BindSlot(VisualElement slotElement, UIInventoryItem item)
        {
            var icon = slotElement.Q<Image>("icon");
            var stack = slotElement.Q<Label>("stack-size");

            // Apply icon sizing
            if (icon != null)
            {
                icon.style.width = Length.Percent(100);
                icon.style.height = Length.Percent(100);
                icon.style.position = Position.Absolute;
                icon.style.top = 0;
                icon.style.left = 0;
                icon.scaleMode = ScaleMode.ScaleToFit;
            }

            // Remove previous state classes
            slotElement.RemoveFromClassList("slot--empty");

            if (item != null)
            {
                // Has item - show icon
                if (icon != null)
                {
                    icon.sprite = item.Icon;
                    icon.style.display = DisplayStyle.Flex;
                }
                if (stack != null)
                {
                    stack.text = item.StackSize > 1 ? item.StackSize.ToString() : "";
                }

                // Apply custom background color if set (for seeds)
                if (item.HasCustomColor())
                {
                    slotElement.style.backgroundColor = item.BackgroundColor;
                }
                else
                {
                    slotElement.style.backgroundColor = new Color(0, 0, 0, 0.4f);
                }
            }
            else
            {
                // NULL item - show as EMPTY slot
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.style.display = DisplayStyle.None;
                }
                if (stack != null)
                {
                    stack.text = "";
                }

                // Apply empty slot styling
                slotElement.AddToClassList("slot--empty");
                slotElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.5f);
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

            // Re-apply selection visual
            UpdateSelectionVisual();
        }

        /// <summary>
        /// Handle hotbar input - number keys 1-8 (top row of keyboard)
        /// </summary>
        public void HandleInput()
        {
            // Use Alpha keys (top number row) - works on all keyboard layouts
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
        /// Select a hotbar slot by index - INTEGRATES WITH HotbarSelectionService
        /// </summary>
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= maxHotbarSlots) return;

            selectedHotbarIndex = index;

            // Update visual selection (CSS class only - no selector element)
            UpdateSelectionVisual();

            // Get the item at this index (may be null for empty slot)
            UIInventoryItem selectedItem = null;
            if (hotbarItems != null && index < hotbarItems.Count)
            {
                selectedItem = hotbarItems[index];
            }

            // CRITICAL: Notify the HotbarSelectionService (static bridge to game systems)
            HotbarSelectionService.SelectItem(index, selectedItem);
            
            string itemName = selectedItem != null ? selectedItem.GetDisplayName() : "Empty";
            Debug.Log($"[UIHotbarController] Selected slot {index + 1}: {itemName}");
        }

        /// <summary>
        /// Update the visual selection indicator - ONLY uses CSS class, no selector element
        /// </summary>
        private void UpdateSelectionVisual()
        {
            // Remove selection from all slots
            foreach (var slot in slotElements)
            {
                slot.RemoveFromClassList("slot--selected");
            }

            // Add selection to current slot
            if (selectedHotbarIndex >= 0 && selectedHotbarIndex < slotElements.Count)
            {
                slotElements[selectedHotbarIndex].AddToClassList("slot--selected");
            }
        }

        /// <summary>
        /// Get the currently selected hotbar index
        /// </summary>
        public int GetSelectedIndex() => selectedHotbarIndex;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.UI.Genes; // For InventoryBarController

// NOTE: InventoryBarController is in Abracodabra.UI.Genes namespace

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Manages the hotbar display with MANUAL container (no ListView) and integrates with InventoryBarController
    /// </summary>
    public class UIHotbarController
    {
        // References
        private VisualElement hotbarContainer; // The parent Hotbar element
        private VisualElement slotsContainer; // Manual container for slots
        private VisualElement hotbarSelector;
        private VisualTreeAsset slotTemplate;
        private List<UIInventoryItem> hotbarItems;
        private List<VisualElement> slotElements = new List<VisualElement>();
        
        // State
        private int selectedHotbarIndex = 0;
        private int maxHotbarSlots = 8;

        /// <summary>
        /// Initialize the hotbar controller - NOTE: We use the parent element, not ListView
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
                slotElement.AddToClassList("slot");
                
                // Bind the item data
                BindSlot(slotElement, items[i]);
                
                // Add click handler for selection
                slotElement.RegisterCallback<ClickEvent>(evt =>
                {
                    SelectSlot(slotIndex);
                });
                
                slotElements.Add(slotElement);
                slotsContainer.Add(slotElement);
            }
            
            // Calculate total width and set it
            int totalWidth = items.Count * 74; // 64px slot + 10px margin
            slotsContainer.style.width = totalWidth;
            slotsContainer.style.minWidth = totalWidth;
            slotsContainer.style.maxWidth = totalWidth;
            
            // Set hotbar container width (with padding)
            hotbarContainer.style.width = totalWidth + 10; // 5px padding on each side
            hotbarContainer.style.minWidth = totalWidth + 10;
            hotbarContainer.style.maxWidth = totalWidth + 10;
            
            Debug.Log($"[UIHotbarController] Created {items.Count} manual slots, total width: {totalWidth}px");
            
            // Initial selection
            SelectSlot(0);
        }
        
        /// <summary>
        /// Bind item data to a slot
        /// </summary>
        private void BindSlot(VisualElement slotElement, UIInventoryItem item)
        {
            var icon = slotElement.Q<Image>("icon");
            var stack = slotElement.Q<Label>("stack-size");

            if (item != null)
            {
                icon.sprite = item.Icon;
                icon.style.display = DisplayStyle.Flex;
                stack.text = item.StackSize > 1 ? item.StackSize.ToString() : "";
                
                // Apply custom background color if set (for seeds)
                if (item.HasCustomColor())
                {
                    slotElement.style.backgroundColor = item.BackgroundColor;
                }
                else
                {
                    slotElement.style.backgroundColor = new Color(0, 0, 0, 0); // Transparent
                }
            }
            else
            {
                icon.style.display = DisplayStyle.None;
                stack.text = "";
                slotElement.style.backgroundColor = new Color(0, 0, 0, 0); // Transparent
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
        /// Select a hotbar slot by index - INTEGRATES WITH INVENTORYBARCONTROLLER
        /// </summary>
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= maxHotbarSlots) return;

            selectedHotbarIndex = index;
            
            // Update visual selector position
            if (slotElements != null && index < slotElements.Count)
            {
                var selectedSlot = slotElements[index];
                if (selectedSlot != null && hotbarSelector != null)
                {
                    hotbarSelector.style.left = selectedSlot.layout.xMin;
                    hotbarSelector.style.display = DisplayStyle.Flex;
                }
            }
            
            // CRITICAL: Notify the old InventoryBarController system (from Abracodabra.UI.Genes namespace)
            if (InventoryBarController.Instance != null)
            {
                InventoryBarController.Instance.SelectSlotByIndex(index);
                Debug.Log($"[UIHotbarController] Selected slot {index + 1}, notified InventoryBarController");
            }
            else
            {
                Debug.LogWarning($"[UIHotbarController] Selected slot {index + 1} but InventoryBarController.Instance is null!");
            }
        }

        /// <summary>
        /// Get the currently selected hotbar index
        /// </summary>
        public int GetSelectedIndex() => selectedHotbarIndex;
    }
}
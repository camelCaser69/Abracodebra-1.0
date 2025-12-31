using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Manages the inventory grid visual representation and selection state
    /// </summary>
    public class UIInventoryGridController
    {
        // Events
        public event Action<int> OnSlotClicked;
        public event Action<int> OnSlotPointerDown;
        public event Action<int> OnSlotHoverEnter;
        public event Action OnSlotHoverExit;

        // State
        private List<UIInventoryItem> inventory;
        private List<VisualElement> inventorySlots = new List<VisualElement>();
        private int selectedInventoryIndex = -1;
        private int lockedSeedIndex = -1;

        // References
        private VisualElement inventoryGrid;
        private VisualTreeAsset slotTemplate;

        /// <summary>
        /// Initialize the inventory grid controller
        /// </summary>
        public void Initialize(VisualElement gridElement, VisualTreeAsset template, List<UIInventoryItem> inventoryData)
        {
            inventoryGrid = gridElement;
            slotTemplate = template;
            inventory = inventoryData;
        }

        /// <summary>
        /// Populate the grid with inventory slots
        /// </summary>
        public void PopulateGrid()
        {
            inventoryGrid.Clear();
            inventorySlots.Clear();

            for (int i = 0; i < inventory.Count; i++)
            {
                var newSlot = slotTemplate.Instantiate();
                newSlot.userData = i;

                int slotIndex = i;

                // CRITICAL FIX: Get the actual slot element from the template
                // The template instantiates a TemplateContainer, we need to work with the slot inside
                var actualSlot = newSlot.Q(className: "slot");
                if (actualSlot == null)
                {
                    // If the template root IS the slot, use it directly
                    actualSlot = newSlot;
                    actualSlot.AddToClassList("slot");
                }

                // Ensure the slot has proper relative positioning for absolute children
                actualSlot.style.position = Position.Relative;
                actualSlot.style.overflow = Overflow.Hidden;

                // Register events on the actual slot element
                actualSlot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnSlotClicked?.Invoke(slotIndex);
                    OnSlotPointerDown?.Invoke(slotIndex);
                });

                actualSlot.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    OnSlotHoverEnter?.Invoke(slotIndex);
                });

                actualSlot.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    OnSlotHoverExit?.Invoke();
                });

                inventorySlots.Add(actualSlot);
                inventoryGrid.Add(newSlot);
            }

            RefreshVisuals();
        }

        /// <summary>
        /// Refresh all slot visuals to match current state
        /// </summary>
        public void RefreshVisuals()
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                var element = inventorySlots[i];
                var item = inventory[i];

                BindSlot(element, item, i);
            }
        }

        /// <summary>
        /// Bind item data to a slot with PROPER icon sizing
        /// </summary>
        private void BindSlot(VisualElement element, UIInventoryItem item, int index)
        {
            var icon = element.Q<Image>("icon");
            var stack = element.Q<Label>("stack-size");

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
                element.style.backgroundColor = item.BackgroundColor;
            }
            else
            {
                element.style.backgroundColor = StyleKeyword.Null; // Clear to use CSS default
            }

            // Update visual states
            element.RemoveFromClassList("slot--selected");
            element.RemoveFromClassList("slot--locked-for-editing");

            if (index == selectedInventoryIndex)
            {
                element.AddToClassList("slot--selected");
            }
            if (index == lockedSeedIndex)
            {
                element.AddToClassList("slot--locked-for-editing");
            }
        }

        /// <summary>
        /// Select a slot (updates spec sheet display)
        /// </summary>
        public void SetSelectedSlot(int index)
        {
            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventorySlots.Count)
            {
                inventorySlots[selectedInventoryIndex].RemoveFromClassList("slot--selected");
            }

            selectedInventoryIndex = index;

            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventorySlots.Count)
            {
                inventorySlots[selectedInventoryIndex].AddToClassList("slot--selected");
            }
        }

        /// <summary>
        /// Lock a seed slot for editing (updates gene editor)
        /// </summary>
        public void SetLockedSeedSlot(int index)
        {
            if (lockedSeedIndex >= 0 && lockedSeedIndex < inventorySlots.Count)
            {
                inventorySlots[lockedSeedIndex].RemoveFromClassList("slot--locked-for-editing");
            }

            lockedSeedIndex = index;

            if (lockedSeedIndex >= 0 && lockedSeedIndex < inventorySlots.Count)
            {
                inventorySlots[lockedSeedIndex].AddToClassList("slot--locked-for-editing");
            }
        }

        /// <summary>
        /// Update selection indices after a swap operation
        /// </summary>
        public void UpdateIndicesAfterSwap(int fromIndex, int toIndex)
        {
            if (selectedInventoryIndex == fromIndex)
                selectedInventoryIndex = toIndex;
            else if (selectedInventoryIndex == toIndex)
                selectedInventoryIndex = fromIndex;

            if (lockedSeedIndex == fromIndex)
                lockedSeedIndex = toIndex;
            else if (lockedSeedIndex == toIndex)
                lockedSeedIndex = fromIndex;
        }

        // Getters
        public List<VisualElement> GetSlots() => inventorySlots;
        public int GetSelectedIndex() => selectedInventoryIndex;
        public int GetLockedSeedIndex() => lockedSeedIndex;
    }
}

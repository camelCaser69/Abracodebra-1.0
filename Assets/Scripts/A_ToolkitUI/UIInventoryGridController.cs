using System;
using System.Collections.Generic;
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
                
                // Selection handler (don't trigger during drag - handled by GameUIManager)
                int slotIndex = i; // Capture for lambda
                newSlot.RegisterCallback<PointerDownEvent>(evt => 
                {
                    OnSlotClicked?.Invoke(slotIndex);
                });
                
                // Notify about pointer down for drag system
                newSlot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnSlotPointerDown?.Invoke(slotIndex);
                });
                
                inventorySlots.Add(newSlot);
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
                
                var icon = element.Q<Image>("icon");
                var stack = element.Q<Label>("stack-size");

                if (item != null)
                {
                    icon.sprite = item.Icon;
                    icon.style.display = DisplayStyle.Flex;
                    stack.text = item.StackSize > 1 ? item.StackSize.ToString() : "";
                }
                else
                {
                    icon.style.display = DisplayStyle.None;
                    stack.text = "";
                }
                
                // Update visual states
                element.RemoveFromClassList("slot--selected");
                element.RemoveFromClassList("slot--locked-for-editing");
                
                if (i == selectedInventoryIndex)
                {
                    element.AddToClassList("slot--selected");
                }
                if (i == lockedSeedIndex)
                {
                    element.AddToClassList("slot--locked-for-editing");
                }
            }
        }

        /// <summary>
        /// Select a slot (updates spec sheet display)
        /// </summary>
        public void SetSelectedSlot(int index)
        {
            // Clear previous selection highlight
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
            // Clear previous lock highlight
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
            // Update selection indices if they were swapped
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

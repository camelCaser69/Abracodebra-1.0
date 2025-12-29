using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Manages the hotbar display and input handling
    /// </summary>
    public class UIHotbarController
    {
        // References
        private ListView hotbarList;
        private VisualElement hotbarSelector;
        private VisualTreeAsset slotTemplate;
        
        // State
        private int selectedHotbarIndex = 0;

        /// <summary>
        /// Initialize the hotbar controller
        /// </summary>
        public void Initialize(ListView listView, VisualElement selector, VisualTreeAsset template)
        {
            hotbarList = listView;
            hotbarSelector = selector;
            slotTemplate = template;
        }

        /// <summary>
        /// Setup the hotbar with items
        /// </summary>
        public void SetupHotbar(List<UIInventoryItem> items)
        {
            if (hotbarList == null) return;
            
            hotbarList.fixedItemHeight = 74;
            hotbarList.selectionType = SelectionType.None;
            hotbarList.itemsSource = items;
            hotbarList.makeItem = () => slotTemplate.Instantiate();
            hotbarList.bindItem = (element, index) =>
            {
                var icon = element.Q<Image>("icon");
                var stack = element.Q<Label>("stack-size");

                if (items[index] != null)
                {
                    icon.sprite = items[index].Icon;
                    icon.style.display = DisplayStyle.Flex;
                    stack.text = items[index].StackSize > 1 ? items[index].StackSize.ToString() : "";
                }
                else
                {
                    icon.style.display = DisplayStyle.None;
                    stack.text = "";
                }
            };
        }

        /// <summary>
        /// Handle hotbar input (number keys 1-8)
        /// </summary>
        public void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);
        }

        /// <summary>
        /// Select a hotbar slot by index
        /// </summary>
        public void SelectSlot(int index)
        {
            if (index < 0 || hotbarList == null || index >= hotbarList.itemsSource.Count) return;

            selectedHotbarIndex = index;
            
            var selectedSlotElement = hotbarList.Query(className: "slot").AtIndex(selectedHotbarIndex);

            if (selectedSlotElement != null)
            {
                hotbarSelector.style.left = selectedSlotElement.layout.xMin;
            }
        }

        /// <summary>
        /// Get the currently selected hotbar index
        /// </summary>
        public int GetSelectedIndex() => selectedHotbarIndex;
    }
}

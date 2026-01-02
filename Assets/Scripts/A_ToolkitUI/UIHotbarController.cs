using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.UI.Genes; // For HotbarSelectionService

namespace Abracodabra.UI.Toolkit
{
    public class UIHotbarController
    {
        VisualElement hotbarContainer; // The parent Hotbar element
        VisualElement slotsContainer; // Manual container for slots
        VisualElement hotbarSelector; // Legacy selector element (hidden, we use CSS)
        VisualTreeAsset slotTemplate;
        List<UIInventoryItem> hotbarItems;
        List<VisualElement> slotElements = new List<VisualElement>();

        int selectedHotbarIndex = 0;
        int maxHotbarSlots = 8;

        const int SLOT_WIDTH = 64;
        const int SLOT_MARGIN = 5; // margin on each side
        const int SLOT_TOTAL_WIDTH = SLOT_WIDTH + SLOT_MARGIN * 2; // 74px total per slot

        public void Initialize(ListView listView, VisualElement selector, VisualTreeAsset template)
        {
            hotbarContainer = listView.parent;
            hotbarSelector = selector;
            slotTemplate = template;

            if (hotbarSelector != null)
            {
                hotbarSelector.style.display = DisplayStyle.None;
            }

            if (listView != null)
            {
                listView.RemoveFromHierarchy();
                Debug.Log("[UIHotbarController] Removed ListView, creating manual container");
            }

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

        public void SetupHotbar(List<UIInventoryItem> items)
        {
            if (slotsContainer == null) return;

            hotbarItems = items;
            maxHotbarSlots = items.Count;

            slotsContainer.Clear();
            slotElements.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                int slotIndex = i; // Capture for closure
                var item = items[i]; // May be null!

                var slotElement = slotTemplate.Instantiate();

                var slotContainer = slotElement.Q<VisualElement>("slot-container");
                if (slotContainer == null)
                {
                    slotContainer = slotElement.childCount > 0 ? slotElement[0] : slotElement;
                }

                slotContainer.AddToClassList("slot");

                slotContainer.style.width = SLOT_WIDTH;
                slotContainer.style.height = SLOT_WIDTH;
                slotContainer.style.marginLeft = SLOT_MARGIN;
                slotContainer.style.marginRight = SLOT_MARGIN;
                slotContainer.style.flexShrink = 0;
                slotContainer.style.flexGrow = 0;

                BindSlot(slotContainer, item);

                slotContainer.RegisterCallback<ClickEvent>(evt =>
                {
                    SelectSlot(slotIndex);
                });

                slotElements.Add(slotContainer);
                slotsContainer.Add(slotContainer);
            }

            int totalWidth = items.Count * SLOT_TOTAL_WIDTH;
            slotsContainer.style.width = totalWidth;
            slotsContainer.style.minWidth = totalWidth;
            slotsContainer.style.maxWidth = totalWidth;

            hotbarContainer.style.width = totalWidth + 10; // 5px padding on each side
            hotbarContainer.style.minWidth = totalWidth + 10;
            hotbarContainer.style.maxWidth = totalWidth + 10;

            Debug.Log($"[UIHotbarController] Created {items.Count} slots (including empty), total width: {totalWidth}px");

            SelectSlot(0);
        }

        void BindSlot(VisualElement slotElement, UIInventoryItem item)
        {
            var icon = slotElement.Q<Image>("icon");
            var stack = slotElement.Q<Label>("stack-size");

            if (icon != null)
            {
                icon.style.width = Length.Percent(100);
                icon.style.height = Length.Percent(100);
                icon.style.position = Position.Absolute;
                icon.style.top = 0;
                icon.style.left = 0;
                icon.scaleMode = ScaleMode.ScaleToFit;
            }

            slotElement.RemoveFromClassList("slot--empty");

            if (item != null)
            {
                if (icon != null)
                {
                    icon.sprite = item.Icon;
                    icon.style.display = DisplayStyle.Flex;
                }
                
                if (stack != null)
                {
                    // Use the new ShouldShowCounter and GetDisplayCount methods
                    if (item.ShouldShowCounter())
                    {
                        int displayCount = item.GetDisplayCount();
                        stack.text = displayCount.ToString();
                        stack.style.display = DisplayStyle.Flex;
                        
                        // Visual hint for low counts
                        if (displayCount <= 1)
                        {
                            stack.style.color = new Color(1f, 0.6f, 0.6f); // Reddish for low
                        }
                        else if (displayCount <= 3)
                        {
                            stack.style.color = new Color(1f, 0.9f, 0.6f); // Yellowish for medium-low
                        }
                        else
                        {
                            stack.style.color = Color.white; // Normal
                        }
                    }
                    else
                    {
                        stack.text = "";
                        stack.style.display = DisplayStyle.None;
                    }
                }

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
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.style.display = DisplayStyle.None;
                }
                if (stack != null)
                {
                    stack.text = "";
                    stack.style.display = DisplayStyle.None;
                }

                slotElement.AddToClassList("slot--empty");
                slotElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.5f);
            }
        }

        public void RefreshHotbar()
        {
            if (slotElements == null || hotbarItems == null) return;

            for (int i = 0; i < slotElements.Count && i < hotbarItems.Count; i++)
            {
                BindSlot(slotElements[i], hotbarItems[i]);
            }

            UpdateSelectionVisual();
        }

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

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= maxHotbarSlots) return;

            selectedHotbarIndex = index;

            UpdateSelectionVisual();

            UIInventoryItem selectedItem = null;
            if (hotbarItems != null && index < hotbarItems.Count)
            {
                selectedItem = hotbarItems[index];
            }

            HotbarSelectionService.SelectItem(index, selectedItem);

            string itemName = selectedItem != null ? selectedItem.GetDisplayName() : "Empty";
            Debug.Log($"[UIHotbarController] Selected slot {index + 1}: {itemName}");
        }

        void UpdateSelectionVisual()
        {
            foreach (var slot in slotElements)
            {
                slot.RemoveFromClassList("slot--selected");
            }

            if (selectedHotbarIndex >= 0 && selectedHotbarIndex < slotElements.Count)
            {
                slotElements[selectedHotbarIndex].AddToClassList("slot--selected");
            }
        }

        public int GetSelectedIndex() => selectedHotbarIndex;
    }
}

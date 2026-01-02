using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.UI.Genes;

namespace Abracodabra.UI.Toolkit {
    public class UIHotbarController {
        VisualElement hotbarContainer;
        VisualElement slotsContainer;
        VisualElement hotbarSelector;
        VisualTreeAsset slotTemplate;
        List<UIInventoryItem> hotbarItems;
        List<VisualElement> slotElements = new List<VisualElement>();

        int selectedHotbarIndex = 0;
        int maxHotbarSlots = 8;

        const int SLOT_WIDTH = 64;
        const int SLOT_MARGIN = 5;
        const int SLOT_TOTAL_WIDTH = SLOT_WIDTH + SLOT_MARGIN * 2;

        public void Initialize(ListView hotbarList, VisualElement selector, VisualTreeAsset template) {
            if (hotbarList != null) {
                hotbarList.style.display = DisplayStyle.None;
            }

            hotbarSelector = selector;
            if (hotbarSelector != null) {
                hotbarSelector.style.display = DisplayStyle.None;
            }

            slotTemplate = template;

            var hotbarPanel = hotbarList?.parent;
            if (hotbarPanel == null) {
                Debug.LogError("[UIHotbarController] Cannot find hotbar panel!");
                return;
            }

            slotsContainer = new VisualElement();
            slotsContainer.name = "hotbar-slots-container";
            slotsContainer.AddToClassList("hotbar-slots-container");
            slotsContainer.style.flexDirection = FlexDirection.Row;
            slotsContainer.style.justifyContent = Justify.Center;
            slotsContainer.style.alignItems = Align.Center;
            slotsContainer.style.height = SLOT_WIDTH + 10;

            hotbarPanel.Add(slotsContainer);
        }

        public void SetupHotbar(List<UIInventoryItem> items) {
            hotbarItems = items ?? new List<UIInventoryItem>();

            slotsContainer.Clear();
            slotElements.Clear();

            int slotCount = Mathf.Min(hotbarItems.Count, maxHotbarSlots);

            for (int i = 0; i < slotCount; i++) {
                var slotInstance = slotTemplate.Instantiate();
                var slotElement = slotInstance.Q(className: "slot");
                if (slotElement == null) {
                    slotElement = slotInstance;
                    slotElement.AddToClassList("slot");
                }

                slotElement.style.width = SLOT_WIDTH;
                slotElement.style.height = SLOT_WIDTH;
                slotElement.style.marginLeft = SLOT_MARGIN;
                slotElement.style.marginRight = SLOT_MARGIN;
                slotElement.style.position = Position.Relative;
                slotElement.style.overflow = Overflow.Hidden;

                int index = i;
                slotElement.RegisterCallback<PointerDownEvent>(evt => {
                    SelectSlot(index);
                });

                slotElements.Add(slotElement);
                slotsContainer.Add(slotInstance);

                BindSlot(slotElement, hotbarItems[i]);
            }

            UpdateSelectionVisual();
        }

        void BindSlot(VisualElement slotElement, UIInventoryItem item) {
            var icon = slotElement.Q<Image>("icon");
            var stack = slotElement.Q<Label>("stack-size");

            if (icon != null) {
                icon.style.width = Length.Percent(100);
                icon.style.height = Length.Percent(100);
                icon.style.position = Position.Absolute;
                icon.style.top = 0;
                icon.style.left = 0;
                icon.scaleMode = ScaleMode.ScaleToFit;

                if (item != null && item.Icon != null) {
                    icon.sprite = item.Icon;
                    icon.style.display = DisplayStyle.Flex;
                }
                else {
                    icon.sprite = null;
                    icon.style.display = DisplayStyle.None;
                }
            }

            if (stack != null) {
                stack.style.position = Position.Absolute;
                stack.style.bottom = 2;
                stack.style.right = 4;
                stack.style.fontSize = 12;
                stack.style.unityFontStyleAndWeight = FontStyle.Bold;
                stack.style.textShadow = new TextShadow {
                    offset = new Vector2(1, 1),
                    blurRadius = 0,
                    color = Color.black
                };

                if (item != null && item.ShouldShowCounter()) {
                    int count = item.GetDisplayCount();
                    stack.text = count.ToString();
                    stack.style.display = DisplayStyle.Flex;
                    
                    // Color coding for low counts
                    if (count <= 1) {
                        stack.style.color = new Color(1f, 0.6f, 0.6f); // Red
                    }
                    else if (count <= 3) {
                        stack.style.color = new Color(1f, 0.9f, 0.6f); // Yellow
                    }
                    else {
                        stack.style.color = Color.white;
                    }
                }
                else {
                    stack.text = "";
                    stack.style.display = DisplayStyle.None;
                }
            }

            if (item != null && item.HasCustomColor()) {
                slotElement.style.backgroundColor = item.BackgroundColor;
            }
            else {
                slotElement.style.backgroundColor = StyleKeyword.Null;
            }

            slotElement.RemoveFromClassList("slot--selected");
            slotElement.RemoveFromClassList("slot--empty");

            if (item == null || item.Icon == null) {
                slotElement.AddToClassList("slot--empty");
                slotElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.5f);
            }
        }

        public void RefreshHotbar() {
            if (slotElements == null || hotbarItems == null) return;

            for (int i = 0; i < slotElements.Count && i < hotbarItems.Count; i++) {
                BindSlot(slotElements[i], hotbarItems[i]);
            }

            UpdateSelectionVisual();
        }

        public void HandleInput() {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SelectSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SelectSlot(4);
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) SelectSlot(5);
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) SelectSlot(6);
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) SelectSlot(7);
        }

        public void SelectSlot(int index) {
            if (index < 0 || index >= maxHotbarSlots) return;

            selectedHotbarIndex = index;

            UpdateSelectionVisual();

            UIInventoryItem selectedItem = null;
            if (hotbarItems != null && index < hotbarItems.Count) {
                selectedItem = hotbarItems[index];
            }

            HotbarSelectionService.SelectItem(index, selectedItem);

            string itemName = selectedItem != null ? selectedItem.GetDisplayName() : "Empty";
            Debug.Log($"[UIHotbarController] Selected slot {index + 1}: {itemName}");
        }

        void UpdateSelectionVisual() {
            foreach (var slot in slotElements) {
                slot.RemoveFromClassList("slot--selected");
            }

            if (selectedHotbarIndex >= 0 && selectedHotbarIndex < slotElements.Count) {
                slotElements[selectedHotbarIndex].AddToClassList("slot--selected");
            }
        }

        public int GetSelectedIndex() => selectedHotbarIndex;
    }
}

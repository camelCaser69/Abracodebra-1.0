using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abracodabra.UI.Toolkit {
    public class UIInventoryGridController {
        public event Action<int> OnSlotClicked;
        public event Action<int> OnSlotPointerDown;
        public event Action<int> OnSlotHoverEnter;
        public event Action OnSlotHoverExit;

        List<UIInventoryItem> inventory;
        List<VisualElement> inventorySlots = new List<VisualElement>();
        int selectedInventoryIndex = -1;
        int lockedSeedIndex = -1;

        VisualElement inventoryGrid;
        VisualTreeAsset slotTemplate;

        public void Initialize(VisualElement gridElement, VisualTreeAsset template, List<UIInventoryItem> inventoryData) {
            inventoryGrid = gridElement;
            slotTemplate = template;
            inventory = inventoryData;
        }

        public void PopulateGrid() {
            inventoryGrid.Clear();
            inventorySlots.Clear();

            for (int i = 0; i < inventory.Count; i++) {
                var newSlot = slotTemplate.Instantiate();
                newSlot.userData = i;

                int slotIndex = i;

                var actualSlot = newSlot.Q(className: "slot");
                if (actualSlot == null) {
                    actualSlot = newSlot;
                    actualSlot.AddToClassList("slot");
                }

                actualSlot.style.position = Position.Relative;
                actualSlot.style.overflow = Overflow.Hidden;

                actualSlot.RegisterCallback<PointerDownEvent>(evt => {
                    OnSlotClicked?.Invoke(slotIndex);
                    OnSlotPointerDown?.Invoke(slotIndex);
                });

                actualSlot.RegisterCallback<PointerEnterEvent>(evt => {
                    OnSlotHoverEnter?.Invoke(slotIndex);
                });

                actualSlot.RegisterCallback<PointerLeaveEvent>(evt => {
                    OnSlotHoverExit?.Invoke();
                });

                inventorySlots.Add(actualSlot);
                inventoryGrid.Add(newSlot);
            }

            RefreshVisuals();
        }

        public void RefreshVisuals() {
            for (int i = 0; i < inventorySlots.Count; i++) {
                var element = inventorySlots[i];
                var item = inventory[i];

                BindSlot(element, item, i);
            }
        }

        void BindSlot(VisualElement element, UIInventoryItem item, int index) {
            var icon = element.Q<Image>("icon");
            var stack = element.Q<Label>("stack-size");

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
                element.style.backgroundColor = item.BackgroundColor;
            }
            else {
                element.style.backgroundColor = StyleKeyword.Null;
            }

            element.RemoveFromClassList("slot--selected");
            element.RemoveFromClassList("slot--locked-for-editing");

            if (index == selectedInventoryIndex) {
                element.AddToClassList("slot--selected");
            }
            if (index == lockedSeedIndex) {
                element.AddToClassList("slot--locked-for-editing");
            }
        }

        public void SetSelectedSlot(int index) {
            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventorySlots.Count) {
                inventorySlots[selectedInventoryIndex].RemoveFromClassList("slot--selected");
            }

            selectedInventoryIndex = index;

            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventorySlots.Count) {
                inventorySlots[selectedInventoryIndex].AddToClassList("slot--selected");
            }
        }

        public void SetLockedSeedSlot(int index) {
            if (lockedSeedIndex >= 0 && lockedSeedIndex < inventorySlots.Count) {
                inventorySlots[lockedSeedIndex].RemoveFromClassList("slot--locked-for-editing");
            }

            lockedSeedIndex = index;

            if (lockedSeedIndex >= 0 && lockedSeedIndex < inventorySlots.Count) {
                inventorySlots[lockedSeedIndex].AddToClassList("slot--locked-for-editing");
            }
        }

        public void UpdateIndicesAfterSwap(int fromIndex, int toIndex) {
            if (selectedInventoryIndex == fromIndex)
                selectedInventoryIndex = toIndex;
            else if (selectedInventoryIndex == toIndex)
                selectedInventoryIndex = fromIndex;

            if (lockedSeedIndex == fromIndex)
                lockedSeedIndex = toIndex;
            else if (lockedSeedIndex == toIndex)
                lockedSeedIndex = fromIndex;
        }

        public List<VisualElement> GetSlots() => inventorySlots;
        public int GetSelectedIndex() => selectedInventoryIndex;
        public int GetLockedSeedIndex() => lockedSeedIndex;
    }
}

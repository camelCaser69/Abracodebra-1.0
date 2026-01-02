using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Core;

namespace Abracodabra.UI.Toolkit {
    public class UIDragDropController {
        public event Action<int, int> OnInventorySwapRequested;
        public event Action<int, VisualElement, string> OnGeneDropRequested;
        public event Action<GeneCategory?> OnDragStarted;
        public event Action OnDragEnded;
        
        // Event for dropping gene from editor to inventory
        public event Action<GeneBase, int, int, string> OnGeneDroppedToInventory;
        
        // Event for moving genes within the editor
        public event Action<GeneBase, int, string, int, string> OnGeneEditorInternalMove;

        VisualElement rootElement;
        VisualElement dragPreview;
        List<UIInventoryItem> inventory;
        List<VisualElement> inventorySlots;

        VisualElement seedDropSlotContainer;
        VisualElement passiveGenesContainer;
        VisualElement activeSequenceContainer;

        int dragSourceIndex = -1;
        GeneBase draggedGene = null;
        VisualElement draggedGeneSlot = null;
        int draggedGeneSlotIndex = -1;
        string draggedGeneSlotType = null;
        bool isDragging = false;

        public void Initialize(VisualElement root, List<UIInventoryItem> inventoryData) {
            rootElement = root;
            inventory = inventoryData;

            rootElement.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            rootElement.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }

        public void SetInventorySlots(List<VisualElement> slots) {
            inventorySlots = slots;
        }

        public void SetGeneEditorSlots(VisualElement seedContainer, VisualElement passiveContainer, VisualElement activeContainer) {
            seedDropSlotContainer = seedContainer;
            passiveGenesContainer = passiveContainer;
            activeSequenceContainer = activeContainer;
        }

        public void StartDrag(int sourceIndex) {
            if (inventory[sourceIndex] == null) return;

            dragSourceIndex = sourceIndex;
            draggedGene = null;
            draggedGeneSlot = null;
            draggedGeneSlotIndex = -1;
            draggedGeneSlotType = null;
            isDragging = false;
        }

        public void StartDragFromGeneEditor(GeneBase gene, VisualElement sourceSlot, int slotIndex, string slotType) {
            draggedGene = gene;
            draggedGeneSlot = sourceSlot;
            draggedGeneSlotIndex = slotIndex;
            draggedGeneSlotType = slotType;
            dragSourceIndex = -1;
            isDragging = false;
        }

        public bool IsDragging() => isDragging;

        void OnGlobalPointerMove(PointerMoveEvent evt) {
            if (dragSourceIndex == -1 && draggedGene == null) return;

            if (!isDragging) {
                isDragging = true;

                if (draggedGene != null) {
                    CreateDragPreviewFromGene(draggedGene);
                    OnDragStarted?.Invoke(draggedGene.Category);
                }
                else {
                    CreateDragPreview(dragSourceIndex);
                    var item = inventory[dragSourceIndex];
                    if (item?.OriginalData is GeneBase gene) {
                        OnDragStarted?.Invoke(gene.Category);
                    }
                    else {
                        OnDragStarted?.Invoke(null);
                    }
                }
            }

            if (dragPreview != null) {
                dragPreview.style.left = evt.position.x - 32;
                dragPreview.style.top = evt.position.y - 32;
            }
        }

        void OnGlobalPointerUp(PointerUpEvent evt) {
            if (!isDragging || (dragSourceIndex == -1 && draggedGene == null)) {
                dragSourceIndex = -1;
                draggedGene = null;
                draggedGeneSlot = null;
                draggedGeneSlotIndex = -1;
                draggedGeneSlotType = null;
                return;
            }

            bool dropHandled = false;

            // Check if dropping on inventory slot
            int inventoryDropIndex = GetInventorySlotAtPosition(evt.position);
            if (inventoryDropIndex >= 0) {
                if (dragSourceIndex >= 0 && dragSourceIndex != inventoryDropIndex) {
                    // Inventory to inventory swap
                    OnInventorySwapRequested?.Invoke(dragSourceIndex, inventoryDropIndex);
                    dropHandled = true;
                }
                else if (draggedGene != null && draggedGeneSlotType != null) {
                    // Gene editor to inventory drop
                    OnGeneDroppedToInventory?.Invoke(
                        draggedGene,
                        inventoryDropIndex,
                        draggedGeneSlotIndex,
                        draggedGeneSlotType
                    );
                    dropHandled = true;
                }
            }

            // Check if dropping on gene editor slot
            if (!dropHandled) {
                var geneSlotDrop = GetGeneSlotAtPositionWithIndex(evt.position);
                
                if (geneSlotDrop.slot != null) {
                    if (dragSourceIndex >= 0) {
                        // Inventory to gene editor drop
                        OnGeneDropRequested?.Invoke(dragSourceIndex, geneSlotDrop.slot, geneSlotDrop.slotType);
                        dropHandled = true;
                    }
                    else if (draggedGene != null && draggedGeneSlotType != null) {
                        // Gene editor to gene editor move (internal)
                        // Only fire if it's a different slot
                        bool isDifferentSlot = (geneSlotDrop.slotIndex != draggedGeneSlotIndex) || 
                                               (geneSlotDrop.slotType != draggedGeneSlotType);
                        
                        if (isDifferentSlot) {
                            OnGeneEditorInternalMove?.Invoke(
                                draggedGene,
                                draggedGeneSlotIndex,
                                draggedGeneSlotType,
                                geneSlotDrop.slotIndex,
                                geneSlotDrop.slotType
                            );
                        }
                        dropHandled = true;
                    }
                }
            }

            CleanupDrag();
        }

        int GetInventorySlotAtPosition(Vector2 screenPos) {
            if (inventorySlots == null) return -1;

            for (int i = 0; i < inventorySlots.Count; i++) {
                var slot = inventorySlots[i];
                if (slot.worldBound.Contains(screenPos)) {
                    return i;
                }
            }
            return -1;
        }

        (VisualElement slot, string slotType, int slotIndex) GetGeneSlotAtPositionWithIndex(Vector2 screenPos) {
            if (seedDropSlotContainer != null && seedDropSlotContainer.childCount > 0) {
                var seedSlot = seedDropSlotContainer.ElementAt(0);
                if (seedSlot.worldBound.Contains(screenPos)) {
                    return (seedSlot, "seed", 0);
                }
            }

            if (passiveGenesContainer != null) {
                int passiveIndex = 0;
                foreach (var wrapper in passiveGenesContainer.Children()) {
                    if (wrapper.worldBound.Contains(screenPos)) {
                        return (wrapper, "passive", passiveIndex);
                    }
                    passiveIndex++;
                }
            }

            if (activeSequenceContainer != null) {
                int rowIndex = 0;
                foreach (var row in activeSequenceContainer.Children()) {
                    if (row.ClassListContains("active-sequence-header")) continue;
                    
                    int columnIndex = 0;
                    foreach (var wrapper in row.Children()) {
                        if (wrapper.worldBound.Contains(screenPos)) {
                            string slotType = columnIndex switch {
                                0 => "active",
                                1 => "modifier",
                                2 => "payload",
                                _ => "unknown"
                            };
                            return (wrapper, slotType, rowIndex);
                        }
                        columnIndex++;
                    }
                    rowIndex++;
                }
            }

            return (null, null, -1);
        }

        void CleanupDrag() {
            if (dragPreview != null) {
                dragPreview.RemoveFromHierarchy();
                dragPreview = null;
            }

            if (isDragging) {
                OnDragEnded?.Invoke();
            }

            isDragging = false;
            dragSourceIndex = -1;
            draggedGene = null;
            draggedGeneSlot = null;
            draggedGeneSlotIndex = -1;
            draggedGeneSlotType = null;
        }

        void CreateDragPreview(int index) {
            var item = inventory[index];
            if (item == null) return;

            dragPreview = new VisualElement();
            dragPreview.AddToClassList("slot");
            dragPreview.style.position = Position.Absolute;
            dragPreview.style.width = 64;
            dragPreview.style.height = 64;
            dragPreview.style.overflow = Overflow.Hidden;
            dragPreview.pickingMode = PickingMode.Ignore;

            var icon = new Image();
            icon.sprite = item.Icon;
            icon.AddToClassList("slot-icon");

            icon.style.width = Length.Percent(100);
            icon.style.height = Length.Percent(100);
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.left = 0;
            icon.scaleMode = ScaleMode.ScaleToFit;

            dragPreview.Add(icon);

            rootElement.Add(dragPreview);
        }

        void CreateDragPreviewFromGene(GeneBase gene) {
            if (gene == null) return;

            dragPreview = new VisualElement();
            dragPreview.AddToClassList("slot");
            dragPreview.style.position = Position.Absolute;
            dragPreview.style.width = 64;
            dragPreview.style.height = 64;
            dragPreview.style.overflow = Overflow.Hidden;
            dragPreview.pickingMode = PickingMode.Ignore;

            var icon = new Image();
            icon.sprite = gene.icon;
            icon.AddToClassList("slot-icon");

            icon.style.width = Length.Percent(100);
            icon.style.height = Length.Percent(100);
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.left = 0;
            icon.scaleMode = ScaleMode.ScaleToFit;

            dragPreview.Add(icon);

            rootElement.Add(dragPreview);
        }
    }
}

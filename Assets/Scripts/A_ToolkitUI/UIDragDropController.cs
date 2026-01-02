using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

namespace Abracodabra.UI.Toolkit
{
    public class UIDragDropController
    {
        public event Action<int, int> OnInventorySwapRequested;
        public event Action<int, VisualElement, string> OnGeneDropRequested;
        public event Action<GeneCategory?> OnDragStarted;
        public event Action OnDragEnded;
        
        // New event for when a gene is dropped from editor to inventory
        public event Action<GeneBase, int, int, string> OnGeneDroppedToInventory;

        bool isDragging = false;
        int dragSourceIndex = -1;
        GeneBase draggedGene = null;
        VisualElement draggedGeneSlot = null;
        VisualElement dragPreview;
        
        // Store source slot metadata when dragging from gene editor
        int draggedGeneSlotIndex = -1;
        string draggedGeneSlotType = null;

        VisualElement rootElement;
        List<UIInventoryItem> inventory;
        List<VisualElement> inventorySlots;

        VisualElement seedDropSlotContainer;
        VisualElement passiveGenesContainer;
        VisualElement activeSequenceContainer;

        public void Initialize(VisualElement root, List<UIInventoryItem> inventoryData)
        {
            rootElement = root;
            inventory = inventoryData;

            rootElement.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            rootElement.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }

        public void SetInventorySlots(List<VisualElement> slots)
        {
            inventorySlots = slots;
        }

        public void SetGeneEditorSlots(VisualElement seedContainer, VisualElement passiveContainer, VisualElement activeContainer)
        {
            seedDropSlotContainer = seedContainer;
            passiveGenesContainer = passiveContainer;
            activeSequenceContainer = activeContainer;
        }

        public void StartDrag(int sourceIndex)
        {
            if (inventory[sourceIndex] == null) return; // Can't drag empty slots

            dragSourceIndex = sourceIndex;
            draggedGene = null; // Not dragging from gene editor
            draggedGeneSlot = null;
            draggedGeneSlotIndex = -1;
            draggedGeneSlotType = null;
            isDragging = false; // Not dragging yet, just pressed
        }

        public void StartDragFromGeneEditor(GeneBase gene, VisualElement sourceSlot, int slotIndex, string slotType)
        {
            draggedGene = gene;
            draggedGeneSlot = sourceSlot;
            draggedGeneSlotIndex = slotIndex;
            draggedGeneSlotType = slotType;
            dragSourceIndex = -1; // Not dragging from inventory
            isDragging = false; // Not dragging yet, just pressed
        }

        public bool IsDragging() => isDragging;

        void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (dragSourceIndex == -1 && draggedGene == null) return;

            if (!isDragging)
            {
                isDragging = true;

                if (draggedGene != null)
                {
                    CreateDragPreviewFromGene(draggedGene);
                    OnDragStarted?.Invoke(draggedGene.Category);
                }
                else
                {
                    CreateDragPreview(dragSourceIndex);
                    var item = inventory[dragSourceIndex];
                    if (item?.OriginalData is GeneBase gene)
                    {
                        OnDragStarted?.Invoke(gene.Category);
                    }
                    else
                    {
                        OnDragStarted?.Invoke(null); // Not a gene
                    }
                }
            }

            if (dragPreview != null)
            {
                dragPreview.style.left = evt.position.x - 32;
                dragPreview.style.top = evt.position.y - 32;
            }
        }

        void OnGlobalPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || (dragSourceIndex == -1 && draggedGene == null))
            {
                dragSourceIndex = -1;
                draggedGene = null;
                draggedGeneSlot = null;
                draggedGeneSlotIndex = -1;
                draggedGeneSlotType = null;
                return;
            }

            bool dropHandled = false;

            int inventoryDropIndex = GetInventorySlotAtPosition(evt.position);
            if (inventoryDropIndex >= 0)
            {
                if (dragSourceIndex >= 0 && dragSourceIndex != inventoryDropIndex)
                {
                    // Dragging from inventory to inventory - swap
                    OnInventorySwapRequested?.Invoke(dragSourceIndex, inventoryDropIndex);
                    dropHandled = true;
                }
                else if (draggedGene != null && draggedGeneSlotType != null)
                {
                    // Dragging from gene editor to inventory
                    // Fire event to handle the drop (remove from editor, add to inventory)
                    OnGeneDroppedToInventory?.Invoke(
                        draggedGene, 
                        inventoryDropIndex, 
                        draggedGeneSlotIndex, 
                        draggedGeneSlotType
                    );
                    dropHandled = true;
                }
            }

            // Check for gene editor slot drops (from inventory to gene editor)
            if (!dropHandled && dragSourceIndex >= 0)
            {
                var geneSlotDrop = GetGeneSlotAtPosition(evt.position);
                if (geneSlotDrop.slot != null)
                {
                    OnGeneDropRequested?.Invoke(dragSourceIndex, geneSlotDrop.slot, geneSlotDrop.slotType);
                    dropHandled = true;
                }
            }
            
            // Check for gene editor to gene editor slot drops
            if (!dropHandled && draggedGene != null && draggedGeneSlotType != null)
            {
                var geneSlotDrop = GetGeneSlotAtPosition(evt.position);
                if (geneSlotDrop.slot != null && geneSlotDrop.slotType == draggedGeneSlotType)
                {
                    // Same type slot - this could be a swap or move within the editor
                    // For now, we'll let it fall through (no action)
                    // Future enhancement: implement gene-to-gene slot swapping
                }
            }

            CleanupDrag();
        }

        int GetInventorySlotAtPosition(Vector2 screenPos)
        {
            if (inventorySlots == null) return -1;

            for (int i = 0; i < inventorySlots.Count; i++)
            {
                var slot = inventorySlots[i];
                if (slot.worldBound.Contains(screenPos))
                {
                    return i;
                }
            }
            return -1;
        }

        (VisualElement slot, string slotType) GetGeneSlotAtPosition(Vector2 screenPos)
        {
            if (seedDropSlotContainer != null && seedDropSlotContainer.childCount > 0)
            {
                var seedSlot = seedDropSlotContainer.ElementAt(0);
                if (seedSlot.worldBound.Contains(screenPos))
                {
                    return (seedSlot, "seed");
                }
            }

            if (passiveGenesContainer != null)
            {
                foreach (var slot in passiveGenesContainer.Children())
                {
                    if (slot.worldBound.Contains(screenPos))
                    {
                        return (slot, "passive");
                    }
                }
            }

            if (activeSequenceContainer != null)
            {
                foreach (var row in activeSequenceContainer.Children())
                {
                    if (row.ClassListContains("active-sequence-header")) continue;
                    
                    int slotIndex = 0;
                    foreach (var slot in row.Children())
                    {
                        if (slot.worldBound.Contains(screenPos))
                        {
                            string slotType = slotIndex switch
                            {
                                0 => "active",
                                1 => "modifier",
                                2 => "payload",
                                _ => "unknown"
                            };
                            return (slot, slotType);
                        }
                        slotIndex++;
                    }
                }
            }

            return (null, null);
        }

        void CleanupDrag()
        {
            if (dragPreview != null)
            {
                dragPreview.RemoveFromHierarchy();
                dragPreview = null;
            }

            if (isDragging)
            {
                OnDragEnded?.Invoke();
            }

            isDragging = false;
            dragSourceIndex = -1;
            draggedGene = null;
            draggedGeneSlot = null;
            draggedGeneSlotIndex = -1;
            draggedGeneSlotType = null;
        }

        void CreateDragPreview(int index)
        {
            var item = inventory[index];
            if (item == null) return;

            dragPreview = new VisualElement();
            dragPreview.AddToClassList("slot");
            dragPreview.style.position = Position.Absolute;
            dragPreview.style.width = 64;
            dragPreview.style.height = 64;
            dragPreview.style.overflow = Overflow.Hidden;
            dragPreview.pickingMode = PickingMode.Ignore; // Don't interfere with hit detection

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

        void CreateDragPreviewFromGene(GeneBase gene)
        {
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

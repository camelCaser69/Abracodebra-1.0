using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

namespace Abracodabra.UI.Toolkit
{
    /// <summary>
    /// Handles drag and drop operations for inventory and gene editor
    /// </summary>
    public class UIDragDropController
    {
        // Events
        public event Action<int, int> OnInventorySwapRequested;
        public event Action<int, VisualElement, string> OnGeneDropRequested;
        public event Action<GeneCategory?> OnDragStarted;
        public event Action OnDragEnded;

        // State
        private bool isDragging = false;
        private int dragSourceIndex = -1;
        private GeneBase draggedGene = null;
        private VisualElement draggedGeneSlot = null;
        private VisualElement dragPreview;

        // References
        private VisualElement rootElement;
        private List<UIInventoryItem> inventory;
        private List<VisualElement> inventorySlots;

        // Gene editor slot references
        private VisualElement seedDropSlotContainer;
        private VisualElement passiveGenesContainer;
        private VisualElement activeSequenceContainer;

        /// <summary>
        /// Initialize the drag-drop controller and register global mouse handlers
        /// </summary>
        public void Initialize(VisualElement root, List<UIInventoryItem> inventoryData)
        {
            rootElement = root;
            inventory = inventoryData;

            // Register GLOBAL mouse handlers for drag & drop
            rootElement.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            rootElement.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }

        /// <summary>
        /// Update references to inventory slots (call after grid population)
        /// </summary>
        public void SetInventorySlots(List<VisualElement> slots)
        {
            inventorySlots = slots;
        }

        /// <summary>
        /// Update references to gene editor containers
        /// </summary>
        public void SetGeneEditorSlots(VisualElement seedContainer, VisualElement passiveContainer, VisualElement activeContainer)
        {
            seedDropSlotContainer = seedContainer;
            passiveGenesContainer = passiveContainer;
            activeSequenceContainer = activeContainer;
        }

        /// <summary>
        /// Start a drag operation from an inventory slot
        /// </summary>
        public void StartDrag(int sourceIndex)
        {
            if (inventory[sourceIndex] == null) return; // Can't drag empty slots

            dragSourceIndex = sourceIndex;
            draggedGene = null; // Not dragging from gene editor
            draggedGeneSlot = null;
            isDragging = false; // Not dragging yet, just pressed
        }

        /// <summary>
        /// Start a drag operation from a gene editor slot
        /// </summary>
        public void StartDragFromGeneEditor(GeneBase gene, VisualElement sourceSlot)
        {
            draggedGene = gene;
            draggedGeneSlot = sourceSlot;
            dragSourceIndex = -1; // Not dragging from inventory
            isDragging = false; // Not dragging yet, just pressed
        }

        /// <summary>
        /// Check if currently dragging
        /// </summary>
        public bool IsDragging() => isDragging;

        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (dragSourceIndex == -1 && draggedGene == null) return;

            // Start dragging if moved
            if (!isDragging)
            {
                isDragging = true;

                if (draggedGene != null)
                {
                    // Dragging from gene editor
                    CreateDragPreviewFromGene(draggedGene);
                    OnDragStarted?.Invoke(draggedGene.Category);
                }
                else
                {
                    // Dragging from inventory
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
                // Use absolute screen position for smooth tracking
                dragPreview.style.left = evt.position.x - 32;
                dragPreview.style.top = evt.position.y - 32;
            }
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || (dragSourceIndex == -1 && draggedGene == null))
            {
                dragSourceIndex = -1;
                draggedGene = null;
                draggedGeneSlot = null;
                return;
            }

            bool dropHandled = false;

            // Check if dropped on inventory slot
            int inventoryDropIndex = GetInventorySlotAtPosition(evt.position);
            if (inventoryDropIndex >= 0)
            {
                if (dragSourceIndex >= 0 && dragSourceIndex != inventoryDropIndex)
                {
                    // Dragging from inventory to inventory
                    OnInventorySwapRequested?.Invoke(dragSourceIndex, inventoryDropIndex);
                    dropHandled = true;
                }
                else if (draggedGene != null)
                {
                    // Dragging from gene editor to inventory
                    if (draggedGeneSlot != null)
                    {
                        // Clear the gene editor slot visually
                        var background = draggedGeneSlot.Q("background");
                        var icon = draggedGeneSlot.Q<Image>("icon");
                        var tierLabel = draggedGeneSlot.Q<Label>("tier-label");

                        if (icon != null) icon.style.display = DisplayStyle.None;
                        if (tierLabel != null) tierLabel.text = "";
                        if (background != null)
                        {
                            background.ClearClassList();
                            background.AddToClassList("gene-slot__background");
                        }
                    }

                    Debug.Log($"Would add {draggedGene.geneName} to inventory slot {inventoryDropIndex}");
                    dropHandled = true;
                }
            }

            // Check if dropped on gene editor slot (only from inventory)
            if (!dropHandled && dragSourceIndex >= 0)
            {
                var geneSlotDrop = GetGeneSlotAtPosition(evt.position);
                if (geneSlotDrop.slot != null)
                {
                    OnGeneDropRequested?.Invoke(dragSourceIndex, geneSlotDrop.slot, geneSlotDrop.slotType);
                    dropHandled = true;
                }
            }

            // Always cleanup
            CleanupDrag();
        }

        private int GetInventorySlotAtPosition(Vector2 screenPos)
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

        private (VisualElement slot, string slotType) GetGeneSlotAtPosition(Vector2 screenPos)
        {
            // Check seed drop slot
            if (seedDropSlotContainer != null && seedDropSlotContainer.childCount > 0)
            {
                var seedSlot = seedDropSlotContainer.ElementAt(0);
                if (seedSlot.worldBound.Contains(screenPos))
                {
                    return (seedSlot, "seed");
                }
            }

            // Check passive slots
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

            // Check active sequence slots
            if (activeSequenceContainer != null)
            {
                foreach (var row in activeSequenceContainer.Children())
                {
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

        private void CleanupDrag()
        {
            if (dragPreview != null)
            {
                dragPreview.RemoveFromHierarchy();
                dragPreview = null;
            }

            // Notify that drag ended
            if (isDragging)
            {
                OnDragEnded?.Invoke();
            }

            isDragging = false;
            dragSourceIndex = -1;
            draggedGene = null;
            draggedGeneSlot = null;
        }

        /// <summary>
        /// Create a drag preview for an inventory item - WITH PROPER ICON SIZING
        /// </summary>
        private void CreateDragPreview(int index)
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
            
            // CRITICAL FIX: Ensure icon fills the preview properly (inline styling as backup)
            icon.style.width = Length.Percent(100);
            icon.style.height = Length.Percent(100);
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.left = 0;
            icon.scaleMode = ScaleMode.ScaleToFit;
            
            dragPreview.Add(icon);

            // Add to root element so it's above everything
            rootElement.Add(dragPreview);
        }

        /// <summary>
        /// Create a drag preview for a gene from the editor - WITH PROPER ICON SIZING
        /// </summary>
        private void CreateDragPreviewFromGene(GeneBase gene)
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
            
            // CRITICAL FIX: Ensure icon fills the preview properly (inline styling as backup)
            icon.style.width = Length.Percent(100);
            icon.style.height = Length.Percent(100);
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.left = 0;
            icon.scaleMode = ScaleMode.ScaleToFit;
            
            dragPreview.Add(icon);

            // Add to root element so it's above everything
            rootElement.Add(dragPreview);
        }
    }
}

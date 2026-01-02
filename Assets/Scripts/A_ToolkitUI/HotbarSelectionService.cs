using System;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Toolkit;

namespace Abracodabra.UI.Genes
{
    /// <summary>
    /// Static service that bridges the UI Toolkit hotbar system with game systems.
    /// This replaces the old InventoryBarController singleton pattern for item selection.
    /// Game systems should use this to get the currently selected item.
    /// </summary>
    public static class HotbarSelectionService
    {
        // Current selection state
        private static InventoryBarItem _selectedItem;
        private static int _selectedIndex = 0;

        /// <summary>
        /// The currently selected item in the hotbar, as an InventoryBarItem for game system compatibility
        /// </summary>
        public static InventoryBarItem SelectedItem => _selectedItem;

        /// <summary>
        /// The currently selected index (0-based)
        /// </summary>
        public static int SelectedIndex => _selectedIndex;

        /// <summary>
        /// Event fired when selection changes - provides the InventoryBarItem
        /// </summary>
        public static event Action<InventoryBarItem> OnSelectionChanged;

        /// <summary>
        /// PRIMARY METHOD: Select an item from UI Toolkit's UIInventoryItem
        /// Converts to InventoryBarItem for game system compatibility
        /// </summary>
        public static void SelectItem(int index, UIInventoryItem uiItem)
        {
            _selectedIndex = index;

            if (uiItem == null)
            {
                _selectedItem = null;
                Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: Empty");
                OnSelectionChanged?.Invoke(null);
                return;
            }

            // Convert UIInventoryItem to InventoryBarItem based on type
            if (uiItem.OriginalData is SeedTemplate seed)
            {
                _selectedItem = InventoryBarItem.FromSeed(seed);
                if (_selectedItem != null && uiItem.SeedRuntimeState != null)
                {
                    _selectedItem.SeedRuntimeState = uiItem.SeedRuntimeState;
                }
            }
            else if (uiItem.OriginalData is ToolDefinition tool)
            {
                _selectedItem = InventoryBarItem.FromTool(tool);
            }
            else if (uiItem.OriginalData is GeneBase gene)
            {
                var runtimeInstance = new RuntimeGeneInstance(gene);
                _selectedItem = InventoryBarItem.FromGene(runtimeInstance);
            }
            else if (uiItem.ResourceInstance != null)
            {
                _selectedItem = InventoryBarItem.FromItem(uiItem.ResourceInstance);
            }
            else if (uiItem.OriginalData is ItemDefinition itemDef)
            {
                // Create an ItemInstance from the definition
                var itemInstance = new ItemInstance(itemDef);
                itemInstance.stackCount = uiItem.StackSize;
                _selectedItem = InventoryBarItem.FromItem(itemInstance);
            }
            else
            {
                _selectedItem = null;
            }

            string itemName = _selectedItem?.GetDisplayName() ?? "Unknown";
            Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: {itemName}");
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        /// <summary>
        /// Select a seed item (legacy method for compatibility)
        /// </summary>
        public static void SelectSeed(int index, SeedTemplate seedTemplate, PlantGeneRuntimeState runtimeState = null)
        {
            _selectedIndex = index;
            _selectedItem = InventoryBarItem.FromSeed(seedTemplate);
            if (runtimeState != null && _selectedItem != null)
            {
                _selectedItem.SeedRuntimeState = runtimeState;
            }
            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        /// <summary>
        /// Select a tool item (legacy method for compatibility)
        /// </summary>
        public static void SelectTool(int index, ToolDefinition toolDef)
        {
            _selectedIndex = index;
            _selectedItem = InventoryBarItem.FromTool(toolDef);
            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        /// <summary>
        /// Select a gene item (legacy method for compatibility)
        /// </summary>
        public static void SelectGene(int index, GeneBase gene)
        {
            _selectedIndex = index;
            var runtimeInstance = new RuntimeGeneInstance(gene);
            _selectedItem = InventoryBarItem.FromGene(runtimeInstance);
            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        /// <summary>
        /// Select a resource/item (legacy method for compatibility)
        /// </summary>
        public static void SelectResource(int index, ItemInstance itemInstance)
        {
            _selectedIndex = index;
            _selectedItem = InventoryBarItem.FromItem(itemInstance);
            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        /// <summary>
        /// Select empty slot
        /// </summary>
        public static void SelectEmpty(int index)
        {
            _selectedIndex = index;
            _selectedItem = null;
            Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: Empty");
            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Clear the current selection
        /// </summary>
        public static void ClearSelection()
        {
            _selectedIndex = -1;
            _selectedItem = null;
            OnSelectionChanged?.Invoke(null);
        }

        private static void LogSelection(int index)
        {
            string itemName = _selectedItem?.GetDisplayName() ?? "Empty";
            Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: {itemName}");
        }
    }
}

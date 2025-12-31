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
        private static UIInventoryItem _selectedUIItem;

        /// <summary>
        /// The currently selected item in the hotbar, as an InventoryBarItem for game system compatibility
        /// </summary>
        public static InventoryBarItem SelectedItem => _selectedItem;

        /// <summary>
        /// The currently selected index (0-based)
        /// </summary>
        public static int SelectedIndex => _selectedIndex;

        /// <summary>
        /// The currently selected UI item (for UI Toolkit systems)
        /// </summary>
        public static UIInventoryItem SelectedUIItem => _selectedUIItem;

        /// <summary>
        /// Event fired when selection changes
        /// </summary>
        public static event Action<InventoryBarItem> OnSelectionChanged;

        /// <summary>
        /// Select an item at the given index using a UIInventoryItem
        /// Called by UIHotbarController when selection changes
        /// </summary>
        public static void SelectItem(int index, UIInventoryItem uiItem)
        {
            _selectedIndex = index;
            _selectedUIItem = uiItem;
            _selectedItem = ConvertToInventoryBarItem(uiItem);

            Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: {(_selectedItem?.GetDisplayName() ?? "Empty")}");
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        /// <summary>
        /// Clear the current selection
        /// </summary>
        public static void ClearSelection()
        {
            _selectedIndex = -1;
            _selectedUIItem = null;
            _selectedItem = null;
            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Convert a UIInventoryItem to an InventoryBarItem for game system compatibility
        /// </summary>
        private static InventoryBarItem ConvertToInventoryBarItem(UIInventoryItem uiItem)
        {
            if (uiItem == null) return null;

            var originalData = uiItem.OriginalData;

            // Handle SeedTemplate
            if (originalData is SeedTemplate seedTemplate)
            {
                var item = InventoryBarItem.FromSeed(seedTemplate);
                // If the UIInventoryItem has runtime state, use it
                if (uiItem.SeedRuntimeState != null)
                {
                    item.SeedRuntimeState = uiItem.SeedRuntimeState;
                }
                return item;
            }

            // Handle ToolDefinition
            if (originalData is ToolDefinition toolDef)
            {
                return InventoryBarItem.FromTool(toolDef);
            }

            // Handle GeneBase (wrapped in RuntimeGeneInstance for compatibility)
            if (originalData is GeneBase gene)
            {
                var runtimeInstance = new RuntimeGeneInstance(gene);
                return InventoryBarItem.FromGene(runtimeInstance);
            }

            // Handle RuntimeGeneInstance directly
            if (originalData is RuntimeGeneInstance runtimeGene)
            {
                return InventoryBarItem.FromGene(runtimeGene);
            }

            // Handle ItemInstance (resources)
            if (originalData is ItemInstance itemInstance)
            {
                return InventoryBarItem.FromItem(itemInstance);
            }

            return null;
        }

        /// <summary>
        /// Force refresh the current selection (useful after inventory changes)
        /// </summary>
        public static void RefreshCurrentSelection(UIInventoryItem uiItem)
        {
            _selectedUIItem = uiItem;
            _selectedItem = ConvertToInventoryBarItem(uiItem);
            OnSelectionChanged?.Invoke(_selectedItem);
        }
    }
}

// File: Assets/Scripts/A_ToolkitUI/HotbarSelectionService.cs
using UnityEngine;
using System;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Toolkit;

namespace Abracodabra.UI.Genes
{
    /// <summary>
    /// Static service that tracks the currently selected hotbar item.
    /// Used by PlayerTileInteractor and other systems to know what tool/seed is active.
    /// </summary>
    public static class HotbarSelectionService
    {
        private static UIInventoryItem _selectedItem;
        private static int _selectedIndex = 0;

        public static UIInventoryItem SelectedItem => _selectedItem;
        public static int SelectedIndex => _selectedIndex;

        public static event Action<UIInventoryItem> OnSelectionChanged;

        public static void SelectItem(int index, UIInventoryItem uiItem)
        {
            _selectedIndex = index;
            _selectedItem = uiItem;

            string itemName = _selectedItem?.GetDisplayName() ?? "Empty";
            Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: {itemName}");

            OnSelectionChanged?.Invoke(_selectedItem);
        }

        public static void SelectSeed(int index, SeedTemplate seedTemplate, PlantGeneRuntimeState runtimeState = null)
        {
            _selectedIndex = index;
            _selectedItem = UIInventoryItem.FromSeed(seedTemplate);

            if (runtimeState != null && _selectedItem != null)
            {
                _selectedItem.SeedRuntimeState = runtimeState;
            }

            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        public static void SelectTool(int index, ToolDefinition toolDef)
        {
            _selectedIndex = index;
            _selectedItem = UIInventoryItem.FromTool(toolDef);

            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        public static void SelectGene(int index, GeneBase gene)
        {
            _selectedIndex = index;
            _selectedItem = UIInventoryItem.FromGene(gene);

            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        public static void SelectResource(int index, ItemInstance itemInstance)
        {
            _selectedIndex = index;
            _selectedItem = UIInventoryItem.FromItem(itemInstance);

            LogSelection(index);
            OnSelectionChanged?.Invoke(_selectedItem);
        }

        public static void SelectEmpty(int index)
        {
            _selectedIndex = index;
            _selectedItem = null;

            Debug.Log($"[HotbarSelectionService] Selected slot {index + 1}: Empty");
            OnSelectionChanged?.Invoke(null);
        }

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
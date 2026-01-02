using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.UI.Toolkit; // For UIInventoryItem

namespace Abracodabra.UI.Genes
{
    /// <summary>
    /// Static service that manages the player's inventory for the UI Toolkit system.
    /// This bridges the UI inventory with game systems (planting, harvesting, eating).
    /// 
    /// Game systems should use this service instead of the old InventoryGridController.
    /// </summary>
    public static class InventoryService
    {
        // Inventory data (owned by GameUIManager, registered here)
        private static List<UIInventoryItem> _inventory;
        private static int _inventoryColumns;
        private static int _inventoryRows;

        /// <summary>
        /// Event fired when inventory contents change (item added, removed, or swapped)
        /// </summary>
        public static event Action OnInventoryChanged;

        /// <summary>
        /// Event fired when a specific slot changes - provides the index
        /// </summary>
        public static event Action<int> OnSlotChanged;

        /// <summary>
        /// Whether the service has been initialized with an inventory
        /// </summary>
        public static bool IsInitialized => _inventory != null;

        /// <summary>
        /// Total number of inventory slots
        /// </summary>
        public static int TotalSlots => _inventory?.Count ?? 0;

        /// <summary>
        /// Number of columns (also = hotbar size)
        /// </summary>
        public static int Columns => _inventoryColumns;

        /// <summary>
        /// Register the inventory list from GameUIManager.
        /// Called once during UI initialization.
        /// </summary>
        public static void Register(List<UIInventoryItem> inventory, int columns, int rows)
        {
            _inventory = inventory;
            _inventoryColumns = columns;
            _inventoryRows = rows;
            Debug.Log($"[InventoryService] Registered inventory with {inventory.Count} slots ({columns}x{rows})");
        }

        /// <summary>
        /// Unregister the inventory (called when UI is destroyed)
        /// </summary>
        public static void Unregister()
        {
            _inventory = null;
            Debug.Log("[InventoryService] Inventory unregistered");
        }

        /// <summary>
        /// Get the item at a specific inventory index
        /// </summary>
        public static UIInventoryItem GetItemAt(int index)
        {
            if (_inventory == null || index < 0 || index >= _inventory.Count)
                return null;
            return _inventory[index];
        }

        /// <summary>
        /// Remove the item at a specific index (sets slot to null)
        /// Returns the removed item, or null if slot was already empty
        /// </summary>
        public static UIInventoryItem RemoveItemAtIndex(int index)
        {
            if (_inventory == null || index < 0 || index >= _inventory.Count)
            {
                Debug.LogWarning($"[InventoryService] Cannot remove item: invalid index {index}");
                return null;
            }

            var removedItem = _inventory[index];
            if (removedItem == null)
            {
                Debug.Log($"[InventoryService] Slot {index} was already empty");
                return null;
            }

            _inventory[index] = null;
            
            string itemName = removedItem.GetDisplayName();
            Debug.Log($"[InventoryService] Removed '{itemName}' from slot {index}");

            OnSlotChanged?.Invoke(index);
            OnInventoryChanged?.Invoke();

            return removedItem;
        }

        /// <summary>
        /// Add an item to the first available empty slot
        /// Returns the index where item was placed, or -1 if inventory is full
        /// </summary>
        public static int AddItem(UIInventoryItem item)
        {
            if (_inventory == null || item == null)
                return -1;

            int emptyIndex = GetFirstEmptySlot();
            if (emptyIndex < 0)
            {
                Debug.LogWarning("[InventoryService] Cannot add item: inventory is full");
                return -1;
            }

            _inventory[emptyIndex] = item;
            
            string itemName = item.GetDisplayName();
            Debug.Log($"[InventoryService] Added '{itemName}' to slot {emptyIndex}");

            OnSlotChanged?.Invoke(emptyIndex);
            OnInventoryChanged?.Invoke();

            return emptyIndex;
        }

        /// <summary>
        /// Add an item at a specific index (overwrites existing item!)
        /// Use with caution - prefer AddItem() for safe insertion
        /// </summary>
        public static bool SetItemAt(int index, UIInventoryItem item)
        {
            if (_inventory == null || index < 0 || index >= _inventory.Count)
                return false;

            _inventory[index] = item;

            OnSlotChanged?.Invoke(index);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Find the first empty slot in the inventory
        /// Returns -1 if inventory is full
        /// </summary>
        public static int GetFirstEmptySlot()
        {
            if (_inventory == null) return -1;

            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i] == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Check if inventory has any empty slots
        /// </summary>
        public static bool HasEmptySlot()
        {
            return GetFirstEmptySlot() >= 0;
        }

        /// <summary>
        /// Get all items in the first row (hotbar items)
        /// This returns the actual list segment, preserving nulls for empty slots
        /// </summary>
        public static List<UIInventoryItem> GetHotbarItems()
        {
            if (_inventory == null || _inventoryColumns <= 0)
                return new List<UIInventoryItem>();

            var hotbarItems = new List<UIInventoryItem>();
            int count = Mathf.Min(_inventoryColumns, _inventory.Count);
            
            for (int i = 0; i < count; i++)
            {
                hotbarItems.Add(_inventory[i]); // Add even if null!
            }
            
            return hotbarItems;
        }

        /// <summary>
        /// Check if the given index is in the hotbar (first row)
        /// </summary>
        public static bool IsHotbarIndex(int index)
        {
            return index >= 0 && index < _inventoryColumns;
        }

        /// <summary>
        /// Convert an InventoryBarItem to UIInventoryItem for adding to inventory
        /// Used when harvesting returns InventoryBarItem
        /// </summary>
        public static UIInventoryItem ConvertFromLegacy(InventoryBarItem legacyItem)
        {
            if (legacyItem == null) return null;

            switch (legacyItem.Type)
            {
                case InventoryBarItem.ItemType.Seed:
                    var seedItem = new UIInventoryItem(legacyItem.SeedTemplate);
                    if (legacyItem.SeedRuntimeState != null)
                    {
                        seedItem.SeedRuntimeState = legacyItem.SeedRuntimeState;
                    }
                    return seedItem;

                case InventoryBarItem.ItemType.Tool:
                    return new UIInventoryItem(legacyItem.ToolDefinition);

                case InventoryBarItem.ItemType.Gene:
                    var gene = legacyItem.GeneInstance?.GetGene();
                    if (gene != null)
                        return new UIInventoryItem(gene);
                    return null;

                case InventoryBarItem.ItemType.Resource:
                    if (legacyItem.ItemInstance != null)
                        return new UIInventoryItem(legacyItem.ItemInstance);
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Add a harvested item to inventory (convenience method)
        /// </summary>
        public static bool AddHarvestedItem(ItemInstance itemInstance)
        {
            if (itemInstance == null || itemInstance.definition == null)
                return false;

            var uiItem = new UIInventoryItem(itemInstance);
            return AddItem(uiItem) >= 0;
        }
    }
}

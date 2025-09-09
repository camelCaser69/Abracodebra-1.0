using UnityEngine;
using System.Collections.Generic;

// Note: ItemEffect is not defined yet, so it's commented out for now to prevent errors.
// public class ItemEffect : ScriptableObject { /* ... effect logic ... */ }

// REFINED: This enum is now ONLY for categorizing items defined by this script.
public enum ItemCategory
{
    Consumable,  // Food, potions
    Resource,    // Crafting materials
    QuestItem    // Example for future expansion
}

[CreateAssetMenu(fileName = "NewItemDef", menuName = "Abracodabra/Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("Item Properties")]
    public ItemCategory category = ItemCategory.Consumable;
    public int maxStackSize = 99;
    public float baseValue = 1f;

    [Header("World Representation")]
    public GameObject droppedItemPrefab; // For dropping items in the world

    [Header("Consumption Effects")]
    public bool isConsumable = false;
    public float baseNutrition = 0f;
    public float baseHealing = 0f;

    // For other effects (future expansion) - commented out until ItemEffect is defined
    // public List<ItemEffect> additionalEffects = new List<ItemEffect>();
}
// FILE: Assets/Scripts/Ecosystem/Feeding/ConsumableData.cs
using UnityEngine;

namespace Abracodabra.Ecosystem.Feeding
{
    /// <summary>
    /// Wrapper class that represents consumable food data.
    /// Can be created from either a FoodType ScriptableObject or a consumable ItemDefinition.
    /// This bridges the gap between the FoodType system (for animal diets) and 
    /// the ItemDefinition system (for inventory items).
    /// </summary>
    public class ConsumableData
    {
        public string Name { get; private set; }
        public Sprite Icon { get; private set; }
        public float NutritionValue { get; private set; }
        public FoodType.FoodCategory Category { get; private set; }
        
        // Original references (one will be null)
        public FoodType FoodType { get; private set; }
        public ItemDefinition ItemDefinition { get; private set; }
        
        // Track source for debugging
        public bool IsFromFoodType => FoodType != null;
        public bool IsFromItemDefinition => ItemDefinition != null;

        /// <summary>
        /// Create from a FoodType ScriptableObject
        /// </summary>
        public ConsumableData(FoodType foodType)
        {
            if (foodType == null)
            {
                Debug.LogError("[ConsumableData] Created with null FoodType");
                return;
            }

            FoodType = foodType;
            ItemDefinition = null;
            
            Name = foodType.foodName;
            Icon = foodType.icon;
            NutritionValue = foodType.baseSatiationValue;
            Category = foodType.category;
        }

        /// <summary>
        /// Create from an ItemDefinition (must be consumable)
        /// </summary>
        public ConsumableData(ItemDefinition itemDef)
        {
            if (itemDef == null)
            {
                Debug.LogError("[ConsumableData] Created with null ItemDefinition");
                return;
            }

            FoodType = null;
            ItemDefinition = itemDef;
            
            Name = itemDef.itemName;
            Icon = itemDef.icon;
            NutritionValue = itemDef.baseNutrition;
            
            // Try to infer category from item name/category
            Category = InferCategoryFromItem(itemDef);
        }

        /// <summary>
        /// Create from an ItemInstance
        /// </summary>
        public ConsumableData(ItemInstance itemInstance)
        {
            if (itemInstance?.definition == null)
            {
                Debug.LogError("[ConsumableData] Created with null ItemInstance or definition");
                return;
            }

            var itemDef = itemInstance.definition;
            FoodType = null;
            ItemDefinition = itemDef;
            
            Name = itemDef.itemName;
            Icon = itemDef.icon;
            NutritionValue = itemInstance.GetNutrition(); // Uses dynamic properties
            Category = InferCategoryFromItem(itemDef);
        }

        /// <summary>
        /// Infer food category from ItemDefinition.
        /// This is a heuristic - you can expand this or add a FoodCategory field to ItemDefinition.
        /// </summary>
        private FoodType.FoodCategory InferCategoryFromItem(ItemDefinition itemDef)
        {
            if (itemDef == null) return FoodType.FoodCategory.Other;

            string nameLower = itemDef.itemName?.ToLower() ?? "";
            
            // Try to match based on name
            if (nameLower.Contains("fruit") || nameLower.Contains("berry") || nameLower.Contains("apple"))
                return FoodType.FoodCategory.Plant_Fruit;
            
            if (nameLower.Contains("leaf") || nameLower.Contains("lettuce") || nameLower.Contains("herb"))
                return FoodType.FoodCategory.Plant_Leaf;
            
            if (nameLower.Contains("seed"))
                return FoodType.FoodCategory.Plant_Seed;
            
            if (nameLower.Contains("stem") || nameLower.Contains("stalk"))
                return FoodType.FoodCategory.Plant_Stem;
            
            return FoodType.FoodCategory.Other;
        }

        /// <summary>
        /// Check if this consumable matches a specific food category
        /// </summary>
        public bool MatchesCategory(FoodType.FoodCategory category)
        {
            return Category == category;
        }

        /// <summary>
        /// Try to create ConsumableData from any object.
        /// Returns null if the object is not a valid consumable.
        /// </summary>
        public static ConsumableData TryCreate(object data)
        {
            if (data == null) return null;

            // Direct FoodType
            if (data is FoodType foodType)
            {
                return new ConsumableData(foodType);
            }

            // ItemDefinition (must be consumable)
            if (data is ItemDefinition itemDef)
            {
                if (itemDef.isConsumable && itemDef.baseNutrition > 0)
                {
                    return new ConsumableData(itemDef);
                }
                return null;
            }

            // ItemInstance
            if (data is ItemInstance itemInstance)
            {
                if (itemInstance.definition?.isConsumable == true)
                {
                    return new ConsumableData(itemInstance);
                }
                return null;
            }

            return null;
        }

        public override string ToString()
        {
            return $"[ConsumableData: {Name}, Nutrition={NutritionValue}, Category={Category}, Source={(IsFromFoodType ? "FoodType" : "ItemDef")}]";
        }
    }
}
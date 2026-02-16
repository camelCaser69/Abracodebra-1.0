using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes.Runtime; // For Payloads

namespace Abracodabra.Ecosystem.Feeding {
    public class ConsumableData {
        public string Name { get; set; }
        public Sprite Icon { get; set; }
        public float NutritionValue { get; set; }
        public FoodType.FoodCategory Category { get; set; }

        public FoodType FoodType { get; set; }
        public ItemDefinition ItemDefinition { get; set; }

        // ✅ ADDED: Payloads carried by this consumable
        public List<RuntimeGeneInstance> Payloads { get; set; } = new List<RuntimeGeneInstance>();

        // Track source for debugging
        public bool IsFromFoodType => FoodType != null;
        public bool IsFromItemDefinition => ItemDefinition != null;

        public ConsumableData(FoodType foodType) {
            if (foodType == null) {
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

        public ConsumableData(ItemDefinition itemDef) {
            if (itemDef == null) {
                Debug.LogError("[ConsumableData] Created with null ItemDefinition");
                return;
            }

            FoodType = null;
            ItemDefinition = itemDef;

            Name = itemDef.itemName;
            Icon = itemDef.icon;
            NutritionValue = itemDef.baseNutrition;

            Category = InferCategoryFromItem(itemDef);
        }

        public ConsumableData(ItemInstance itemInstance) {
            if (itemInstance?.definition == null) {
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

            // ✅ MODIFIED: Copy payloads
            if (itemInstance.payloads != null) {
                Payloads = new List<RuntimeGeneInstance>(itemInstance.payloads);
            }
        }

        FoodType.FoodCategory InferCategoryFromItem(ItemDefinition itemDef) {
            if (itemDef == null) return FoodType.FoodCategory.Other;

            string nameLower = itemDef.itemName?.ToLower() ?? "";

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

        public bool MatchesCategory(FoodType.FoodCategory category) {
            return Category == category;
        }

        public static ConsumableData TryCreate(object data) {
            if (data == null) return null;

            if (data is FoodType foodType) {
                return new ConsumableData(foodType);
            }

            if (data is ItemDefinition itemDef) {
                if (itemDef.isConsumable && itemDef.baseNutrition > 0) {
                    return new ConsumableData(itemDef);
                }
                return null;
            }

            if (data is ItemInstance itemInstance) {
                if (itemInstance.definition?.isConsumable == true) {
                    return new ConsumableData(itemInstance);
                }
                return null;
            }

            return null;
        }

        public override string ToString() {
            return $"[ConsumableData: {Name}, Nutrition={NutritionValue}, Category={Category}, Source={(IsFromFoodType ? "FoodType" : "ItemDef")}, Payloads={Payloads.Count}]";
        }
    }
}
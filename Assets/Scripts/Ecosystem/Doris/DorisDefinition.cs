// FILE: Assets/Scripts/Ecosystem/Doris/DorisDefinition.cs
using System;
using UnityEngine;

namespace Abracodabra.Ecosystem
{
    /// <summary>
    /// ScriptableObject defining Doris's properties and behaviors.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDorisDefinition", menuName = "Abracodabra/Doris Definition")]
    public class DorisDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name for UI and logs.")]
        public string displayName = "Doris";

        [Tooltip("Description shown in tooltips/UI.")]
        public string description = "A mysterious creature living in a stump. Always hungry.";

        [Header("Hunger Configuration")]
        [Tooltip("Maximum hunger value. When reached, Doris is fully starving.")]
        [Min(1f)]
        public float maxHunger = 100f;

        [Tooltip("How much hunger increases per tick during Growth & Threat phase.")]
        [Min(0f)]
        public float hungerPerTick = 0.25f;

        [Tooltip("Percentage of maxHunger where Doris becomes 'hungry' (visual/audio cues).")]
        [Range(0f, 1f)]
        public float hungryThreshold = 0.4f;

        [Tooltip("Percentage of maxHunger where Doris becomes 'starving' (will eat plants).")]
        [Range(0f, 1f)]
        public float starvingThreshold = 0.8f;

        [Header("Starvation Behavior")]
        [Tooltip("How many ticks between each plant eating attempt when starving.")]
        [Min(1)]
        public int ticksBetweenPlantEating = 5;

        [Tooltip("How far (in tiles) Doris can reach to eat plants when starving.")]
        [Min(1)]
        public int plantEatingRadius = 4;

        [Tooltip("How much hunger is reduced when Doris eats a plant (less than player feeding).")]
        [Min(0f)]
        public float hungerReductionFromPlant = 10f;

        // Legacy alias for backwards compatibility
        public int starvationPlantEatInterval => ticksBetweenPlantEating;
        public int starvationPlantSearchRadius => plantEatingRadius;

        [Header("Feeding")]
        [Tooltip("Base multiplier for nutrition when player feeds Doris.")]
        [Min(0.1f)]
        public float feedingEfficiency = 1f;

        [Header("Diet Configuration")]
        [Tooltip("Food categories Doris accepts. Empty = accepts all.")]
        public FoodType.FoodCategory[] acceptedFoodCategories;

        [Tooltip("Multipliers for different food categories")]
        public FoodCategoryMultiplier[] foodCategoryMultipliers;

        [Serializable]
        public struct FoodCategoryMultiplier
        {
            public FoodType.FoodCategory category;
            [Range(0.1f, 3f)]
            public float multiplier;
        }

        [Header("Visual")]
        [Tooltip("Sprite for happy state (low hunger).")]
        public Sprite happySprite;

        [Tooltip("Sprite for hungry state (medium hunger).")]
        public Sprite hungrySprite;

        [Tooltip("Sprite for starving/hangry state (high hunger).")]
        public Sprite starvingSprite;

        [Header("Audio")]
        [Tooltip("Sound when fed.")]
        public AudioClip feedSound;

        [Tooltip("Sound when becoming hungry.")]
        public AudioClip hungrySound;

        [Tooltip("Sound when eating a plant (starving behavior).")]
        public AudioClip eatPlantSound;

        // Computed properties
        public float HungryHungerValue => maxHunger * hungryThreshold;
        public float StarvingHungerValue => maxHunger * starvingThreshold;

        /// <summary>
        /// Get the satiation multiplier for a food category.
        /// Returns 1.0 if no multiplier is defined.
        /// </summary>
        public float GetFoodCategoryMultiplier(FoodType.FoodCategory category)
        {
            if (foodCategoryMultipliers == null || foodCategoryMultipliers.Length == 0)
                return 1f;

            foreach (var mult in foodCategoryMultipliers)
            {
                if (mult.category == category)
                    return mult.multiplier;
            }

            return 1f;
        }

        /// <summary>
        /// Check if Doris can eat a specific food category.
        /// </summary>
        public bool CanEatCategory(FoodType.FoodCategory category)
        {
            if (acceptedFoodCategories == null || acceptedFoodCategories.Length == 0)
                return true; // Accept all if not specified

            foreach (var accepted in acceptedFoodCategories)
            {
                if (accepted == category)
                    return true;
            }

            return false;
        }

        private void OnValidate()
        {
            maxHunger = Mathf.Max(1f, maxHunger);
            hungerPerTick = Mathf.Max(0f, hungerPerTick);
            ticksBetweenPlantEating = Mathf.Max(1, ticksBetweenPlantEating);
            plantEatingRadius = Mathf.Max(1, plantEatingRadius);
            hungerReductionFromPlant = Mathf.Max(0f, hungerReductionFromPlant);
            feedingEfficiency = Mathf.Max(0.1f, feedingEfficiency);

            if (starvingThreshold < hungryThreshold)
            {
                starvingThreshold = hungryThreshold;
            }
        }
    }
}
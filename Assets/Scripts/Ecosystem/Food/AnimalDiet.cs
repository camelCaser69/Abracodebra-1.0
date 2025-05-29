using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Simplified Preference: Links FoodType to satiation amount and behavior priority
[System.Serializable]
public class DietPreferenceSimplified
{
    [Tooltip("The specific FoodType this preference applies to.")]
    public FoodType foodType;

    [Tooltip("How much satiation (hunger reduction) is gained when this food is eaten.")]
    public float satiationAmount = 5f;

    [Tooltip("Priority for seeking this food (higher value = higher priority). Used for choosing between nearby valid foods.")]
    [Range(0.1f, 5f)]
    public float preferencePriority = 1f;
}


// Simplified Diet: Defines hunger stats and list of preferences
[CreateAssetMenu(fileName = "Diet_", menuName = "Ecosystem/Animal Diet (Simplified)")]
public class AnimalDiet : ScriptableObject
{
    [Header("Diet Preferences")]
    [Tooltip("List of foods this animal can eat and how much they satisfy hunger.")]
    public List<DietPreferenceSimplified> acceptableFoods = new List<DietPreferenceSimplified>();

    [Header("Hunger Mechanics")]
    [Tooltip("Maximum hunger level.")]
    public float maxHunger = 20f;
    [Tooltip("Rate at which hunger increases per second.")]
    public float hungerIncreaseRate = 0.5f;
    [Tooltip("Hunger level above which the animal will actively seek food.")]
    public float hungerThreshold = 10f;

    // Removed starvation for simplicity, can be added back later if needed
    // [Header("Starvation")]
    // public float starvationDamageRate = 0.5f;

    /// <summary>
    /// Checks if a specific FoodType is included in this diet's acceptable foods.
    /// </summary>
    public bool CanEat(FoodType food)
    {
        if (food == null) return false;
        return acceptableFoods.Any(pref => pref.foodType == food);
    }

    /// <summary>
    /// Gets the DietPreferenceSimplified entry for a specific FoodType.
    /// </summary>
    public DietPreferenceSimplified GetPreference(FoodType food)
    {
         if (food == null) return null;
         return acceptableFoods.FirstOrDefault(p => p.foodType == food);
    }

    /// <summary>
    /// Gets the satiation amount provided by a specific FoodType for this diet.
    /// </summary>
    public float GetSatiationValue(FoodType food)
    {
        var pref = GetPreference(food);
        return pref != null ? pref.satiationAmount : 0f;
    }

    /// <summary>
    /// Finds the best food target from nearby colliders based on preference and distance.
    /// </summary>
    public GameObject FindBestFood(Collider2D[] nearbyColliders, Vector3 animalPosition)
    {
        GameObject bestTarget = null;
        float highestScore = -1f; // Start below any possible score

        foreach (var collider in nearbyColliders)
        {
            if (collider == null) continue;

            // BUGFIX: Skip objects with PoopController to prevent animals from eating poop
            PoopController poopController = collider.GetComponent<PoopController>();
            if (poopController != null) continue;

            FoodItem foodItem = collider.GetComponent<FoodItem>();

            // Must have FoodItem and its FoodType must be edible by this diet
            if (foodItem != null && foodItem.foodType != null && CanEat(foodItem.foodType))
            {
                DietPreferenceSimplified pref = GetPreference(foodItem.foodType);
                if (pref == null) continue; // Should be caught by CanEat, but safe check

                float distance = Vector3.Distance(animalPosition, collider.transform.position);
                // Simple score: Higher preference is better, closer is better.
                // Avoid division by zero or very small distances inflating score excessively.
                float score = pref.preferencePriority / (1f + distance); // Inverse distance weighting

                if (score > highestScore)
                {
                    highestScore = score;
                    bestTarget = collider.gameObject;
                }
            }
        }
        return bestTarget;
    }
}

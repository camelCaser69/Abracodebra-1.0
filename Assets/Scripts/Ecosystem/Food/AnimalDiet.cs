using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class DietPreferenceSimplified {
    public FoodType foodType;
    [Tooltip("How much hunger this food reduces when eaten")]
    public float satiationAmount = 5f;
    [Tooltip("Higher values = more preferred. Used for AI decision making")]
    public float preferencePriority = 1f;
}

[CreateAssetMenu(fileName = "NewAnimalDiet", menuName = "Ecosystem/Animal Diet")]
public class AnimalDiet : ScriptableObject {
    [Header("Food Preferences")]
    public List<DietPreferenceSimplified> acceptableFoods = new List<DietPreferenceSimplified>();

    [Header("Hunger Settings")]
    [Tooltip("Maximum hunger value before starvation damage begins")]
    public float maxHunger = 20f;
    
    [Tooltip("How much hunger increases per hunger tick (see TickManager config)")]
    public float hungerIncreaseRate = 0.5f;
    
    [Tooltip("When hunger reaches this value, animal will seek food")]
    public float hungerThreshold = 10f;

    public bool CanEat(FoodType food) {
        if (food == null) return false;
        return acceptableFoods.Any(pref => pref.foodType == food);
    }

    public DietPreferenceSimplified GetPreference(FoodType food) {
        if (food == null) return null;
        return acceptableFoods.FirstOrDefault(p => p.foodType == food);
    }

    public float GetSatiationValue(FoodType food) {
        var pref = GetPreference(food);
        return pref != null ? pref.satiationAmount : 0f;
    }

    public GameObject FindBestFood(Collider2D[] nearbyColliders, Vector3 animalPosition) {
        GameObject bestTarget = null;
        float highestScore = -1f;

        foreach (var collider in nearbyColliders) {
            if (collider == null) continue;

            // Skip poop
            PoopController poopController = collider.GetComponent<PoopController>();
            if (poopController != null) continue;

            // Check for food
            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem != null && foodItem.foodType != null && CanEat(foodItem.foodType)) {
                DietPreferenceSimplified pref = GetPreference(foodItem.foodType);
                if (pref == null) continue;

                float distance = Vector3.Distance(animalPosition, collider.transform.position);
                float score = pref.preferencePriority / (1f + distance); // Inverse distance weighting

                if (score > highestScore) {
                    highestScore = score;
                    bestTarget = collider.gameObject;
                }
            }
        }
        return bestTarget;
    }

    void OnValidate() {
        // Ensure all values are positive and make sense
        maxHunger = Mathf.Max(1f, maxHunger);
        hungerIncreaseRate = Mathf.Max(0.1f, hungerIncreaseRate);
        hungerThreshold = Mathf.Clamp(hungerThreshold, 0f, maxHunger);

        // Validate food preferences
        foreach (var pref in acceptableFoods) {
            if (pref != null) {
                pref.satiationAmount = Mathf.Max(0.1f, pref.satiationAmount);
                pref.preferencePriority = Mathf.Max(0.1f, pref.preferencePriority);
            }
        }
    }
}
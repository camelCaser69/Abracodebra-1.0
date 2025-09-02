using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DietPreferenceSimplified
{
    public FoodType foodType;
    // Note: satiationAmount has been removed from here.
    [Tooltip("How much the animal prefers this food. Higher values are prioritized from further away.")]
    public float preferencePriority = 1f;
}

[CreateAssetMenu(fileName = "NewAnimalDiet", menuName = "Ecosystem/Animal Diet")]
public class AnimalDiet : ScriptableObject
{
    public List<DietPreferenceSimplified> acceptableFoods = new List<DietPreferenceSimplified>();

    [Tooltip("The maximum hunger an animal can have before it starts starving.")]
    public float maxHunger = 20f;

    [Tooltip("How much hunger the animal gains per hunger tick interval.")]
    public float hungerIncreaseRate = 0.5f;

    [Tooltip("The hunger level at which the animal will start actively seeking food.")]
    public float hungerThreshold = 10f;

    public bool CanEat(FoodType food)
    {
        if (food == null) return false;
        return acceptableFoods.Any(pref => pref.foodType == food);
    }

    public DietPreferenceSimplified GetPreference(FoodType food)
    {
        if (food == null) return null;
        return acceptableFoods.FirstOrDefault(p => p.foodType == food);
    }

    public float GetSatiationValue(FoodType food)
    {
        // The value is now retrieved directly from the FoodType itself.
        if (food != null && CanEat(food))
        {
            return food.baseSatiationValue;
        }
        return 0f;
    }

    public GameObject FindBestFood(Collider2D[] nearbyColliders, Vector3 animalPosition)
    {
        GameObject bestTarget = null;
        float highestScore = -1f;

        foreach (var collider in nearbyColliders)
        {
            if (collider == null) continue;

            PoopController poopController = collider.GetComponent<PoopController>();
            if (poopController != null) continue;

            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem != null && foodItem.foodType != null && CanEat(foodItem.foodType))
            {
                DietPreferenceSimplified pref = GetPreference(foodItem.foodType);
                if (pref == null) continue;

                float distance = Vector3.Distance(animalPosition, collider.transform.position);
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

    void OnValidate()
    {
        maxHunger = Mathf.Max(1f, maxHunger);
        hungerIncreaseRate = Mathf.Max(0.1f, hungerIncreaseRate);
        hungerThreshold = Mathf.Clamp(hungerThreshold, 0f, maxHunger);

        foreach (var pref in acceptableFoods)
        {
            if (pref != null)
            {
                pref.preferencePriority = Mathf.Max(0.1f, pref.preferencePriority);
            }
        }
    }
}
// FILE: Assets/Scripts/Ecosystem/Feeding/AnimalFeedableAdapter.cs
using System;
using UnityEngine;
using Abracodabra.Ecosystem.Feeding;

/// <summary>
/// Adapter component that adds IFeedable support to existing AnimalController.
/// Add this component alongside AnimalController to enable player feeding via the unified system.
/// </summary>
[RequireComponent(typeof(AnimalController))]
public class AnimalFeedableAdapter : MonoBehaviour, IFeedable
{
    [Header("Feeding Configuration")]
    [SerializeField] private Vector3 feedPopupOffset = new Vector3(0f, 1f, 0f);
    
    [Header("Category Fallback")]
    [Tooltip("If true, allows feeding based on category match when no FoodType reference exists")]
    [SerializeField] private bool allowCategoryFallback = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    // Cached references
    private AnimalController animalController;
    private AnimalNeeds animalNeeds;

    // Events
    public event Action<ConsumableData, float> OnFed;

    #region IFeedable Implementation

    public string FeedableName => animalController?.SpeciesName ?? "Animal";

    public Vector3 FeedPopupAnchor => transform.position + feedPopupOffset;

    public bool CanAcceptFood(ConsumableData consumable)
    {
        if (consumable == null) return false;
        if (animalController == null || animalController.Definition == null) return false;

        var diet = animalController.Definition.diet;
        if (diet == null) return false;

        // If the consumable came from a FoodType, use the diet's native check
        if (consumable.FoodType != null)
        {
            return diet.CanEat(consumable.FoodType);
        }

        // For ItemDefinition-based consumables, check by category if fallback is enabled
        if (allowCategoryFallback)
        {
            // Check if any acceptable food in the diet matches the category
            foreach (var pref in diet.acceptableFoods)
            {
                if (pref.foodType != null && pref.foodType.category == consumable.Category)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public float ReceiveFood(ConsumableData consumable, GameObject feeder)
    {
        if (consumable == null || animalNeeds == null)
        {
            return 0f;
        }

        // Get satiation value
        float satiation = consumable.NutritionValue;
        
        // If we have a FoodType, use the diet's satiation calculation
        if (consumable.FoodType != null && animalController?.Definition?.diet != null)
        {
            satiation = animalController.Definition.diet.GetSatiationValue(consumable.FoodType);
        }

        // Feed the animal through its needs system
        float oldHunger = animalNeeds.CurrentHunger;
        animalNeeds.ModifyHunger(-satiation);
        float actualReduction = oldHunger - animalNeeds.CurrentHunger;

        if (debugLog)
        {
            Debug.Log($"[AnimalFeedableAdapter] Fed {consumable.Name} to {FeedableName}. " +
                      $"Satiation: {satiation:F1}, Actual reduction: {actualReduction:F1}");
        }

        OnFed?.Invoke(consumable, actualReduction);
        return actualReduction;
    }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        animalController = GetComponent<AnimalController>();
        animalNeeds = GetComponent<AnimalNeeds>();
    }

    void Start()
    {
        // Register with feeding system
        if (FeedingSystem.Instance != null)
        {
            FeedingSystem.Instance.RegisterFeedable(this);
            
            if (debugLog)
            {
                Debug.Log($"[AnimalFeedableAdapter] Registered {FeedableName} with FeedingSystem");
            }
        }
        else
        {
            // Delay registration in case FeedingSystem isn't initialized yet
            Invoke(nameof(DelayedFeedingRegistration), 0.3f);
        }
    }

    void DelayedFeedingRegistration()
    {
        if (FeedingSystem.Instance != null)
        {
            FeedingSystem.Instance.RegisterFeedable(this);
            if (debugLog) Debug.Log($"[AnimalFeedableAdapter] Registered {FeedableName} with FeedingSystem (delayed)");
        }
    }

    void OnDestroy()
    {
        // Unregister from feeding system
        if (FeedingSystem.Instance != null)
        {
            FeedingSystem.Instance.UnregisterFeedable(this);
        }
    }

    void OnValidate()
    {
        if (animalController == null)
        {
            animalController = GetComponent<AnimalController>();
        }
    }

    #endregion
}
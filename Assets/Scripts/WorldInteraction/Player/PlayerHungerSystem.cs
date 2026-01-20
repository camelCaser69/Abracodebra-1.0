// FILE: Assets/Scripts/WorldInteraction/Player/PlayerHungerSystem.cs
using System;
using UnityEngine;
using WegoSystem;
using Abracodabra.Ecosystem.Feeding;

public class PlayerHungerSystem : MonoBehaviour, ITickUpdateable, IFeedable
{
    [Header("Hunger Configuration")]
    [Tooltip("Maximum hunger value. When reached, player starves.")]
    [SerializeField] private float maxHunger = 100f;

    [Tooltip("Starting hunger as a fraction of max (0 = not hungry, 1 = starving).")]
    [SerializeField] [Range(0f, 1f)] private float startingHungerFraction = 0f;

    [Tooltip("How much hunger increases per tick.")]
    [SerializeField] private float hungerIncreasePerTick = 0.15f;

    [Header("Hunger Thresholds")]
    [Tooltip("Fraction of max hunger where 'hungry' state begins (yellow warning).")]
    [SerializeField] [Range(0f, 1f)] private float hungryThreshold = 0.5f;

    [Tooltip("Fraction of max hunger where 'starving' state begins (red warning).")]
    [SerializeField] [Range(0f, 1f)] private float starvingThreshold = 0.8f;

    [Header("Diet Configuration")]
    [Tooltip("Food categories player accepts. Empty = accepts all.")]
    [SerializeField] private FoodType.FoodCategory[] acceptedFoodCategories;

    [Header("Feeding")]
    [Tooltip("Offset from player position for food selection popup")]
    [SerializeField] private Vector3 feedPopupOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private float currentHunger;
    private bool hasStarved = false;
    private HungerState currentState = HungerState.Satisfied;

    // Properties
    public float CurrentHunger => currentHunger;
    public float MaxHunger => maxHunger;
    public float HungerPercentage => maxHunger > 0 ? currentHunger / maxHunger : 0f;
    public HungerState CurrentState => currentState;
    public bool IsHungry => currentHunger >= maxHunger * hungryThreshold;
    public bool IsStarving => currentHunger >= maxHunger * starvingThreshold;
    public bool HasStarved => hasStarved;

    // Events
    public event Action<float, float> OnHungerChanged;
    public event Action<HungerState> OnStateChanged;
    public event Action OnStarvation;
    public event Action OnBecameHungry;
    public event Action OnBecameStarving;
    public event Action<ConsumableData, float> OnFed;

    public enum HungerState
    {
        Satisfied,  // 0% to hungryThreshold
        Hungry,     // hungryThreshold to starvingThreshold
        Starving    // starvingThreshold to 100%
    }

    #region IFeedable Implementation

    public string FeedableName => "Player";

    public Vector3 FeedPopupAnchor => transform.position + feedPopupOffset;

    public bool CanAcceptFood(ConsumableData consumable)
    {
        if (consumable == null) return false;

        // Check if player has died
        if (hasStarved) return false;

        // Check diet restrictions
        if (acceptedFoodCategories != null && acceptedFoodCategories.Length > 0)
        {
            foreach (var category in acceptedFoodCategories)
            {
                if (consumable.Category == category)
                    return true;
            }
            return false;
        }

        // Default: accept all food
        return true;
    }

    public float ReceiveFood(ConsumableData consumable, GameObject feeder)
    {
        if (consumable == null || hasStarved)
        {
            return 0f;
        }

        float nutritionValue = consumable.NutritionValue;
        
        float oldHunger = currentHunger;
        currentHunger -= nutritionValue;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        float actualReduction = oldHunger - currentHunger;

        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnFed?.Invoke(consumable, actualReduction);

        if (debugLog)
        {
            Debug.Log($"[PlayerHungerSystem] Player ate {consumable.Name}. " +
                      $"Reduced hunger by {actualReduction:F1}. " +
                      $"Hunger: {oldHunger:F1} -> {currentHunger:F1}/{maxHunger}");
        }

        return actualReduction;
    }

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        currentHunger = maxHunger * startingHungerFraction;
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else
        {
            Debug.LogError("[PlayerHungerSystem] TickManager not found! Hunger will not increase.");
        }

        // Register with FeedingSystem
        if (FeedingSystem.Instance != null)
        {
            FeedingSystem.Instance.RegisterFeedable(this);
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
            if (debugLog) Debug.Log("[PlayerHungerSystem] Registered with FeedingSystem (delayed)");
        }
    }

    void OnDestroy()
    {
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }

        // Unregister from FeedingSystem
        if (FeedingSystem.Instance != null)
        {
            FeedingSystem.Instance.UnregisterFeedable(this);
        }
    }

    #endregion

    #region Tick System

    public void OnTickUpdate(int currentTick)
    {
        if (hasStarved) return;

        if (RunManager.HasInstance && RunManager.Instance.CurrentState != RunState.GrowthAndThreat)
        {
            return;
        }

        currentHunger += hungerIncreasePerTick;
        currentHunger = Mathf.Min(currentHunger, maxHunger);

        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        if (currentHunger >= maxHunger)
        {
            hasStarved = true;
            OnStarvation?.Invoke();
            Debug.LogWarning("[PlayerHungerSystem] Player has starved!");
        }
    }

    #endregion

    #region State Management

    void UpdateHungerState()
    {
        HungerState newState;

        if (currentHunger >= maxHunger * starvingThreshold)
        {
            newState = HungerState.Starving;
        }
        else if (currentHunger >= maxHunger * hungryThreshold)
        {
            newState = HungerState.Hungry;
        }
        else
        {
            newState = HungerState.Satisfied;
        }

        if (newState != currentState)
        {
            HungerState oldState = currentState;
            currentState = newState;

            OnStateChanged?.Invoke(newState);

            if (newState == HungerState.Hungry && oldState == HungerState.Satisfied)
            {
                OnBecameHungry?.Invoke();
            }
            else if (newState == HungerState.Starving)
            {
                OnBecameStarving?.Invoke();
            }

            if (debugLog)
            {
                Debug.Log($"[PlayerHungerSystem] State changed: {oldState} -> {newState}");
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Legacy eat method - use IFeedable.ReceiveFood for new code
    /// </summary>
    public void Eat(float nutritionValue)
    {
        if (hasStarved || nutritionValue <= 0) return;

        float oldHunger = currentHunger;
        currentHunger -= nutritionValue;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        UpdateHungerState();

        Debug.Log($"[PlayerHungerSystem] Player ate food. Reduced hunger by {nutritionValue}. " +
            $"Hunger: {oldHunger:F1} -> {currentHunger:F1}/{maxHunger}");

        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void SetHunger(float value)
    {
        currentHunger = Mathf.Clamp(value, 0, maxHunger);
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void ResetHunger()
    {
        currentHunger = maxHunger * startingHungerFraction;
        hasStarved = false;
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    #endregion
}
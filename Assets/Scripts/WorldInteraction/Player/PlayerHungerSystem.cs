// FILE: Assets/Scripts/WorldInteraction/Player/PlayerHungerSystem.cs
using System;
using UnityEngine;
using WegoSystem;

/// <summary>
/// Player hunger system where hunger INCREASES over time.
/// When hunger reaches max, bad things happen (starvation).
/// Eating DECREASES hunger by the nutrition value.
/// </summary>
public class PlayerHungerSystem : MonoBehaviour, ITickUpdateable {
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

    // Runtime state
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

    public enum HungerState {
        Satisfied,  // 0% to hungryThreshold
        Hungry,     // hungryThreshold to starvingThreshold
        Starving    // starvingThreshold to 100%
    }

    void Start() {
        // maxHunger is now solely controlled by this component's inspector
        currentHunger = maxHunger * startingHungerFraction;
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        if (TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else {
            Debug.LogError("[PlayerHungerSystem] TickManager not found! Hunger will not increase.");
        }
    }

    void OnDestroy() {
        var tickManager = TickManager.Instance;
        if (tickManager != null) {
            tickManager.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (hasStarved) return;

        // Only increase hunger during active gameplay
        if (RunManager.HasInstance && RunManager.Instance.CurrentState != RunState.GrowthAndThreat) {
            return;
        }

        // Increase hunger over time
        currentHunger += hungerIncreasePerTick;
        currentHunger = Mathf.Min(currentHunger, maxHunger);

        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        // Check for starvation (reached max hunger)
        if (currentHunger >= maxHunger) {
            hasStarved = true;
            OnStarvation?.Invoke();
            Debug.LogWarning("[PlayerHungerSystem] Player has starved!");
        }
    }

    private void UpdateHungerState() {
        HungerState newState;

        if (currentHunger >= maxHunger * starvingThreshold) {
            newState = HungerState.Starving;
        }
        else if (currentHunger >= maxHunger * hungryThreshold) {
            newState = HungerState.Hungry;
        }
        else {
            newState = HungerState.Satisfied;
        }

        if (newState != currentState) {
            HungerState oldState = currentState;
            currentState = newState;

            OnStateChanged?.Invoke(newState);

            // Fire specific events for state transitions
            if (newState == HungerState.Hungry && oldState == HungerState.Satisfied) {
                OnBecameHungry?.Invoke();
            }
            else if (newState == HungerState.Starving) {
                OnBecameStarving?.Invoke();
            }

            Debug.Log($"[PlayerHungerSystem] State changed: {oldState} -> {newState}");
        }
    }

    /// <summary>
    /// Eat food to DECREASE hunger by the nutrition value.
    /// </summary>
    public void Eat(float nutritionValue) {
        if (hasStarved || nutritionValue <= 0) return;

        float oldHunger = currentHunger;
        currentHunger -= nutritionValue;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        UpdateHungerState();

        Debug.Log($"[PlayerHungerSystem] Player ate food. Reduced hunger by {nutritionValue}. " +
                  $"Hunger: {oldHunger:F1} -> {currentHunger:F1}/{maxHunger}");

        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    /// <summary>
    /// Directly set hunger value (for debugging or special events).
    /// </summary>
    public void SetHunger(float value) {
        currentHunger = Mathf.Clamp(value, 0, maxHunger);
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    /// <summary>
    /// Reset hunger to starting value (for new rounds).
    /// </summary>
    public void ResetHunger() {
        currentHunger = maxHunger * startingHungerFraction;
        hasStarved = false;
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    /// <summary>
    /// Modify hunger directly (positive = increase, negative = decrease).
    /// </summary>
    public void ModifyHunger(float amount) {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        UpdateHungerState();
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }
}
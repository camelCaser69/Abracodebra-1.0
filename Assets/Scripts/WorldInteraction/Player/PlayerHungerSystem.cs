using System;
using UnityEngine;
using WegoSystem;

public class PlayerHungerSystem : MonoBehaviour, ITickUpdateable
{
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float startingHunger = 100f;
    [SerializeField] private float hungerDepletionPerTick = 0.1f; // Default: 1 hunger per 10 ticks

    public float CurrentHunger { get; private set; }
    public float MaxHunger => maxHunger;

    public event Action<float, float> OnHungerChanged; // current, max
    public event Action OnStarvation;

    private bool hasStarved = false;

    void Start()
    {
        CurrentHunger = startingHunger;
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else
        {
            Debug.LogError("[PlayerHungerSystem] TickManager not found! Hunger will not deplete.");
        }
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (hasStarved) return;

        CurrentHunger -= hungerDepletionPerTick;
        CurrentHunger = Mathf.Max(0, CurrentHunger);

        OnHungerChanged?.Invoke(CurrentHunger, maxHunger);

        if (CurrentHunger <= 0)
        {
            hasStarved = true;
            OnStarvation?.Invoke();
            Debug.LogWarning("Player has starved to death!");
            // Here you would typically trigger a game over state via the RunManager
        }
    }

    public void Eat(float nutritionValue)
    {
        if (hasStarved || nutritionValue <= 0) return;

        CurrentHunger += nutritionValue;
        CurrentHunger = Mathf.Clamp(CurrentHunger, 0, maxHunger);

        Debug.Log($"Player ate food. Restored {nutritionValue} hunger. Current hunger: {CurrentHunger}/{maxHunger}");
        OnHungerChanged?.Invoke(CurrentHunger, maxHunger);
    }
}
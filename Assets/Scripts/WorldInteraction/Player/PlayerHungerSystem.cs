using UnityEngine;
using System;
using WegoSystem;

public class PlayerHungerSystem : MonoBehaviour, ITickUpdateable
{
    // MODIFIED: No longer serialized, will be set by RunManager
    private float maxHunger; 
    
    // MODIFIED: Starting hunger is now a fraction of the max hunger
    [Tooltip("What fraction of max hunger the player starts with (e.g., 1.0 for 100%).")]
    [SerializeField] [Range(0f, 1f)] private float startingHungerFraction = 1.0f;
    
    [SerializeField] private float hungerDepletionPerTick = 0.1f;

    public float CurrentHunger { get; private set; }
    public float MaxHunger => maxHunger;

    public event Action<float, float> OnHungerChanged;
    public event Action OnStarvation;

    private bool hasStarved = false;

    private void Start()
    {
        // Get max hunger from the central RunManager.
        if (RunManager.HasInstance)
        {
            maxHunger = RunManager.Instance.playerMaxHunger;
        }
        else
        {
            Debug.LogError("[PlayerHungerSystem] RunManager not found! Defaulting max hunger to 100.");
            maxHunger = 100f;
        }

        CurrentHunger = maxHunger * startingHungerFraction;
        OnHungerChanged?.Invoke(CurrentHunger, maxHunger);

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else
        {
            Debug.LogError("[PlayerHungerSystem] TickManager not found! Hunger will not deplete.");
        }
    }

    private void OnDestroy()
    {
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (hasStarved || (RunManager.HasInstance && RunManager.Instance.CurrentState != RunState.GrowthAndThreat))
        {
            return;
        }

        CurrentHunger -= hungerDepletionPerTick;
        CurrentHunger = Mathf.Max(0, CurrentHunger);

        OnHungerChanged?.Invoke(CurrentHunger, maxHunger);

        if (CurrentHunger <= 0)
        {
            hasStarved = true;
            OnStarvation?.Invoke();
            Debug.LogWarning("Player has starved!");
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
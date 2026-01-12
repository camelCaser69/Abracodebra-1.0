// FILE: Assets/Scripts/Ecosystem/Doris/DorisHungerSystem.cs
using System;
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Ecosystem {
    /// <summary>
    /// Manages Doris's hunger state. Hunger INCREASES over time (she gets hungrier).
    /// Feeding DECREASES hunger. When starving, Doris will eat nearby plants.
    /// 
    /// This is intentionally different from PlayerHungerSystem which works inversely
    /// (player's hunger decreases over time, eating refills it).
    /// </summary>
    public class DorisHungerSystem : MonoBehaviour, ITickUpdateable {
        [Header("Configuration")]
        [SerializeField] private DorisDefinition definition;

        [Header("Runtime State (Debug)")]
        [SerializeField] private float currentHunger = 0f;
        [SerializeField] private HungerState currentState = HungerState.Satisfied;

        // Events for other systems to react
        public event Action<float, float> OnHungerChanged;           // (currentHunger, maxHunger)
        public event Action<HungerState, HungerState> OnStateChanged; // (oldState, newState)
        public event Action OnBecameHungry;                           // Crossed into hungry zone
        public event Action OnBecameStarving;                         // Crossed into starving zone
        public event Action OnBecameSatisfied;                        // Fed enough to exit hungry/starving
        public event Action OnStarvationTick;                         // Fires each tick while starving
        public event Action<float> OnFed;                             // (amountFed) when feeding occurs

        // Properties
        public float CurrentHunger => currentHunger;
        public float MaxHunger => definition != null ? definition.maxHunger : 100f;
        public float HungerPercent => definition != null ? currentHunger / definition.maxHunger : 0f;
        public HungerState CurrentState => currentState;
        public DorisDefinition Definition => definition;

        // Convenience properties
        public bool IsSatisfied => currentState == HungerState.Satisfied;
        public bool IsHungry => currentState == HungerState.Hungry || currentState == HungerState.Starving;
        public bool IsStarving => currentState == HungerState.Starving;

        private bool isInitialized = false;

        public enum HungerState {
            Satisfied,  // Low hunger - Doris is happy
            Hungry,     // Medium hunger - visual/audio warnings
            Starving    // High hunger - will eat plants
        }

        private void Start() {
            Initialize();
        }

        public void Initialize() {
            if (isInitialized) return;

            if (definition == null) {
                Debug.LogError("[DorisHungerSystem] No DorisDefinition assigned! Creating default values.");
                return;
            }

            currentHunger = 0f;
            currentState = HungerState.Satisfied;

            if (TickManager.Instance != null) {
                TickManager.Instance.RegisterTickUpdateable(this);
                Debug.Log("[DorisHungerSystem] Registered with TickManager.");
            } else {
                Debug.LogError("[DorisHungerSystem] TickManager not found! Hunger will not increase.");
            }

            isInitialized = true;
            OnHungerChanged?.Invoke(currentHunger, definition.maxHunger);
        }

        public void Initialize(DorisDefinition newDefinition) {
            definition = newDefinition;
            isInitialized = false;
            Initialize();
        }

        private void OnDestroy() {
            if (TickManager.Instance != null) {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }
        }

        public void OnTickUpdate(int currentTick) {
            if (definition == null) return;

            // Only increase hunger during Growth & Threat phase
            if (RunManager.HasInstance && RunManager.Instance.CurrentState != RunState.GrowthAndThreat) {
                return;
            }

            // Increase hunger each tick
            float previousHunger = currentHunger;
            currentHunger += definition.hungerPerTick;
            currentHunger = Mathf.Clamp(currentHunger, 0f, definition.maxHunger);

            // Check for state changes
            UpdateHungerState();

            // Fire hunger changed event if value actually changed
            if (!Mathf.Approximately(previousHunger, currentHunger)) {
                OnHungerChanged?.Invoke(currentHunger, definition.maxHunger);
            }

            // Fire starvation tick event when starving
            if (currentState == HungerState.Starving) {
                OnStarvationTick?.Invoke();
            }
        }

        private void UpdateHungerState() {
            HungerState newState = CalculateState();

            if (newState != currentState) {
                HungerState oldState = currentState;
                currentState = newState;

                OnStateChanged?.Invoke(oldState, newState);

                // Fire specific transition events
                switch (newState) {
                    case HungerState.Hungry when oldState == HungerState.Satisfied:
                        OnBecameHungry?.Invoke();
                        Debug.Log("[DorisHungerSystem] Doris is getting hungry!");
                        break;

                    case HungerState.Starving:
                        OnBecameStarving?.Invoke();
                        Debug.LogWarning("[DorisHungerSystem] Doris is STARVING! She will start eating plants!");
                        break;

                    case HungerState.Satisfied when oldState != HungerState.Satisfied:
                        OnBecameSatisfied?.Invoke();
                        Debug.Log("[DorisHungerSystem] Doris is satisfied!");
                        break;
                }
            }
        }

        private HungerState CalculateState() {
            if (definition == null) return HungerState.Satisfied;

            float hungerPercent = currentHunger / definition.maxHunger;

            if (hungerPercent >= definition.starvingThreshold) {
                return HungerState.Starving;
            }
            if (hungerPercent >= definition.hungryThreshold) {
                return HungerState.Hungry;
            }
            return HungerState.Satisfied;
        }

        /// <summary>
        /// Feed Doris to reduce her hunger.
        /// </summary>
        /// <param name="nutritionValue">Base nutrition value of the food.</param>
        /// <returns>Actual amount of hunger reduced.</returns>
        public float Feed(float nutritionValue) {
            if (definition == null || nutritionValue <= 0) return 0f;

            float effectiveNutrition = nutritionValue * definition.feedingEfficiency;
            float previousHunger = currentHunger;

            currentHunger -= effectiveNutrition;
            currentHunger = Mathf.Clamp(currentHunger, 0f, definition.maxHunger);

            float actualReduction = previousHunger - currentHunger;

            Debug.Log($"[DorisHungerSystem] Fed {nutritionValue:F1} (effective: {effectiveNutrition:F1}). " +
                      $"Hunger: {currentHunger:F1}/{definition.maxHunger:F0}");

            OnFed?.Invoke(actualReduction);
            OnHungerChanged?.Invoke(currentHunger, definition.maxHunger);
            UpdateHungerState();

            return actualReduction;
        }

        /// <summary>
        /// Called when Doris eats a plant (starvation behavior).
        /// Reduces hunger by a smaller amount than player feeding.
        /// </summary>
        public void OnAtePlant() {
            if (definition == null) return;

            float previousHunger = currentHunger;
            currentHunger -= definition.hungerReductionFromPlant;
            currentHunger = Mathf.Clamp(currentHunger, 0f, definition.maxHunger);

            Debug.Log($"[DorisHungerSystem] Doris ate a plant! Hunger reduced by {definition.hungerReductionFromPlant:F1}. " +
                      $"Hunger: {currentHunger:F1}/{definition.maxHunger:F0}");

            OnHungerChanged?.Invoke(currentHunger, definition.maxHunger);
            UpdateHungerState();
        }

        /// <summary>
        /// Reset hunger to zero (e.g., at start of new round).
        /// </summary>
        public void ResetHunger() {
            currentHunger = 0f;
            UpdateHungerState();
            OnHungerChanged?.Invoke(currentHunger, definition?.maxHunger ?? 100f);
            Debug.Log("[DorisHungerSystem] Hunger reset to 0.");
        }

        /// <summary>
        /// Set hunger to a specific value (for testing/debugging).
        /// </summary>
        public void SetHunger(float value) {
            if (definition == null) return;

            currentHunger = Mathf.Clamp(value, 0f, definition.maxHunger);
            UpdateHungerState();
            OnHungerChanged?.Invoke(currentHunger, definition.maxHunger);
        }

        private void OnValidate() {
            if (definition != null) {
                currentHunger = Mathf.Clamp(currentHunger, 0f, definition.maxHunger);
            }
        }
    }
}
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

namespace WegoSystem {
    public enum TurnPhase {
        Planning,
        Execution
    }

    public class TurnPhaseManager : MonoBehaviour, ITickUpdateable {
        public static TurnPhaseManager Instance { get; private set; }

        [SerializeField] TurnPhase currentPhase = TurnPhase.Planning;
        [SerializeField] int currentPhaseTicks = 0;
        [SerializeField] bool debugMode = false;
        
        // Track if player has moves to process
        [SerializeField] bool autoAdvanceTicks = true;
        [SerializeField] float tickInterval = 0.5f; // Time between automatic ticks during execution
        
        float tickTimer = 0f;

        public TurnPhase CurrentPhase => currentPhase;
        public int CurrentPhaseTicks => currentPhaseTicks;
        public bool IsInPlanningPhase => currentPhase == TurnPhase.Planning;
        public bool IsInExecutionPhase => currentPhase == TurnPhase.Execution;

        public event Action<TurnPhase, TurnPhase> OnPhaseChanged;
        public event Action OnPlanningPhaseStarted;
        public event Action OnExecutionPhaseStarted;

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start() {
            if (TickManager.Instance != null) {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
            else {
                Debug.LogError("[TurnPhaseManager] TickManager not found!");
            }

            TransitionToPhase(TurnPhase.Planning);
        }

        void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }

            if (TickManager.Instance != null) {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }
        }

        void Update() {
            // During execution phase, automatically advance ticks
            if (currentPhase == TurnPhase.Execution && autoAdvanceTicks) {
                tickTimer += Time.deltaTime;
                
                if (tickTimer >= tickInterval) {
                    tickTimer = 0f;
                    
                    // Check if anyone has actions to process
                    if (HasActionsToProcess()) {
                        TickManager.Instance?.AdvanceTick();
                    } else {
                        // No more actions, return to planning
                        TransitionToPhase(TurnPhase.Planning);
                    }
                }
            }
        }

        public void OnTickUpdate(int currentTick) {
            currentPhaseTicks++;
            
            // Don't auto-transition based on tick count alone
            // Let the system handle it based on actions
        }

        bool HasActionsToProcess() {
            // Check player moves
            var gardeners = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);
            foreach (var gardener in gardeners) {
                if (gardener.GetQueuedMoveCount() > 0) return true;
            }
            
            // Check animal moves
            var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
            foreach (var animal in animals) {
                // Animals process every thinking interval
                if (currentPhaseTicks % animal.thinkingTickInterval == 0) return true;
            }
            
            // Check plants
            var plants = PlantGrowth.AllActivePlants;
            if (plants.Count > 0) return true; // Plants always need processing
            
            return false;
        }

        public void EndPlanningPhase() {
            if (currentPhase == TurnPhase.Planning) {
                TransitionToPhase(TurnPhase.Execution);
            }
            else {
                Debug.LogWarning($"[TurnPhaseManager] Cannot end planning phase - current phase is {currentPhase}");
            }
        }

        public void TransitionToPhase(TurnPhase newPhase) {
            if (currentPhase == newPhase) return;

            TurnPhase oldPhase = currentPhase;
            currentPhase = newPhase;
            currentPhaseTicks = 0;
            tickTimer = 0f;

            switch (newPhase) {
                case TurnPhase.Planning:
                    OnPlanningPhaseStarted?.Invoke();
                    break;

                case TurnPhase.Execution:
                    OnExecutionPhaseStarted?.Invoke();
                    break;
            }

            OnPhaseChanged?.Invoke(oldPhase, newPhase);

            if (debugMode) {
                Debug.Log($"[TurnPhaseManager] Phase transition: {oldPhase} -> {newPhase}");
            }
        }

        public void ForcePhase(TurnPhase phase) {
            if (Application.isEditor || Debug.isDebugBuild) {
                TransitionToPhase(phase);
            }
        }

        public float GetPhaseProgress() {
            return currentPhase == TurnPhase.Execution ? 1f : 0f;
        }

        public int GetRemainingPhaseTicks() {
            return -1;
        }
    }
}
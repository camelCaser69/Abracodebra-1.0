using System;
using UnityEngine;

namespace WegoSystem {
    public enum TurnPhase {
        Planning,
        Execution,
        Resolution
    }

    public class TurnPhaseManager : MonoBehaviour, ITickUpdateable {
        public static TurnPhaseManager Instance { get; private set; }

        [SerializeField] TurnPhase currentPhase = TurnPhase.Planning;
        [SerializeField] bool autoAdvancePhases = false;
        [SerializeField] int planningPhaseStartTick = 0;
        [SerializeField] int currentPhaseTicks = 0;

        [SerializeField] bool debugMode = false;

        public TurnPhase CurrentPhase => currentPhase;
        public int CurrentPhaseTicks => currentPhaseTicks;
        public bool IsInPlanningPhase => currentPhase == TurnPhase.Planning;
        public bool IsInExecutionPhase => currentPhase == TurnPhase.Execution;
        public bool IsInResolutionPhase => currentPhase == TurnPhase.Resolution;

        public event Action<TurnPhase, TurnPhase> OnPhaseChanged; // oldPhase, newPhase
        public event Action OnPlanningPhaseStarted;
        public event Action OnExecutionPhaseStarted;
        public event Action OnResolutionPhaseStarted;
        public event Action OnTurnCycleCompleted;

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

            EnterPhase(TurnPhase.Planning);
        }

        void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }

            if (TickManager.Instance != null) {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }
        }

        public void OnTickUpdate(int currentTick) {
            currentPhaseTicks++;
    
            // Remove automatic phase advancement
            // if (autoAdvancePhases) {
            //     CheckAutoAdvance();
            // }
        }

        void CheckAutoAdvance() {
            if (TickManager.Instance?.Config == null) return;

            var config = TickManager.Instance.Config;
            bool shouldAdvance = false;

            switch (currentPhase) {
                case TurnPhase.Planning:
                    if (config.maxPlanningPhaseTicks > 0 && currentPhaseTicks >= config.maxPlanningPhaseTicks) {
                        shouldAdvance = true;
                    }
                    break;

                case TurnPhase.Execution:
                    if (currentPhaseTicks >= config.executionPhaseTicks) {
                        shouldAdvance = true;
                    }
                    break;

                case TurnPhase.Resolution:
                    if (currentPhaseTicks >= config.resolutionPhaseTicks) {
                        shouldAdvance = true;
                    }
                    break;
            }

            if (shouldAdvance) {
                AdvanceToNextPhase();
            }
        }

        public void AdvanceToNextPhase() {
            TurnPhase nextPhase = GetNextPhase(currentPhase);
            TransitionToPhase(nextPhase);
        }

        public void TransitionToPhase(TurnPhase newPhase) {
            if (currentPhase == newPhase) return;

            TurnPhase oldPhase = currentPhase;

            ExitPhase(oldPhase);

            EnterPhase(newPhase);

            OnPhaseChanged?.Invoke(oldPhase, newPhase);

            if (debugMode) {
                Debug.Log($"[TurnPhaseManager] Phase transition: {oldPhase} -> {newPhase}");
            }

            if (oldPhase == TurnPhase.Resolution && newPhase == TurnPhase.Planning) {
                OnTurnCycleCompleted?.Invoke();
                if (debugMode) {
                    Debug.Log("[TurnPhaseManager] Turn cycle completed");
                }
            }
        }

        void EnterPhase(TurnPhase phase) {
            currentPhase = phase;
            currentPhaseTicks = 0;
            planningPhaseStartTick = TickManager.Instance?.CurrentTick ?? 0;

            switch (phase) {
                case TurnPhase.Planning:
                    OnPlanningPhaseStarted?.Invoke();
                    // Remove tick control - ticks only advance by player action
                    // if (TickManager.Instance != null && !autoAdvancePhases) {
                    //     TickManager.Instance.StopTicking();
                    // }
                    break;

                case TurnPhase.Execution:
                    OnExecutionPhaseStarted?.Invoke();
                    // Remove tick control
                    // if (TickManager.Instance != null) {
                    //     TickManager.Instance.StartTicking();
                    // }
                    break;

                case TurnPhase.Resolution:
                    OnResolutionPhaseStarted?.Invoke();
                    break;
            }
        }

        void ExitPhase(TurnPhase phase) {
            switch (phase) {
                case TurnPhase.Planning:
                    break;

                case TurnPhase.Execution:
                    break;

                case TurnPhase.Resolution:
                    break;
            }
        }

        TurnPhase GetNextPhase(TurnPhase current) {
            switch (current) {
                case TurnPhase.Planning:
                    return TurnPhase.Execution;
                case TurnPhase.Execution:
                    return TurnPhase.Resolution;
                case TurnPhase.Resolution:
                    return TurnPhase.Planning;
                default:
                    return TurnPhase.Planning;
            }
        }

        public void EndPlanningPhase() {
            if (currentPhase == TurnPhase.Planning) {
                TransitionToPhase(TurnPhase.Execution);
            }
            else {
                Debug.LogWarning($"[TurnPhaseManager] Cannot end planning phase - current phase is {currentPhase}");
            }
        }

        public void ForcePhase(TurnPhase phase) {
            if (Application.isEditor || Debug.isDebugBuild) {
                TransitionToPhase(phase);
            }
        }

        public float GetPhaseProgress() {
            if (TickManager.Instance?.Config == null) return 0f;

            var config = TickManager.Instance.Config;
            int maxTicks = 0;

            switch (currentPhase) {
                case TurnPhase.Planning:
                    maxTicks = config.maxPlanningPhaseTicks;
                    if (maxTicks <= 0) return 0f; // Unlimited planning time
                    break;
                case TurnPhase.Execution:
                    maxTicks = config.executionPhaseTicks;
                    break;
                case TurnPhase.Resolution:
                    maxTicks = config.resolutionPhaseTicks;
                    break;
            }

            return maxTicks > 0 ? (float)currentPhaseTicks / maxTicks : 0f;
        }

        public int GetRemainingPhaseTicks() {
            if (TickManager.Instance?.Config == null) return -1;

            var config = TickManager.Instance.Config;
            int maxTicks = 0;

            switch (currentPhase) {
                case TurnPhase.Planning:
                    maxTicks = config.maxPlanningPhaseTicks;
                    if (maxTicks <= 0) return -1; // Unlimited
                    break;
                case TurnPhase.Execution:
                    maxTicks = config.executionPhaseTicks;
                    break;
                case TurnPhase.Resolution:
                    maxTicks = config.resolutionPhaseTicks;
                    break;
            }

            return Mathf.Max(0, maxTicks - currentPhaseTicks);
        }
    }
}
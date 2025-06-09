// Assets\Scripts\Ticks\TurnPhaseManager.cs

using System;
using UnityEngine;

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

            // FIXED: Changed from EnterPhase to TransitionToPhase
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

        public void OnTickUpdate(int currentTick) {
            currentPhaseTicks++;

            if (currentPhase == TurnPhase.Execution && AllEntitiesActed()) {
                TransitionToPhase(TurnPhase.Planning);
            }
        }

        bool AllEntitiesActed() {
            var gardeners = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);
            foreach (var gardener in gardeners) {
                if (gardener.GetQueuedMoveCount() > 0) return false;
            }

            return true;
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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WegoSystem {
    public interface ITickUpdateable {
        void OnTickUpdate(int currentTick);
    }

    public class TickManager : MonoBehaviour {
        public static TickManager Instance { get; private set; }

        [SerializeField] TickConfiguration tickConfig;
        [SerializeField] bool autoAdvanceTicks = true;
        [SerializeField] bool pauseOnPlanningPhase = true;

        [SerializeField] bool debugMode = false;
        [SerializeField] int currentTick = 0;
        [SerializeField] float tickAccumulator = 0f;

        public int CurrentTick => currentTick;
        public TickConfiguration Config => tickConfig;
        public bool IsRunning { get; private set; }

        public event Action<int> OnTickAdvanced;
        public event Action<int> OnTickStarted;
        public event Action<int> OnTickCompleted;

        readonly List<ITickUpdateable> tickUpdateables = new List<ITickUpdateable>();
        readonly List<ITickUpdateable> pendingAdditions = new List<ITickUpdateable>();
        readonly List<ITickUpdateable> pendingRemovals = new List<ITickUpdateable>();
        bool isProcessingTick = false;

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (tickConfig == null) {
                Debug.LogError("[TickManager] No TickConfiguration assigned! Creating default config.");
                tickConfig = ScriptableObject.CreateInstance<TickConfiguration>();
            }

            IsRunning = autoAdvanceTicks;
        }

        void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        void Update() {
            if (!IsRunning || tickConfig == null) return;

            if (TurnPhaseManager.Instance != null && pauseOnPlanningPhase) {
                if (TurnPhaseManager.Instance.CurrentPhase == TurnPhase.Planning) {
                    return; // Don't auto-advance during planning
                }
            }

            tickAccumulator += Time.deltaTime;
            float secondsPerTick = tickConfig.GetRealSecondsPerTick();

            while (tickAccumulator >= secondsPerTick) {
                tickAccumulator -= secondsPerTick;
                AdvanceTick();
            }
        }

        public void AdvanceTick() {
            AdvanceMultipleTicks(1);
        }

        public void AdvanceMultipleTicks(int tickCount) {
            if (tickCount <= 0) return;

            for (int i = 0; i < tickCount; i++) {
                currentTick++;
                ProcessTick();
            }
        }

        void ProcessTick() {
            if (debugMode) {
                Debug.Log($"[TickManager] Processing tick {currentTick}");
            }

            OnTickStarted?.Invoke(currentTick);

            ProcessPendingUpdates();

            isProcessingTick = true;
            foreach (var tickUpdateable in tickUpdateables) {
                try {
                    tickUpdateable?.OnTickUpdate(currentTick);
                }
                catch (Exception e) {
                    Debug.LogError($"[TickManager] Error in tick update: {e.Message}");
                }
            }
            isProcessingTick = false;

            OnTickAdvanced?.Invoke(currentTick);
            OnTickCompleted?.Invoke(currentTick);
        }

        public void RegisterTickUpdateable(ITickUpdateable updateable) {
            if (updateable == null) return;

            if (isProcessingTick) {
                if (!pendingAdditions.Contains(updateable))
                    pendingAdditions.Add(updateable);
            }
            else {
                if (!tickUpdateables.Contains(updateable))
                    tickUpdateables.Add(updateable);
            }
        }

        public void UnregisterTickUpdateable(ITickUpdateable updateable) {
            if (updateable == null) return;

            if (isProcessingTick) {
                if (!pendingRemovals.Contains(updateable))
                    pendingRemovals.Add(updateable);
            }
            else {
                tickUpdateables.Remove(updateable);
            }
        }

        void ProcessPendingUpdates() {
            foreach (var updateable in pendingAdditions) {
                if (!tickUpdateables.Contains(updateable))
                    tickUpdateables.Add(updateable);
            }
            pendingAdditions.Clear();

            foreach (var updateable in pendingRemovals) {
                tickUpdateables.Remove(updateable);
            }
            pendingRemovals.Clear();
        }

        public void StartTicking() {
            IsRunning = true;
            if (debugMode) Debug.Log("[TickManager] Started ticking");
        }

        public void StopTicking() {
            IsRunning = false;
            tickAccumulator = 0f;
            if (debugMode) Debug.Log("[TickManager] Stopped ticking");
        }

        public void ResetTicks() {
            currentTick = 0;
            tickAccumulator = 0f;
            if (debugMode) Debug.Log("[TickManager] Reset tick counter");
        }

        public void SetTickSpeed(float multiplier) {
            if (tickConfig != null) {
                tickConfig.SetTicksPerSecond(Mathf.Max(0.1f, multiplier * 2f)); // Base is 2 ticks/second
            }
        }

        public int GetTicksSince(int pastTick) {
            return currentTick - pastTick;
        }

        public bool HasTicksPassed(int lastTick, int tickInterval) {
            return GetTicksSince(lastTick) >= tickInterval;
        }

        public int GetNextIntervalTick(int tickInterval) {
            return currentTick + tickInterval;
        }

        public void DebugAdvanceTick() {
            if (Application.isEditor || Debug.isDebugBuild) {
                AdvanceTick();
            }
        }

        public int GetRegisteredUpdateableCount() {
            return tickUpdateables.Count;
        }
    }
}
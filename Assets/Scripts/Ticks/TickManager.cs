// Assets/Scripts/Ticks/TickManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WegoSystem {
    public interface ITickUpdateable {
        void OnTickUpdate(int currentTick);
    }

    public class TickManager : SingletonMonoBehaviour<TickManager> {
        [SerializeField] TickConfiguration tickConfig;
        [SerializeField] bool debugMode = false;
        [SerializeField] int currentTick = 0;

        public int CurrentTick => currentTick;
        public TickConfiguration Config => tickConfig;

        public event Action<int> OnTickAdvanced;
        public event Action<int> OnTickStarted;
        public event Action<int> OnTickCompleted;

        readonly List<ITickUpdateable> tickUpdateables = new List<ITickUpdateable>();
        readonly List<ITickUpdateable> pendingAdditions = new List<ITickUpdateable>();
        readonly List<ITickUpdateable> pendingRemovals = new List<ITickUpdateable>();
        bool isProcessingTick = false;

        // ---- A1: Centralized tick authority -------------------------------------
        // Planning  -> player actions drive time (Stoneshard-style).
        // Growth&Threat -> ExecutionPhaseDriver advances time automatically, so
        // player actions must NOT advance ticks (otherwise a move/feed double-advances).
        public bool ActionsDriveTicks {
            get {
                if (RunManager.Instance == null) return true; // pre-game default = action-driven
                return RunManager.Instance.CurrentState == RunState.Planning;
            }
        }

        // Single entry point for player-action-driven time. Every gameplay action
        // that used to call AdvanceTick() directly should call this instead.
        public void RequestActionTicks(int count) {
            if (count <= 0) return;
            if (ActionsDriveTicks) {
                AdvanceMultipleTicks(count);
            }
            // else: time is auto-driven this phase; the action is "free".
        }
        // -------------------------------------------------------------------------

        protected override void OnAwake() {
            if (tickConfig == null) {
                Debug.LogError("[TickManager] No TickConfiguration assigned! Creating default config.");
                tickConfig = ScriptableObject.CreateInstance<TickConfiguration>();
            }
        }

        void OnDestroy() {
            if (Instance == this) {
            }
        }

        void Update() {
            #if UNITY_EDITOR
            if (debugMode && Input.GetKeyDown(KeyCode.T)) {
                Debug.Log("[TickManager] Debug: Manual tick advance");
                AdvanceTick();
            }
            #endif
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

        public void ResetTicks() {
            currentTick = 0;
            if (debugMode) Debug.Log("[TickManager] Reset tick counter");
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

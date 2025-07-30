// Assets/Scripts/Ticks/TickManager.cs
using System;
using UnityEngine;
using System.Collections.Generic;

namespace WegoSystem
{
    public interface ITickUpdateable
    {
        void OnTickUpdate(int currentTick);
    }

    public class TickManager : SingletonMonoBehaviour<TickManager>
    {
        [SerializeField] private TickConfiguration tickConfig;
        [SerializeField] private bool debugMode = false;
        [SerializeField] private int currentTick = 0;

        public int CurrentTick => currentTick;
        public TickConfiguration Config => tickConfig;

        public event Action<int> OnTickAdvanced;
        public event Action<int> OnTickStarted;
        public event Action<int> OnTickCompleted;

        private readonly List<ITickUpdateable> tickUpdateables = new List<ITickUpdateable>();
        private readonly List<ITickUpdateable> pendingAdditions = new List<ITickUpdateable>();
        private readonly List<ITickUpdateable> pendingRemovals = new List<ITickUpdateable>();
        private bool isProcessingTick = false;
        
        protected override void OnAwake()
        {
            if (tickConfig == null)
            {
                Debug.LogError("[TickManager] No TickConfiguration assigned! Creating default config.");
                tickConfig = ScriptableObject.CreateInstance<TickConfiguration>();
            }
        }

        void OnDestroy()
        {
            // This is still needed for when the application quits
            if (Instance == this)
            {
                // Potentially clear static references if needed, though C# handles this on app close
            }
        }
        
        void Update()
        {
            #if UNITY_EDITOR
            if (debugMode && Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("[TickManager] Debug: Manual tick advance");
                AdvanceTick();
            }
            #endif
        }

        public void AdvanceTick()
        {
            AdvanceMultipleTicks(1);
        }
        
        public void AdvanceMultipleTicks(int tickCount)
        {
            if (tickCount <= 0) return;

            for (int i = 0; i < tickCount; i++)
            {
                currentTick++;
                ProcessTick();
            }
        }

        private void ProcessTick()
        {
            if (debugMode)
            {
                Debug.Log($"[TickManager] Processing tick {currentTick}");
            }

            OnTickStarted?.Invoke(currentTick);

            ProcessPendingUpdates();

            isProcessingTick = true;
            foreach (var tickUpdateable in tickUpdateables)
            {
                try
                {
                    tickUpdateable?.OnTickUpdate(currentTick);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TickManager] Error in tick update: {e.Message}");
                }
            }
            isProcessingTick = false;

            OnTickAdvanced?.Invoke(currentTick);
            OnTickCompleted?.Invoke(currentTick);
        }

        public void RegisterTickUpdateable(ITickUpdateable updateable)
        {
            if (updateable == null) return;

            if (isProcessingTick)
            {
                if (!pendingAdditions.Contains(updateable))
                    pendingAdditions.Add(updateable);
            }
            else
            {
                if (!tickUpdateables.Contains(updateable))
                    tickUpdateables.Add(updateable);
            }
        }

        public void UnregisterTickUpdateable(ITickUpdateable updateable)
        {
            if (updateable == null) return;

            if (isProcessingTick)
            {
                if (!pendingRemovals.Contains(updateable))
                    pendingRemovals.Add(updateable);
            }
            else
            {
                tickUpdateables.Remove(updateable);
            }
        }
        
        private void ProcessPendingUpdates()
        {
            foreach (var updateable in pendingAdditions)
            {
                if (!tickUpdateables.Contains(updateable))
                    tickUpdateables.Add(updateable);
            }
            pendingAdditions.Clear();

            foreach (var updateable in pendingRemovals)
            {
                tickUpdateables.Remove(updateable);
            }
            pendingRemovals.Clear();
        }

        public void ResetTicks()
        {
            currentTick = 0;
            if (debugMode) Debug.Log("[TickManager] Reset tick counter");
        }

        public int GetTicksSince(int pastTick)
        {
            return currentTick - pastTick;
        }

        public bool HasTicksPassed(int lastTick, int tickInterval)
        {
            return GetTicksSince(lastTick) >= tickInterval;
        }
        
        public int GetNextIntervalTick(int tickInterval)
        {
            return currentTick + tickInterval;
        }

        public void DebugAdvanceTick()
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                AdvanceTick();
            }
        }
        
        public int GetRegisteredUpdateableCount()
        {
            return tickUpdateables.Count;
        }
    }
}
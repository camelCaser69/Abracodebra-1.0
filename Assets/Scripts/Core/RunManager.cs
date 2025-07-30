// Assets/Scripts/Core/RunManager.cs
using System;
using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    public enum RunState
    {
        Planning,
        GrowthAndThreat
    }

    public enum GamePhase
    {
        Planning,      // Players place nodes/plan strategy
        Execution      // Game executes actions/waves
    }

    public class RunManager : SingletonMonoBehaviour<RunManager>
    {
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private WaveManager waveManager;

        [SerializeField] private RunState currentState = RunState.Planning;
        [SerializeField] private GamePhase currentPhase = GamePhase.Planning;
        [SerializeField] private int currentRoundNumber = 1;
        [SerializeField] private int currentPhaseTicks = 0;

        public RunState CurrentState => currentState;
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentRoundNumber => currentRoundNumber;
        public int CurrentPhaseTicks => currentPhaseTicks;

        public event Action<RunState> OnRunStateChanged;
        public event Action<GamePhase, GamePhase> OnPhaseChanged;
        public event Action<int> OnRoundChanged;
        
        protected override void OnAwake()
        {
            // Initialization logic from the old Awake method.
            SetState(RunState.Planning, true);
        }

        void Start()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(new PhaseTickHandler(this));
            }
        }

        private void SetState(RunState newState, bool force = false)
        {
            if (currentState == newState && !force) return;

            currentState = newState;
            Debug.Log($"[RunManager] State changed to: {currentState}");

            switch (currentState)
            {
                case RunState.Planning:
                    weatherManager?.PauseCycleAtDay();
                    SetPhase(GamePhase.Planning);
                    break;

                case RunState.GrowthAndThreat:
                    weatherManager?.ResumeCycle();
                    waveManager?.StartWaveForRound(currentRoundNumber);
                    SetPhase(GamePhase.Execution);
                    break;
            }

            OnRunStateChanged?.Invoke(currentState);
        }
        
        private void SetPhase(GamePhase newPhase)
        {
            if (currentPhase == newPhase) return;

            GamePhase oldPhase = currentPhase;
            currentPhase = newPhase;
            currentPhaseTicks = 0;

            Debug.Log($"[RunManager] Phase changed: {oldPhase} -> {newPhase}");
            OnPhaseChanged?.Invoke(oldPhase, newPhase);
        }

        public void StartGrowthAndThreatPhase()
        {
            if (currentState == RunState.Planning)
            {
                Debug.Log($"[RunManager] Starting Growth & Threat for Round {currentRoundNumber}");
                SetState(RunState.GrowthAndThreat);
            }
        }

        public void EndPlanningPhase()
        {
            if (currentState == RunState.Planning && currentPhase == GamePhase.Planning)
            {
                SetPhase(GamePhase.Execution);
                StartGrowthAndThreatPhase();
            }
        }

        public void StartNewPlanningPhase()
        {
            Debug.Log("[RunManager] Starting new planning phase");

            if (currentState != RunState.Planning)
            {
                if (waveManager != null && waveManager.IsCurrentWaveDefeated())
                {
                    StartNewRound();
                }
                else
                {
                    SetState(RunState.Planning);
                }
            }
        }

        private void StartNewRound()
        {
            currentRoundNumber++;
            Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");

            waveManager?.ResetForNewRound();
            SetState(RunState.Planning);

            OnRoundChanged?.Invoke(currentRoundNumber);
        }
        
        private class PhaseTickHandler : ITickUpdateable
        {
            private RunManager manager;

            public PhaseTickHandler(RunManager manager)
            {
                this.manager = manager;
            }

            public void OnTickUpdate(int currentTick)
            {
                manager.currentPhaseTicks++;
            }
        }

        public void ForcePhase(GamePhase phase)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                SetPhase(phase);
            }
        }
    }
}
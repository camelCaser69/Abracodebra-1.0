using UnityEngine;
using System;
using WegoSystem;
using UnityEngine.SceneManagement; // Required for restarting the scene

namespace WegoSystem
{
    public enum RunState
    {
        Planning,
        GrowthAndThreat,
        GameOver // NEW: State for when the game has ended
    }

    public enum GamePhase
    {
        Planning,
        Execution
    }

    public class RunManager : SingletonMonoBehaviour<RunManager>
    {
        [Header("Game State")]
        [SerializeField] RunState currentState = RunState.Planning;
        [SerializeField] GamePhase currentPhase = GamePhase.Planning;
        [SerializeField] int currentRoundNumber = 1;
        [SerializeField] int currentPhaseTicks = 0;

        [Header("Player Death")]
        [Tooltip("If checked, the game will end when the player's hunger reaches max.")]
        public bool playerDeathEnabled = true;

        public RunState CurrentState => currentState;
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentRoundNumber => currentRoundNumber;
        public int CurrentPhaseTicks => currentPhaseTicks;

        public event Action<RunState> OnRunStateChanged;
        public event Action<GamePhase, GamePhase> OnPhaseChanged;
        public event Action<int> OnRoundChanged;

        protected override void OnAwake() {
            SetState(RunState.Planning, true);
        }

        public void Initialize() {
            if (TickManager.Instance != null) {
                TickManager.Instance.RegisterTickUpdateable(new PhaseTickHandler(this));
            }
            else {
                Debug.LogError("[RunManager] Initialization failed: TickManager not found!");
            }

            PlayerHungerSystem playerHunger = FindFirstObjectByType<PlayerHungerSystem>();
            if (playerHunger != null) {
                playerHunger.OnStarvation += HandlePlayerStarvation;
            }
            else {
                Debug.LogError("[RunManager] Could not find PlayerHungerSystem to subscribe to OnStarvation event!");
            }
        }

        void OnDestroy() {
            PlayerHungerSystem playerHunger = FindFirstObjectByType<PlayerHungerSystem>();
            if (playerHunger != null) {
                playerHunger.OnStarvation -= HandlePlayerStarvation;
            }
        }

        void HandlePlayerStarvation() {
            if (!playerDeathEnabled) {
                Debug.Log("[RunManager] Player has starved, but player death is disabled. No action taken.");
                return;
            }

            Debug.Log("[RunManager] Player has starved! Triggering Game Over.");
            SetState(RunState.GameOver);
        }

        void SetState(RunState newState, bool force = false) {
            if (currentState == newState && !force) return;

            currentState = newState;
            Debug.Log($"[RunManager] State changed to: {currentState}");

            switch (currentState) {
                case RunState.Planning:
                    WeatherManager.Instance?.PauseCycleAtDay();
                    SetPhase(GamePhase.Planning);
                    break;

                case RunState.GrowthAndThreat:
                    WeatherManager.Instance?.ResumeCycle();
                    WaveManager.Instance?.StartWaveForRound(currentRoundNumber);
                    SetPhase(GamePhase.Execution);
                    break;

                case RunState.GameOver:
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
            if (currentState != RunState.Planning)
            {
                if (WaveManager.Instance != null && WaveManager.Instance.IsCurrentWaveDefeated())
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

            WaveManager.Instance?.ResetForNewRound();
            SetState(RunState.Planning);

            OnRoundChanged?.Invoke(currentRoundNumber);
        }
        
        public void RestartGame()
        {
            // Simple restart logic: reload the current scene.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private class PhaseTickHandler : ITickUpdateable
        {
            private RunManager manager;
            public PhaseTickHandler(RunManager manager) { this.manager = manager; }
            public void OnTickUpdate(int currentTick) { manager.currentPhaseTicks++; }
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
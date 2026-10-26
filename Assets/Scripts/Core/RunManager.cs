// Assets/Scripts/Core/RunManager.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for restarting the scene
using Abracodabra.Genes.Services;  // A6: deterministic gameplay RNG
using Abracodabra.Ecosystem;       // B3: supper at dusk

namespace WegoSystem {
    public enum RunState {
        Planning,
        GrowthAndThreat,
        GameOver,
        Dusk            // B3: the Day is over, ticks are stopped, supper runs, then the dusk check
    }

    public enum GamePhase {
        Planning,
        Execution
    }

    public class RunManager : SingletonMonoBehaviour<RunManager> {
        [Header("Game State")]
        [SerializeField] RunState currentState = RunState.Planning;
        [SerializeField] GamePhase currentPhase = GamePhase.Planning;
        [SerializeField] int currentRoundNumber = 1;
        [SerializeField] int currentPhaseTicks = 0;

        [Header("Player Death")]
        [Tooltip("If checked, the game will end when the player's hunger reaches max. " +
                 "B3: set from DuskRules.playerStarvationEndsRun at Initialize (off in Block 1, D4).")]
        public bool playerDeathEnabled = true;

        [Header("Determinism (A6)")]
        [Tooltip("If checked, a fresh random seed is generated each run. Uncheck and set 'Run Seed' to replay a specific run.")]
        [SerializeField] bool randomizeSeedOnStart = true;
        [Tooltip("The seed used for all gameplay randomness this run. Shown in logs; surface it on the game-over screen later.")]
        [SerializeField] int runSeed = 0;

        public RunState CurrentState => currentState;
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentRoundNumber => currentRoundNumber;
        public int CurrentPhaseTicks => currentPhaseTicks;
        public int RunSeed => runSeed;

        public event Action<RunState> OnRunStateChanged;
        public event Action<GamePhase, GamePhase> OnPhaseChanged;
        public event Action<int> OnRoundChanged;

        SupperRunner supperRunner;
        public SupperRunner Supper => supperRunner;

        protected override void OnAwake() {
            SetState(RunState.Planning, true);
        }

        public void Initialize() {
            // A6: seed the deterministic RNG once per run, before any gameplay randomness.
            InitializeRunSeed();

            // B3 (D4): only Doris and the garden end the run, unless DuskRules says otherwise.
            playerDeathEnabled = DuskRules.Active.playerStarvationEndsRun;
            EnsureSupperRunner();

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

        void InitializeRunSeed() {
            if (randomizeSeedOnStart) {
                runSeed = Environment.TickCount;
            }

            var rng = GeneServices.Get<IDeterministicRandom>();
            if (rng != null) {
                rng.SetSeed(runSeed);
                Debug.Log($"[RunManager] Run seed = {runSeed} (deterministic gameplay RNG seeded).");
            }
            else {
                Debug.LogWarning("[RunManager] IDeterministicRandom service unavailable; gameplay RNG not seeded.");
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

                case RunState.Dusk:
                    // Phase stays Execution; nothing ticks (every Day system is gated on GrowthAndThreat).
                    break;
            }

            OnRunStateChanged?.Invoke(currentState);
        }

        void SetPhase(GamePhase newPhase) {
            if (currentPhase == newPhase) return;

            GamePhase oldPhase = currentPhase;
            currentPhase = newPhase;
            currentPhaseTicks = 0;

            Debug.Log($"[RunManager] Phase changed: {oldPhase} -> {newPhase}");
            OnPhaseChanged?.Invoke(oldPhase, newPhase);
        }

        public void StartGrowthAndThreatPhase() {
            if (currentState == RunState.Planning) {
                Debug.Log($"[RunManager] Starting Growth & Threat for Round {currentRoundNumber}");
                SetState(RunState.GrowthAndThreat);
            }
        }

        public void EndPlanningPhase() {
            if (currentState == RunState.Planning && currentPhase == GamePhase.Planning) {
                SetPhase(GamePhase.Execution);
                StartGrowthAndThreatPhase();
            }
        }

        public void StartNewPlanningPhase() {
            // B3: once the run is over, only a restart starts another round.
            if (currentState == RunState.GameOver) {
                Debug.Log("[RunManager] The run is over; StartNewPlanningPhase ignored.");
                return;
            }

            if (currentState != RunState.Planning) {
                // A3: rounds are survive-the-timer, not kill-based. When the wave timer
                // has elapsed, advance to the next round; otherwise just return to Planning.
                if (WaveManager.Instance != null && WaveManager.Instance.IsWaveTimerComplete()) {
                    // B3: the Day's end is dusk: supper first, then the dusk check decides.
                    if (currentState == RunState.GrowthAndThreat) {
                        BeginDusk();
                        return;
                    }
                    StartNewRound();
                }
                else {
                    SetState(RunState.Planning);
                }
            }
        }

        void EnsureSupperRunner() {
            if (supperRunner != null) return;
            supperRunner = GetComponent<SupperRunner>();
            if (supperRunner == null) supperRunner = gameObject.AddComponent<SupperRunner>();
        }

        void BeginDusk() {
            Debug.Log($"[RunManager] Dusk of round {currentRoundNumber}: supper.");
            SetState(RunState.Dusk);
            EnsureSupperRunner();
            supperRunner.Run(HandleSupperFinished);
        }

        void HandleSupperFinished(SupperRunner.SupperResult result) {
            if (currentState != RunState.Dusk) return;
            StartNewRound();
        }

        void StartNewRound() {
            currentRoundNumber++;
            Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");

            WaveManager.Instance?.ResetForNewRound();
            SetState(RunState.Planning);

            OnRoundChanged?.Invoke(currentRoundNumber);
        }

        public void RestartGame() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        class PhaseTickHandler : ITickUpdateable {
            RunManager manager;
            public PhaseTickHandler(RunManager manager) { this.manager = manager; }
            public void OnTickUpdate(int currentTick) { manager.currentPhaseTicks++; }
        }

        public void ForcePhase(GamePhase phase) {
            if (Application.isEditor || Debug.isDebugBuild) {
                SetPhase(phase);
            }
        }
    }
}

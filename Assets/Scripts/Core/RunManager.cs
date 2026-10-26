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

    // B3: how a run ended. GameOver is the end state for both; this says which end.
    public enum RunOutcome {
        InProgress,
        Lost,
        Won
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
        [Tooltip("The seed used for all gameplay randomness this run. Shown in logs and on the end screen.")]
        [SerializeField] int runSeed = 0;

        [Header("Run End (B3, read only)")]
        [SerializeField] RunOutcome outcome = RunOutcome.InProgress;
        [SerializeField] string endReason = "";

        public RunState CurrentState => currentState;
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentRoundNumber => currentRoundNumber;
        public int CurrentPhaseTicks => currentPhaseTicks;
        public int RunSeed => runSeed;
        public RunOutcome Outcome => outcome;
        public string EndReason => endReason;
        public DuskVerdict LastVerdict { get; private set; } = DuskVerdict.NextRound;

        public event Action<RunState> OnRunStateChanged;
        public event Action<GamePhase, GamePhase> OnPhaseChanged;
        public event Action<int> OnRoundChanged;
        public event Action<RunOutcome, string> OnRunEnded;   // B3: (outcome, reason), fired once per run

        SupperRunner supperRunner;
        public SupperRunner Supper => supperRunner;

        // B3: the seed the next scene load uses (Restart same seed / New seed). Static, so it survives the reload.
        static bool hasPendingRunSeed;
        static int pendingRunSeed;

        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetStaticData() {
            hasPendingRunSeed = false;
            pendingRunSeed = 0;
        }
        #endif

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
            if (hasPendingRunSeed) {
                // B3: a restart asked for this seed (same seed again, or a new one).
                runSeed = pendingRunSeed;
                hasPendingRunSeed = false;
            }
            else if (randomizeSeedOnStart) {
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
            EndRun(RunOutcome.Lost, "You starved.");
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
                DayTally.BeginDay(currentRoundNumber); // B5: dawn resets the Day's counts (before the state event)
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

        // B3: the dusk check, after supper (DuskRules.Judge): no living plant, then an empty belly, then the win.
        void HandleSupperFinished(SupperRunner.SupperResult result) {
            if (currentState != RunState.Dusk) return;

            var rules = DuskRules.Active;
            int livingPlants = SupperRunner.CountLivingPlants();
            int bellyPips = result.dorisFound ? result.pipsAfter : rules.bellyPips;
            DuskVerdict verdict = rules.Judge(livingPlants, bellyPips, currentRoundNumber);
            LastVerdict = verdict;

            Debug.Log($"[RunManager] Dusk {currentRoundNumber}/{rules.roundsToWin}: living plants {livingPlants}, " +
                      $"belly {bellyPips}/{rules.bellyPips} -> {verdict}");

            switch (verdict) {
                case DuskVerdict.NextRound:
                    StartNewRound();
                    break;
                case DuskVerdict.Won:
                    EndRun(RunOutcome.Won, rules.TextFor(verdict));
                    break;
                default:
                    EndRun(RunOutcome.Lost, rules.TextFor(verdict));
                    break;
            }
        }

        /// <summary>B3: ends the run for good (GameOver is sticky; only a restart leaves it).</summary>
        void EndRun(RunOutcome runOutcome, string reason) {
            if (currentState == RunState.GameOver) return;

            outcome = runOutcome;
            endReason = reason;
            WaveManager.Instance?.StopCurrentWaveSpawning();

            Debug.Log($"[RunManager] Run {(runOutcome == RunOutcome.Won ? "WON" : "LOST")} at round {currentRoundNumber}: " +
                      $"\"{reason}\" (seed {runSeed})");
            SetState(RunState.GameOver);
            OnRunEnded?.Invoke(outcome, endReason);
        }

        void StartNewRound() {
            currentRoundNumber++;
            Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");

            WaveManager.Instance?.ResetForNewRound();
            SetState(RunState.Planning);

            OnRoundChanged?.Invoke(currentRoundNumber);
        }

        /// <summary>Restart with the same seed (kept for older callers).</summary>
        public void RestartGame() {
            RestartRun(true);
        }

        /// <summary>
        /// B3: reloads the scene as a fresh run. sameSeed replays this run's seed, otherwise a new one.
        /// The persistent singletons (this one included) are torn down first so nothing carries over.
        /// </summary>
        public void RestartRun(bool sameSeed) {
            int nextSeed = sameSeed ? runSeed : NewSeed();
            pendingRunSeed = nextSeed;
            hasPendingRunSeed = true;

            Debug.Log($"[RunManager] Restart ({(sameSeed ? "same seed" : "new seed")}): seed {nextSeed}.");

            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            SingletonRuntime.TearDownForRestart();
            SceneManager.LoadScene(sceneIndex);
        }

        int NewSeed() {
            int seed = Environment.TickCount;
            if (seed == runSeed) seed++;
            return seed;
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

# Abracodabra — A-Category Implementation Pack

> **STATUS 2026-07-05: Parts 1 & 2 APPLIED to the repo (all 15 files, verified on disk — `ExecutionPhaseDriver.cs` created, every symbol cross-checked).**
> **Remaining for Milan in the Unity Editor: Part 3 Step 2 (ExecutionPhaseDriver GameObject), Step 3 (RunManager seed fields), Step 4 (pick ONE starting-loadout source), then the Part 4 test checklist. When the checklist passes, move this file to `03_Tasks/Done/` and re-run the extractor.**

This completes all six **A-category** items from the foundation review against your current code:

- **A1** — Auto-tick driver for Growth & Threat + centralized tick authority
- **A2** — Plant death pipeline (cleanup, unregister, destroy, free tile)
- **A3** — Round-end decision + honest naming (timer-based "survive the day")
- **A4** — `BaseEnergyPerLeaf` zeroing bug made idempotent + per-tick `GetComponent` micro-fix
- **A5** — Init cleanup: kill timed `Invoke` delays, deterministic readiness, one loadout source
- **A6** — Determinism committed: per-run seed + gameplay RNG routed through `IDeterministicRandom`

**Two delivery styles below, both copy-paste ready:**
- **FULL FILES** — small/self-contained scripts and all new files. Replace wholesale.
- **METHOD-LEVEL PATCHES** — for large files (`PlantGrowth`, `PlayerActionManager`, `FeedingSystem`, `GardenerController`, `WaveManager`, `InventoryService`). Your project files are the uncompressed originals; pasting whole 300–670-line reconstructions from the compressed extract would risk corrupting untouched methods. Each patch gives the complete method(s) to swap in, with exact anchors.

Apply in the order listed. Nothing here depends on B-category work.

---

# PART 1 — FULL FILES (replace wholesale)

## 1.1 `Assets/Scripts/Ticks/TickManager.cs` *(A1)*

Adds the central authority: `ActionsDriveTicks` and `RequestActionTicks`. All other logic unchanged.

```csharp
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
```

## 1.2 `Assets/Scripts/Ticks/ExecutionPhaseDriver.cs` *(A1 — NEW FILE)*

Drives time automatically during Growth & Threat. Pause / speed-cycle hotkeys included; a HUD can drive it via the public API/events later.

```csharp
// Assets/Scripts/Ticks/ExecutionPhaseDriver.cs
using UnityEngine;

namespace WegoSystem {
    /// <summary>
    /// Advances ticks automatically during the Growth & Threat phase.
    /// During Planning, ticks stay action-driven (see TickManager.RequestActionTicks).
    /// </summary>
    public class ExecutionPhaseDriver : MonoBehaviour {
        [Header("Speed")]
        [Tooltip("Speed multipliers applied on top of TickConfiguration.ticksPerRealSecond. " +
                 "Index 0 is the default speed selected when a phase begins.")]
        [SerializeField] float[] speedMultipliers = { 1f, 2f };
        [SerializeField] int currentSpeedIndex = 0;
        [SerializeField] bool startPaused = false;

        [Header("Hotkeys (optional)")]
        [SerializeField] KeyCode pauseKey = KeyCode.Space;
        [SerializeField] KeyCode cycleSpeedKey = KeyCode.Tab;

        [Header("Debug")]
        [SerializeField] bool debugLog = false;

        bool isRunning = false;
        bool isPaused = false;
        float tickAccumulator = 0f;

        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;

        public float CurrentSpeedMultiplier =>
            (speedMultipliers != null && speedMultipliers.Length > 0)
                ? Mathf.Max(0.01f, speedMultipliers[Mathf.Clamp(currentSpeedIndex, 0, speedMultipliers.Length - 1)])
                : 1f;

        public event System.Action<bool> OnPauseChanged;
        public event System.Action<float> OnSpeedChanged;

        void Start() {
            isPaused = startPaused;

            if (RunManager.Instance != null) {
                RunManager.Instance.OnRunStateChanged += HandleRunStateChanged;
                HandleRunStateChanged(RunManager.Instance.CurrentState);
            }
            else {
                Debug.LogWarning("[ExecutionPhaseDriver] RunManager not found at Start. Driver will stay idle.");
            }
        }

        void OnDestroy() {
            if (RunManager.Instance != null) {
                RunManager.Instance.OnRunStateChanged -= HandleRunStateChanged;
            }
        }

        void HandleRunStateChanged(RunState newState) {
            isRunning = (newState == RunState.GrowthAndThreat);
            tickAccumulator = 0f;
            if (debugLog) Debug.Log($"[ExecutionPhaseDriver] State -> {newState}. Auto-tick running: {isRunning}");
        }

        void Update() {
            HandleHotkeys();

            if (!isRunning || isPaused) return;

            var tm = TickManager.Instance;
            var cfg = tm != null ? tm.Config : null;
            if (cfg == null) return;

            float ticksPerSecond = cfg.ticksPerRealSecond * CurrentSpeedMultiplier;
            if (ticksPerSecond <= 0f) return;

            tickAccumulator += Time.deltaTime * ticksPerSecond;

            int safety = 0;
            while (tickAccumulator >= 1f) {
                tickAccumulator -= 1f;
                tm.AdvanceTick();

                // A tick can end the wave -> RunManager switches to Planning -> stop here.
                if (RunManager.Instance == null ||
                    RunManager.Instance.CurrentState != RunState.GrowthAndThreat) {
                    isRunning = false;
                    tickAccumulator = 0f;
                    break;
                }

                if (++safety > 1000) {
                    Debug.LogWarning("[ExecutionPhaseDriver] Tick flood guard tripped; clearing accumulator.");
                    tickAccumulator = 0f;
                    break;
                }
            }
        }

        void HandleHotkeys() {
            if (!isRunning) return;
            if (pauseKey != KeyCode.None && Input.GetKeyDown(pauseKey)) TogglePause();
            if (cycleSpeedKey != KeyCode.None && Input.GetKeyDown(cycleSpeedKey)) CycleSpeed();
        }

        public void SetPaused(bool paused) {
            if (isPaused == paused) return;
            isPaused = paused;
            tickAccumulator = 0f;
            OnPauseChanged?.Invoke(isPaused);
            if (debugLog) Debug.Log($"[ExecutionPhaseDriver] Paused: {isPaused}");
        }

        public void TogglePause() => SetPaused(!isPaused);

        public void CycleSpeed() {
            if (speedMultipliers == null || speedMultipliers.Length == 0) return;
            currentSpeedIndex = (currentSpeedIndex + 1) % speedMultipliers.Length;
            OnSpeedChanged?.Invoke(CurrentSpeedMultiplier);
            if (debugLog) Debug.Log($"[ExecutionPhaseDriver] Speed -> {CurrentSpeedMultiplier}x");
        }

        public void SetSpeedIndex(int index) {
            if (speedMultipliers == null || speedMultipliers.Length == 0) return;
            currentSpeedIndex = Mathf.Clamp(index, 0, speedMultipliers.Length - 1);
            OnSpeedChanged?.Invoke(CurrentSpeedMultiplier);
        }
    }
}
```

## 1.3 `Assets/Scripts/Core/RunManager.cs` *(A1 awareness + A3 rename + A6 seed)*

Adds a per-run seed that seeds the deterministic gameplay RNG, and switches `StartNewPlanningPhase` to the honestly-named timer check.

```csharp
// Assets/Scripts/Core/RunManager.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for restarting the scene
using Abracodabra.Genes.Services;  // A6: deterministic gameplay RNG

namespace WegoSystem {
    public enum RunState {
        Planning,
        GrowthAndThreat,
        GameOver
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
        [Tooltip("If checked, the game will end when the player's hunger reaches max.")]
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

        protected override void OnAwake() {
            SetState(RunState.Planning, true);
        }

        public void Initialize() {
            // A6: seed the deterministic RNG once per run, before any gameplay randomness.
            InitializeRunSeed();

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
            if (currentState != RunState.Planning) {
                // A3: rounds are survive-the-timer, not kill-based. When the wave timer
                // has elapsed, advance to the next round; otherwise just return to Planning.
                if (WaveManager.Instance != null && WaveManager.Instance.IsWaveTimerComplete()) {
                    StartNewRound();
                }
                else {
                    SetState(RunState.Planning);
                }
            }
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
```

## 1.4 `Assets/Scripts/PlantSystem/Growth/PlantGrowthLogic.cs` *(A4)*

Only `CalculateAndApplyPassiveStats` changes — it now derives the photosynthesis base from the template every call, so it's idempotent and safe to re-invoke. Added `using ...Templates;`.

```csharp
// FILE: Assets/Scripts/PlantSystem/Growth/PlantGrowthLogic.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Templates; // A4: access SeedTemplate.energyRegenRate

namespace Abracodabra.Genes {
    public class PlantGrowthLogic {
        readonly PlantGrowth plant;

        public int TargetStemLength { get; set; }
        public int GrowthTicksPerStage { get; set; }
        public float PhotosynthesisEfficiencyPerLeaf { get; set; }

        public PlantGrowthLogic(PlantGrowth plant) {
            this.plant = plant;
        }

        public void CalculateAndApplyPassiveStats() {
            if (plant.geneRuntimeState == null) {
                Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null geneRuntimeState!");
                return;
            }

            // A4 FIX: derive the base photosynthesis rate from the template every time.
            // Previously PhotosynthesisEfficiencyPerLeaf was never assigned (always 0), so any
            // re-invocation of this method silently zeroed BaseEnergyPerLeaf. This makes the
            // method idempotent and safe to call on any mid-run stat refresh.
            PhotosynthesisEfficiencyPerLeaf = (plant.seedTemplate != null)
                ? plant.seedTemplate.energyRegenRate
                : PhotosynthesisEfficiencyPerLeaf;

            plant.growthSpeedMultiplier = 1f;
            plant.energyGenerationMultiplier = 1f;
            plant.energyStorageMultiplier = 1f;
            plant.fruitYieldMultiplier = 1f;
            plant.leafDurabilityMultiplier = 1f;
            plant.leafRegrowthRate = 0f;
            plant.thornDamage = 0f;

            var additiveBonuses = new Dictionary<PassiveStatType, float>();
            var multiplicativeBonuses = new Dictionary<PassiveStatType, float>();

            float thornDamageAccumulator = 0f;
            int regrowthStackCount = 0;
            float regrowthBaseValue = 0f;

            foreach (var instance in plant.geneRuntimeState.passiveInstances) {
                var passiveGene = instance?.GetGene<PassiveGene>();
                if (passiveGene == null) continue;

                if (passiveGene.statToModify == PassiveStatType.None) {
                    Debug.Log($"[{plant.gameObject.name}] Passive gene '{passiveGene.geneName}' has statToModify=None, skipping stat application.");
                    continue;
                }

                float value = passiveGene.baseValue * instance.GetValue("power_multiplier", 1f);

                if (passiveGene.statToModify == PassiveStatType.ThornDamage) {
                    thornDamageAccumulator += value;
                    continue;
                }

                if (passiveGene.statToModify == PassiveStatType.LeafRegrowth) {
                    regrowthStackCount++;
                    regrowthBaseValue = value; // all stacks have same base
                    continue;
                }

                if (passiveGene.stacksAdditively) {
                    if (!additiveBonuses.ContainsKey(passiveGene.statToModify))
                        additiveBonuses[passiveGene.statToModify] = 0f;
                    additiveBonuses[passiveGene.statToModify] += (value - 1f);
                }
                else {
                    if (!multiplicativeBonuses.ContainsKey(passiveGene.statToModify))
                        multiplicativeBonuses[passiveGene.statToModify] = 1f;
                    multiplicativeBonuses[passiveGene.statToModify] *= value;
                }
            }

            foreach (var kvp in additiveBonuses) {
                ApplyStat(kvp.Key, 1f + kvp.Value);
            }
            foreach (var kvp in multiplicativeBonuses) {
                ApplyStat(kvp.Key, kvp.Value);
            }

            plant.thornDamage = thornDamageAccumulator;

            if (regrowthStackCount > 0) {
                plant.leafRegrowthRate = Mathf.Max(2f, regrowthBaseValue - (regrowthStackCount - 1));
            }

            if (plant.EnergySystem != null) {
                plant.EnergySystem.BaseEnergyPerLeaf = PhotosynthesisEfficiencyPerLeaf;
            }

            Debug.Log($"[{plant.gameObject.name}] Final stats after passives: " +
                $"GrowthSpeed={plant.growthSpeedMultiplier:F2}x, " +
                $"EnergyGen={plant.energyGenerationMultiplier:F2}x, " +
                $"EnergyStore={plant.energyStorageMultiplier:F2}x, " +
                $"FruitYield={plant.fruitYieldMultiplier:F2}x, " +
                $"LeafDurability={plant.leafDurabilityMultiplier:F2}x, " +
                $"LeafRegrowth={plant.leafRegrowthRate:F1}t, " +
                $"ThornDmg={plant.thornDamage:F1}");
        }

        void ApplyStat(PassiveStatType stat, float value) {
            switch (stat) {
                case PassiveStatType.None:
                    break;
                case PassiveStatType.GrowthSpeed:
                    plant.growthSpeedMultiplier *= value;
                    break;
                case PassiveStatType.EnergyGeneration:
                    plant.energyGenerationMultiplier *= value;
                    break;
                case PassiveStatType.EnergyStorage:
                    plant.energyStorageMultiplier *= value;
                    break;
                case PassiveStatType.FruitYield:
                    plant.fruitYieldMultiplier *= value;
                    break;
                case PassiveStatType.Defense:
                    plant.leafDurabilityMultiplier *= value;
                    break;
                case PassiveStatType.LeafRegrowth:
                case PassiveStatType.ThornDamage:
                    break;
            }
        }
    }
}
```

## 1.5 `Assets/Scripts/PlantSystem/Growth/PlantEnergySystem.cs` *(A4 micro-fix)*

`OnTickUpdate` no longer does a redundant per-tick `GetComponent` — it uses the `plant` reference it already holds.

```csharp
// FILE: Assets/Scripts/PlantSystem/Growth/PlantEnergySystem.cs
using UnityEngine;
using Abracodabra.Genes;
using WegoSystem;

public class PlantEnergySystem {
    readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; }
    public float MaxEnergy { get; set; }
    public float BaseEnergyPerLeaf { get; set; } // Base rate from template

    public float EnergySpentThisCycle { get; set; }

    readonly FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant) {
        this.plant = plant;
        this.fireflyManagerInstance = FireflyManager.Instance;
    }

    public void OnTickUpdate() {
        // A4 micro-fix: 'plant' is already a PlantGrowth reference; no per-tick GetComponent.
        if (plant.CurrentState == PlantState.Growing && plant.rechargeEnergyDuringGrowth == false) {
            return;
        }

        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;

        int leafCount = plant.CellManager.GetActiveLeafCount();
        if (leafCount <= 0) return;

        float sunlight = (WeatherManager.Instance != null) ? WeatherManager.Instance.sunIntensity : 1f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null && fireflyManagerInstance.isActiveAndEnabled) {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(plant.transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(
                nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus
            );
        }

        float effectiveRate = BaseEnergyPerLeaf * plant.energyGenerationMultiplier;
        float totalPhotosynthesisRatePerLeaf = (effectiveRate * sunlight) + fireflyBonusRate;
        float energyThisTick = totalPhotosynthesisRatePerLeaf * leafCount;

        CurrentEnergy = Mathf.Clamp(CurrentEnergy + energyThisTick, 0f, MaxEnergy);
    }

    public void SpendEnergy(float amount) {
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
        EnergySpentThisCycle += amount;
    }

    public void AddEnergy(float amount) {
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, MaxEnergy);
    }

    public bool HasEnergy(float amount) {
        return CurrentEnergy >= amount;
    }
}
```

## 1.6 `Assets/Scripts/Genes/Services/GeneServices.cs` *(A6)*

Default RNG seed is now deterministic (`0`) instead of `DateTime.Now.Millisecond`. `RunManager` re-seeds it per run.

```csharp
// FILE: Assets/Scripts/Genes/Services/GeneServices.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Services {
    public static class GeneServices {
        // A6: deterministic default. RunManager.InitializeRunSeed() overrides this per run.
        const int DefaultSeed = 0;

        static Dictionary<Type, object> services = new Dictionary<Type, object>();
        static bool isInitialized = false;

        public static bool IsInitialized => isInitialized;

        public static void Initialize() {
            if (isInitialized) return;

            Register<IGeneEventBus>(new GeneEventBus());
            Register<IDeterministicRandom>(new DeterministicRandom(DefaultSeed));

            isInitialized = true;
            Debug.Log("Core Gene Services initialized (EventBus, Random).");
        }

        public static void Register<T>(T service) where T : class {
            services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class {
            if (!isInitialized) {
                Debug.LogWarning($"GeneServices accessed for type '{typeof(T).Name}' before explicit initialization. Auto-initializing now. Please check script execution order.");
                Initialize(); // Auto-initialize if needed
            }

            if (typeof(T) == typeof(IGeneLibrary) && !services.ContainsKey(typeof(T))) {
                Debug.LogWarning("IGeneLibrary service was requested before it was registered. Attempting to find and register it now.");
                var library = GeneLibrary.Instance ?? Resources.FindObjectsOfTypeAll<GeneLibrary>().FirstOrDefault();
                if (library != null) {
                    if (GeneLibrary.Instance == null) {
                        library.SetActiveInstance(); // Ensures the lookups are built
                    }
                    Register<IGeneLibrary>(library);
                    Debug.Log("Successfully found and registered IGeneLibrary service on-demand.");
                }
                else {
                    Debug.LogError("CRITICAL: Could not find any GeneLibrary asset to register as a fallback service!");
                }
            }

            if (services.TryGetValue(typeof(T), out object service)) {
                return (T)service;
            }

            Debug.LogError($"Service {typeof(T).Name} not registered!");
            return null;
        }

        public static void Reset() {
            services.Clear();
            isInitialized = false;
        }
    }

    public interface IGeneLibrary {
        GeneBase GetGeneByGUID(string guid);
        GeneBase GetGeneByName(string name);
        GeneBase GetPlaceholderGene();
        List<GeneBase> GetGenesOfCategory(GeneCategory category);
    }

    public interface IGeneEventBus {
        void Subscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action<T> handler) where T : class;
        void Publish<T>(T message) where T : class;
    }

    public interface IGeneEffectPool {
        GameObject GetEffect(GameObject prefab, Vector3 position, Quaternion rotation);
        void ReturnEffect(GameObject effect, GameObject sourcePrefab);
    }

    public interface IDeterministicRandom {
        float Range(float min, float max);
        int Range(int min, int max);
        void SetSeed(int seed);
    }
}
```

## 1.7 `Assets/Scripts/Genes/Config/GeneRewardSystem.cs` *(A5 + A6)*

Removes the `Invoke(..., 0.5f)` late-bind (RunManager is a singleton, guaranteed by any `Start()`), and routes reward randomness through the deterministic RNG.

```csharp
// FILE: Assets/Scripts/Genes/Config/GeneRewardSystem.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services; // A6: deterministic RNG
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;
using WegoSystem;

namespace Abracodabra.Genes.Config {
    public class GeneRewardSystem : MonoBehaviour {
        [Header("Reward Settings")]
        [Tooltip("Minimum number of genes awarded per round.")]
        [SerializeField] int minGenesPerRound = 2;

        [Tooltip("Maximum number of genes awarded per round.")]
        [SerializeField] int maxGenesPerRound = 4;

        [Header("References")]
        [SerializeField] GeneLibrary geneLibrary;

        void Start() {
            if (geneLibrary == null) {
                geneLibrary = GeneLibrary.Instance;
            }

            // A5: RunManager is a SingletonMonoBehaviour; its Instance is set in Awake,
            // so it is always available by Start. No timed late-binding needed.
            if (RunManager.Instance != null) {
                RunManager.Instance.OnRoundChanged += OnRoundChanged;
            }
            else {
                Debug.LogError("[GeneRewardSystem] RunManager not found at Start. Round rewards will not fire.");
            }
        }

        void OnDestroy() {
            if (RunManager.HasInstance) {
                RunManager.Instance.OnRoundChanged -= OnRoundChanged;
            }
        }

        void OnRoundChanged(int newRoundNumber) {
            int completedRound = newRoundNumber - 1;
            if (completedRound <= 0) return;

            GiveRoundReward(completedRound);
        }

        public void GiveRoundReward(int completedRoundNumber) {
            if (geneLibrary == null) {
                Debug.LogError("[GeneRewardSystem] GeneLibrary is null! Cannot give rewards.");
                return;
            }

            if (!InventoryService.IsInitialized) {
                Debug.LogError("[GeneRewardSystem] Inventory not available! Cannot give rewards.");
                return;
            }

            // A6: deterministic gameplay randomness.
            var rng = GeneServices.Get<IDeterministicRandom>();

            int geneCount = (rng != null)
                ? rng.Range(minGenesPerRound, maxGenesPerRound + 1)
                : Random.Range(minGenesPerRound, maxGenesPerRound + 1);

            int maxTier = GetMaxTierForRound(completedRoundNumber);

            var eligibleGenes = GetEligibleGenes(maxTier);
            if (eligibleGenes.Count == 0) {
                Debug.LogWarning("[GeneRewardSystem] No eligible genes found in library!");
                return;
            }

            int added = 0;
            for (int i = 0; i < geneCount; i++) {
                if (!InventoryService.HasEmptySlot()) {
                    Debug.LogWarning("[GeneRewardSystem] Inventory full! Could not add all rewards.");
                    break;
                }

                int pick = (rng != null)
                    ? rng.Range(0, eligibleGenes.Count)
                    : Random.Range(0, eligibleGenes.Count);

                GeneBase randomGene = eligibleGenes[pick];
                var item = new UIInventoryItem(randomGene);
                int slot = InventoryService.AddItem(item);

                if (slot >= 0) {
                    added++;
                    Debug.Log($"[GeneRewardSystem] Rewarded: {randomGene.geneName} (T{randomGene.tier})");
                }
            }

            Debug.Log($"[GeneRewardSystem] Round {completedRoundNumber} complete! Awarded {added} gene(s). Max tier: T{maxTier}");
        }

        int GetMaxTierForRound(int roundNumber) {
            if (roundNumber <= 2) return 1;
            if (roundNumber <= 4) return 2;
            return 3;
        }

        List<GeneBase> GetEligibleGenes(int maxTier) {
            var all = new List<GeneBase>();

            foreach (var gene in geneLibrary.GetAllGenes()) {
                if (gene == null) continue;
                if (gene is PlaceholderGene) continue;
                if (gene.tier > maxTier) continue;

                all.Add(gene);
            }

            return all;
        }
    }
}
```

## 1.8 `Assets/Scripts/Genes/Config/StartingLoadoutApplier.cs` *(A5)*

Replaces the `Invoke(ApplyLoadout, 0.5f)` magic delay with a deterministic readiness hook on `InventoryService` (see the patch in 2.6). **See the loadout-consolidation note in the guide — use either this OR `StartingInventory`, not both.**

```csharp
// FILE: Assets/Scripts/Genes/Config/StartingLoadoutApplier.cs
using UnityEngine;
using Abracodabra.Genes.Config;
using Abracodabra.UI.Genes;
using Abracodabra.UI.Toolkit;

namespace Abracodabra.Genes.Config {
    public class StartingLoadoutApplier : MonoBehaviour {
        [Header("Configuration")]
        [SerializeField] StartingLoadoutConfig loadoutConfig;

        bool hasApplied;
        bool subscribed;

        void Start() {
            // A5: deterministic readiness instead of a fixed time delay.
            if (InventoryService.IsInitialized) {
                ApplyLoadout();
            }
            else {
                InventoryService.OnInventoryReady += HandleInventoryReady;
                subscribed = true;
            }
        }

        void OnDestroy() {
            if (subscribed) {
                InventoryService.OnInventoryReady -= HandleInventoryReady;
                subscribed = false;
            }
        }

        void HandleInventoryReady() {
            InventoryService.OnInventoryReady -= HandleInventoryReady;
            subscribed = false;
            ApplyLoadout();
        }

        void ApplyLoadout() {
            if (hasApplied) return;

            if (loadoutConfig == null) {
                Debug.LogWarning("[StartingLoadoutApplier] No StartingLoadoutConfig assigned!");
                return;
            }

            if (!InventoryService.IsInitialized) {
                Debug.LogError("[StartingLoadoutApplier] InventoryService not ready! Cannot apply loadout.");
                return;
            }

            hasApplied = true;

            int totalAdded = 0;

            foreach (var seedEntry in loadoutConfig.startingSeeds) {
                if (seedEntry.seed == null) continue;

                for (int i = 0; i < seedEntry.count; i++) {
                    var item = new UIInventoryItem(seedEntry.seed);
                    int slot = InventoryService.AddItem(item);
                    if (slot >= 0) {
                        totalAdded++;
                    }
                    else {
                        Debug.LogWarning($"[StartingLoadoutApplier] Inventory full! Could not add seed '{seedEntry.seed.templateName}'");
                        break;
                    }
                }
            }

            foreach (var geneEntry in loadoutConfig.startingGenes) {
                if (geneEntry.gene == null) continue;

                for (int i = 0; i < geneEntry.count; i++) {
                    var item = new UIInventoryItem(geneEntry.gene);
                    int slot = InventoryService.AddItem(item);
                    if (slot >= 0) {
                        totalAdded++;
                    }
                    else {
                        Debug.LogWarning($"[StartingLoadoutApplier] Inventory full! Could not add gene '{geneEntry.gene.geneName}'");
                        break;
                    }
                }
            }

            Debug.Log($"[StartingLoadoutApplier] Applied starting loadout: {totalAdded} items added to inventory.");
        }
    }
}
```

## 1.9 `Assets/Scripts/Core/InitializationManager.cs` *(A5)*

Keeps your existing GameEvent phase wiring intact (it's Inspector-configured and works), but adds a deterministic readiness signal (`IsReady` / `OnReady`) so systems can hook the canonical "fully initialized" moment instead of guessing with delays.

```csharp
// Assets/Scripts/Core/InitializationManager.cs
using System.Collections;
using UnityEngine;

namespace WegoSystem {
    public class InitializationManager : SingletonMonoBehaviour<InitializationManager> {
        [SerializeField] GameEvent onCoreSystemsInitialized;
        [SerializeField] GameEvent onGameManagersInitialized;
        [SerializeField] GameEvent onGameplaySystemsInitialized;

        // A5: deterministic "everything is up" signal for late-binding systems.
        public static bool IsReady { get; private set; }
        public static event System.Action OnReady;

        IEnumerator Start() {
            IsReady = false;
            Debug.Log("[InitializationManager] Starting initialization sequence...");

            Debug.Log("[InitializationManager] Phase 1: Initializing Core Systems...");
            onCoreSystemsInitialized.Raise();
            yield return null;

            Debug.Log("[InitializationManager] Phase 2: Initializing Game Managers...");
            onGameManagersInitialized.Raise();
            yield return null;

            if (EnvironmentalStatusEffectSystem.Instance != null) {
                Debug.Log("[InitializationManager] Initializing EnvironmentalStatusEffectSystem...");
                EnvironmentalStatusEffectSystem.Instance.Initialize();
            }
            else {
                Debug.LogWarning("[InitializationManager] EnvironmentalStatusEffectSystem instance not found. Tile-based status effects will not function.");
            }

            Debug.Log("[InitializationManager] Phase 3: Initializing Gameplay Systems & UI...");
            onGameplaySystemsInitialized.Raise();
            yield return null;

            IsReady = true;
            OnReady?.Invoke();
            Debug.Log("[InitializationManager] All systems initialized successfully.");
        }
    }
}
```

---

# PART 2 — METHOD-LEVEL PATCHES (large files — swap the named methods)

> For each, open the real file in your project and replace the named method(s) with the version below. Add the small new members where indicated. Untouched methods stay exactly as they are.

## 2.1 `PlantGrowth.cs` *(A2 + A6)*

**(a) Add these members** near the other fields (e.g. just under the `[Header("Energy Logic")]` block):

```csharp
        [Header("Death (A2)")]
        [SerializeField] float deathFadeDuration = 1f;
        static readonly Color DeathTint = new Color(0.35f, 0.25f, 0.15f);

        // A2: fired the moment a plant dies (UI, stats, Doris reactions can subscribe later).
        public event Action<PlantGrowth> OnPlantDied;
```

**(b) Replace `InitializeWithState`** — removes the now-redundant `BaseEnergyPerLeaf` assignment (the A4 fix in `PlantGrowthLogic` is the single source for it):

```csharp
        public void InitializeWithState(PlantGeneRuntimeState state) {
            if (state == null || state.template == null) {
                Debug.LogError($"Cannot initialize plant on '{gameObject.name}': Provided state or its template is null.", this);
                Destroy(gameObject);
                return;
            }

            this.seedTemplate = state.template;
            this.geneRuntimeState = state;

            this.baseGrowthChance = seedTemplate.baseGrowthChance;
            this.minHeight = seedTemplate.minHeight;
            this.maxHeight = seedTemplate.maxHeight;
            this.leafDensity = seedTemplate.leafDensity;
            this.leafGap = seedTemplate.leafGap;

            sequenceExecutor = GetComponent<PlantSequenceExecutor>();
            if (sequenceExecutor == null) {
                sequenceExecutor = gameObject.AddComponent<PlantSequenceExecutor>();
            }
            sequenceExecutor.plantGrowth = this;

            GrowthLogic.CalculateAndApplyPassiveStats();

            EnergySystem.MaxEnergy = geneRuntimeState.template.maxEnergy * energyStorageMultiplier;
            EnergySystem.CurrentEnergy = geneRuntimeState.template.startingEnergy;
            // A4: BaseEnergyPerLeaf is now set (idempotently) inside CalculateAndApplyPassiveStats().
            // The previous direct assignment here was a second source of truth and has been removed.

            sequenceExecutor.InitializeWithTemplate(this.geneRuntimeState);

            CellManager.SpawnCellVisual(PlantCellType.Seed, Vector2Int.zero);

            CurrentState = PlantState.Initializing;

            destroyedLeafPositions.Clear();
            witheringTicksRemaining = 0;
            regrowthTickCounter = 0;
            preWitheringColors = null;
            _growthStep = 0;

            StartCoroutine(DelayedGrowthStart());

            if (TickManager.Instance != null) {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            Debug.Log($"Plant '{gameObject.name}' initialized from template '{seedTemplate.templateName}' (Archetype: {seedTemplate.archetype}). State: {CurrentState}");
        }
```

**(c) Replace `Die()` and add `DeathSequence()`** right after it:

```csharp
        void Die() {
            if (CurrentState == PlantState.Dead) return;

            CurrentState = PlantState.Dead;
            Debug.Log($"[PlantGrowth] '{name}' has died.");

            OnPlantDied?.Invoke(this);

            // A2: stop ticking immediately, then fade out and destroy so the tile frees up.
            var tickManager = TickManager.Instance;
            if (tickManager != null) {
                tickManager.UnregisterTickUpdateable(this);
            }

            StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence() {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) {
                startColors[i] = (renderers[i] != null) ? renderers[i].color : Color.white;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, deathFadeDuration);

            // Death fade is purely visual, so real time is fine here.
            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < renderers.Length; i++) {
                    if (renderers[i] == null) continue;
                    Color c = Color.Lerp(startColors[i], DeathTint, t);
                    c.a = Mathf.Lerp(startColors[i].a, 0f, t);
                    renderers[i].color = c;
                }
                yield return null;
            }

            // OnDestroy() removes this from AllActivePlants and unregisters from TickManager.
            // PlantPlacementManager.CleanupDestroyedPlants() then frees the grid tile because the
            // dictionary entry becomes a destroyed (null) reference once this GameObject is gone.
            Destroy(gameObject);
        }
```

**(d) Replace `DestroyRandomLeaf`** — the only change is the random index now uses the deterministic RNG (A6):

```csharp
        public bool DestroyRandomLeaf(string source = "unknown") {
            var activeLeaves = CellManager.LeafDataList.Where(l => l.IsActive).ToList();
            if (activeLeaves.Count == 0) return false;

            // A6: deterministic leaf selection (falls back to Unity RNG only if the service is missing).
            int idx = (_deterministicRandom != null)
                ? _deterministicRandom.Range(0, activeLeaves.Count)
                : UnityEngine.Random.Range(0, activeLeaves.Count);

            var target = activeLeaves[idx];
            Vector2Int coord = target.GridCoord;

            GameObject cellObj = CellManager.GetCellGameObjectAt(coord);
            Vector3 vfxPos = cellObj != null
                ? cellObj.transform.position
                : transform.position + new Vector3(coord.x * cellSpacing, coord.y * cellSpacing, 0);

            CellManager.ReportCellDestroyed(coord);

            if (!destroyedLeafPositions.Contains(coord)) {
                destroyedLeafPositions.Add(coord);
            }

            OnLeafConsumed?.Invoke(this, coord);

            StartCoroutine(DamageFlash());
            FloatingCombatText.Spawn(vfxPos, "-1 Leaf", new Color(0.4f, 0.8f, 0.2f));

            Debug.Log($"[PlantGrowth] '{name}' lost a leaf from {source}. Remaining: {ActiveLeafCount}");

            CheckLeafVitality();
            return true;
        }
```

> `System` (for `Action`) and `System.Collections` (for `IEnumerator`) are already used by this file. No new usings required.

## 2.2 `PlayerActionManager.cs` *(A1)*

Replace the static `AdvanceGameTickStatic` method so player actions route through the authority:

```csharp
        static void AdvanceGameTickStatic(int tickCount) {
            if (TickManager.Instance == null) {
                Debug.LogError("[AdvanceGameTickStatic] TickManager.Instance is null!");
                return;
            }

            // A1: route through the central authority. During Growth & Threat the
            // ExecutionPhaseDriver owns the clock, so player actions become free.
            TickManager.Instance.RequestActionTicks(tickCount);
        }
```

## 2.3 `FeedingSystem.cs` *(A1)*

In `ExecuteFeeding`, replace the tick-advance block:

```csharp
            // BEFORE:
            // if (TickManager.Instance != null) {
            //     TickManager.Instance.AdvanceTick();
            // }

            // A1: feeding during Growth & Threat is a real-time reaction; let the driver own time.
            if (TickManager.Instance != null) {
                TickManager.Instance.RequestActionTicks(1);
            }
```

## 2.4 `GardenerController.cs` *(A1)*

Replace `TryMove` and `ProcessMultiTickMovement`. During the auto-driven phase, movement repositions the player but does **not** advance ticks.

```csharp
        void TryMove(GridPosition direction) {
            if (gridEntity == null) return;

            GridPosition targetPos = gridEntity.Position + direction;

            if (GridPositionManager.Instance != null &&
                PlayerActionManager.Instance != null &&
                TickManager.Instance != null &&
                GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsMovementBlockedAt(targetPos)) {

                Vector3 currentWorldPos = GridPositionManager.Instance.GridToWorld(gridEntity.Position);
                int moveCost = PlayerActionManager.Instance.GetMovementTickCost(currentWorldPos, this);

                // A1: in the auto-driven Growth & Threat phase, movement is a free reaction —
                // reposition the player and let ExecutionPhaseDriver advance time.
                if (!TickManager.Instance.ActionsDriveTicks) {
                    gridEntity.SetPosition(targetPos);
                    currentTargetPosition = targetPos;
                    return;
                }

                if (moveCost > 1) {
                    StartCoroutine(ProcessMultiTickMovement(targetPos, moveCost));
                }
                else {
                    gridEntity.SetPosition(targetPos);
                    currentTargetPosition = targetPos;
                    TickManager.Instance.RequestActionTicks(1);
                }
            }
        }

        IEnumerator ProcessMultiTickMovement(GridPosition targetPos, int tickCost) {
            isProcessingMovement = true;
            for (int i = 0; i < tickCost - 1; i++) {
                TickManager.Instance.RequestActionTicks(1);
                yield return new WaitForSeconds(multiTickDelay);
            }
            gridEntity.SetPosition(targetPos);
            currentTargetPosition = targetPos;
            TickManager.Instance.RequestActionTicks(1);
            isProcessingMovement = false;
        }
```

## 2.5 `WaveManager.cs` *(A3)*

Add the honestly-named method and deprecate the old one (the old name keeps working, so nothing else breaks). Place near the existing `IsCurrentWaveDefeated` / `IsWaveActive` properties:

```csharp
        // A3: rounds end on a survive-the-timer basis (the wave window elapsed), not on
        // killing every pest. Named honestly to match behavior.
        public bool IsWaveTimerComplete() => currentState == WaveState.Idle && currentWaveIndex >= 0;

        [System.Obsolete("Use IsWaveTimerComplete(). Rounds are timer-based, not defeat-based.")]
        public bool IsCurrentWaveDefeated() => IsWaveTimerComplete();
```

> If your existing file declares `IsCurrentWaveDefeated()` as an expression-bodied member (`public bool IsCurrentWaveDefeated() => ...;`), **delete that old line** and paste the two methods above in its place. `RunManager` (updated in 1.3) already calls the new name.

## 2.6 `InventoryService.cs` *(A5)*

**(a) Add the event** alongside the other static events (next to `OnInventoryChanged`):

```csharp
        // A5: fired once the inventory model is registered, so loadout/reward systems
        // can bind deterministically instead of waiting on a fixed time delay.
        public static event Action OnInventoryReady;
```

**(b) Replace `Register`** to raise it:

```csharp
        public static void Register(List<UIInventoryItem> inventory, int columns, int rows) {
            _inventory = inventory;
            _inventoryColumns = columns;
            _inventoryRows = rows;
            Debug.Log($"[InventoryService] Registered inventory with {inventory.Count} slots ({columns}x{rows})");

            OnInventoryReady?.Invoke();
        }
```

---

# PART 3 — IMPLEMENTATION GUIDE (Unity / Inspector)

### Step 1 — Add the files
1. Create `Assets/Scripts/Ticks/ExecutionPhaseDriver.cs` (1.2).
2. Replace the seven full files (1.1, 1.3–1.9).
3. Apply the six method-level patches (2.1–2.6).
4. Let Unity recompile. Expected: **zero errors**. (If `StartingLoadoutApplier`'s old `applyDelay` field warns as unused in any leftover reference, ignore — it's been removed from the script.)

### Step 2 — Wire the ExecutionPhaseDriver (the one required scene change)
1. In your gameplay scene, add an empty GameObject named **`ExecutionPhaseDriver`** (or drop the component onto an existing always-present manager object).
2. Add the **Execution Phase Driver** component.
3. Inspector fields:
   - **Speed Multipliers**: leave `[1, 2]` (gives 1× / 2× via Tab), or add `0.5`, `3`, etc.
   - **Start Paused**: off.
   - **Pause Key / Cycle Speed Key**: defaults Space / Tab. Set either to `None` if they clash with your bindings.
4. Nothing else to wire — it self-subscribes to `RunManager`. It reads tick rate from your existing **TickConfiguration** asset (`ticksPerRealSecond`, currently `2`), so a 2× multiplier = 4 ticks/sec.

> **It does *not* tick during Planning.** Planning stays action-driven exactly as before.

### Step 3 — Determinism (A6) on RunManager
On your **RunManager** GameObject:
- **Randomize Seed On Start**: ON for normal play. Turn OFF and set **Run Seed** to replay/repro a specific run.
- The seed is logged each run (`[RunManager] Run seed = …`). When you build the game-over screen (C-category), display `RunManager.Instance.RunSeed` there.

> Seeding happens inside `RunManager.Initialize()`, which your init flow already calls via the GameEvent chain — **no new wiring**. It runs after `GameServices` is up, so the RNG is seeded before any plant grows.

### Step 4 — Starting loadout: pick ONE source (A5)
You currently have **two** systems that can populate the starting inventory, and if both are configured the player gets double items:
- **`StartingInventory`** (ScriptableObject) — consumed directly by `GameUIManager` (`startingInventory` field). Handles **tools + seeds + genes**, runs synchronously, race-free.
- **`StartingLoadoutConfig` + `StartingLoadoutApplier`** (component) — seeds + genes only, with per-entry counts.

**Recommended:** keep **`StartingInventory`** (it covers tools and is already race-free) and remove the `StartingLoadoutApplier` component from the scene. Or, if you prefer the count-based config, clear the `Starting Seeds`/`Starting Genes`/`Starting Tools` lists on the `StartingInventory` asset and keep only `StartingLoadoutApplier`. Either way the patched `StartingLoadoutApplier` no longer uses a 0.5 s delay — it binds to `InventoryService.OnInventoryReady`.

### Step 5 — Nothing to wire for A2 / A3 / A4
- **A2** plant death is automatic — when a plant hits 0 leaves and the withering window expires it now fades and frees its tile. Optional: subscribe to `PlantGrowth.OnPlantDied` later for stats/Doris reactions.
- **A3** round flow is internal. Survivors are still cleared at the round boundary (existing behavior); the round ends when the wave **timer** elapses.
- **A4** is a pure code fix.

---

# PART 4 — TEST CHECKLIST (done-when criteria)

- [ ] **A1 — auto-tick:** Start a day, then *don't touch anything*. Plants grow, the wave timer counts down, Doris hunger rises, and the round ends on its own. *Done when the farm runs hands-free.*
- [ ] **A1 — speed/pause:** During Growth & Threat, **Tab** toggles 1×/2× (visible difference in tick log cadence) and **Space** pauses/resumes. *Done when both respond.*
- [ ] **A1 — no double-advance:** During Growth & Threat, walking around does **not** make the wave timer jump faster than standing still. *Done when movement is free.*
- [ ] **A1 — Planning unchanged:** During Planning, planting a seed still advances ticks as before. *Done when action-driven time still works.*
- [ ] **A2 — tile frees:** Let a plant get stripped to death (or starve Doris so she eats one). It fades out and disappears; you can replant on that exact tile next Planning phase. *Done when the tile is reusable.*
- [ ] **A2 — no zombie ticks:** Console shows no withering/grow logs from a plant after it dies.
- [ ] **A3 — round ends on timer:** A round completes even with pests still alive at dawn; next Planning phase begins and round number increments.
- [ ] **A4 — energy survives recalc:** Plants generate energy normally. (Sanity: temporarily call `plant.GrowthLogic.CalculateAndApplyPassiveStats()` twice in a debug build — energy generation must be unchanged, not zeroed.)
- [ ] **A5 — clean cold start:** Enter Play from a cold domain reload. No "InventoryService not ready" / "RunManager not found" errors; starting items appear exactly once.
- [ ] **A6 — reproducibility:** Set **Randomize Seed On Start = OFF**, fix a seed, play two runs taking identical actions → identical reward genes and identical which-leaf-dies outcomes. *Done when the same seed reproduces the run.*

---

# PART 5 — Notes, scope boundaries & recommended follow-ups

- **Determinism coverage (A6).** Fully wired: per-run seeding, reward selection, and leaf-death selection. `PlantGrowth.GrowSomething`'s growth roll already used the deterministic service. **Still on `UnityEngine.Random`** (visual or not-yet-migrated): `PlantPlacementManager.GetRandomizedPlantingPosition` (cosmetic jitter), firefly drift, damage-flash timing, and fauna spawn/movement randomness in `FaunaManager`/`AnimalMovement`. For *full* run reproducibility, migrate the fauna ones next using the same pattern: cache `GeneServices.Get<IDeterministicRandom>()` and call `rng.Range(...)`. They were left out here because those files weren't part of this change set and migrating them blind risks regressions — do them as a focused follow-up with the files open.
- **B5 interaction (heads-up, not in scope).** With auto-tick now live, `PlantGrowth.DelayedGrowthStart()`'s `WaitForSeconds(0.5f)` means a plant ignores ~1 growth tick right after spawn, non-deterministically by frame timing. It's harmless for the demo but is exactly the wall-clock-in-tick-logic issue from B5 — convert it to a tick counter when you pick up the B list.
- **Movement during Planning.** Your `GardenerController` currently only reads movement input during Growth & Threat, so the action-driven branch of `TryMove`/`ProcessMultiTickMovement` is dormant today. It's written correctly so that if/when you enable Planning-phase movement (the design's Stoneshard-style intent), it already does the right thing.
- **Single source of truth wins booked:** energy base rate (A4), starting loadout (A5), and tick advancement (A1) each now have exactly one authority.
```


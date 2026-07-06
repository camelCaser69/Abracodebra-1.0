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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

public enum InternalWaveState {
    Idle,
    WaitingForSpawnTime,
    SpawningInProgress,
    WaveActive
}

public class WaveManager : MonoBehaviour {
    public static WaveManager Instance { get; private set; }

    [SerializeField] FaunaManager faunaManager;
    [SerializeField] WeatherManager weatherManager;
    [SerializeField] Camera mainCamera;

    [SerializeField] List<WaveDefinition> wavesSequence;

    [SerializeField][Range(1, 10)] int waveDurationInDayCycles = 1;
    [SerializeField] WeatherManager.CyclePhase spawnStartPhase = WeatherManager.CyclePhase.Day;
    [SerializeField][Range(0f, 100f)] float spawnStartPercentage = 50f;
    [SerializeField] bool deletePreviousWaveAnimals = true;

    [SerializeField] TextMeshProUGUI waveStatusText;
    [SerializeField] TextMeshProUGUI timeTrackerText;

    [SerializeField] InternalWaveState currentInternalState = InternalWaveState.Idle;
    [SerializeField] int activeWaveDefinitionIndex = -1;

    WaveDefinition currentActiveWaveDef = null;
    bool hasSpawnedForThisWeatherCycle = false;
    Coroutine activeWaveExecutionCoroutine;
    
    // New tick-based tracking
    int waveStartTick = 0;
    int waveEndTick = 0;

    public bool IsCurrentWaveDefeated() {
        if (currentInternalState == InternalWaveState.Idle && activeWaveDefinitionIndex != -1) {
            return true;
        }
        
        // Check tick-based completion
        if (currentActiveWaveDef != null && TickManager.Instance != null) {
            int currentTick = TickManager.Instance.CurrentTick;
            return currentTick >= waveEndTick;
        }
        
        return false;
    }

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager missing!", this);
        if (weatherManager == null) Debug.LogError("[WaveManager] WeatherManager missing!", this);
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence empty. No waves will spawn.", this);
        
        // Register for tick updates
        if (TickManager.Instance != null) {
            TickManager.Instance.OnTickAdvanced += OnTickAdvanced;
        }
    }

    void Start() {
        if (weatherManager != null) weatherManager.OnPhaseChanged += HandleWeatherPhaseChange;
        SetInternalState(InternalWaveState.Idle);
    }

    void OnDestroy() {
        if (weatherManager != null) weatherManager.OnPhaseChanged -= HandleWeatherPhaseChange;
        if (TickManager.Instance != null) {
            TickManager.Instance.OnTickAdvanced -= OnTickAdvanced;
        }
        StopAllCoroutines();
    }
    
    void OnTickAdvanced(int currentTick) {
        // Check for wave completion
        if (currentActiveWaveDef != null && currentTick >= waveEndTick) {
            EndCurrentWave();
        }
    }
    
    int GetWaveDurationTicks() {
        var config = TickManager.Instance?.Config;
        if (config == null) return 50;
        
        return config.wavesDependOnDayCycle ? 
            config.ticksPerDay * waveDurationInDayCycles : 
            config.ticksPerWave;
    }

    void SetInternalState(InternalWaveState newState) {
        if (currentInternalState == newState) return;
        if (Debug.isDebugBuild) Debug.Log($"[WaveManager] Internal State Change: {currentInternalState} -> {newState}");
        currentInternalState = newState;
        UpdateLegacyWaveStatusText();
    }

    public void StartWaveForRound(int roundNumber) {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat) {
            Debug.LogWarning("[WaveManager] Attempted to StartWaveForRound, but RunManager not in GrowthAndThreat state.");
            return;
        }

        activeWaveDefinitionIndex = roundNumber - 1;

        if (wavesSequence == null || activeWaveDefinitionIndex < 0 || activeWaveDefinitionIndex >= wavesSequence.Count) {
            Debug.LogWarning($"[WaveManager] No WaveDefinition for round {roundNumber} (index {activeWaveDefinitionIndex}). Max index: {(wavesSequence?.Count - 1) ?? -1}. No wave will start.");
            currentActiveWaveDef = null;
            SetInternalState(InternalWaveState.Idle);
            return;
        }

        currentActiveWaveDef = wavesSequence[activeWaveDefinitionIndex];
        if (currentActiveWaveDef == null) {
            Debug.LogError($"[WaveManager] WaveDefinition at index {activeWaveDefinitionIndex} for round {roundNumber} is NULL.");
            SetInternalState(InternalWaveState.Idle);
            return;
        }

        if (deletePreviousWaveAnimals) {
            ClearAllActiveAnimals();
        }

        // Set tick-based wave duration
        waveStartTick = TickManager.Instance.CurrentTick;
        int waveDuration = GetWaveDurationTicks();
        waveEndTick = waveStartTick + waveDuration;
        
        Debug.Log($"[WaveManager] Starting wave '{currentActiveWaveDef.waveName}' for Round {roundNumber}. Duration: {waveDuration} ticks (ends at tick {waveEndTick}).");
        
        hasSpawnedForThisWeatherCycle = false;
        SetInternalState(InternalWaveState.WaitingForSpawnTime);
        Update_WaitingForSpawnTimeCheck();
    }
    
    void EndCurrentWave() {
        Debug.Log($"[WaveManager] Ending wave '{currentActiveWaveDef?.waveName}'");
        
        StopCurrentWaveSpawning();
        currentActiveWaveDef = null;
        SetInternalState(InternalWaveState.Idle);
        
        // Force transition to planning phase
        if (RunManager.Instance != null) {
            RunManager.Instance.StartNewPlanningPhase();
        }
        
        // Ensure turn phase manager is in planning
        if (TurnPhaseManager.Instance != null && 
            TurnPhaseManager.Instance.CurrentPhase != TurnPhase.Planning) {
            TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
        }
    }

    public void StopCurrentWaveSpawning() {
        Debug.Log("[WaveManager] StopCurrentWaveSpawning called (e.g., round ending).");
        if (activeWaveExecutionCoroutine != null) {
            StopCoroutine(activeWaveExecutionCoroutine);
            activeWaveExecutionCoroutine = null;
        }
        faunaManager?.StopAllSpawnCoroutines();
        SetInternalState(InternalWaveState.Idle);
    }

    void HandleWeatherPhaseChange(WeatherManager.CyclePhase newPhase) {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat || currentActiveWaveDef == null) return;

        if (newPhase == WeatherManager.CyclePhase.TransitionToDay) {
            hasSpawnedForThisWeatherCycle = false;
        }
        else if (newPhase == spawnStartPhase && (currentInternalState == InternalWaveState.WaitingForSpawnTime || currentInternalState == InternalWaveState.WaveActive)) {
            Update_WaitingForSpawnTimeCheck();
        }
    }

    void Update_WaitingForSpawnTimeCheck() {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat || currentActiveWaveDef == null) return;
        if (currentInternalState != InternalWaveState.WaitingForSpawnTime && currentInternalState != InternalWaveState.WaveActive) return;
        if (hasSpawnedForThisWeatherCycle) return;

        if (weatherManager == null) return;
    
        // Simplified spawn check - just spawn immediately at start of day phase
        WeatherManager.CyclePhase currentPhase = weatherManager.CurrentPhase;
    
        if (currentPhase == spawnStartPhase) {
            float phaseProgress = weatherManager.GetPhaseProgress() * 100f;
        
            Debug.Log($"[WaveManager] Checking spawn: Phase={currentPhase}, Progress={phaseProgress:F1}%, Required={spawnStartPercentage}%");
        
            if (phaseProgress >= spawnStartPercentage) {
                TriggerFaunaSpawning();
            }
        }
    }

    void TriggerFaunaSpawning() {
        if (currentActiveWaveDef == null) return;
        if (faunaManager == null) { Debug.LogError("[WaveManager] FaunaManager missing, cannot spawn!"); return; }

        Debug.Log($"[WaveManager] Spawning animals for WaveDefinition: '{currentActiveWaveDef.waveName}' (Phase: {spawnStartPhase} >= {spawnStartPercentage}%)");
        SetInternalState(InternalWaveState.SpawningInProgress);
        hasSpawnedForThisWeatherCycle = true;

        faunaManager.ExecuteSpawnWave(currentActiveWaveDef);

        SetInternalState(InternalWaveState.WaveActive);
    }

    void ClearAllActiveAnimals() {
        if(Debug.isDebugBuild) Debug.Log("[WaveManager] Clearing all active animals.");
        AnimalController[] activeAnimals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        int count = 0;
        foreach(AnimalController animal in activeAnimals) {
            if(animal != null) { Destroy(animal.gameObject); count++; }
        }
        if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Destroyed {count} animals.");
    }

    public void ResetForNewRound() {
        Debug.Log("[WaveManager] Resetting for new round.");
        StopCurrentWaveSpawning();
        if (deletePreviousWaveAnimals) {
            ClearAllActiveAnimals();
        }
        currentActiveWaveDef = null;
        activeWaveDefinitionIndex = -1;
        hasSpawnedForThisWeatherCycle = false;
        waveStartTick = 0;
        waveEndTick = 0;
        SetInternalState(InternalWaveState.Idle);
    }

    void Update() {
        if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat) {
            if (currentInternalState == InternalWaveState.WaitingForSpawnTime) {
                Update_WaitingForSpawnTimeCheck();
            }
        }
        UpdateLegacyTimeTrackerUI();
        UpdateLegacyWaveStatusText();
    }

    void UpdateLegacyTimeTrackerUI() {
        if (timeTrackerText == null || weatherManager == null) return;
        WeatherManager.CyclePhase phase = weatherManager.CurrentPhase;
        float total = weatherManager.CurrentTotalPhaseTime;
        float remaining = weatherManager.CurrentPhaseTimer;
        float progressPercent = (total > 0) ? (1f - (remaining / total)) * 100f : 0f;
        string phaseName = phase.ToString().Replace("Transition", "");
        timeTrackerText.text = $"{phaseName} [{progressPercent:F0}%]";
        if (RunManager.Instance != null) {
            if(RunManager.Instance.CurrentState == RunState.Planning) timeTrackerText.text += " (Planning)";
            else if(RunManager.Instance.CurrentState == RunState.GrowthAndThreat) timeTrackerText.text += " (Growth & Threat)";
        }
    }

    void UpdateLegacyWaveStatusText() {
        if (waveStatusText == null) return;
        if (RunManager.Instance == null) return;

        if (RunManager.Instance.CurrentState == RunState.Planning) {
            waveStatusText.text = $"Prepare for Round {RunManager.Instance.CurrentRoundNumber}";
        }
        else if (RunManager.Instance.CurrentState == RunState.GrowthAndThreat) {
            if (currentActiveWaveDef != null) {
                string waveNamePart = string.IsNullOrEmpty(currentActiveWaveDef.waveName) ? $"Wave {activeWaveDefinitionIndex + 1}" : currentActiveWaveDef.waveName;
                
                // Show tick-based progress
                if (TickManager.Instance != null) {
                    int ticksRemaining = Mathf.Max(0, waveEndTick - TickManager.Instance.CurrentTick);
                    waveStatusText.text = $"{waveNamePart} [{ticksRemaining} ticks left]";
                } else {
                    waveStatusText.text = waveNamePart;
                }
            }
            else if (currentInternalState == InternalWaveState.Idle) {
                waveStatusText.text = "All waves for round complete.";
            }
        }
    }

    public Camera GetMainCamera() { return mainCamera; }
}
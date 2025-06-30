using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WegoSystem;

public enum WaveState {
    Idle,           // No wave active
    Active,         // Wave is currently running
    Spawning        // Currently spawning enemies
}

public class WaveManager : MonoBehaviour {
    public static WaveManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] Camera mainCamera;
    [SerializeField] FaunaManager faunaManager;
    [SerializeField] List<WaveDefinition> wavesSequence;
    
    [Header("Wave Timing")]
    [SerializeField] int waveDurationInDays = 1; // Default: one wave per full day/night cycle
    [SerializeField] float spawnTimeNormalized = 0.1f; // When in the wave to start spawning (0-1)
    [SerializeField] bool continuousSpawning = false; // If true, spawns throughout the wave
    
    [Header("Wave Settings")]
    [SerializeField] bool deletePreviousWaveAnimals = true;
    
    [Header("UI")]
    [SerializeField] TextMeshProUGUI waveStatusText;
    [SerializeField] TextMeshProUGUI timeTrackerText;
    
    // State tracking
    WaveState currentState = WaveState.Idle;
    WaveDefinition currentWaveDef = null;
    int currentWaveIndex = -1;
    
    // Timing
    int waveStartTick = 0;
    int waveEndTick = 0;
    int waveSpawnTick = 0;
    bool hasSpawnedThisWave = false;
    
    // Coroutines
    Coroutine activeSpawnCoroutine = null;

    public bool IsWaveActive => currentState != WaveState.Idle;
    public bool IsCurrentWaveDefeated() => currentState == WaveState.Idle && currentWaveIndex >= 0;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        ValidateReferences();
    }

    void Start() {
        if (TickManager.Instance != null) {
            TickManager.Instance.OnTickAdvanced += OnTickAdvanced;
        }
    }

    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.OnTickAdvanced -= OnTickAdvanced;
        }
        StopAllCoroutines();
    }

    void ValidateReferences() {
        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager missing!", this);
        if (wavesSequence == null || wavesSequence.Count == 0) 
            Debug.LogWarning("[WaveManager] Wave Sequence empty. No waves will spawn.", this);
    }

    void OnTickAdvanced(int currentTick) {
        if (currentState == WaveState.Active) {
            // Check if wave should end
            if (currentTick >= waveEndTick) {
                EndCurrentWave();
            }
            // Check if we should spawn
            else if (!hasSpawnedThisWave && currentTick >= waveSpawnTick) {
                StartSpawning();
            }
            // Handle continuous spawning
            else if (continuousSpawning && hasSpawnedThisWave) {
                // Could implement periodic spawning here if needed
            }
        }
    }

    public void StartWaveForRound(int roundNumber) {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat) {
            Debug.LogWarning("[WaveManager] Cannot start wave - not in GrowthAndThreat state.");
            return;
        }

        currentWaveIndex = roundNumber - 1;
        
        if (!IsValidWaveIndex(currentWaveIndex)) {
            Debug.LogWarning($"[WaveManager] No wave definition for round {roundNumber}");
            currentState = WaveState.Idle;
            return;
        }

        currentWaveDef = wavesSequence[currentWaveIndex];
        if (currentWaveDef == null) {
            Debug.LogError($"[WaveManager] Wave definition at index {currentWaveIndex} is null!");
            currentState = WaveState.Idle;
            return;
        }

        StartWave();
    }

    void StartWave() {
        if (deletePreviousWaveAnimals) {
            ClearAllActiveAnimals();
        }

        // Calculate wave timing based on day cycles
        var config = TickManager.Instance?.Config;
        if (config == null) {
            Debug.LogError("[WaveManager] No TickConfiguration found!");
            return;
        }

        waveStartTick = TickManager.Instance.CurrentTick;
        int waveDurationTicks = config.ticksPerDay * waveDurationInDays;
        waveEndTick = waveStartTick + waveDurationTicks;
        
        // Calculate when to spawn within the wave
        waveSpawnTick = waveStartTick + Mathf.RoundToInt(waveDurationTicks * spawnTimeNormalized);
        
        hasSpawnedThisWave = false;
        currentState = WaveState.Active;
        
        Debug.Log($"[WaveManager] Starting wave '{currentWaveDef.waveName}' " +
                  $"Duration: {waveDurationTicks} ticks ({waveDurationInDays} days) " +
                  $"Spawn at tick: {waveSpawnTick}");
    }

    void StartSpawning() {
        if (currentWaveDef == null || faunaManager == null) return;
        
        hasSpawnedThisWave = true;
        currentState = WaveState.Spawning;
        
        Debug.Log($"[WaveManager] Beginning spawn for wave '{currentWaveDef.waveName}'");
        
        if (activeSpawnCoroutine != null) {
            StopCoroutine(activeSpawnCoroutine);
        }
        
        activeSpawnCoroutine = StartCoroutine(ExecuteWaveSpawn());
    }

    IEnumerator ExecuteWaveSpawn() {
        faunaManager.ExecuteSpawnWave(currentWaveDef);
        
        // Wait a bit for spawning to complete
        yield return new WaitForSeconds(1f);
        
        // Return to active state after spawning
        if (currentState == WaveState.Spawning) {
            currentState = WaveState.Active;
        }
        
        activeSpawnCoroutine = null;
    }

    void EndCurrentWave() {
        Debug.Log($"[WaveManager] Ending wave '{currentWaveDef?.waveName}'");
        
        StopCurrentWaveSpawning();
        currentWaveDef = null;
        currentState = WaveState.Idle;
        
        // Notify other systems
        if (RunManager.Instance != null) {
            RunManager.Instance.StartNewPlanningPhase();
        }
    }

    public void StopCurrentWaveSpawning() {
        if (activeSpawnCoroutine != null) {
            StopCoroutine(activeSpawnCoroutine);
            activeSpawnCoroutine = null;
        }
        
        faunaManager?.StopAllSpawnCoroutines();
    }

    public void ResetForNewRound() {
        Debug.Log("[WaveManager] Resetting for new round");
        
        StopCurrentWaveSpawning();
        
        if (deletePreviousWaveAnimals) {
            ClearAllActiveAnimals();
        }
        
        currentWaveDef = null;
        currentWaveIndex = -1;
        currentState = WaveState.Idle;
        hasSpawnedThisWave = false;
        waveStartTick = 0;
        waveEndTick = 0;
        waveSpawnTick = 0;
    }

    void ClearAllActiveAnimals() {
        AnimalController[] animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        int count = 0;
        
        foreach (var animal in animals) {
            if (animal != null) {
                Destroy(animal.gameObject);
                count++;
            }
        }
        
        Debug.Log($"[WaveManager] Cleared {count} animals");
    }

    bool IsValidWaveIndex(int index) {
        return wavesSequence != null && 
               index >= 0 && 
               index < wavesSequence.Count;
    }

    void Update() {
        UpdateUI();
    }

    void UpdateUI() {
        UpdateTimeTracker();
        UpdateWaveStatus();
    }

    void UpdateTimeTracker() {
        if (timeTrackerText == null || TickManager.Instance == null) return;
        
        var config = TickManager.Instance.Config;
        if (config == null) return;
        
        // Show current day progress
        float dayProgress = config.GetDayProgressNormalized(TickManager.Instance.CurrentTick);
        int dayNumber = TickManager.Instance.CurrentTick / config.ticksPerDay + 1;
        
        timeTrackerText.text = $"Day {dayNumber} - {(dayProgress * 100):F0}%";
        
        // Add phase info if in a wave
        if (currentState != WaveState.Idle) {
            int ticksIntoWave = TickManager.Instance.CurrentTick - waveStartTick;
            int totalWaveTicks = waveEndTick - waveStartTick;
            float waveProgress = totalWaveTicks > 0 ? (float)ticksIntoWave / totalWaveTicks : 0;
            timeTrackerText.text += $" | Wave: {(waveProgress * 100):F0}%";
        }
    }

    void UpdateWaveStatus() {
        if (waveStatusText == null) return;
        
        if (RunManager.Instance == null) {
            waveStatusText.text = "System Offline";
            return;
        }

        if (RunManager.Instance.CurrentState == RunState.Planning) {
            waveStatusText.text = $"Prepare for Round {RunManager.Instance.CurrentRoundNumber}";
        }
        else if (RunManager.Instance.CurrentState == RunState.GrowthAndThreat) {
            if (currentWaveDef != null) {
                int ticksRemaining = Mathf.Max(0, waveEndTick - TickManager.Instance.CurrentTick);
                string waveName = string.IsNullOrEmpty(currentWaveDef.waveName) 
                    ? $"Wave {currentWaveIndex + 1}" 
                    : currentWaveDef.waveName;
                
                string status = currentState == WaveState.Spawning ? " [SPAWNING]" : "";
                waveStatusText.text = $"{waveName}{status} - {ticksRemaining} ticks left";
            }
            else {
                waveStatusText.text = "No active wave";
            }
        }
    }

    public Camera GetMainCamera() => mainCamera;

    // Editor helpers
    [ContextMenu("Force End Current Wave")]
    void Debug_ForceEndWave() {
        if (Application.isEditor && currentState != WaveState.Idle) {
            EndCurrentWave();
        }
    }

    [ContextMenu("Force Start Spawning")]
    void Debug_ForceSpawn() {
        if (Application.isEditor && currentState == WaveState.Active && !hasSpawnedThisWave) {
            StartSpawning();
        }
    }
}
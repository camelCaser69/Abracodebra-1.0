using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using WegoSystem;

public enum InternalWaveState 
{
    Idle,
    WaitingForSpawnTime,
    Spawning
}

public class WaveManager : MonoBehaviour 
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] Camera mainCamera;
    [SerializeField] FaunaManager faunaManager;
    [SerializeField] WeatherManager weatherManager;
    [SerializeField] List<WaveDefinition> wavesSequence;
    
    [SerializeField] int waveDurationInDayCycles = 1;
    [SerializeField] float spawnStartPercentage = 50f;
    [SerializeField] bool deletePreviousWaveAnimals = true;

    [SerializeField] TextMeshProUGUI waveStatusText;
    [SerializeField] TextMeshProUGUI timeTrackerText;

    [SerializeField] InternalWaveState currentInternalState = InternalWaveState.Idle;
    [SerializeField] int activeWaveDefinitionIndex = -1;

    WaveDefinition currentActiveWaveDef = null;
    bool hasSpawnedForThisWeatherCycle = false;
    Coroutine activeWaveExecutionCoroutine;

    int waveStartTick = 0;
    int waveEndTick = 0;

    public bool IsCurrentWaveDefeated() 
    {
        if (currentInternalState == InternalWaveState.Idle && activeWaveDefinitionIndex != -1) 
        {
            return true;
        }

        if (currentActiveWaveDef != null && TickManager.Instance != null) 
        {
            int currentTick = TickManager.Instance.CurrentTick;
            return currentTick >= waveEndTick;
        }

        return false;
    }

    void Awake() 
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager missing!", this);
        if (weatherManager == null) Debug.LogError("[WaveManager] WeatherManager missing!", this);
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence empty. No waves will spawn.", this);

        if (TickManager.Instance != null) 
        {
            TickManager.Instance.OnTickAdvanced += OnTickAdvanced;
        }
    }

    void Start() 
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged += HandleWeatherPhaseChange;
        SetInternalState(InternalWaveState.Idle);
    }

    void OnDestroy() 
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged -= HandleWeatherPhaseChange;
        if (TickManager.Instance != null) 
        {
            TickManager.Instance.OnTickAdvanced -= OnTickAdvanced;
        }
        StopAllCoroutines();
    }

    void OnTickAdvanced(int currentTick) 
    {
        if (currentActiveWaveDef != null && currentTick >= waveEndTick) 
        {
            EndCurrentWave();
        }
    }

    int GetWaveDurationTicks() 
    {
        var config = TickManager.Instance?.Config;
        if (config == null) return 50;

        // Simplified: Waves always use day cycles as their duration unit
        // This is clearer and more consistent
        return config.ticksPerDay * waveDurationInDayCycles;
    }

    void SetInternalState(InternalWaveState newState) 
    {
        if (currentInternalState == newState) return;
        if (Debug.isDebugBuild) Debug.Log($"[WaveManager] Internal State Change: {currentInternalState} -> {newState}");
        currentInternalState = newState;
        UpdateLegacyWaveStatusText();
    }

    public void StartWaveForRound(int roundNumber) 
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat) 
        {
            Debug.LogWarning("[WaveManager] Attempted to StartWaveForRound, but RunManager not in GrowthAndThreat state.");
            return;
        }

        activeWaveDefinitionIndex = roundNumber - 1;

        if (wavesSequence == null || activeWaveDefinitionIndex < 0 || activeWaveDefinitionIndex >= wavesSequence.Count) 
        {
            Debug.LogWarning($"[WaveManager] No WaveDefinition for round {roundNumber} (index {activeWaveDefinitionIndex}). Max index: {(wavesSequence?.Count - 1) ?? -1}. No wave will start.");
            currentActiveWaveDef = null;
            SetInternalState(InternalWaveState.Idle);
            return;
        }

        currentActiveWaveDef = wavesSequence[activeWaveDefinitionIndex];
        if (currentActiveWaveDef == null) 
        {
            Debug.LogError($"[WaveManager] WaveDefinition at index {activeWaveDefinitionIndex} for round {roundNumber} is NULL.");
            SetInternalState(InternalWaveState.Idle);
            return;
        }

        if (deletePreviousWaveAnimals) 
        {
            ClearAllActiveAnimals();
        }

        waveStartTick = TickManager.Instance.CurrentTick;
        int waveDuration = GetWaveDurationTicks();
        waveEndTick = waveStartTick + waveDuration;

        Debug.Log($"[WaveManager] Starting wave '{currentActiveWaveDef.waveName}' for Round {roundNumber}. Duration: {waveDuration} ticks (ends at tick {waveEndTick}).");

        hasSpawnedForThisWeatherCycle = false;
        SetInternalState(InternalWaveState.WaitingForSpawnTime);
        Update_WaitingForSpawnTimeCheck();
    }

    void EndCurrentWave() 
    {
        Debug.Log($"[WaveManager] Ending wave '{currentActiveWaveDef?.waveName}'");

        StopCurrentWaveSpawning();
        currentActiveWaveDef = null;
        SetInternalState(InternalWaveState.Idle);

        if (RunManager.Instance != null) 
        {
            RunManager.Instance.StartNewPlanningPhase();
        }

        if (TurnPhaseManager.Instance != null &&
            TurnPhaseManager.Instance.CurrentPhase != TurnPhase.Planning) 
        {
            TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
        }
    }

    public void StopCurrentWaveSpawning() 
    {
        Debug.Log("[WaveManager] StopCurrentWaveSpawning called (e.g., round ending).");
        if (activeWaveExecutionCoroutine != null) 
        {
            StopCoroutine(activeWaveExecutionCoroutine);
            activeWaveExecutionCoroutine = null;
        }
        faunaManager.StopAllSpawnCoroutines();
    }

    void HandleWeatherPhaseChange(WeatherManager.CyclePhase newPhase) 
    {
        if (currentActiveWaveDef == null || currentInternalState != InternalWaveState.WaitingForSpawnTime) return;

        hasSpawnedForThisWeatherCycle = false;
        Update_WaitingForSpawnTimeCheck();
    }

    void Update_WaitingForSpawnTimeCheck() 
    {
        if (currentInternalState != InternalWaveState.WaitingForSpawnTime || currentActiveWaveDef == null) return;

        float currentPhaseProgress = 0f;
        if (weatherManager != null) 
        {
            float totalPhaseTime = weatherManager.CurrentTotalPhaseTime;
            float phaseTimer = weatherManager.CurrentPhaseTimer;
            currentPhaseProgress = (totalPhaseTime > 0) ? ((totalPhaseTime - phaseTimer) / totalPhaseTime) * 100f : 0f;
        }

        if (!hasSpawnedForThisWeatherCycle && currentPhaseProgress >= spawnStartPercentage) 
        {
            hasSpawnedForThisWeatherCycle = true;
            Debug.Log($"[WaveManager] Weather phase progress ({currentPhaseProgress:F0}%) reached spawn threshold ({spawnStartPercentage:F0}%). Starting wave spawn for '{currentActiveWaveDef.waveName}'.");

            SetInternalState(InternalWaveState.Spawning);
            activeWaveExecutionCoroutine = StartCoroutine(ExecuteWaveSpawnCoroutine());
        }
    }

    IEnumerator ExecuteWaveSpawnCoroutine() 
    {
        if (faunaManager == null || currentActiveWaveDef == null) yield break;

        faunaManager.ExecuteSpawnWave(currentActiveWaveDef);

        yield return new WaitForSeconds(1f);

        Debug.Log($"[WaveManager] Finished initiating spawn coroutines for wave '{currentActiveWaveDef.waveName}'. FaunaManager is now handling individual spawn entries.");
        SetInternalState(InternalWaveState.WaitingForSpawnTime);
    }

    void ClearAllActiveAnimals() 
    {
        Debug.Log("[WaveManager] Clearing all active animals.");
        AnimalController[] activeAnimals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        int count = 0;
        foreach(AnimalController animal in activeAnimals) 
        {
            if(animal != null) { Destroy(animal.gameObject); count++; }
        }
        if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Destroyed {count} animals.");
    }

    public void ResetForNewRound() 
    {
        Debug.Log("[WaveManager] Resetting for new round.");
        StopCurrentWaveSpawning();
        if (deletePreviousWaveAnimals) 
        {
            ClearAllActiveAnimals();
        }
        currentActiveWaveDef = null;
        activeWaveDefinitionIndex = -1;
        hasSpawnedForThisWeatherCycle = false;
        waveStartTick = 0;
        waveEndTick = 0;
        SetInternalState(InternalWaveState.Idle);
    }

    void Update() 
    {
        if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat) 
        {
            if (currentInternalState == InternalWaveState.WaitingForSpawnTime) 
            {
                Update_WaitingForSpawnTimeCheck();
            }
        }
        UpdateLegacyTimeTrackerUI();
        UpdateLegacyWaveStatusText();
    }

    void UpdateLegacyTimeTrackerUI() 
    {
        if (timeTrackerText == null || weatherManager == null) return;
        WeatherManager.CyclePhase phase = weatherManager.CurrentPhase;
        float total = weatherManager.CurrentTotalPhaseTime;
        float remaining = weatherManager.CurrentPhaseTimer;
        float progressPercent = (total > 0) ? (1f - (remaining / total)) * 100f : 0f;
        string phaseName = phase.ToString().Replace("Transition", "");
        timeTrackerText.text = $"{phaseName} [{progressPercent:F0}%]";
        if (RunManager.Instance != null) 
        {
            if(RunManager.Instance.CurrentState == RunState.Planning) timeTrackerText.text += " (Planning)";
            else if(RunManager.Instance.CurrentState == RunState.GrowthAndThreat) timeTrackerText.text += " (Growth & Threat)";
        }
    }

    void UpdateLegacyWaveStatusText() 
    {
        if (waveStatusText == null) return;
        if (RunManager.Instance == null) return;

        if (RunManager.Instance.CurrentState == RunState.Planning) 
        {
            waveStatusText.text = $"Prepare for Round {RunManager.Instance.CurrentRoundNumber}";
        }
        else if (RunManager.Instance.CurrentState == RunState.GrowthAndThreat) 
        {
            if (currentActiveWaveDef != null) 
            {
                string waveNamePart = string.IsNullOrEmpty(currentActiveWaveDef.waveName) ? 
                    $"Wave {activeWaveDefinitionIndex + 1}" : currentActiveWaveDef.waveName;

                if (TickManager.Instance != null) 
                {
                    int ticksRemaining = Mathf.Max(0, waveEndTick - TickManager.Instance.CurrentTick);
                    waveStatusText.text = $"{waveNamePart} [{ticksRemaining} ticks left]";
                } 
                else 
                {
                    waveStatusText.text = waveNamePart;
                }
            }
            else if (currentInternalState == InternalWaveState.Idle) 
            {
                waveStatusText.text = "All waves for round complete.";
            }
        }
    }

    public Camera GetMainCamera() { return mainCamera; }
}
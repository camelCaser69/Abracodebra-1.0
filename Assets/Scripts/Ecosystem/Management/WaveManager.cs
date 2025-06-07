using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Keep for legacy UI if any part is still used
using UnityEngine.UI; // Keep for legacy UI

// Renamed internal state for clarity, as RunManager now controls overall game state
public enum InternalWaveState
{
    Idle,                // Not currently processing a wave
    WaitingForSpawnTime, // Wave active, waiting for correct time in WeatherManager cycle to spawn
    SpawningInProgress,  // FaunaManager has been told to execute spawn entries
    WaveActive           // Spawns done for this wave def, wave duration timer running (dayCyclesRemaining)
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Core Dependencies")]
    [SerializeField] private FaunaManager faunaManager;
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private Camera mainCamera; // Keep for FaunaManager if it still needs it

    [Header("Wave Sequence (Played sequentially per round)")]
    [SerializeField] private List<WaveDefinition> wavesSequence;

    [Header("Wave Timing & Spawning (For a single WaveDefinition)")]
    [Tooltip("How many full Day+Night cycles each WaveDefinition lasts.")]
    [SerializeField][Range(1, 10)] private int waveDurationInDayCycles = 1;
    [Tooltip("The phase during which spawning should occur each cycle for the active WaveDefinition.")]
    [SerializeField] private WeatherManager.CyclePhase spawnStartPhase = WeatherManager.CyclePhase.Day;
    [Tooltip("The percentage progress within the Spawn Start Phase when spawning triggers (0-100).")]
    [SerializeField][Range(0f, 100f)] private float spawnStartPercentage = 50f;
    // REMOVED: loopSequence - RunManager handles overall game looping/progression
    [Tooltip("If checked, animals from the previous wave are destroyed when a new wave (within the same round or new round) starts.")]
    [SerializeField] private bool deletePreviousWaveAnimals = true;

    [Header("UI & Feedback (Legacy - May be replaced by UIManager)")]
    [SerializeField] private TextMeshProUGUI waveStatusText;
    [SerializeField] private TextMeshProUGUI timeTrackerText;
    // REMOVED: startRunButton - UIManager controls this

    [Header("State (Read Only - Internal)")]
    [SerializeField] private InternalWaveState currentInternalState = InternalWaveState.Idle;
    [SerializeField] private int activeWaveDefinitionIndex = -1; // Index within wavesSequence for the current round's wave

    private WaveDefinition currentActiveWaveDef = null;
    private int dayCyclesRemainingForThisWaveDef = 0;
    private bool hasSpawnedForThisWeatherCycle = false;
    private Coroutine activeWaveExecutionCoroutine; // To manage FaunaManager spawning for current WaveDef

    // Public property for RunManager to check
    public bool IsCurrentWaveDefeated()
    {
        // A wave is "defeated" if:
        // 1. It's in Idle state (meaning it finished or was never started properly for the round)
        // 2. OR its dayCyclesRemainingForThisWaveDef is <= 0 (duration met)
        //    AND no animals are left (this part is trickier and might need FaunaManager input)
        // For now, let's simplify: duration met means "wave part" is done.
        // RunManager might have additional conditions (like all animals cleared).
        bool durationMet = currentInternalState == InternalWaveState.WaveActive && dayCyclesRemainingForThisWaveDef <= 0;
        bool isIdle = currentInternalState == InternalWaveState.Idle;

        // For a more robust check, we'd also ask FaunaManager if any wave-spawned animals are left.
        // bool noActiveThreats = faunaManager != null ? faunaManager.AreAllSpawnedAnimalsDefeated() : true;
        // For now, relying on duration or being idle.
        if (isIdle && activeWaveDefinitionIndex != -1)
        {
            // This means a wave was processed and finished, ready for next.
            return true;
        }
        return durationMet;
    }


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager missing!", this);
        if (weatherManager == null) Debug.LogError("[WaveManager] WeatherManager missing!", this);
        // if (mainCamera == null) Debug.LogError("[WaveManager] Main Camera missing!", this); // Less critical now if FaunaManager has its own
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence empty. No waves will spawn.", this);
    }

    void Start()
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged += HandleWeatherPhaseChange;
        SetInternalState(InternalWaveState.Idle); // Start idle, wait for RunManager
    }

    void OnDestroy()
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged -= HandleWeatherPhaseChange;
        StopAllCoroutines(); // Ensure any wave execution stops
    }

    private void SetInternalState(InternalWaveState newState)
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

        // Determine which wave definition to use for this round.
        // Simple approach: use roundNumber as an index (1-based round to 0-based index)
        // This assumes wavesSequence contains definitions for multiple rounds.
        activeWaveDefinitionIndex = roundNumber - 1;

        if (wavesSequence == null || activeWaveDefinitionIndex < 0 || activeWaveDefinitionIndex >= wavesSequence.Count)
        {
            Debug.LogWarning($"[WaveManager] No WaveDefinition for round {roundNumber} (index {activeWaveDefinitionIndex}). Max index: {(wavesSequence?.Count - 1) ?? -1}. No wave will start.");
            currentActiveWaveDef = null;
            SetInternalState(InternalWaveState.Idle); // Effectively, this round has no wave.
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

        Debug.Log($"[WaveManager] Starting wave '{currentActiveWaveDef.waveName}' for Round {roundNumber}. Duration: {waveDurationInDayCycles} day cycles.");
        dayCyclesRemainingForThisWaveDef = waveDurationInDayCycles;
        hasSpawnedForThisWeatherCycle = false;
        SetInternalState(InternalWaveState.WaitingForSpawnTime);
        Update_WaitingForSpawnTimeCheck(); // Initial check in case conditions are already met
    }

    public void StopCurrentWaveSpawning()
    {
        Debug.Log("[WaveManager] StopCurrentWaveSpawning called (e.g., round ending).");
        if (activeWaveExecutionCoroutine != null)
        {
            StopCoroutine(activeWaveExecutionCoroutine);
            activeWaveExecutionCoroutine = null;
        }
        // FaunaManager itself should also stop its individual spawn coroutines if it has any running
        // from a previous ExecuteSpawnWave call that might not have completed all its entries.
        faunaManager?.StopAllSpawnCoroutines();
        SetInternalState(InternalWaveState.Idle); // Wave is considered done for this round.
    }

    void HandleWeatherPhaseChange(WeatherManager.CyclePhase newPhase)
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat || currentActiveWaveDef == null) return;

        if (newPhase == WeatherManager.CyclePhase.TransitionToDay) // A new day starts
        {
            if (currentInternalState == InternalWaveState.WaveActive || currentInternalState == InternalWaveState.SpawningInProgress || currentInternalState == InternalWaveState.WaitingForSpawnTime)
            {
                dayCyclesRemainingForThisWaveDef--;
                hasSpawnedForThisWeatherCycle = false; // Reset for the new day cycle
                if (Debug.isDebugBuild) Debug.Log($"[WaveManager] Day cycle ended for wave '{currentActiveWaveDef.waveName}'. Cycles remaining: {dayCyclesRemainingForThisWaveDef}");

                if (dayCyclesRemainingForThisWaveDef <= 0)
                {
                    Debug.Log($"[WaveManager] Wave '{currentActiveWaveDef.waveName}' duration met. Marking as complete for this round.");
                    StopCurrentWaveSpawning(); // This will set state to Idle
                    // RunManager will detect IsCurrentWaveDefeated()
                }
                else
                {
                    // Still more days for this wave definition
                    SetInternalState(InternalWaveState.WaitingForSpawnTime);
                }
            }
        }
        // If the phase changed TO our spawnStartPhase, re-evaluate spawning
        else if (newPhase == spawnStartPhase && (currentInternalState == InternalWaveState.WaitingForSpawnTime || currentInternalState == InternalWaveState.WaveActive) )
        {
            Update_WaitingForSpawnTimeCheck();
        }
    }

    void Update_WaitingForSpawnTimeCheck()
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat || currentActiveWaveDef == null) return;
        if (currentInternalState != InternalWaveState.WaitingForSpawnTime && currentInternalState != InternalWaveState.WaveActive) return; // Only proceed if waiting or already active (for subsequent day spawns)
        if (hasSpawnedForThisWeatherCycle) return; // Already spawned in this specific weather cycle

        if (weatherManager == null) return;
        WeatherManager.CyclePhase currentPhase = weatherManager.CurrentPhase;
        float totalPhaseTime = weatherManager.CurrentTotalPhaseTime;
        float remainingPhaseTime = weatherManager.CurrentPhaseTimer;
        float progressPercent = (totalPhaseTime > 0) ? (1f - (remainingPhaseTime / totalPhaseTime)) * 100f : 0f;

        if (currentPhase == spawnStartPhase && progressPercent >= spawnStartPercentage)
        {
            TriggerFaunaSpawning();
        }
    }

    private void TriggerFaunaSpawning()
    {
        if (currentActiveWaveDef == null) return;
        if (faunaManager == null) { Debug.LogError("[WaveManager] FaunaManager missing, cannot spawn!"); return; }

        Debug.Log($"[WaveManager] Spawning animals for WaveDefinition: '{currentActiveWaveDef.waveName}' (Phase: {spawnStartPhase} >= {spawnStartPercentage}%)");
        SetInternalState(InternalWaveState.SpawningInProgress); // Indicate spawning has started
        hasSpawnedForThisWeatherCycle = true; // Mark as spawned for *this* specific day's spawn window

        // FaunaManager's ExecuteSpawnWave will run its coroutines.
        // We might not need to hold onto the coroutine here if FaunaManager manages its own lifecycle.
        // However, if WaveDefinition contains multiple entries with delays,
        // ExecuteSpawnWave might be a long-running operation.
        faunaManager.ExecuteSpawnWave(currentActiveWaveDef);

        // After telling FaunaManager to spawn, transition to WaveActive.
        // This implies the wave is "active" for its duration, even if FaunaManager is still
        // trickling out spawns from the definition's entries.
        SetInternalState(InternalWaveState.WaveActive);
    }

    void ClearAllActiveAnimals()
    {
        if(Debug.isDebugBuild) Debug.Log("[WaveManager] Clearing all active animals.");
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
        StopCurrentWaveSpawning(); // Stop any ongoing coroutines and clear FaunaManager
        if (deletePreviousWaveAnimals) // This flag can decide if animals carry over or not
        {
            ClearAllActiveAnimals();
        }
        currentActiveWaveDef = null;
        activeWaveDefinitionIndex = -1;
        dayCyclesRemainingForThisWaveDef = 0;
        hasSpawnedForThisWeatherCycle = false;
        SetInternalState(InternalWaveState.Idle);
    }

    // --- Legacy UI Update Methods (can be phased out if UIManager takes full control) ---
    void Update() // Keep this for legacy UI or other periodic checks if necessary
    {
        if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat)
        {
            // If we are in WaitingForSpawnTime, constantly check if conditions are met
            if (currentInternalState == InternalWaveState.WaitingForSpawnTime)
            {
                Update_WaitingForSpawnTimeCheck();
            }
        }
        UpdateLegacyTimeTrackerUI();
        UpdateLegacyWaveStatusText();
    }

    private void UpdateLegacyTimeTrackerUI()
    {
        if (timeTrackerText == null || weatherManager == null) return;
        // ... (same as before, but consider if this UI is still needed)
        WeatherManager.CyclePhase phase = weatherManager.CurrentPhase;
        float total = weatherManager.CurrentTotalPhaseTime;
        float remaining = weatherManager.CurrentPhaseTimer;
        float progressPercent = (total > 0) ? (1f - (remaining / total)) * 100f : 0f;
        string phaseName = phase.ToString().Replace("Transition", "");
        timeTrackerText.text = $"{phaseName} [{progressPercent:F0}%]";
         if (RunManager.Instance != null)
         {
            if(RunManager.Instance.CurrentState == RunState.Planning) timeTrackerText.text += " (Planning)";
            else if(RunManager.Instance.CurrentState == RunState.Recovery) timeTrackerText.text += " (Recovery)";
         }
    }

    private void UpdateLegacyWaveStatusText()
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
                string waveNamePart = string.IsNullOrEmpty(currentActiveWaveDef.waveName) ? $"Wave {activeWaveDefinitionIndex + 1}" : currentActiveWaveDef.waveName;
                if (currentInternalState == InternalWaveState.WaitingForSpawnTime)
                    waveStatusText.text = $"{waveNamePart} - Waiting...";
                else if (currentInternalState == InternalWaveState.SpawningInProgress)
                    waveStatusText.text = $"{waveNamePart} - Spawning...";
                else if (currentInternalState == InternalWaveState.WaveActive)
                    waveStatusText.text = $"{waveNamePart} [{dayCyclesRemainingForThisWaveDef} cycles left]";
                else if (currentInternalState == InternalWaveState.Idle && dayCyclesRemainingForThisWaveDef <=0) // Wave finished
                     waveStatusText.text = $"{waveNamePart} - Cleared";
            }
            else if (currentInternalState == InternalWaveState.Idle) // No wave for this round, or sequence finished
            {
                waveStatusText.text = "All waves for round complete.";
            }
        }
        else if (RunManager.Instance.CurrentState == RunState.Recovery)
        {
            waveStatusText.text = "Round Recovering";
        }
    }
    public Camera GetMainCamera() { return mainCamera; } // If FaunaManager still needs it via WaveManager
}
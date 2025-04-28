// FILE: Assets/Scripts/Managers/WaveManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public enum WaveManagerState
{
    PausedBeforeRun,     // Initial state, game logic paused
    WaitingForSpawnTime, // Run started, waiting for correct time in cycle
    WaveInProgress,      // Spawning triggered for this cycle, counting down day cycles
    SequenceComplete     // All waves done
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Core Dependencies")]
    [SerializeField] private FaunaManager faunaManager;
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private Camera mainCamera;

    [Header("Wave Sequence")]
    [SerializeField] private List<WaveDefinition> wavesSequence;

    [Header("Wave Timing & Spawning (Global)")]
    [Tooltip("How many full Day+Night cycles each wave lasts.")]
    [SerializeField][Range(1, 10)] private int waveDurationInDayCycles = 1;
    [Tooltip("The phase during which spawning should occur each cycle.")]
    [SerializeField] private WeatherManager.CyclePhase spawnStartPhase = WeatherManager.CyclePhase.Day;
    [Tooltip("The percentage progress within the Spawn Start Phase when spawning triggers (0-100).")]
    [SerializeField][Range(0f, 100f)] private float spawnStartPercentage = 50f;
    [SerializeField] private bool loopSequence = false;
    [Tooltip("If checked, animals from the previous wave are destroyed when a new wave starts.")]
    [SerializeField] private bool deletePreviousWaveAnimals = true;

    [Header("UI & Feedback")]
    [SerializeField] private TextMeshProUGUI waveStatusText;
    [SerializeField] private Button startRunButton;
    [SerializeField] private TextMeshProUGUI timeTrackerText;

    [Header("State (Read Only)")]
    [SerializeField] private WaveManagerState currentState = WaveManagerState.PausedBeforeRun;
    [SerializeField] private int currentWaveIndex = -1;

    // --- Runtime State ---
    private WaveDefinition activeWaveDefinition = null;
    private int dayCyclesRemainingForWave = 0;
    private bool hasSpawnedThisCycle = false;
    private bool isInitialPause = true; // <<< NEW: Flag for initial pause

    // --- Public Accessors ---
    public WaveManagerState CurrentState => currentState;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => wavesSequence != null ? wavesSequence.Count : 0;
    public bool IsRunActive => currentState != WaveManagerState.PausedBeforeRun && currentState != WaveManagerState.SequenceComplete;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validations (same as before)
        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager missing!", this);
        if (weatherManager == null) Debug.LogError("[WaveManager] WeatherManager missing!", this);
        if (mainCamera == null) Debug.LogError("[WaveManager] Main Camera missing!", this);
        if (waveStatusText == null) Debug.LogWarning("[WaveManager] Wave Status Text missing.", this);
        if (timeTrackerText == null) Debug.LogWarning("[WaveManager] Time Tracker Text missing.", this);
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence empty.", this);
        if (startRunButton == null) Debug.LogWarning("[WaveManager] Start Run Button missing.", this);
        else startRunButton.onClick.AddListener(TryStartRun);

        // <<< SET INITIAL PAUSE >>>
        Debug.Log("[WaveManager Awake] Setting initial Time.timeScale = 0");
        Time.timeScale = 0f;
        isInitialPause = true;
        // -------------------------
    }

    void Start()
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged += HandleWeatherPhaseChange;
        InitializeManager(); // Sets state, updates button
    }

     void OnDestroy()
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged -= HandleWeatherPhaseChange;
        if (startRunButton != null) startRunButton.onClick.RemoveListener(TryStartRun);
        // Ensure timescale is reset if manager is destroyed
        if(isInitialPause || Time.timeScale != 1f) Time.timeScale = 1f;
    }

    void InitializeManager()
    {
        currentWaveIndex = -1;
        activeWaveDefinition = null;
        dayCyclesRemainingForWave = 0;
        hasSpawnedThisCycle = false;
        // Don't set timescale here anymore, Awake handles initial pause
        SetState(WaveManagerState.PausedBeforeRun);
    }

    void Update()
    {
        // Always update UI if possible
        if (weatherManager != null) UpdateTimeTrackerUI();

        // Skip logic if paused initially OR dependencies missing
        if (isInitialPause || faunaManager == null || weatherManager == null || mainCamera == null)
        {
            // Still need to manage button state even if paused initially
             if (currentState == WaveManagerState.PausedBeforeRun && startRunButton != null)
             {
                  startRunButton.gameObject.SetActive(true);
                  startRunButton.interactable = true;
             }
             return;
        }

        // State Machine Update (only runs after initial pause is over)
        switch (currentState)
        {
            case WaveManagerState.WaitingForSpawnTime: Update_WaitingForSpawnTime(); break;
            case WaveManagerState.WaveInProgress: /* Handled by event */ break;
            case WaveManagerState.SequenceComplete: Update_IdleReady(); break;
             // PausedBeforeRun is handled above
        }
    }

    // --- Event Handler (HandleWeatherPhaseChange) --- (Unchanged)
    void HandleWeatherPhaseChange(WeatherManager.CyclePhase newPhase) { if (currentState == WaveManagerState.PausedBeforeRun || isInitialPause) return; if (newPhase == WeatherManager.CyclePhase.TransitionToDay) { if (currentState == WaveManagerState.WaveInProgress) { dayCyclesRemainingForWave--; hasSpawnedThisCycle = false; if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Day cycle complete. Cycles remaining: {dayCyclesRemainingForWave}"); if (dayCyclesRemainingForWave <= 0) { EndWaveGameplay(); } else { SetState(WaveManagerState.WaitingForSpawnTime); UpdateWaveStatusText(); } } else if (currentState == WaveManagerState.WaitingForSpawnTime) { hasSpawnedThisCycle = false; } } else if (newPhase == spawnStartPhase && currentState == WaveManagerState.WaitingForSpawnTime && !hasSpawnedThisCycle) { Update_WaitingForSpawnTime(); } }


    // --- State Update Methods ---

    void Update_WaitingForSpawnTime() // (Unchanged)
    { if (hasSpawnedThisCycle || weatherManager == null) return; WeatherManager.CyclePhase currentPhase = weatherManager.CurrentPhase; float totalPhaseTime = weatherManager.CurrentTotalPhaseTime; float remainingPhaseTime = weatherManager.CurrentPhaseTimer; float progressPercent = (totalPhaseTime > 0) ? (1f - (remainingPhaseTime / totalPhaseTime)) * 100f : 0f; if (currentPhase == spawnStartPhase && progressPercent >= spawnStartPercentage) { StartWaveSpawning(); } }

    void Update_IdleReady() // (Now only manages button for looping)
    {
         if (startRunButton != null)
         {
            bool showButton = (currentState == WaveManagerState.SequenceComplete && loopSequence);
            startRunButton.gameObject.SetActive(showButton);
            startRunButton.interactable = showButton;
         }
    }

    void UpdateTimeTrackerUI() // (Unchanged)
    { if (timeTrackerText == null || weatherManager == null) return; WeatherManager.CyclePhase phase = weatherManager.CurrentPhase; float total = weatherManager.CurrentTotalPhaseTime; float remaining = weatherManager.CurrentPhaseTimer; float progressPercent = (total > 0) ? (1f - (remaining / total)) * 100f : 0f; string phaseName = phase.ToString().Replace("Transition", ""); timeTrackerText.text = $"{phaseName} [{progressPercent:F0}%]"; if (Time.timeScale == 0f && isInitialPause) timeTrackerText.text += " (Paused)"; } // Show paused during initial pause

    void UpdateWaveStatusText() // (Added PausedBeforeRun case)
    {
         if (waveStatusText == null) return;
         switch(currentState)
         {
             case WaveManagerState.PausedBeforeRun: waveStatusText.text = "Press Start Run"; break;
             case WaveManagerState.WaitingForSpawnTime: waveStatusText.text = $"Wave {CurrentWaveNumber} - Waiting..."; break;
             case WaveManagerState.WaveInProgress: waveStatusText.text = $"Wave {CurrentWaveNumber} [{dayCyclesRemainingForWave} cycles left]"; break;
            case WaveManagerState.SequenceComplete: waveStatusText.text = loopSequence ? "Sequence Done. Start Again?" : "All Waves Cleared!"; break;
             default: waveStatusText.text = ""; break;
         }
    }

    // --- State Transition and Action Methods ---

    void SetState(WaveManagerState newState) // (Removed weather pause logic)
    {
        if (currentState == newState) return;
        if(Debug.isDebugBuild) Debug.Log($"[WaveManager] State Change: {currentState} -> {newState}");
        currentState = newState;

        // Update Button State
        if (startRunButton != null)
        {
            bool showButton = (newState == WaveManagerState.PausedBeforeRun) || (newState == WaveManagerState.SequenceComplete && loopSequence);
            startRunButton.gameObject.SetActive(showButton);
            startRunButton.interactable = showButton;
        }

        // Update Status Text
        UpdateWaveStatusText();
    }

    public void TryStartRun() // <<< MODIFIED
    {
        // Only allow starting from PausedBeforeRun OR SequenceComplete if looping
        if (currentState != WaveManagerState.PausedBeforeRun && !(currentState == WaveManagerState.SequenceComplete && loopSequence))
        {
             if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Cannot start run. State: {currentState}, Looping: {loopSequence}");
             return;
        }

        Debug.Log("[WaveManager] Starting Run...");

        // <<< RESUME TIME >>>
        if (isInitialPause || Time.timeScale != 1f)
        {
            Debug.Log("[WaveManager] Setting Time.timeScale = 1");
            Time.timeScale = 1f;
            isInitialPause = false; // Mark initial pause as over
        }
        // ----------------

        InitializeRun(); // Prepare first wave state
        SetState(WaveManagerState.WaitingForSpawnTime); // Transition state
    }

    void InitializeRun() // (Unchanged)
    { currentWaveIndex = 0; if (wavesSequence == null || wavesSequence.Count == 0 || wavesSequence[currentWaveIndex] == null) { Debug.LogError("[WaveManager] Cannot initialize run: Bad wave sequence!"); SetState(WaveManagerState.SequenceComplete); return; } if(deletePreviousWaveAnimals && currentState == WaveManagerState.SequenceComplete && loopSequence) { ClearAllActiveAnimals(); } activeWaveDefinition = wavesSequence[currentWaveIndex]; dayCyclesRemainingForWave = waveDurationInDayCycles; hasSpawnedThisCycle = false; Debug.Log($"[WaveManager] Run initialized. Starting Wave {CurrentWaveNumber}. Duration: {dayCyclesRemainingForWave} cycles."); }

    void StartWaveSpawning() // (Unchanged)
    { if (activeWaveDefinition == null || currentState != WaveManagerState.WaitingForSpawnTime) { return; } Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} - Spawning Triggered (Phase: {spawnStartPhase} >= {spawnStartPercentage}%)"); hasSpawnedThisCycle = true; SetState(WaveManagerState.WaveInProgress); if (faunaManager != null) faunaManager.ExecuteSpawnWave(activeWaveDefinition); else Debug.LogError("[WaveManager] Cannot execute spawn wave, FaunaManager missing!"); }

    void EndWaveGameplay() // (Unchanged)
    { Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} Gameplay Ended (Duration Met)."); if (faunaManager != null) faunaManager.StopAllSpawnCoroutines(); if (deletePreviousWaveAnimals) { ClearAllActiveAnimals(); } activeWaveDefinition = null; currentWaveIndex++; if (currentWaveIndex >= wavesSequence.Count) { if (loopSequence) { Debug.Log("[WaveManager] Looping back."); InitializeRun(); SetState(WaveManagerState.WaitingForSpawnTime); } else { Debug.Log("[WaveManager] Sequence complete."); SetState(WaveManagerState.SequenceComplete); } } else { if (wavesSequence[currentWaveIndex] == null) { Debug.LogError($"[WaveManager] Wave definition {currentWaveIndex} NULL!"); SetState(WaveManagerState.SequenceComplete); return; } activeWaveDefinition = wavesSequence[currentWaveIndex]; dayCyclesRemainingForWave = waveDurationInDayCycles; hasSpawnedThisCycle = false; Debug.Log($"[WaveManager] Preparing Wave {CurrentWaveNumber}. Duration: {dayCyclesRemainingForWave} cycles."); SetState(WaveManagerState.WaitingForSpawnTime); } }

    void ClearAllActiveAnimals() // (Unchanged)
    { if(Debug.isDebugBuild) Debug.Log("[WaveManager] Clearing all active animals."); AnimalController[] activeAnimals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None); int count = 0; foreach(AnimalController animal in activeAnimals) { if(animal != null) { Destroy(animal.gameObject); count++; } } if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Destroyed {count} animals."); }

    public Camera GetMainCamera() { return mainCamera; } // (Unchanged)
}
// FILE: Assets/Scripts/Managers/WaveManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public enum WaveManagerState
{
    Idle_ReadyToStart,
    FastForwardingToDay,
    WaveIncomingDisplay,
    WaveInProgress,
    WaveClearedDisplay,
    BetweenWavesDelay,
    SequenceComplete
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

    [Header("Wave Timing & Flow")]
    [Tooltip("Duration (in seconds) each wave lasts IF 'Sync With Day Night' is FALSE.")]
    [SerializeField][Min(1f)] private float waveDurationSeconds = 60f;
    [Tooltip("Pause duration (in seconds) after a wave is cleared before the next one can be initiated.")]
    [SerializeField][Min(0f)] private float delayBetweenWaves = 10f;
    [Tooltip("How long (in seconds) the 'Wave Incoming' message displays.")]
    [SerializeField][Min(0.1f)] private float waveIncomingDisplayTime = 3.0f;
    [Tooltip("How long (in seconds) the 'Wave Cleared' message displays.")]
    [SerializeField][Min(0.1f)] private float waveClearedDisplayTime = 3.0f;
    [SerializeField] private bool loopSequence = false;
    [Tooltip("If checked, animals from the previous wave are destroyed when a new wave starts.")] // <<< NEW FIELD
    [SerializeField] private bool deletePreviousWaveAnimals = true; // <<< NEW FIELD

    [Header("Day/Night Synchronization")]
    [Tooltip("If true, waves start at Day and end when Night finishes. Ignores 'Wave Duration Seconds'.")]
    [SerializeField] private bool syncWithDayNight = false;
    [Tooltip("The speed multiplier applied to the WeatherManager when fast-forwarding to the start of the next day.")]
    [SerializeField][Range(1f, 100f)] private float fastForwardMultiplier = 10f;
    [Tooltip("If checked, the WeatherManager's time progression will be paused between waves.")] // <<< NEW FIELD
    [SerializeField] private bool pauseWeatherBetweenWaves = false; // <<< NEW FIELD

    [Header("UI & Feedback")]
    [SerializeField] private TextMeshProUGUI waveStatusText;
    [SerializeField] private string waveIncomingFormat = "Wave {0} Incoming!";
    [SerializeField] private string waveClearedFormat = "Wave {0} Cleared!";
    [SerializeField] private Button startWaveButton;
    [Tooltip("Assign TextMeshProUGUI to display current Day/Night progress.")] // <<< NEW FIELD
    [SerializeField] private TextMeshProUGUI timeTrackerText; // <<< NEW FIELD

    [Header("State (Read Only)")]
    [SerializeField] private WaveManagerState currentState = WaveManagerState.Idle_ReadyToStart;
    [SerializeField] private int currentWaveIndex = -1;

    // --- Runtime State ---
    private float currentTimer = 0f;
    private WaveDefinition activeWaveDefinition = null;
    private bool isFastForwarding = false;
    private Coroutine fastForwardCoroutine = null;

    // --- Public Accessors ---
    public WaveManagerState CurrentState => currentState;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => wavesSequence != null ? wavesSequence.Count : 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validations
        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager reference missing!", this);
        if (weatherManager == null) Debug.LogError("[WaveManager] WeatherManager reference missing!", this); // Now always required
        if (mainCamera == null) Debug.LogError("[WaveManager] Main Camera reference missing!", this);
        if (waveStatusText == null) Debug.LogWarning("[WaveManager] Wave Status Text reference missing.", this);
        if (timeTrackerText == null) Debug.LogWarning("[WaveManager] Time Tracker Text reference missing.", this); // <<< NEW VALIDATION
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence is empty.", this);
        if (startWaveButton != null) startWaveButton.onClick.AddListener(TryManualStartWave);
    }

    void Start()
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged += HandleWeatherPhaseChange;
        InitializeManager();
    }

    void OnDestroy()
    {
        if (weatherManager != null) weatherManager.OnPhaseChanged -= HandleWeatherPhaseChange;
        if (startWaveButton != null) startWaveButton.onClick.RemoveListener(TryManualStartWave);
        // Ensure weather is unpaused on destroy
        if (weatherManager != null) weatherManager.IsPaused = false;
    }

    void InitializeManager()
    {
        currentWaveIndex = -1;
        activeWaveDefinition = null;
        isFastForwarding = false;
        StopExistingFastForward();
        SetState(WaveManagerState.Idle_ReadyToStart);
    }

    void Update()
    {
        if (faunaManager == null || weatherManager == null || mainCamera == null) return;

        // Update Time Tracker UI regardless of state (as long as weather manager exists)
        UpdateTimeTrackerUI(); // <<< CALL UI UPDATE

        switch (currentState)
        {
            case WaveManagerState.Idle_ReadyToStart: Update_IdleReady(); break;
            case WaveManagerState.FastForwardingToDay: Update_FastForwarding(); break;
            case WaveManagerState.WaveIncomingDisplay: Update_TimerBasedStates(); break;
            case WaveManagerState.WaveClearedDisplay: Update_TimerBasedStates(); break;
            case WaveManagerState.BetweenWavesDelay: Update_TimerBasedStates(); break;
            case WaveManagerState.WaveInProgress: Update_WaveInProgress(); break;
            case WaveManagerState.SequenceComplete: Update_IdleReady(); break;
        }
    }

    // --- Event Handler ---
    void HandleWeatherPhaseChange(WeatherManager.CyclePhase newPhase)
    {
        // End synced wave when night ends
        if (syncWithDayNight && newPhase == WeatherManager.CyclePhase.TransitionToDay && currentState == WaveManagerState.WaveInProgress)
        {
            EndWaveGameplay();
        }

        // Stop fast forward when Day starts
        if (isFastForwarding && newPhase == WeatherManager.CyclePhase.Day)
        {
            // Coroutine handles the transition
        }
    }

    // --- State Update Methods ---

    void Update_IdleReady()
    {
        // Manage button state
        if (startWaveButton != null)
        {
            bool canStartNow = !syncWithDayNight || (weatherManager.CurrentPhase == WeatherManager.CyclePhase.Day);
            // Button interactable if not sequence complete OR looping enabled
            startWaveButton.interactable = !(currentState == WaveManagerState.SequenceComplete && !loopSequence);
             // Button visible unless sequence is complete AND not looping
            startWaveButton.gameObject.SetActive(!(currentState == WaveManagerState.SequenceComplete && !loopSequence));
        }
    }

     void Update_FastForwarding()
    {
        // Keep button disabled
        if (startWaveButton != null)
        {
             startWaveButton.interactable = false;
             startWaveButton.gameObject.SetActive(true);
        }
    }

    void Update_TimerBasedStates()
    {
        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            if (currentState == WaveManagerState.WaveIncomingDisplay) StartWaveGameplay();
            else if (currentState == WaveManagerState.WaveClearedDisplay) SetState(WaveManagerState.BetweenWavesDelay);
            else if (currentState == WaveManagerState.BetweenWavesDelay) SetState(WaveManagerState.Idle_ReadyToStart);
        }
    }

    void Update_WaveInProgress()
    {
        if (!syncWithDayNight)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0f) EndWaveGameplay();
        }
        // Synced end handled by phase change event
    }

    /// <summary>
    /// Updates the optional Time Tracker UI element.
    /// </summary>
    void UpdateTimeTrackerUI() // <<< NEW METHOD
    {
        if (timeTrackerText == null || weatherManager == null) return;

        WeatherManager.CyclePhase phase = weatherManager.CurrentPhase;
        float total = weatherManager.CurrentTotalPhaseTime;
        float remaining = weatherManager.CurrentPhaseTimer;
        float progressPercent = 0f;

        if (total > 0)
        {
            progressPercent = (1f - (remaining / total)) * 100f;
        }

        // Format the string based on the phase
        string phaseName = phase.ToString();
        // Optional: Make phase names more user-friendly
        if (phase == WeatherManager.CyclePhase.TransitionToDay) phaseName = "Sunrise";
        else if (phase == WeatherManager.CyclePhase.TransitionToNight) phaseName = "Sunset";

        timeTrackerText.text = $"{phaseName} [{progressPercent:F0}%]";

        // Optionally add paused indicator
        if (weatherManager.IsPaused)
        {
             timeTrackerText.text += " (Paused)";
        }
    }


    // --- State Transition and Action Methods ---

    void SetState(WaveManagerState newState)
    {
        if (currentState == newState) return;
        if(Debug.isDebugBuild) Debug.Log($"[WaveManager] State Change: {currentState} -> {newState}");
        currentState = newState;

        // --- Handle Weather Pause ---
        if (weatherManager != null)
        {
            bool shouldPause = pauseWeatherBetweenWaves &&
                               (newState == WaveManagerState.Idle_ReadyToStart ||
                                newState == WaveManagerState.BetweenWavesDelay ||
                                newState == WaveManagerState.SequenceComplete); // Also pause when sequence complete

            if (shouldPause && !weatherManager.IsPaused)
            {
                if(Debug.isDebugBuild) Debug.Log("[WaveManager] Pausing WeatherManager.");
                weatherManager.IsPaused = true;
                StopExistingFastForward(); // Ensure FF stops if we pause
            }
            else if (!shouldPause && weatherManager.IsPaused)
            {
                 if(Debug.isDebugBuild) Debug.Log("[WaveManager] Unpausing WeatherManager.");
                 weatherManager.IsPaused = false;
            }
        }
        // -------------------------

        // Stop fast forward if leaving FF state or entering pause-compatible state
        if (isFastForwarding && newState != WaveManagerState.FastForwardingToDay)
        {
            StopExistingFastForward();
        }


        // Entry actions
        switch (newState)
        {
            case WaveManagerState.Idle_ReadyToStart:
                if (waveStatusText != null) waveStatusText.text = ""; // Clear status text
                Update_IdleReady(); // Update button
                break;

            case WaveManagerState.FastForwardingToDay:
                 if (waveStatusText != null) waveStatusText.text = "Fast Forwarding to Day...";
                 Update_FastForwarding(); // Update button
                 break;

            case WaveManagerState.WaveIncomingDisplay:
                if (waveStatusText != null) waveStatusText.text = string.Format(waveIncomingFormat, CurrentWaveNumber);
                currentTimer = waveIncomingDisplayTime;
                if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
                break;

            case WaveManagerState.WaveInProgress:
                if (waveStatusText != null) waveStatusText.text = "";
                currentTimer = syncWithDayNight ? float.MaxValue : waveDurationSeconds;
                if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
                break;

            case WaveManagerState.WaveClearedDisplay:
                if (waveStatusText != null) waveStatusText.text = string.Format(waveClearedFormat, CurrentWaveNumber);
                currentTimer = waveClearedDisplayTime;
                if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
                break;

            case WaveManagerState.BetweenWavesDelay:
                 if (waveStatusText != null) waveStatusText.text = "";
                 currentTimer = delayBetweenWaves;
                 if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
                break;

             case WaveManagerState.SequenceComplete:
                 if (waveStatusText != null) waveStatusText.text = "All Waves Cleared!";
                 activeWaveDefinition = null;
                 Update_IdleReady(); // Update button visibility based on loop setting
                 break;
        }
    }

    public void TryManualStartWave()
    {
        if (currentState != WaveManagerState.Idle_ReadyToStart || isFastForwarding) { return; }

        // If paused, unpause first - starting a wave always resumes time
        if(weatherManager != null && weatherManager.IsPaused)
        {
             if(Debug.isDebugBuild) Debug.Log("[WaveManager] Start Wave button clicked while paused. Unpausing.");
             weatherManager.IsPaused = false;
             // Update UI immediately if needed
             UpdateTimeTrackerUI();
        }

        if (syncWithDayNight && weatherManager != null && weatherManager.CurrentPhase != WeatherManager.CyclePhase.Day)
        {
            if (isFastForwarding) return; // Don't start another if already running
            StopExistingFastForward(); // Stop just in case (shouldn't be needed)
            fastForwardCoroutine = StartCoroutine(FastForwardToDayCoroutine());
        }
        else
        {
            ProceedToNextWave();
        }
    }

    private void ProceedToNextWave()
    {
         int nextIndex = currentWaveIndex + 1;
        if (nextIndex >= wavesSequence.Count)
        {
            if (loopSequence) nextIndex = 0;
            else { SetState(WaveManagerState.SequenceComplete); return; }
        }
        if (wavesSequence[nextIndex] == null)
        {
             Debug.LogError($"[WaveManager] Wave definition at index {nextIndex} is NULL!");
             currentWaveIndex = nextIndex; SetState(WaveManagerState.Idle_ReadyToStart); return;
        }

        // --- Delete previous animals IF flag is set ---
        if(deletePreviousWaveAnimals && currentWaveIndex >= 0) // Only delete if not the very first wave
        {
             ClearAllActiveAnimals();
        }
        // ----------------------------------------------

        currentWaveIndex = nextIndex;
        activeWaveDefinition = wavesSequence[currentWaveIndex];
        SetState(WaveManagerState.WaveIncomingDisplay);
    }

    private IEnumerator FastForwardToDayCoroutine()
    {
        isFastForwarding = true;
        SetState(WaveManagerState.FastForwardingToDay);

        if (weatherManager != null)
        {
             // Ensure unpaused during fast forward
             weatherManager.IsPaused = false;
             weatherManager.timeScaleMultiplier = fastForwardMultiplier;
             if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Fast Forward started. Weather time scale: {weatherManager.timeScaleMultiplier:F1}");

            while (weatherManager.CurrentPhase != WeatherManager.CyclePhase.Day && isFastForwarding)
            { yield return null; }

            if (isFastForwarding) // Check flag again
            {
                 StopExistingFastForward(); // Resets speed and flag
                 Debug.Log("[WaveManager] Fast Forward finished. Day reached.");
                 ProceedToNextWave();
            }
        } else { /* Error handling */ isFastForwarding = false; SetState(WaveManagerState.Idle_ReadyToStart); }
        fastForwardCoroutine = null;
    }

    private void StopExistingFastForward()
    {
        if (isFastForwarding)
        {
            if (Debug.isDebugBuild) Debug.Log("[WaveManager] Stopping Fast Forward.");
            isFastForwarding = false;
            if (fastForwardCoroutine != null) { StopCoroutine(fastForwardCoroutine); fastForwardCoroutine = null; }
            if (weatherManager != null && weatherManager.timeScaleMultiplier != 1f) { weatherManager.timeScaleMultiplier = 1f; }
            // Re-evaluate pause state after stopping FF
            if(pauseWeatherBetweenWaves && currentState == WaveManagerState.Idle_ReadyToStart) {
                if(weatherManager != null) weatherManager.IsPaused = true;
            }
        }
    }

    void StartWaveGameplay()
    {
        if (activeWaveDefinition == null) { Debug.LogError("[WaveManager] ActiveWaveDefinition null!"); SetState(WaveManagerState.Idle_ReadyToStart); return; }
        Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} Gameplay Starting!");
        // Ensure weather is running
        if(weatherManager != null) weatherManager.IsPaused = false;
        SetState(WaveManagerState.WaveInProgress);
        if (faunaManager != null) faunaManager.ExecuteSpawnWave(activeWaveDefinition);
        else Debug.LogError("[WaveManager] Cannot execute spawn wave, FaunaManager missing!");
    }

    void EndWaveGameplay()
    {
        Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} Gameplay Ended.");
        if (faunaManager != null) faunaManager.StopAllSpawnCoroutines();
        activeWaveDefinition = null;
        bool isLastWave = currentWaveIndex + 1 >= wavesSequence.Count;
        // Transition to Cleared Display FIRST, which then transitions to Delay/Complete/Idle
        SetState(WaveManagerState.WaveClearedDisplay);
    }

    /// <summary>
    /// Destroys all GameObjects with an AnimalController component in the scene.
    /// </summary>
    void ClearAllActiveAnimals() // <<< NEW METHOD
    {
         if(Debug.isDebugBuild) Debug.Log("[WaveManager] Clearing all active animals.");
         // Use FindObjectsByType for better performance than FindObjectsOfType
         AnimalController[] activeAnimals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
         int count = 0;
         foreach(AnimalController animal in activeAnimals)
         {
             if(animal != null) // Extra safety check
             {
                 Destroy(animal.gameObject);
                 count++;
             }
         }
         if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Destroyed {count} animals.");
    }


    // --- Get Main Camera ---
    public Camera GetMainCamera() { return mainCamera; }
}
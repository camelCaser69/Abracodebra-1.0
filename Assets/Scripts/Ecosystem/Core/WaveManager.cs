// FILE: Assets/Scripts/Managers/WaveManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI; // Still needed for Button

public enum WaveManagerState
{
    Idle_ReadyToStart,
    FastForwardingToDay, // <<< NEW STATE
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

    [Header("Day/Night Synchronization")]
    [Tooltip("If true, waves start at Day and end when Night finishes. Ignores 'Wave Duration Seconds'.")]
    [SerializeField] private bool syncWithDayNight = false;
    [Tooltip("The speed multiplier applied to the WeatherManager when fast-forwarding to the start of the next day.")] // <<< NEW FIELD
    [SerializeField][Range(1f, 100f)] private float fastForwardMultiplier = 10f; // <<< NEW FIELD

    [Header("UI & Feedback")]
    [SerializeField] private TextMeshProUGUI waveStatusText;
    [SerializeField] private string waveIncomingFormat = "Wave {0} Incoming!";
    [SerializeField] private string waveClearedFormat = "Wave {0} Cleared!";
    [SerializeField] private Button startWaveButton;

    [Header("State (Read Only)")]
    [SerializeField] private WaveManagerState currentState = WaveManagerState.Idle_ReadyToStart;
    [SerializeField] private int currentWaveIndex = -1;

    // --- Runtime State ---
    private float currentTimer = 0f;
    private WaveDefinition activeWaveDefinition = null;
    // REMOVED: waitingForDayStart flag (state handles this now)
    private bool isFastForwarding = false; // <<< NEW: Track FF state
    private Coroutine fastForwardCoroutine = null; // <<< NEW: Track FF coroutine

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
        if (weatherManager == null) Debug.LogError("[WaveManager] WeatherManager reference is missing!", this);
        if (mainCamera == null) Debug.LogError("[WaveManager] Main Camera reference is missing!", this);
        if (waveStatusText == null) Debug.LogWarning("[WaveManager] Wave Status Text reference missing.", this);
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence is empty.", this);

        if (startWaveButton != null) startWaveButton.onClick.AddListener(TryManualStartWave);
        // REMOVED: Slider setup
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
        // REMOVED: Slider listener removal
    }


    void InitializeManager()
    {
        currentWaveIndex = -1;
        activeWaveDefinition = null;
        isFastForwarding = false;
        StopExistingFastForward(); // Ensure any previous FF is stopped
        SetState(WaveManagerState.Idle_ReadyToStart);
    }

    void Update()
    {
        if (faunaManager == null || weatherManager == null || mainCamera == null) return;

        switch (currentState)
        {
            case WaveManagerState.Idle_ReadyToStart:
                Update_IdleReady(); // Manages button interactivity
                break;
            // FastForwardingToDay has no timer, waits for phase change event
            case WaveManagerState.FastForwardingToDay:
                 Update_FastForwarding(); // Manage button interactivity during FF
                 break;
            case WaveManagerState.WaveIncomingDisplay:
            case WaveManagerState.WaveClearedDisplay:
            case WaveManagerState.BetweenWavesDelay:
                Update_TimerBasedStates();
                break;
            case WaveManagerState.WaveInProgress:
                Update_WaveInProgress();
                break;
            case WaveManagerState.SequenceComplete:
                Update_IdleReady(); // Still manage button if looping
                break;
        }
    }

    // --- Event Handler ---
    void HandleWeatherPhaseChange(WeatherManager.CyclePhase newPhase)
    {
        // --- Handle ending a synced wave ---
        if (syncWithDayNight && newPhase == WeatherManager.CyclePhase.TransitionToDay && currentState == WaveManagerState.WaveInProgress)
        {
            Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} ending: TransitionToDay started.");
            EndWaveGameplay();
        }

        // --- Handle ending fast-forward ---
        if (isFastForwarding && newPhase == WeatherManager.CyclePhase.Day)
        {
            Debug.Log("[WaveManager] Day phase reached during fast forward.");
            // The coroutine will handle stopping FF and starting the wave
            // No direct action needed here, coroutine is waiting for this phase
        }
    }

    // --- State Update Methods ---

    void Update_IdleReady()
    {
        if (startWaveButton != null)
        {
            // Can start if not syncing OR (if syncing AND it's currently Day)
            bool canStartImmediately = !syncWithDayNight || (weatherManager != null && weatherManager.CurrentPhase == WeatherManager.CyclePhase.Day);
            // Can *trigger* start if not syncing OR syncing (will fast forward if needed)
            bool canTriggerStart = !syncWithDayNight || syncWithDayNight;
            // Button is interactable if we can trigger a start AND not currently completing sequence
            startWaveButton.interactable = canTriggerStart && currentState != WaveManagerState.SequenceComplete;
            // Button is visible unless sequence is complete AND not looping
            startWaveButton.gameObject.SetActive(!(currentState == WaveManagerState.SequenceComplete && !loopSequence));
        }
    }

    void Update_FastForwarding()
    {
        // Keep start button disabled while fast forwarding
        if (startWaveButton != null)
        {
             startWaveButton.interactable = false;
             startWaveButton.gameObject.SetActive(true); // Keep it visible but disabled
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
        // Synced wave end is handled by HandleWeatherPhaseChange
    }

    // --- State Transition and Action Methods ---

    void SetState(WaveManagerState newState)
    {
        if (currentState == newState) return;

        if(Debug.isDebugBuild) Debug.Log($"[WaveManager] State Change: {currentState} -> {newState}");
        WaveManagerState previousState = currentState;
        currentState = newState;

        // Stop fast forward if leaving FF state or entering a state incompatible with FF
        if (isFastForwarding && newState != WaveManagerState.FastForwardingToDay)
        {
            StopExistingFastForward();
        }

        // Handle entry actions
        switch (newState)
        {
            case WaveManagerState.Idle_ReadyToStart:
                if (waveStatusText != null) waveStatusText.text = "";
                Update_IdleReady(); // Update button state
                break;

            case WaveManagerState.FastForwardingToDay: // <<< NEW ENTRY ACTION
                 if (waveStatusText != null) waveStatusText.text = "Fast Forwarding to Day...";
                 Update_FastForwarding(); // Update button state
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
                 Update_IdleReady(); // Update button state for looping etc.
                 break;
        }
    }

    /// <summary>
    /// Called by UI Button. Attempts to start the next wave, initiating fast-forward if needed.
    /// </summary>
    public void TryManualStartWave()
    {
        // Prevent starting if not ready, or already fast-forwarding/in progress
        if (currentState != WaveManagerState.Idle_ReadyToStart || isFastForwarding)
        {
             if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Cannot start wave. State: {currentState}, IsFastForwarding: {isFastForwarding}");
             return;
        }

        // Check if we need to fast forward (Sync enabled and not Day phase)
        if (syncWithDayNight && weatherManager != null && weatherManager.CurrentPhase != WeatherManager.CyclePhase.Day)
        {
            Debug.Log("[WaveManager] Start triggered, but not Day phase. Initiating Fast Forward.");
            StopExistingFastForward(); // Ensure no previous FF running
            fastForwardCoroutine = StartCoroutine(FastForwardToDayCoroutine());
        }
        else
        {
            // Start immediately (either not syncing, or already Day)
            ProceedToNextWave();
        }
    }

    /// <summary>
    /// Advances the wave index and transitions to the WaveIncomingDisplay state.
    /// Called either directly or by the FastForward coroutine.
    /// </summary>
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
             currentWaveIndex = nextIndex; // Skip it
             SetState(WaveManagerState.Idle_ReadyToStart);
             return;
        }

        // Set up and start the wave sequence
        currentWaveIndex = nextIndex;
        activeWaveDefinition = wavesSequence[currentWaveIndex];
        SetState(WaveManagerState.WaveIncomingDisplay); // Transition to incoming message
    }

    /// <summary>
    /// Coroutine that speeds up time until the Day phase begins.
    /// </summary>
    private IEnumerator FastForwardToDayCoroutine()
    {
        isFastForwarding = true;
        SetState(WaveManagerState.FastForwardingToDay); // Update state

        if (weatherManager != null)
        {
            weatherManager.timeScaleMultiplier = fastForwardMultiplier;
            if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Fast Forward started. Weather time scale: {weatherManager.timeScaleMultiplier}");

            // Wait until the phase changes TO Day
            while (weatherManager.CurrentPhase != WeatherManager.CyclePhase.Day && isFastForwarding) // Check isFastForwarding flag in case it's stopped externally
            {
                yield return null; // Wait for the next frame
            }

            // Stop fast forward ONLY if this coroutine instance is the one that finished it
            if (isFastForwarding) // Check flag again
            {
                 StopExistingFastForward(); // Resets speed and flag
                 Debug.Log("[WaveManager] Fast Forward finished. Day reached.");
                 ProceedToNextWave(); // Now start the actual wave
            }
        }
        else {
             Debug.LogError("[WaveManager] Cannot Fast Forward: WeatherManager is null.");
             isFastForwarding = false; // Ensure flag is reset
             SetState(WaveManagerState.Idle_ReadyToStart); // Go back to idle if FF failed
        }
        fastForwardCoroutine = null; // Clear coroutine reference
    }

    /// <summary>
    /// Safely stops the fast forward coroutine and resets weather time scale.
    /// </summary>
    private void StopExistingFastForward()
    {
        if (isFastForwarding)
        {
            if (Debug.isDebugBuild) Debug.Log("[WaveManager] Stopping Fast Forward.");
            isFastForwarding = false;
            if (fastForwardCoroutine != null)
            {
                StopCoroutine(fastForwardCoroutine);
                fastForwardCoroutine = null;
            }
            if (weatherManager != null && weatherManager.timeScaleMultiplier != 1f)
            {
                weatherManager.timeScaleMultiplier = 1f; // Reset weather speed
            }
        }
    }


    void StartWaveGameplay()
    {
        if (activeWaveDefinition == null) { Debug.LogError("[WaveManager] ActiveWaveDefinition null in StartWaveGameplay!"); SetState(WaveManagerState.Idle_ReadyToStart); return; }
        Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} Gameplay Starting!");
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
        if (isLastWave && !loopSequence)
        { SetState(WaveManagerState.WaveClearedDisplay); /* Transition handled in SetState */ }
        else
        { SetState(WaveManagerState.WaveClearedDisplay); /* Transition handled in SetState */ }
    }


    // --- Fast Forward Slider --- (REMOVED)
    // private void OnFastForwardSliderChanged(float value) { ... }

    // --- Get Main Camera ---
    public Camera GetMainCamera() { return mainCamera; }
}
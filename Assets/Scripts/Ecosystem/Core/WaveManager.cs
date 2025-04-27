// FILE: Assets/Scripts/Managers/WaveManager.cs (Example Path)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Required for TextMeshPro UI
using UnityEngine.UI; // Required for Button

public enum WaveManagerState // Renamed slightly for clarity
{
    Idle_ReadyToStart, // Waiting for manual start or sync condition
    WaveIncomingDisplay, // Showing "Wave Incoming" message
    WaveInProgress,      // Spawning and duration timer running
    WaveClearedDisplay,  // Showing "Wave Cleared" message
    BetweenWavesDelay,   // Waiting for delay between waves to pass
    SequenceComplete     // All waves done
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; } // Optional Singleton

    [Header("Core Dependencies")]
    [Tooltip("Reference to the FaunaManager that handles actual spawning.")]
    [SerializeField] private FaunaManager faunaManager;
    [Tooltip("Reference to the WeatherManager for day/night cycle info.")]
    [SerializeField] private WeatherManager weatherManager;
    [Tooltip("Reference to the main game camera.")]
    [SerializeField] private Camera mainCamera; // Needed for offscreen spawning checks

    [Header("Wave Sequence")]
    [Tooltip("The sequence of Wave Definitions to execute.")]
    [SerializeField] private List<WaveDefinition> wavesSequence;

    [Header("Wave Timing & Flow")]
    [Tooltip("Duration (in seconds) each wave lasts. Timer starts AFTER the 'Incoming' display.")]
    [SerializeField][Min(1f)] private float waveDurationSeconds = 60f;
    [Tooltip("Pause duration (in seconds) after a wave is cleared before the next one can be initiated.")]
    [SerializeField][Min(0f)] private float delayBetweenWaves = 10f;
    [Tooltip("How long (in seconds) the 'Wave Incoming' message displays before the wave timer starts.")]
    [SerializeField][Min(0.1f)] private float waveIncomingDisplayTime = 3.0f;
    [Tooltip("How long (in seconds) the 'Wave Cleared' message displays.")]
    [SerializeField][Min(0.1f)] private float waveClearedDisplayTime = 3.0f;
    [SerializeField] private bool loopSequence = false; // Should waves repeat after finishing?

    [Header("Day/Night Synchronization (Optional)")]
    [Tooltip("If true, wave start/end might be influenced by the day/night cycle.")]
    [SerializeField] private bool syncWithDayNight = false;
    [Tooltip("If Sync is true, wave can only START when sunIntensity is >= this value (e.g., 0.9 for near sunrise).")]
    [SerializeField][Range(0f, 1f)] private float startWaveAtSunIntensity = 0.9f;
    [Tooltip("If Sync is true, wave will be forced to END when sunIntensity is <= this value (e.g., 0.1 for near sunset).")]
    [SerializeField][Range(0f, 1f)] private float forceEndWaveAtSunIntensity = 0.1f;

    [Header("UI & Feedback")]
    [Tooltip("Assign the TextMeshProUGUI element for wave status messages.")]
    [SerializeField] private TextMeshProUGUI waveStatusText;
    [Tooltip("Format string for incoming wave message. {0} = Wave Number.")]
    [SerializeField] private string waveIncomingFormat = "Wave {0} Incoming!";
    [Tooltip("Format string for wave cleared message. {0} = Wave Number.")]
    [SerializeField] private string waveClearedFormat = "Wave {0} Cleared!";
    [Tooltip("Assign the UI Button used to manually start the next wave.")]
    [SerializeField] private Button startWaveButton; // Optional: Can be triggered via code too

    [Header("State (Read Only)")]
    [SerializeField] private WaveManagerState currentState = WaveManagerState.Idle_ReadyToStart;
    [SerializeField] private int currentWaveIndex = -1; // -1 means not started yet

    // --- Runtime State ---
    private float currentTimer = 0f; // Used for various timed states
    private WaveDefinition activeWaveDefinition = null;

    // --- Public Accessors ---
    public WaveManagerState CurrentState => currentState;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => wavesSequence != null ? wavesSequence.Count : 0;

    void Awake()
    {
        // Optional Singleton Pattern
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validations
        if (faunaManager == null) Debug.LogError("[WaveManager] FaunaManager reference is missing!", this);
        if (weatherManager == null && syncWithDayNight) Debug.LogError("[WaveManager] WeatherManager reference is missing but Sync With Day/Night is enabled!", this);
        if (mainCamera == null) Debug.LogError("[WaveManager] Main Camera reference is missing!", this); // Needed for offscreen calc
        if (waveStatusText == null) Debug.LogWarning("[WaveManager] Wave Status Text reference is missing.", this);
        if (wavesSequence == null || wavesSequence.Count == 0) Debug.LogWarning("[WaveManager] Wave Sequence is empty.", this);
        if (startWaveButton != null) startWaveButton.onClick.AddListener(TryManualStartWave); // Hook up button listener
    }

    void Start()
    {
        InitializeManager();
    }

    void InitializeManager()
    {
        currentWaveIndex = -1;
        activeWaveDefinition = null;
        SetState(WaveManagerState.Idle_ReadyToStart); // Start ready
    }

    void Update()
    {
        // Don't run update logic if dependencies are missing
        if (faunaManager == null || mainCamera == null) return;
        if (syncWithDayNight && weatherManager == null) return;

        // Main State Machine Update
        switch (currentState)
        {
            case WaveManagerState.Idle_ReadyToStart:
                Update_IdleReady();
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
                // Do nothing unless looping is enabled and triggered
                break;
        }
    }

    // --- State Update Methods ---

    void Update_IdleReady()
    {
        // State waits for manual trigger (TryManualStartWave)
        // OR checks sync condition if enabled
        if (syncWithDayNight && weatherManager != null)
        {
            if (weatherManager.sunIntensity >= startWaveAtSunIntensity)
            {
                // Sun condition met, attempt to start (TryManualStartWave handles sequence checks)
                TryManualStartWave();
            }
        }
        // Button interaction is handled by the Button's OnClick event calling TryManualStartWave
        if (startWaveButton != null)
        {
             // Enable button only when ready (and sync conditions met if applicable)
             bool canStartSync = !syncWithDayNight || (weatherManager != null && weatherManager.sunIntensity >= startWaveAtSunIntensity);
             startWaveButton.interactable = canStartSync;
        }
    }

    void Update_TimerBasedStates()
    {
        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            // Timer elapsed, transition based on current state
            if (currentState == WaveManagerState.WaveIncomingDisplay)
            {
                StartWaveGameplay();
            }
            else if (currentState == WaveManagerState.WaveClearedDisplay || currentState == WaveManagerState.BetweenWavesDelay)
            {
                SetState(WaveManagerState.Idle_ReadyToStart);
            }
        }
    }

    void Update_WaveInProgress()
    {
        currentTimer -= Time.deltaTime;

        // Check forced end condition by time-of-day first
        if (syncWithDayNight && weatherManager != null)
        {
            if (weatherManager.sunIntensity <= forceEndWaveAtSunIntensity)
            {
                Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} forcibly ended due to time of day (Sun Intensity <= {forceEndWaveAtSunIntensity}).");
                currentTimer = 0f; // Force timer end
            }
        }

        // Check if timer has run out
        if (currentTimer <= 0f)
        {
            EndWaveGameplay();
        }
    }

    // --- State Transition and Action Methods ---

    void SetState(WaveManagerState newState)
    {
        if (currentState == newState) return; // No change

        if(Debug.isDebugBuild) Debug.Log($"[WaveManager] State Change: {currentState} -> {newState}");
        currentState = newState;

        // Handle entry actions for the new state
        switch (newState)
        {
            case WaveManagerState.Idle_ReadyToStart:
                if (waveStatusText != null) waveStatusText.text = ""; // Clear status
                if (startWaveButton != null) startWaveButton.gameObject.SetActive(true); // Show start button
                // Check sync immediately
                Update_IdleReady();
                break;

            case WaveManagerState.WaveIncomingDisplay:
                if (waveStatusText != null) waveStatusText.text = string.Format(waveIncomingFormat, CurrentWaveNumber);
                currentTimer = waveIncomingDisplayTime;
                if (startWaveButton != null) startWaveButton.gameObject.SetActive(false); // Hide start button
                break;

            case WaveManagerState.WaveInProgress:
                if (waveStatusText != null) waveStatusText.text = ""; // Clear text during wave
                currentTimer = waveDurationSeconds;
                break;

            case WaveManagerState.WaveClearedDisplay:
                if (waveStatusText != null) waveStatusText.text = string.Format(waveClearedFormat, CurrentWaveNumber);
                currentTimer = waveClearedDisplayTime;
                 if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
                break;

            case WaveManagerState.BetweenWavesDelay:
                 if (waveStatusText != null) waveStatusText.text = ""; // Optionally clear text or show "Preparing..."
                 currentTimer = delayBetweenWaves;
                 if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
                break;

             case WaveManagerState.SequenceComplete:
                 if (waveStatusText != null) waveStatusText.text = "All Waves Cleared!";
                 activeWaveDefinition = null; // Clear current wave def
                 if (startWaveButton != null) startWaveButton.gameObject.SetActive(!loopSequence); // Hide if not looping
                 break;
        }
    }

    /// <summary>
    /// Called by UI Button or potentially other game events.
    /// </summary>
    public void TryManualStartWave()
    {
        if (currentState != WaveManagerState.Idle_ReadyToStart)
        {
             if(Debug.isDebugBuild) Debug.Log("[WaveManager] Cannot start wave, not in Idle_ReadyToStart state.");
             return;
        }

        // Check day/night sync condition again just before starting
        if (syncWithDayNight && weatherManager != null)
        {
            if (weatherManager.sunIntensity < startWaveAtSunIntensity)
            {
                 if(Debug.isDebugBuild) Debug.Log($"[WaveManager] Cannot start wave, waiting for sun intensity >= {startWaveAtSunIntensity} (currently {weatherManager.sunIntensity:F2}).");
                 // Optionally display a message to the player
                 return;
            }
        }

        // Proceed to start the next wave
        int nextIndex = currentWaveIndex + 1;

        // Check sequence bounds
        if (nextIndex >= wavesSequence.Count)
        {
            if (loopSequence)
            {
                Debug.Log("[WaveManager] Reached end of sequence, looping.");
                nextIndex = 0;
            }
            else
            {
                Debug.Log("[WaveManager] Reached end of sequence, no more waves.");
                SetState(WaveManagerState.SequenceComplete);
                return;
            }
        }

        // Validate the wave definition itself
        if (wavesSequence[nextIndex] == null)
        {
             Debug.LogError($"[WaveManager] Wave definition at index {nextIndex} is NULL! Cannot start wave.");
             // Maybe try to advance *past* the null one? Risky. Better to fix the setup.
             currentWaveIndex = nextIndex; // Advance index anyway to potentially skip null on next attempt
             SetState(WaveManagerState.Idle_ReadyToStart); // Stay ready
             return;
        }

        // --- Start the wave ---
        currentWaveIndex = nextIndex;
        activeWaveDefinition = wavesSequence[currentWaveIndex];
        SetState(WaveManagerState.WaveIncomingDisplay); // Start with the "Incoming" message
    }

    void StartWaveGameplay()
    {
        if (activeWaveDefinition == null)
        {
            Debug.LogError("[WaveManager] Cannot start wave gameplay, activeWaveDefinition is null!");
            SetState(WaveManagerState.Idle_ReadyToStart); // Go back to safety
            return;
        }

        Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} Gameplay Starting! Duration: {waveDurationSeconds}s");
        SetState(WaveManagerState.WaveInProgress);

        // Tell FaunaManager to start spawning this wave's content
        if (faunaManager != null)
        {
            faunaManager.ExecuteSpawnWave(activeWaveDefinition);
        } else {
             Debug.LogError("[WaveManager] Cannot execute spawn wave, FaunaManager reference is missing!");
        }
    }

    void EndWaveGameplay()
    {
        Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} Gameplay Ended.");

        // TODO: Optionally tell FaunaManager to clean up remaining wave enemies?
        // faunaManager.ClearWaveEnemies(activeWaveDefinition); // Needs implementation in FaunaManager if desired

        activeWaveDefinition = null; // Clear the active wave

        // Decide next state based on whether sequence is complete
        if (currentWaveIndex + 1 >= wavesSequence.Count && !loopSequence)
        {
            // This was the last wave
             SetState(WaveManagerState.WaveClearedDisplay); // Show cleared message first
             // After cleared display timer, transition to SequenceComplete
             StartCoroutine(TransitionAfterDelay(waveClearedDisplayTime, WaveManagerState.SequenceComplete));
        }
        else
        {
            // More waves remaining, or looping
            SetState(WaveManagerState.WaveClearedDisplay); // Show cleared message
            // After cleared display timer, transition to BetweenWavesDelay
            StartCoroutine(TransitionAfterDelay(waveClearedDisplayTime, WaveManagerState.BetweenWavesDelay));
        }
    }

    // Helper coroutine for delayed state transitions after displaying messages
    private IEnumerator TransitionAfterDelay(float delay, WaveManagerState nextState)
    {
        // This coroutine just waits, the actual timer logic is in Update_TimerBasedStates
        // However, we can use this structure if we want actions *between* states
        yield return new WaitForSeconds(delay);
        // Double check we are still in the expected state before transitioning
        if (currentState == WaveManagerState.WaveClearedDisplay || currentState == WaveManagerState.WaveIncomingDisplay) // Add other states if needed
        {
            SetState(nextState);
        }
    }

    // --- Provide camera for FaunaManager ---
    public Camera GetMainCamera()
    {
        return mainCamera;
    }
}
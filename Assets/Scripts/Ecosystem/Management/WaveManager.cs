// Assets/Scripts/Ecosystem/Management/WaveManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using WegoSystem;

// This enum definition was missing in the previous response. It must be here.
public enum WaveState
{
    Idle,           // No wave active
    Active,         // Wave is currently running
    Spawning        // Currently spawning enemies
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private FaunaManager faunaManager;
    [SerializeField] private List<WaveDefinition> wavesSequence;

    [SerializeField] private int waveDurationInDays = 1;
    [SerializeField] private float spawnTimeNormalized = 0.1f;
    [SerializeField] private bool continuousSpawning = false;

    [SerializeField] private bool deletePreviousWaveAnimals = true;

    [SerializeField] private TextMeshProUGUI waveStatusText;
    [SerializeField] private TextMeshProUGUI timeTrackerText;

    private WaveState currentState = WaveState.Idle;
    private WaveDefinition currentWaveDef = null;
    private int currentWaveIndex = -1;

    private int waveStartTick = 0;
    private int waveEndTick = 0;
    private int waveSpawnTick = 0;
    private bool hasSpawnedThisWave = false;

    private Coroutine activeSpawnCoroutine = null;

    public bool IsWaveActive => currentState != WaveState.Idle;
    public bool IsCurrentWaveDefeated() => currentState == WaveState.Idle && currentWaveIndex >= 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ValidateReferences();
    }

    public void Initialize()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnTickAdvanced += OnTickAdvanced;
            Debug.Log("[WaveManager] Initialized and subscribed to TickManager events.");
        }
        else
        {
            Debug.LogError("[WaveManager] Initialization failed: TickManager not found!");
        }
    }

    void OnDestroy()
    {
        if (TickManager.HasInstance)
        {
            TickManager.Instance.OnTickAdvanced -= OnTickAdvanced;
        }
        StopAllCoroutines();
    }

    private void ValidateReferences()
    {
        if (faunaManager == null)
        {
            faunaManager = FindAnyObjectByType<FaunaManager>();
            if (faunaManager != null)
            {
                Debug.LogWarning("[WaveManager] FaunaManager was not assigned in the Inspector. Found it automatically.", this);
            }
        }

        if (faunaManager == null)
        {
            Debug.LogError("[WaveManager] CRITICAL: FaunaManager is missing and could not be found in the scene! Waves will not spawn.", this);
        }

        if (wavesSequence == null || wavesSequence.Count == 0)
            Debug.LogWarning("[WaveManager] Wave Sequence empty. No waves will spawn.", this);
    }

    private void OnTickAdvanced(int currentTick)
    {
        if (currentState == WaveState.Active)
        {
            if (currentTick >= waveEndTick)
            {
                EndCurrentWave();
            }
            else if (!hasSpawnedThisWave && currentTick >= waveSpawnTick)
            {
                StartSpawning();
            }
            else if (continuousSpawning && hasSpawnedThisWave)
            {
                // Future logic for continuous spawning can go here
            }
        }
    }

    public void StartWaveForRound(int roundNumber)
    {
        if (RunManager.Instance?.CurrentState != RunState.GrowthAndThreat)
        {
            Debug.LogWarning("[WaveManager] Cannot start wave - not in GrowthAndThreat state.");
            return;
        }

        currentWaveIndex = roundNumber - 1;

        if (!IsValidWaveIndex(currentWaveIndex))
        {
            Debug.LogWarning($"[WaveManager] No wave definition for round {roundNumber}");
            currentState = WaveState.Idle;
            return;
        }

        currentWaveDef = wavesSequence[currentWaveIndex];
        if (currentWaveDef == null)
        {
            Debug.LogError($"[WaveManager] Wave definition at index {currentWaveIndex} is null!");
            currentState = WaveState.Idle;
            return;
        }

        StartWave();
    }

    private void StartWave()
    {
        if (deletePreviousWaveAnimals)
        {
            ClearAllActiveAnimals();
        }

        var config = TickManager.Instance?.Config;
        if (config == null)
        {
            Debug.LogError("[WaveManager] No TickConfiguration found!");
            return;
        }

        waveStartTick = TickManager.Instance.CurrentTick;
        int waveDurationTicks = config.ticksPerDay * waveDurationInDays;
        waveEndTick = waveStartTick + waveDurationTicks;

        waveSpawnTick = waveStartTick + Mathf.RoundToInt(waveDurationTicks * spawnTimeNormalized);

        hasSpawnedThisWave = false;
        currentState = WaveState.Active;

        Debug.Log($"[WaveManager] Starting wave '{currentWaveDef.waveName}' " +
                  $"Duration: {waveDurationTicks} ticks ({waveDurationInDays} days) " +
                  $"Spawn at tick: {waveSpawnTick}");
    }

    private void StartSpawning()
    {
        if (currentWaveDef == null || faunaManager == null) return;

        hasSpawnedThisWave = true;
        currentState = WaveState.Spawning;

        Debug.Log($"[WaveManager] Beginning spawn for wave '{currentWaveDef.waveName}'");

        if (activeSpawnCoroutine != null)
        {
            StopCoroutine(activeSpawnCoroutine);
        }

        activeSpawnCoroutine = StartCoroutine(ExecuteWaveSpawn());
    }

    private IEnumerator ExecuteWaveSpawn()
    {
        faunaManager.ExecuteSpawnWave(currentWaveDef);

        yield return new WaitForSeconds(1f);

        if (currentState == WaveState.Spawning)
        {
            currentState = WaveState.Active;
        }

        activeSpawnCoroutine = null;
    }

    private void EndCurrentWave()
    {
        Debug.Log($"[WaveManager] Ending wave '{currentWaveDef?.waveName}'");

        StopCurrentWaveSpawning();
        currentWaveDef = null;
        currentState = WaveState.Idle;

        if (RunManager.Instance != null)
        {
            RunManager.Instance.StartNewPlanningPhase();
        }
    }

    public void StopCurrentWaveSpawning()
    {
        if (activeSpawnCoroutine != null)
        {
            StopCoroutine(activeSpawnCoroutine);
            activeSpawnCoroutine = null;
        }

        faunaManager?.StopAllSpawnCoroutines();
    }

    public void ResetForNewRound()
    {
        Debug.Log("[WaveManager] Resetting for new round");

        StopCurrentWaveSpawning();

        if (deletePreviousWaveAnimals)
        {
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

    private void ClearAllActiveAnimals()
    {
        AnimalController[] animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var animal in animals)
        {
            if (animal != null)
            {
                Destroy(animal.gameObject);
                count++;
            }
        }

        Debug.Log($"[WaveManager] Cleared {count} animals");
    }

    private bool IsValidWaveIndex(int index)
    {
        return wavesSequence != null &&
               index >= 0 &&
               index < wavesSequence.Count;
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        UpdateTimeTracker();
        UpdateWaveStatus();
    }

    private void UpdateTimeTracker()
    {
        if (timeTrackerText == null || TickManager.Instance == null) return;

        var config = TickManager.Instance.Config;
        if (config == null) return;

        float dayProgress = config.GetDayProgressNormalized(TickManager.Instance.CurrentTick);
        int dayNumber = TickManager.Instance.CurrentTick / config.ticksPerDay + 1;

        timeTrackerText.text = $"Day {dayNumber} - {(dayProgress * 100):F0}%";

        if (currentState != WaveState.Idle)
        {
            int ticksIntoWave = TickManager.Instance.CurrentTick - waveStartTick;
            int totalWaveTicks = waveEndTick - waveStartTick;
            float waveProgress = totalWaveTicks > 0 ? (float)ticksIntoWave / totalWaveTicks : 0;
            timeTrackerText.text += $" | Wave: {(waveProgress * 100):F0}%";
        }
    }

    private void UpdateWaveStatus()
    {
        if (waveStatusText == null) return;

        if (RunManager.Instance == null)
        {
            waveStatusText.text = "System Offline";
            return;
        }

        if (RunManager.Instance.CurrentState == RunState.Planning)
        {
            waveStatusText.text = $"Prepare for Round {RunManager.Instance.CurrentRoundNumber}";
        }
        else if (RunManager.Instance.CurrentState == RunState.GrowthAndThreat)
        {
            if (currentWaveDef != null)
            {
                int ticksRemaining = Mathf.Max(0, waveEndTick - TickManager.Instance.CurrentTick);
                string waveName = string.IsNullOrEmpty(currentWaveDef.waveName)
                    ? $"Wave {currentWaveIndex + 1}"
                    : currentWaveDef.waveName;

                string status = currentState == WaveState.Spawning ? " [SPAWNING]" : "";
                waveStatusText.text = $"{waveName}{status} - {ticksRemaining} ticks left";
            }
            else
            {
                waveStatusText.text = "No active wave";
            }
        }
    }

    public Camera GetMainCamera() => mainCamera;

    void Debug_ForceEndWave()
    {
        if (Application.isEditor && currentState != WaveState.Idle)
        {
            EndCurrentWave();
        }
    }

    void Debug_ForceSpawn()
    {
        if (Application.isEditor && currentState == WaveState.Active && !hasSpawnedThisWave)
        {
            StartSpawning();
        }
    }
}
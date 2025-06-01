// FILE: Assets/Scripts/Core/RunManager.cs
using UnityEngine;
using System;

public enum RunState
{
    Planning,
    GrowthAndThreat,
    Recovery
}

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("State & Timing")]
    [SerializeField] private RunState currentState = RunState.Planning;
    [Tooltip("Time scale multiplier during the Growth & Threat phase.")]
    [SerializeField] private float growthPhaseTimeScale = 6f;
    [Tooltip("Current round number, starts at 1.")]
    [SerializeField] private int currentRoundNumber = 1; // For tracking progression

    [Header("Manager References (Assign in Inspector)")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private WaveManager waveManager;

    public RunState CurrentState => currentState;
    public int CurrentRoundNumber => currentRoundNumber;
    public event Action<RunState> OnRunStateChanged;
    public event Action<int> OnRoundChanged; // Event for new round starting

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetState(RunState.Planning, true);
    }

    private void SetState(RunState newState, bool force = false)
    {
        if (currentState == newState && !force) return;

        currentState = newState;
        Debug.Log($"[RunManager] State changed to: {currentState}");

        switch (currentState)
        {
            case RunState.Planning:
                Time.timeScale = 0f;
                weatherManager?.PauseCycleAtDay(); // Example: Ensure consistent planning environment
                break;
            case RunState.GrowthAndThreat:
                Time.timeScale = growthPhaseTimeScale;
                weatherManager?.ResumeCycle();
                waveManager?.StartWaveForRound(currentRoundNumber); // Tell WaveManager to start
                break;
            case RunState.Recovery:
                Time.timeScale = 0f;
                weatherManager?.PauseCycle(); // Example: Pause environment during recovery
                break;
        }
        OnRunStateChanged?.Invoke(currentState);
    }

    public void StartGrowthAndThreatPhase()
    {
        if (currentState == RunState.Planning)
        {
            Debug.Log($"[RunManager] Transitioning: Planning -> Growth & Threat for Round {currentRoundNumber}");
            SetState(RunState.GrowthAndThreat);
        }
        else
        {
            Debug.LogWarning($"[RunManager] Cannot start Growth phase from state: {currentState}");
        }
    }

    public void StartRecoveryPhase()
    {
        if (currentState == RunState.GrowthAndThreat)
        {
            Debug.Log("[RunManager] Transitioning: Growth & Threat -> Recovery");
            waveManager?.StopCurrentWaveSpawning(); // Tell WaveManager to stop spawning more for this wave
            SetState(RunState.Recovery);
        }
        else
        {
            Debug.LogWarning($"[RunManager] Cannot start Recovery phase from state: {currentState}");
        }
    }

    public void StartNewPlanningPhase()
    {
        if (currentState == RunState.Recovery)
        {
            currentRoundNumber++;
            Debug.Log($"[RunManager] Transitioning: Recovery -> Planning (New Round: {currentRoundNumber})");
            waveManager?.ResetForNewRound(); // Prepare WaveManager for the next round
            // TODO: Add any other game element resets needed for a new round
            SetState(RunState.Planning);
            OnRoundChanged?.Invoke(currentRoundNumber);
        }
        else
        {
            Debug.LogWarning($"[RunManager] Cannot start new Planning phase from state: {currentState}");
        }
    }

    void Update()
    {
        // Example: Automatically transition from GrowthAndThreat to Recovery
        // This condition needs to be robust.
        if (currentState == RunState.GrowthAndThreat)
        {
            if (waveManager != null && waveManager.IsCurrentWaveDefeated())
            {
                Debug.Log("[RunManager] Current wave defeated. Transitioning to Recovery.");
                StartRecoveryPhase();
            }
        }
    }
}
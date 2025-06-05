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
    [SerializeField] private int currentRoundNumber = 1;

    [Header("Manager References (Assign in Inspector)")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private WaveManager waveManager;

    public RunState CurrentState => currentState;
    public int CurrentRoundNumber => currentRoundNumber;
    public event Action<RunState> OnRunStateChanged;
    public event Action<int> OnRoundChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[RunManager] Duplicate instance found on {gameObject.name}. Destroying this one.", gameObject);
            Destroy(gameObject); // Destroy this duplicate instance
            return;
        }
        Instance = this;

        // This line will work without warning IF this GameObject is a root object in the scene.
        // If you intend for RunManager to persist across scene loads.
        // If your game is single-scene, you might not even need DontDestroyOnLoad.
        if (transform.parent == null) // Only call DontDestroyOnLoad if it's a root GameObject
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"[RunManager] {gameObject.name} is not a root GameObject. DontDestroyOnLoad will not be called for it directly. If its parent is persistent, that's fine. Otherwise, make RunManager a root object if it needs to persist across scenes.", gameObject);
        }


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
                weatherManager?.PauseCycleAtDay();
                break;
            case RunState.GrowthAndThreat:
                Time.timeScale = growthPhaseTimeScale;
                weatherManager?.ResumeCycle();
                waveManager?.StartWaveForRound(currentRoundNumber);
                break;
            case RunState.Recovery:
                Time.timeScale = 0f;
                weatherManager?.PauseCycle();
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
            waveManager?.StopCurrentWaveSpawning();
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
            waveManager?.ResetForNewRound();
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
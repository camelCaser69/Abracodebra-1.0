// Assets\Scripts\Core\RunManager.cs

using UnityEngine;
using System;
using WegoSystem;

public enum RunState {
    Planning,
    GrowthAndThreat
}

public class RunManager : MonoBehaviour {
    public static RunManager Instance { get; private set; }

    [SerializeField] bool useWegoSystem = true;
    [SerializeField] RunState currentState = RunState.Planning;
    [SerializeField] int currentRoundNumber = 1;

    [SerializeField] WeatherManager weatherManager;
    [SerializeField] WaveManager waveManager;

    public RunState CurrentState => currentState;
    public int CurrentRoundNumber => currentRoundNumber;
    public event Action<RunState> OnRunStateChanged;
    public event Action<int> OnRoundChanged;

    void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning($"[RunManager] Duplicate instance found on {gameObject.name}. Destroying this one.", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetState(RunState.Planning, true);
    }

    void Start() {
        if (useWegoSystem && TurnPhaseManager.Instance != null) {
            TurnPhaseManager.Instance.OnPhaseChanged += HandleWegoPhaseChanged;
        }
    }

    void OnDestroy() {
        if (TurnPhaseManager.Instance != null) {
            TurnPhaseManager.Instance.OnPhaseChanged -= HandleWegoPhaseChanged;
        }
    }

    void HandleWegoPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase) {
        if (!useWegoSystem) return;

        switch (newPhase) {
            case TurnPhase.Planning:
                if (currentState == RunState.GrowthAndThreat) {
                    StartNewRound();
                }
                break;

            case TurnPhase.Execution:
                if (currentState == RunState.Planning) {
                    StartGrowthAndThreatPhase();
                }
                break;
        }
    }

    void SetState(RunState newState, bool force = false) {
        if (currentState == newState && !force) return;

        currentState = newState;
        Debug.Log($"[RunManager] State changed to: {currentState}");

        switch (currentState) {
            case RunState.Planning:
                weatherManager?.PauseCycleAtDay();
                break;

            case RunState.GrowthAndThreat:
                weatherManager?.ResumeCycle();
                waveManager?.StartWaveForRound(currentRoundNumber);
                break;
        }

        OnRunStateChanged?.Invoke(currentState);
    }

    public void StartGrowthAndThreatPhase() {
        if (currentState == RunState.Planning) {
            Debug.Log($"[RunManager] Starting Growth & Threat for Round {currentRoundNumber}");
            SetState(RunState.GrowthAndThreat);

            if (useWegoSystem && TurnPhaseManager.Instance != null) {
                if (TurnPhaseManager.Instance.CurrentPhase == TurnPhase.Planning) {
                    TurnPhaseManager.Instance.EndPlanningPhase();
                }
            }
        }
    }

    // NEW METHODS - These were missing and causing compiler errors
    public void StartRecoveryPhase() {
        // Recovery phase was removed from the design, so this does nothing
        // or transitions back to planning
        Debug.Log("[RunManager] StartRecoveryPhase called - transitioning to Planning instead");
        StartNewPlanningPhase();
    }

    public void StartNewPlanningPhase() {
        Debug.Log("[RunManager] Starting new planning phase");
        StartNewRound();
    }

    void StartNewRound() {
        currentRoundNumber++;
        Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");
        waveManager?.ResetForNewRound();
        SetState(RunState.Planning);
        OnRoundChanged?.Invoke(currentRoundNumber);
    }

    public void ManualStartGrowthPhase() {
        StartGrowthAndThreatPhase();
    }

    public void SetWegoSystem(bool enabled) {
        useWegoSystem = enabled;
    }

    public bool IsWegoSystemEnabled() {
        return useWegoSystem;
    }
}
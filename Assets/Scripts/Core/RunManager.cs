using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                    // Check if wave is complete
                    if (waveManager != null && waveManager.IsCurrentWaveDefeated()) {
                        StartNewRound();
                    }
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

    public void StartRecoveryPhase() {
        Debug.Log("[RunManager] StartRecoveryPhase called - transitioning to Planning instead");
        StartNewPlanningPhase();
    }

    public void StartNewPlanningPhase() {
        Debug.Log("[RunManager] Starting new planning phase");
        
        // Don't increment round if we're already in planning
        if (currentState != RunState.Planning) {
            StartNewRound();
        }
    }

    void StartNewRound() {
        currentRoundNumber++;
        Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");
        
        // Reset wave manager
        waveManager?.ResetForNewRound();
        
        // Ensure we're in planning state
        SetState(RunState.Planning);
        
        // Force turn phase to planning
        if (useWegoSystem && TurnPhaseManager.Instance != null) {
            TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
        }
        
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
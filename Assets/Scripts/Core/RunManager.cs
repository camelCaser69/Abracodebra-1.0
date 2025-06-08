using UnityEngine;
using System;
using WegoSystem;

public enum RunState {
    Planning,
    GrowthAndThreat,
    Recovery
}

public class RunManager : MonoBehaviour {
    public static RunManager Instance { get; private set; }

    [Header("Wego Integration")]
    [SerializeField] bool useWegoSystem = true;
    [SerializeField] bool autoAdvanceFromPlanning = false;

    [SerializeField] RunState currentState = RunState.Planning;
    [SerializeField] float growthPhaseTimeScale = 6f;
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
        if (useWegoSystem) {
            // Hook into Wego system events
            if (TurnPhaseManager.Instance != null) {
                TurnPhaseManager.Instance.OnPhaseChanged += HandleWegoPhaseChanged;
            }
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
                if (currentState != RunState.Planning) {
                    // Check if we should advance to next round
                    if (currentState == RunState.Recovery) {
                        StartNewPlanningPhase();
                    } else {
                        SetState(RunState.Planning);
                    }
                }
                break;

            case TurnPhase.Execution:
                if (currentState == RunState.Planning) {
                    StartGrowthAndThreatPhase();
                }
                break;

            case TurnPhase.Resolution:
                if (currentState == RunState.GrowthAndThreat) {
                    StartRecoveryPhase();
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
                if (!useWegoSystem) {
                    Time.timeScale = 0f;
                    weatherManager?.PauseCycleAtDay();
                }
                break;
                
            case RunState.GrowthAndThreat:
                if (!useWegoSystem) {
                    Time.timeScale = growthPhaseTimeScale;
                    weatherManager?.ResumeCycle();
                }
                waveManager?.StartWaveForRound(currentRoundNumber);
                break;
                
            case RunState.Recovery:
                if (!useWegoSystem) {
                    Time.timeScale = 0f;
                    weatherManager?.PauseCycle();
                }
                break;
        }
        
        OnRunStateChanged?.Invoke(currentState);
    }

    public void StartGrowthAndThreatPhase() {
        if (currentState == RunState.Planning) {
            Debug.Log($"[RunManager] Transitioning: Planning -> Growth & Threat for Round {currentRoundNumber}");
            SetState(RunState.GrowthAndThreat);

            if (useWegoSystem && TurnPhaseManager.Instance != null) {
                // Ensure Wego system is in execution phase
                if (TurnPhaseManager.Instance.CurrentPhase == TurnPhase.Planning) {
                    TurnPhaseManager.Instance.EndPlanningPhase();
                }
            }
        }
        else {
            Debug.LogWarning($"[RunManager] Cannot start Growth phase from state: {currentState}");
        }
    }

    public void StartRecoveryPhase() {
        if (currentState == RunState.GrowthAndThreat) {
            Debug.Log("[RunManager] Transitioning: Growth & Threat -> Recovery");
            waveManager?.StopCurrentWaveSpawning();
            SetState(RunState.Recovery);

            if (useWegoSystem && TurnPhaseManager.Instance != null) {
                // Force transition to resolution phase
                if (TurnPhaseManager.Instance.CurrentPhase != TurnPhase.Resolution) {
                    TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Resolution);
                }
            }
        }
        else {
            Debug.LogWarning($"[RunManager] Cannot start Recovery phase from state: {currentState}");
        }
    }

    public void StartNewPlanningPhase() {
        if (currentState == RunState.Recovery) {
            currentRoundNumber++;
            Debug.Log($"[RunManager] Transitioning: Recovery -> Planning (New Round: {currentRoundNumber})");
            waveManager?.ResetForNewRound();
            SetState(RunState.Planning);
            OnRoundChanged?.Invoke(currentRoundNumber);

            if (useWegoSystem && TurnPhaseManager.Instance != null) {
                // Force transition to planning phase for new round
                TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
            }
        }
        else {
            Debug.LogWarning($"[RunManager] Cannot start new Planning phase from state: {currentState}");
        }
    }

    void Update() {
        if (!useWegoSystem && currentState == RunState.GrowthAndThreat) {
            // Real-time fallback: check wave completion
            if (waveManager != null && waveManager.IsCurrentWaveDefeated()) {
                Debug.Log("[RunManager] Current wave defeated. Transitioning to Recovery.");
                StartRecoveryPhase();
            }
        }
    }

    // Manual control methods (for UI buttons)
    public void ManualStartGrowthPhase() {
        StartGrowthAndThreatPhase();
    }

    public void ManualStartRecoveryPhase() {
        StartRecoveryPhase();
    }

    public void ManualStartNewPlanningPhase() {
        StartNewPlanningPhase();
    }

    // Wego system integration
    public void SetWegoSystem(bool enabled) {
        bool wasEnabled = useWegoSystem;
        useWegoSystem = enabled;

        if (enabled && !wasEnabled) {
            // Switching to Wego system
            if (TurnPhaseManager.Instance != null) {
                TurnPhaseManager.Instance.OnPhaseChanged += HandleWegoPhaseChanged;
                
                // Sync current state with Wego phase
                switch (currentState) {
                    case RunState.Planning:
                        TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Planning);
                        break;
                    case RunState.GrowthAndThreat:
                        TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Execution);
                        break;
                    case RunState.Recovery:
                        TurnPhaseManager.Instance.TransitionToPhase(TurnPhase.Resolution);
                        break;
                }
            }

            // Reset time scale since Wego system manages timing
            Time.timeScale = 1f;
            
        } else if (!enabled && wasEnabled) {
            // Switching to real-time system
            if (TurnPhaseManager.Instance != null) {
                TurnPhaseManager.Instance.OnPhaseChanged -= HandleWegoPhaseChanged;
            }

            // Set appropriate time scale for current state
            switch (currentState) {
                case RunState.Planning:
                case RunState.Recovery:
                    Time.timeScale = 0f;
                    break;
                case RunState.GrowthAndThreat:
                    Time.timeScale = growthPhaseTimeScale;
                    break;
            }
        }
    }

    public bool IsWegoSystemEnabled() {
        return useWegoSystem;
    }

    public void SetAutoAdvanceFromPlanning(bool enabled) {
        autoAdvanceFromPlanning = enabled;
        
        if (useWegoSystem && TurnPhaseManager.Instance != null) {
            // Enable/disable auto-advance in the turn phase manager
            TurnPhaseManager.Instance.ForcePhase(TurnPhaseManager.Instance.CurrentPhase);
        }
    }

    // Debug methods
    public void DebugForceState(RunState state) {
        if (Application.isEditor || Debug.isDebugBuild) {
            SetState(state, true);
        }
    }

    public void DebugAdvanceRound() {
        if (Application.isEditor || Debug.isDebugBuild) {
            currentRoundNumber++;
            OnRoundChanged?.Invoke(currentRoundNumber);
        }
    }
}
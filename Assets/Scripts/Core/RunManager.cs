using System;
using UnityEngine;
using UnityEngine.Serialization;
using WegoSystem;

// Simplified to just two states - removed Recovery
public enum RunState {
    Planning,
    GrowthAndThreat
}

// Consolidated phase management into RunManager
public enum GamePhase {
    Planning,      // Players place nodes/plan strategy
    Execution      // Game executes actions/waves
}

public class RunManager : MonoBehaviour {
    public static RunManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] WeatherManager weatherManager;
    [SerializeField] WaveManager waveManager;
    
    [Header("State")]
    [SerializeField] RunState currentState = RunState.Planning;
    [SerializeField] GamePhase currentPhase = GamePhase.Planning;
    [SerializeField] int currentRoundNumber = 1;
    [SerializeField] int currentPhaseTicks = 0;
    
    public RunState CurrentState => currentState;
    public GamePhase CurrentPhase => currentPhase;
    public int CurrentRoundNumber => currentRoundNumber;
    public int CurrentPhaseTicks => currentPhaseTicks;
    
    // Consolidated events
    public event Action<RunState> OnRunStateChanged;
    public event Action<GamePhase, GamePhase> OnPhaseChanged;
    public event Action<int> OnRoundChanged;
    
    void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning($"[RunManager] Another instance already exists. Destroying this one.", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        SetState(RunState.Planning, true);
    }
    
    void Start() {
        // Register for tick updates if needed
        if (TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(new PhaseTickHandler(this));
        }
    }
    
    void SetState(RunState newState, bool force = false) {
        if (currentState == newState && !force) return;
        
        currentState = newState;
        Debug.Log($"[RunManager] State changed to: {currentState}");
        
        switch (currentState) {
            case RunState.Planning:
                weatherManager?.PauseCycleAtDay();
                SetPhase(GamePhase.Planning);
                break;
                
            case RunState.GrowthAndThreat:
                weatherManager?.ResumeCycle();
                waveManager?.StartWaveForRound(currentRoundNumber);
                SetPhase(GamePhase.Execution);
                break;
        }
        
        OnRunStateChanged?.Invoke(currentState);
    }
    
    void SetPhase(GamePhase newPhase) {
        if (currentPhase == newPhase) return;
        
        GamePhase oldPhase = currentPhase;
        currentPhase = newPhase;
        currentPhaseTicks = 0;
        
        Debug.Log($"[RunManager] Phase changed: {oldPhase} -> {newPhase}");
        OnPhaseChanged?.Invoke(oldPhase, newPhase);
    }
    
    public void StartGrowthAndThreatPhase() {
        if (currentState == RunState.Planning) {
            Debug.Log($"[RunManager] Starting Growth & Threat for Round {currentRoundNumber}");
            SetState(RunState.GrowthAndThreat);
        }
    }
    
    public void EndPlanningPhase() {
        if (currentState == RunState.Planning && currentPhase == GamePhase.Planning) {
            SetPhase(GamePhase.Execution);
            // Transition to growth phase after a brief planning execution
            StartGrowthAndThreatPhase();
        }
    }
    
    public void StartNewPlanningPhase() {
        Debug.Log("[RunManager] Starting new planning phase");
        
        if (currentState != RunState.Planning) {
            // Check if current wave is complete
            if (waveManager != null && waveManager.IsCurrentWaveDefeated()) {
                StartNewRound();
            }
            else {
                SetState(RunState.Planning);
            }
        }
    }
    
    void StartNewRound() {
        currentRoundNumber++;
        Debug.Log($"[RunManager] Starting new round: {currentRoundNumber}");
        
        waveManager?.ResetForNewRound();
        SetState(RunState.Planning);
        
        OnRoundChanged?.Invoke(currentRoundNumber);
    }
    
    // Helper class to handle tick updates without polluting main class
    private class PhaseTickHandler : ITickUpdateable {
        private RunManager manager;
        
        public PhaseTickHandler(RunManager manager) {
            this.manager = manager;
        }
        
        public void OnTickUpdate(int currentTick) {
            manager.currentPhaseTicks++;
        }
    }
    
    // Debug helpers
    public void ForcePhase(GamePhase phase) {
        if (Application.isEditor || Debug.isDebugBuild) {
            SetPhase(phase);
        }
    }
}
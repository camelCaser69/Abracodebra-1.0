using System;
using UnityEngine;
using WegoSystem;

public enum TurnPhase
{
    Planning,
    Execution
}

public class TurnPhaseManager : MonoBehaviour, ITickUpdateable
{
    public static TurnPhaseManager Instance { get; set; }

    [SerializeField] private TurnPhase currentPhase = TurnPhase.Planning;
    [SerializeField] private int currentPhaseTicks = 0;
    [SerializeField] private bool debugMode = false;

    public TurnPhase CurrentPhase => currentPhase;
    public int CurrentPhaseTicks => currentPhaseTicks;
    public bool IsInPlanningPhase => currentPhase == TurnPhase.Planning;
    public bool IsInExecutionPhase => currentPhase == TurnPhase.Execution;

    public event Action<TurnPhase, TurnPhase> OnPhaseChanged;
    public event Action OnPlanningPhaseStarted;
    public event Action OnExecutionPhaseStarted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else
        {
            Debug.LogError("[TurnPhaseManager] TickManager not found!");
        }

        TransitionToPhase(TurnPhase.Planning);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        currentPhaseTicks++;
    }

    private bool HasActionsToProcess()
    {
        var gardeners = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);
        foreach (var gardener in gardeners)
        {
            // This logic is now obsolete with immediate movement
            // if (gardener.GetQueuedMoveCount() > 0) return true; 
        }

        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        foreach (var animal in animals)
        {
            if (currentPhaseTicks % animal.thinkingTickInterval == 0) return true;
        }

        var plants = PlantGrowth.AllActivePlants;
        if (plants.Count > 0) return true; // Plants always need processing

        return false;
    }

    public void EndPlanningPhase()
    {
        if (currentPhase == TurnPhase.Planning)
        {
            TransitionToPhase(TurnPhase.Execution);
        }
        else
        {
            Debug.LogWarning($"[TurnPhaseManager] Cannot end planning phase - current phase is {currentPhase}");
        }
    }

    public void TransitionToPhase(TurnPhase newPhase)
    {
        if (currentPhase == newPhase) return;

        TurnPhase oldPhase = currentPhase;
        currentPhase = newPhase;
        currentPhaseTicks = 0;

        switch (newPhase)
        {
            case TurnPhase.Planning:
                OnPlanningPhaseStarted?.Invoke();
                break;

            case TurnPhase.Execution:
                OnExecutionPhaseStarted?.Invoke();
                break;
        }

        OnPhaseChanged?.Invoke(oldPhase, newPhase);

        if (debugMode)
        {
            Debug.Log($"[TurnPhaseManager] Phase transition: {oldPhase} -> {newPhase}");
        }
    }

    public void ForcePhase(TurnPhase phase)
    {
        if (Application.isEditor || Debug.isDebugBuild)
        {
            TransitionToPhase(phase);
        }
    }
}
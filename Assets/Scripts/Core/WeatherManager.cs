using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WegoSystem;

public class WeatherManager : MonoBehaviour, ITickUpdateable {
    public static WeatherManager Instance { get; private set; }

    public enum CyclePhase { Day, TransitionToNight, Night, TransitionToDay }

    public bool dayNightCycleEnabled = true;
    public AnimationCurve transitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float sunIntensity = 1f;

    public float fixedSunIntensity = 1f;
    public SpriteRenderer fadeSprite;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    public bool IsPaused { get; private set; } = false;

    int currentPhaseTicks = 0;
    int totalPhaseTicksTarget = 0;

    public CyclePhase CurrentPhase => currentPhase;
    public event Action<CyclePhase> OnPhaseChanged;

    CyclePhase currentPhase = CyclePhase.Day;

    public float CurrentTotalPhaseTime => totalPhaseTicksTarget * (TickManager.Instance?.Config?.GetRealSecondsPerTick() ?? 0.5f);
    public float CurrentPhaseTimer => (totalPhaseTicksTarget - currentPhaseTicks) * (TickManager.Instance?.Config?.GetRealSecondsPerTick() ?? 0.5f);
    
    // Add this for proper phase progress tracking
    public float GetPhaseProgress() {
        if (totalPhaseTicksTarget <= 0) return 0f;
        return (float)currentPhaseTicks / totalPhaseTicksTarget;
    }

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void Start() {
        if (TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        
        // Initialize phase properly
        EnterPhase(CyclePhase.Day, true);
    }

    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!dayNightCycleEnabled || IsPaused) return;

        currentPhaseTicks++;
        
        // Check if we need to advance to next phase
        if (currentPhaseTicks >= totalPhaseTicksTarget) {
            AdvanceToNextPhase();
        }
        
        // Update sun intensity based on current phase
        UpdateSunIntensity();
    }

    void Update() {
        UpdateFadeSprite();
    }

    void UpdateSunIntensity() {
        if (TickManager.Instance?.Config != null) {
            float dayProgress = TickManager.Instance.Config.GetDayProgressNormalized(TickManager.Instance.CurrentTick);

            CyclePhase newPhase = currentPhase;

            if (dayProgress < 0.4f) {
                newPhase = CyclePhase.Day;
                sunIntensity = 1f;
            }
            else if (dayProgress < 0.5f) {
                newPhase = CyclePhase.TransitionToNight;
                float transitionProgress = (dayProgress - 0.4f) / 0.1f;
                sunIntensity = Mathf.Lerp(1f, 0f, transitionCurve.Evaluate(transitionProgress));
            }
            else if (dayProgress < 0.9f) {
                newPhase = CyclePhase.Night;
                sunIntensity = 0f;
            }
            else {
                newPhase = CyclePhase.TransitionToDay;
                float transitionProgress = (dayProgress - 0.9f) / 0.1f;
                sunIntensity = Mathf.Lerp(0f, 1f, transitionCurve.Evaluate(transitionProgress));
            }

            if (newPhase != currentPhase) {
                EnterPhase(newPhase, true);
            }
        }
    }

    void AdvanceToNextPhase() {
        CyclePhase nextPhase = currentPhase;
        switch (currentPhase) {
            case CyclePhase.Day: nextPhase = CyclePhase.TransitionToNight; break;
            case CyclePhase.TransitionToNight: nextPhase = CyclePhase.Night; break;
            case CyclePhase.Night: nextPhase = CyclePhase.TransitionToDay; break;
            case CyclePhase.TransitionToDay: nextPhase = CyclePhase.Day; break;
        }
        EnterPhase(nextPhase);
    }

    void EnterPhase(CyclePhase nextPhase, bool forceEvent = false) {
        CyclePhase previousPhase = currentPhase;
        currentPhase = nextPhase;

        if (TickManager.Instance?.Config != null) {
            var config = TickManager.Instance.Config;
            switch (nextPhase) {
                case CyclePhase.Day:
                    totalPhaseTicksTarget = config.dayPhaseTicks;
                    break;
                case CyclePhase.Night:
                    totalPhaseTicksTarget = config.nightPhaseTicks;
                    break;
                case CyclePhase.TransitionToNight:
                case CyclePhase.TransitionToDay:
                    totalPhaseTicksTarget = config.transitionTicks;
                    break;
            }
            currentPhaseTicks = 0;
        }

        if (previousPhase != currentPhase || forceEvent) {
            if (Debug.isDebugBuild) Debug.Log($"[WeatherManager] Phase Changed To: {currentPhase}");
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    void UpdateFadeSprite() {
        if (fadeSprite != null) {
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, sunIntensity);
            Color c = fadeSprite.color;
            c.a = alpha;
            fadeSprite.color = c;
        }
    }

    public void PauseCycleAtDay() {
        Debug.Log("[WeatherManager] PauseCycleAtDay called.");
        IsPaused = true;
        currentPhase = CyclePhase.Day;
        currentPhaseTicks = 0;
        totalPhaseTicksTarget = TickManager.Instance?.Config?.dayPhaseTicks ?? 60;
        sunIntensity = 1.0f;
        UpdateFadeSprite();
        OnPhaseChanged?.Invoke(CyclePhase.Day);
    }

    public void ResumeCycle() {
        Debug.Log("[WeatherManager] ResumeCycle called.");
        IsPaused = false;
        
        // Force recalculate current phase when resuming
        currentPhaseTicks = 0;
        EnterPhase(CyclePhase.Day, true);
    }

    public void PauseCycle() {
        Debug.Log("[WeatherManager] PauseCycle called.");
        IsPaused = true;
    }

    public int GetCurrentPhaseTicks() {
        return currentPhaseTicks;
    }

    public int GetTotalPhaseTicksTarget() {
        return totalPhaseTicksTarget;
    }

    public void ForcePhase(CyclePhase phase) {
        if (Application.isEditor || Debug.isDebugBuild) {
            EnterPhase(phase, true);
        }
    }
}
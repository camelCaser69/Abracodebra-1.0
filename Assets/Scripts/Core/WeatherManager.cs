using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections;
using System.Collections.Generic;
using WegoSystem;

public class WeatherManager : MonoBehaviour, ITickUpdateable {
    public static WeatherManager Instance { get; private set; }

    [SerializeField] bool useWegoSystem = true;

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
    bool forceDaylight = false;

    public CyclePhase CurrentPhase => currentPhase;
    public event Action<CyclePhase> OnPhaseChanged;

    CyclePhase currentPhase = CyclePhase.Day;

    public float CurrentTotalPhaseTime => totalPhaseTicksTarget * (TickManager.Instance?.Config?.GetRealSecondsPerTick() ?? 0.5f);
    public float CurrentPhaseTimer => (totalPhaseTicksTarget - currentPhaseTicks) * (TickManager.Instance?.Config?.GetRealSecondsPerTick() ?? 0.5f);

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() {
        if (useWegoSystem && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }

        EnterPhase(CyclePhase.Day, true);
    }

    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoSystem || !dayNightCycleEnabled || IsPaused) return;

        // Use tick-based day progress
        if (TickManager.Instance?.Config != null) {
            float dayProgress = TickManager.Instance.Config.GetDayProgressNormalized(currentTick);
            
            // Map progress to cycle phases
            CyclePhase newPhase = currentPhase;
            
            if (dayProgress < 0.4f) {
                newPhase = CyclePhase.Day;
                sunIntensity = 1f;
            } else if (dayProgress < 0.5f) {
                newPhase = CyclePhase.TransitionToNight;
                float transitionProgress = (dayProgress - 0.4f) / 0.1f;
                sunIntensity = Mathf.Lerp(1f, 0f, transitionCurve.Evaluate(transitionProgress));
            } else if (dayProgress < 0.9f) {
                newPhase = CyclePhase.Night;
                sunIntensity = 0f;
            } else {
                newPhase = CyclePhase.TransitionToDay;
                float transitionProgress = (dayProgress - 0.9f) / 0.1f;
                sunIntensity = Mathf.Lerp(0f, 1f, transitionCurve.Evaluate(transitionProgress));
            }
            
            // Update phase if changed
            if (newPhase != currentPhase) {
                currentPhase = newPhase;
                if (Debug.isDebugBuild) Debug.Log($"[WeatherManager] Phase Changed To: {currentPhase}");
                OnPhaseChanged?.Invoke(currentPhase);
            }
        }
        
        UpdateFadeSprite();
    }

    void Update() {
        if (!IsPaused && !forceDaylight && !useWegoSystem) {
            UpdateSunIntensity();
        }
        UpdateFadeSprite();
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

        UpdateSunIntensity();

        if (previousPhase != currentPhase || forceEvent) {
            if (Debug.isDebugBuild) Debug.Log($"[WeatherManager] Phase Changed To: {currentPhase}");
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    void UpdateSunIntensity() {
        if (forceDaylight && IsPaused) {
            sunIntensity = 1.0f;
            return;
        }

        float progress = totalPhaseTicksTarget > 0 ? (float)currentPhaseTicks / totalPhaseTicksTarget : 0f;

        switch (currentPhase) {
            case CyclePhase.Day:
                sunIntensity = 1f;
                break;
            case CyclePhase.TransitionToNight:
                sunIntensity = Mathf.Lerp(1f, 0f, transitionCurve.Evaluate(progress));
                break;
            case CyclePhase.Night:
                sunIntensity = 0f;
                break;
            case CyclePhase.TransitionToDay:
                sunIntensity = Mathf.Lerp(0f, 1f, transitionCurve.Evaluate(progress));
                break;
        }
        sunIntensity = Mathf.Clamp01(sunIntensity);
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
        forceDaylight = true;
        sunIntensity = 1.0f;
        UpdateFadeSprite();
        OnPhaseChanged?.Invoke(CyclePhase.Day);
    }

    public void ResumeCycle() {
        Debug.Log("[WeatherManager] ResumeCycle called.");
        IsPaused = false;
        forceDaylight = false;
    }

    public void PauseCycle() {
        Debug.Log("[WeatherManager] PauseCycle called.");
        IsPaused = true;
        forceDaylight = false;
    }

    public void SetWegoSystem(bool enabled) {
        bool wasEnabled = useWegoSystem;
        useWegoSystem = enabled;

        if (enabled && !wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        } else if (!enabled && wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public float GetPhaseProgress() {
        return totalPhaseTicksTarget > 0 ? (float)currentPhaseTicks / totalPhaseTicksTarget : 0f;
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
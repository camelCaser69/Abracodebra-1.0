using System;
using UnityEngine;
using WegoSystem;

public class WeatherManager : MonoBehaviour, ITickUpdateable {
    public static WeatherManager Instance { get; private set; }

    [Header("Wego System")]
    [SerializeField] bool useWegoSystem = true;

    public enum CyclePhase { Day, TransitionToNight, Night, TransitionToDay }

    [Header("Cycle Configuration")]
    public bool dayNightCycleEnabled = true;
    
    // Real-time fallback values
    public float dayDuration = 20f;
    public float nightDuration = 20f;
    public float transitionDuration = 5f;
    public AnimationCurve transitionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Visual Settings")]
    public float sunIntensity = 1f;
    public float fixedSunIntensity = 1f;
    public SpriteRenderer fadeSprite;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    [Header("Speed Control")]
    public float timeScaleMultiplier = 1f;
    public bool IsPaused { get; private set; } = false;

    // Wego System variables
    int currentPhaseTicks = 0;
    int totalPhaseTicksTarget = 0;
    
    // Real-time fallback variables
    float phaseTimer = 0f;
    float totalPhaseTime = 0f;
    bool forceDaylight = false;

    public CyclePhase CurrentPhase => currentPhase;
    public event Action<CyclePhase> OnPhaseChanged;
    public float CurrentPhaseTimer => useWegoSystem ? (totalPhaseTicksTarget - currentPhaseTicks) : phaseTimer;
    public float CurrentTotalPhaseTime => useWegoSystem ? totalPhaseTicksTarget : totalPhaseTime;

    CyclePhase currentPhase = CyclePhase.Day;

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
        
        EnterPhase(CyclePhase.Day, true); // Start at day
    }

    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoSystem || !dayNightCycleEnabled || IsPaused) return;

        currentPhaseTicks++;

        if (currentPhaseTicks >= totalPhaseTicksTarget) {
            AdvanceToNextPhase();
        } else {
            UpdateSunIntensity();
        }

        UpdateFadeSprite();
    }

    void Update() {
        if (useWegoSystem) {
            // Only handle visual updates in Wego mode
            if (!IsPaused && !forceDaylight) {
                UpdateSunIntensity();
            }
            UpdateFadeSprite();
            return;
        }

        // Real-time fallback mode
        if (Time.timeScale == 0f && !IsPaused) {
            UpdateFadeSprite(); // Still update visual representation of current sunIntensity
            return;
        }

        if (IsPaused) {
            if (forceDaylight) {
                sunIntensity = 1.0f; // Maintain full daylight if forced
            }
            UpdateFadeSprite();
            return;
        }

        if (!dayNightCycleEnabled) {
            sunIntensity = fixedSunIntensity;
            UpdateFadeSprite();
            return;
        }

        phaseTimer -= Time.deltaTime * timeScaleMultiplier;

        if (phaseTimer <= 0f) {
            AdvanceToNextPhase();
        } else {
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

        if (useWegoSystem && TickManager.Instance?.Config != null) {
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
        } else {
            // Real-time fallback
            switch (nextPhase) {
                case CyclePhase.Day: totalPhaseTime = dayDuration; break;
                case CyclePhase.Night: totalPhaseTime = nightDuration; break;
                case CyclePhase.TransitionToNight:
                case CyclePhase.TransitionToDay: totalPhaseTime = transitionDuration; break;
            }
            totalPhaseTime = Mathf.Max(0.01f, totalPhaseTime); // Avoid division by zero
            phaseTimer = totalPhaseTime;
        }

        UpdateSunIntensity(); // Calculate intensity for the new phase start

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

        float progress;
        if (useWegoSystem) {
            progress = totalPhaseTicksTarget > 0 ? (float)currentPhaseTicks / totalPhaseTicksTarget : 0f;
        } else {
            progress = totalPhaseTime > 0 ? 1f - Mathf.Clamp01(phaseTimer / totalPhaseTime) : 0f;
        }

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
        sunIntensity = 1.0f; // Immediately set to full day
        UpdateFadeSprite(); // Update visuals immediately
        OnPhaseChanged?.Invoke(CyclePhase.Day); // Notify that effectively it's day
    }

    public void ResumeCycle() {
        Debug.Log("[WeatherManager] ResumeCycle called.");
        IsPaused = false;
        forceDaylight = false;
    }

    public void PauseCycle() {
        Debug.Log("[WeatherManager] PauseCycle called.");
        IsPaused = true;
        forceDaylight = false; // Don't force daylight, just pause current state
    }

    // Wego-specific methods
    public void SetWegoSystem(bool enabled) {
        bool wasEnabled = useWegoSystem;
        useWegoSystem = enabled;

        if (enabled && !wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
            // Convert current phase to tick-based
            ConvertCurrentPhaseToTicks();
        } else if (!enabled && wasEnabled && TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
            // Convert current phase to real-time
            ConvertCurrentPhaseToRealtime();
        }
    }

    void ConvertCurrentPhaseToTicks() {
        if (TickManager.Instance?.Config == null) return;

        var config = TickManager.Instance.Config;
        float realTimeProgress = totalPhaseTime > 0 ? (1f - (phaseTimer / totalPhaseTime)) : 0f;

        switch (currentPhase) {
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

        currentPhaseTicks = Mathf.RoundToInt(realTimeProgress * totalPhaseTicksTarget);
    }

    void ConvertCurrentPhaseToRealtime() {
        float tickProgress = totalPhaseTicksTarget > 0 ? (float)currentPhaseTicks / totalPhaseTicksTarget : 0f;

        switch (currentPhase) {
            case CyclePhase.Day:
                totalPhaseTime = dayDuration;
                break;
            case CyclePhase.Night:
                totalPhaseTime = nightDuration;
                break;
            case CyclePhase.TransitionToNight:
            case CyclePhase.TransitionToDay:
                totalPhaseTime = transitionDuration;
                break;
        }

        phaseTimer = totalPhaseTime * (1f - tickProgress);
    }

    public float GetPhaseProgress() {
        if (useWegoSystem) {
            return totalPhaseTicksTarget > 0 ? (float)currentPhaseTicks / totalPhaseTicksTarget : 0f;
        } else {
            return totalPhaseTime > 0 ? (1f - (phaseTimer / totalPhaseTime)) : 0f;
        }
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
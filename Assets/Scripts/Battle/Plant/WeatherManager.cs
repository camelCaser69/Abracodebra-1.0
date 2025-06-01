// FILE: Assets/Scripts/Battle/Plant/WeatherManager.cs
using UnityEngine;
using System;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    public enum CyclePhase { Day, TransitionToNight, Night, TransitionToDay }

    [Header("Day/Night Cycle Settings")]
    public bool dayNightCycleEnabled = true;
    public float dayDuration = 20f;
    public float nightDuration = 20f;
    public float transitionDuration = 5f;
    public AnimationCurve transitionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Sunlight Settings")]
    [Range(0f, 1f)]
    public float sunIntensity = 1f;

    [Header("Fixed Sunlight When Cycle Off")]
    [Range(0f, 1f)]
    public float fixedSunIntensity = 1f;

    [Header("Sunlight Visualization")]
    public SpriteRenderer fadeSprite;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    [Header("Time Scaling & Pausing")]
    [Range(1f, 100f)]
    public float timeScaleMultiplier = 1f; // This scales the internal phaseTimer speed
    public bool IsPaused { get; private set; } = false; // MODIFIED: Made setter private

    public CyclePhase CurrentPhase => currentPhase;
    public event Action<CyclePhase> OnPhaseChanged;
    public float CurrentPhaseTimer => phaseTimer;
    public float CurrentTotalPhaseTime => totalPhaseTime;

    private CyclePhase currentPhase = CyclePhase.Day;
    private float phaseTimer = 0f;
    private float totalPhaseTime = 0f;
    private bool forceDaylight = false; // NEW: Flag to hold daylight

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        EnterPhase(CyclePhase.Day, true); // Start at day
    }

    void Update()
    {
        // If RunManager has globally paused time, WeatherManager effectively pauses too.
        // The IsPaused flag here is for internal cycle pausing by RunManager.
        if (Time.timeScale == 0f && !IsPaused) // If global time is 0 but we aren't internally paused, we should probably respect that.
        {
            UpdateFadeSprite(); // Still update visual representation of current sunIntensity
            return;
        }


        if (IsPaused)
        {
            // If explicitly paused by RunManager
            if (forceDaylight)
            {
                sunIntensity = 1.0f; // Maintain full daylight if forced
            }
            // else, sunIntensity remains as it was when PauseCycle() was called.
            UpdateFadeSprite();
            return;
        }

        // Normal cycle update
        if (!dayNightCycleEnabled)
        {
            sunIntensity = fixedSunIntensity;
            UpdateFadeSprite();
            // if (timeScaleMultiplier != 1f) timeScaleMultiplier = 1f; // This seems redundant if cycle is off
            return;
        }

        // Apply our internal timeScaleMultiplier to the phase timer
        phaseTimer -= Time.deltaTime * timeScaleMultiplier; // Time.deltaTime is already affected by Time.timeScale

        if (phaseTimer <= 0f)
        {
            CyclePhase nextPhase = currentPhase;
            switch (currentPhase)
            {
                case CyclePhase.Day: nextPhase = CyclePhase.TransitionToNight; break;
                case CyclePhase.TransitionToNight: nextPhase = CyclePhase.Night; break;
                case CyclePhase.Night: nextPhase = CyclePhase.TransitionToDay; break;
                case CyclePhase.TransitionToDay: nextPhase = CyclePhase.Day; break;
            }
            EnterPhase(nextPhase);
        }
        else
        {
            UpdateSunIntensity();
        }

        UpdateFadeSprite();
    }

    private void EnterPhase(CyclePhase nextPhase, bool forceEvent = false)
    {
        CyclePhase previousPhase = currentPhase;
        currentPhase = nextPhase;

        switch (nextPhase)
        {
            case CyclePhase.Day: totalPhaseTime = dayDuration; break;
            case CyclePhase.Night: totalPhaseTime = nightDuration; break;
            case CyclePhase.TransitionToNight:
            case CyclePhase.TransitionToDay: totalPhaseTime = transitionDuration; break;
        }
        totalPhaseTime = Mathf.Max(0.01f, totalPhaseTime); // Avoid division by zero
        phaseTimer = totalPhaseTime;

        UpdateSunIntensity(); // Calculate intensity for the new phase start

        if (previousPhase != currentPhase || forceEvent)
        {
            if (Debug.isDebugBuild) Debug.Log($"[WeatherManager] Phase Changed To: {currentPhase}");
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    private void UpdateSunIntensity()
    {
        if (forceDaylight && IsPaused) // If forced to day and paused, keep it at 1
        {
            sunIntensity = 1.0f;
            return;
        }
        if (totalPhaseTime <= 0) return; // Should be prevented by EnterPhase

        float progress = 1f - Mathf.Clamp01(phaseTimer / totalPhaseTime);
        switch (currentPhase)
        {
            case CyclePhase.Day: sunIntensity = 1f; break;
            case CyclePhase.TransitionToNight: sunIntensity = Mathf.Lerp(1f, 0f, transitionCurve.Evaluate(progress)); break;
            case CyclePhase.Night: sunIntensity = 0f; break;
            case CyclePhase.TransitionToDay: sunIntensity = Mathf.Lerp(0f, 1f, transitionCurve.Evaluate(progress)); break;
        }
        sunIntensity = Mathf.Clamp01(sunIntensity);
    }

    private void UpdateFadeSprite()
    {
        if (fadeSprite != null)
        {
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, sunIntensity); // Lerp between minAlpha and maxAlpha
            Color c = fadeSprite.color; c.a = alpha; fadeSprite.color = c;
        }
    }

    // --- NEW PUBLIC METHODS FOR RunManager CONTROL ---

    /// <summary>
    /// Pauses the day/night cycle and forces the sun intensity to full day.
    /// This is typically used during the Planning phase.
    /// </summary>
    public void PauseCycleAtDay()
    {
        Debug.Log("[WeatherManager] PauseCycleAtDay called.");
        IsPaused = true;
        forceDaylight = true;
        sunIntensity = 1.0f; // Immediately set to full day
        // Optionally, set currentPhase to Day and reset phaseTimer if needed for consistency,
        // but just forcing sunIntensity might be enough for visual and gameplay effects.
        // currentPhase = CyclePhase.Day;
        // phaseTimer = dayDuration;
        // totalPhaseTime = dayDuration;
        UpdateFadeSprite(); // Update visuals immediately
        OnPhaseChanged?.Invoke(CyclePhase.Day); // Notify that effectively it's day
    }

    /// <summary>
    /// Resumes the normal day/night cycle from its current state.
    /// This is typically used when transitioning to the Growth & Threat phase.
    /// </summary>
    public void ResumeCycle()
    {
        Debug.Log("[WeatherManager] ResumeCycle called.");
        IsPaused = false;
        forceDaylight = false;
        // No need to change sunIntensity here, it will update naturally in Update()
    }

    /// <summary>
    /// Pauses the day/night cycle at its current sun intensity and phase progress.
    /// This is typically used during the Recovery phase or for other general pauses.
    /// </summary>
    public void PauseCycle()
    {
        Debug.Log("[WeatherManager] PauseCycle called.");
        IsPaused = true;
        forceDaylight = false; // Don't force daylight, just pause current state
        // SunIntensity and phaseTimer will remain as they are.
    }
}
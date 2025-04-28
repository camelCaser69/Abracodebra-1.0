// FILE: Assets/Scripts/Battle/Plant/WeatherManager.cs
using UnityEngine;
using System; // Needed for Action

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    // --- Enums (Moved outside class for broader access if needed, or keep inside) ---
    public enum CyclePhase { Day, TransitionToNight, Night, TransitionToDay }

    [Header("Day/Night Cycle Settings")]
    public bool dayNightCycleEnabled = true;
    [Tooltip("Duration of full day (seconds)")]
    public float dayDuration = 20f;
    [Tooltip("Duration of full night (seconds)")]
    public float nightDuration = 20f;
    [Tooltip("Duration of transitions (seconds)")]
    public float transitionDuration = 5f;
    [Tooltip("Curve that controls the sunIntensity during transitions (X=0 start, X=1 end).")]
    public AnimationCurve transitionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Sunlight Settings")]
    [Range(0f, 1f)]
    public float sunIntensity = 1f;  // 0=night, 1=day

    [Header("Fixed Sunlight When Cycle Off")]
    [Tooltip("If dayNightCycleEnabled is false, we use this fixed intensity.")]
    [Range(0f, 1f)]
    public float fixedSunIntensity = 1f;

    [Header("Sunlight Visualization")]
    public SpriteRenderer fadeSprite;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    [Header("Time Scaling")] // <<< NEW HEADER
    [Tooltip("Multiplier for the internal speed of the day/night cycle. Set by WaveManager for fast-forwarding.")]
    [Range(1f, 100f)] // Allow speeding up significantly
    public float timeScaleMultiplier = 1f; // <<< NEW FIELD

    // --- Public Properties & Events ---
    public CyclePhase CurrentPhase => currentPhase; // <<< NEW: Expose current phase
    public event Action<CyclePhase> OnPhaseChanged; // <<< NEW: Event for phase changes

    // --- Internals ---
    private CyclePhase currentPhase = CyclePhase.Day;
    private float phaseTimer = 0f;
    private float totalPhaseTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Initialize to Day phase and trigger initial event
        EnterPhase(CyclePhase.Day, true); // Force event fire on start
    }

    void Update()
    {
        if (!dayNightCycleEnabled)
        {
            sunIntensity = fixedSunIntensity;
            UpdateFadeSprite();
            // Ensure time scale is reset if cycle disabled externally
            if (timeScaleMultiplier != 1f) timeScaleMultiplier = 1f;
            return;
        }

        // Decrement timer based on scaled delta time
        phaseTimer -= Time.deltaTime * timeScaleMultiplier; // <<< USE MULTIPLIER

        if (phaseTimer <= 0f)
        {
            // Move to next phase
            CyclePhase nextPhase = currentPhase; // Determine next phase first
            switch (currentPhase)
            {
                case CyclePhase.Day:                nextPhase = CyclePhase.TransitionToNight; break;
                case CyclePhase.TransitionToNight:  nextPhase = CyclePhase.Night; break;
                case CyclePhase.Night:              nextPhase = CyclePhase.TransitionToDay; break;
                case CyclePhase.TransitionToDay:    nextPhase = CyclePhase.Day; break;
            }
            EnterPhase(nextPhase); // This will calculate new totalPhaseTime and trigger event
        }
        else
        {
            // Update sun intensity based on current phase progress
            UpdateSunIntensity();
        }

        UpdateFadeSprite();
    }

    /// <summary>
    /// Transitions to the specified phase, resets timers, and fires event.
    /// </summary>
    private void EnterPhase(CyclePhase nextPhase, bool forceEvent = false)
    {
        CyclePhase previousPhase = currentPhase;
        currentPhase = nextPhase;

        switch (nextPhase)
        {
            case CyclePhase.Day:                totalPhaseTime = dayDuration; break;
            case CyclePhase.Night:              totalPhaseTime = nightDuration; break;
            case CyclePhase.TransitionToNight:
            case CyclePhase.TransitionToDay:    totalPhaseTime = transitionDuration; break;
        }
        // Prevent division by zero if durations are 0
        totalPhaseTime = Mathf.Max(0.01f, totalPhaseTime);
        phaseTimer = totalPhaseTime; // Reset timer for the new phase

        // Update intensity immediately for the start of the new phase
        UpdateSunIntensity();

        // Fire event if phase actually changed or forced
        if (previousPhase != currentPhase || forceEvent)
        {
            if (Debug.isDebugBuild) Debug.Log($"[WeatherManager] Phase Changed To: {currentPhase}");
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    /// <summary>
    /// Calculates sun intensity based on current phase and timer progress.
    /// </summary>
    private void UpdateSunIntensity()
    {
         // Ensure totalPhaseTime is not zero before dividing
        if (totalPhaseTime <= 0) return;

        float progress = 1f - Mathf.Clamp01(phaseTimer / totalPhaseTime); // Progress within the current phase (0 to 1)

        switch (currentPhase)
        {
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
        sunIntensity = Mathf.Clamp01(sunIntensity); // Ensure it stays within bounds
    }


    private void UpdateFadeSprite()
    {
        if (fadeSprite != null)
        {
            // Adjust alpha based on sun intensity (lerp from maxAlpha (night) to minAlpha (day))
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, sunIntensity);
            Color c = fadeSprite.color;
            c.a = alpha;
            fadeSprite.color = c;
        }
    }
}
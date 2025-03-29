using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

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

    // Internals
    private enum CyclePhase { Day, TransitionToNight, Night, TransitionToDay }
    private CyclePhase currentPhase = CyclePhase.Day;
    private float phaseTimer = 0f; // counts down in each phase
    private float totalPhaseTime = 0f; // length of current phase

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Initialize to Day phase
        EnterPhase(CyclePhase.Day);
    }

    private void Update()
    {
        if (!dayNightCycleEnabled)
        {
            // If the cycle is disabled, just use the fixed sun intensity
            sunIntensity = fixedSunIntensity;
            UpdateFadeSprite();
            return;
        }

        // else dayNightCycle is enabled
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
        {
            // move to next phase
            switch (currentPhase)
            {
                case CyclePhase.Day:
                    EnterPhase(CyclePhase.TransitionToNight);
                    break;
                case CyclePhase.TransitionToNight:
                    EnterPhase(CyclePhase.Night);
                    break;
                case CyclePhase.Night:
                    EnterPhase(CyclePhase.TransitionToDay);
                    break;
                case CyclePhase.TransitionToDay:
                    EnterPhase(CyclePhase.Day);
                    break;
            }
        }
        else
        {
            // while in the current phase, update sunIntensity accordingly
            float progress = 1f - (phaseTimer / totalPhaseTime);
            switch (currentPhase)
            {
                case CyclePhase.Day:
                    // Full day, sunIntensity = 1
                    sunIntensity = 1f;
                    break;
                case CyclePhase.TransitionToNight:
                    // 0..1 => day(1) to night(0) using transitionCurve
                    float tToNight = transitionCurve.Evaluate(progress);
                    sunIntensity = Mathf.Lerp(1f, 0f, tToNight);
                    break;
                case CyclePhase.Night:
                    // Full night, sunIntensity = 0
                    sunIntensity = 0f;
                    break;
                case CyclePhase.TransitionToDay:
                    // 0..1 => night(0) to day(1) using transitionCurve
                    float tToDay = transitionCurve.Evaluate(progress);
                    sunIntensity = Mathf.Lerp(0f, 1f, tToDay);
                    break;
            }
        }

        UpdateFadeSprite();
    }

    private void EnterPhase(CyclePhase nextPhase)
    {
        currentPhase = nextPhase;
        switch (nextPhase)
        {
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
        phaseTimer = totalPhaseTime;
    }

    private void UpdateFadeSprite()
    {
        if (fadeSprite != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, sunIntensity);
            Color c = fadeSprite.color;
            c.a = alpha;
            fadeSprite.color = c;
        }
    }
}

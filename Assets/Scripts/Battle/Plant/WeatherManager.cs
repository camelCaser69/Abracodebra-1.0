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

    [Header("Time Scaling & Pausing")] // <<< UPDATED HEADER
    [Range(1f, 100f)]
    public float timeScaleMultiplier = 1f;
    public bool IsPaused { get; set; } = false; // <<< NEW: Pause flag

    // --- Public Properties & Events ---
    public CyclePhase CurrentPhase => currentPhase;
    public event Action<CyclePhase> OnPhaseChanged;
    public float CurrentPhaseTimer => phaseTimer; // <<< NEW: Expose timer
    public float CurrentTotalPhaseTime => totalPhaseTime; // <<< NEW: Expose total time

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
        EnterPhase(CyclePhase.Day, true);
    }

    void Update()
    {
        // --- PAUSE CHECK ---
        if (IsPaused) // <<< NEW: Check if paused
        {
            // If paused, potentially ensure timeScale is 1? Or leave it? Let's reset it.
            if (timeScaleMultiplier != 1f) timeScaleMultiplier = 1f;
            return; // Do nothing else if paused
        }
        // ---------------------

        if (!dayNightCycleEnabled)
        {
            sunIntensity = fixedSunIntensity;
            UpdateFadeSprite();
            if (timeScaleMultiplier != 1f) timeScaleMultiplier = 1f;
            return;
        }

        phaseTimer -= Time.deltaTime * timeScaleMultiplier;

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
        totalPhaseTime = Mathf.Max(0.01f, totalPhaseTime);
        phaseTimer = totalPhaseTime;

        UpdateSunIntensity();

        if (previousPhase != currentPhase || forceEvent)
        {
            if (Debug.isDebugBuild) Debug.Log($"[WeatherManager] Phase Changed To: {currentPhase}");
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    private void UpdateSunIntensity()
    {
        if (totalPhaseTime <= 0) return;
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
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, sunIntensity);
            Color c = fadeSprite.color; c.a = alpha; fadeSprite.color = c;
        }
    }
}
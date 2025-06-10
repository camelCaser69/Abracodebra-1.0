using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NightColorPostProcess : MonoBehaviour
{
    #region Dependencies
    [Header("Dependencies")]
    [Tooltip("The WeatherManager that controls the day/night cycle.")]
    public WeatherManager weatherManager;

    [Tooltip("The global post-processing Volume to modify.")]
    public Volume globalVolume;
    #endregion

    #region Day/Night Settings
    [Header("Day/Night Settings")]
    public Color dayColorFilter = Color.white;
    public Color nightColorFilter = new Color(0.75f, 0.8f, 1f, 1f);

    public float dayPostExposure = 0f;
    public float nightPostExposure = -0.5f;

    public float daySaturation = 0f;
    public float nightSaturation = -50f;

    [Range(0f, 1f)]
    public float dayFilmGrainIntensity = 0.1f;
    [Range(0f, 1f)]
    public float nightFilmGrainIntensity = 0.5f;

    [Range(0f, 1f)]
    public float dayVignetteIntensity = 0.2f;
    [Range(0f, 1f)]
    public float nightVignetteIntensity = 0.5f;

    [Range(0.01f, 1f)]
    public float dayVignetteSmoothness = 0.2f;
    [Range(0.01f, 1f)]
    public float nightVignetteSmoothness = 0.3f;
    #endregion
    
    #region Transition Smoothing
    [Header("Transition Smoothing")]
    [Tooltip("How quickly the visual effects catch up to the target day/night state. Higher is faster.")]
    [SerializeField] float transitionSmoothingSpeed = 5f;
    #endregion

    // Volume Overrides
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private Vignette vignette;

    // Internal state for smoothing
    private float smoothedSunIntensity;

    void Start()
    {
        if (!weatherManager)
        {
            Debug.LogError($"[{nameof(NightColorPostProcess)}] WeatherManager not assigned!", this);
            enabled = false; // Disable script if core references are missing
            return;
        }
        if (!globalVolume)
        {
            Debug.LogError($"[{nameof(NightColorPostProcess)}] Global Volume not assigned!", this);
            enabled = false;
            return;
        }
        if (globalVolume.profile == null)
        {
            Debug.LogError($"[{nameof(NightColorPostProcess)}] Global Volume has no profile assigned!", this);
            enabled = false;
            return;
        }

        // Try to get the volume overrides
        if (!globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            Debug.LogWarning($"[{nameof(NightColorPostProcess)}] ColorAdjustments override not found in Volume profile.", this);
        }
        if (!globalVolume.profile.TryGet<FilmGrain>(out filmGrain))
        {
            Debug.LogWarning($"[{nameof(NightColorPostProcess)}] FilmGrain override not found in Volume profile.", this);
        }
        if (!globalVolume.profile.TryGet<Vignette>(out vignette))
        {
            Debug.LogWarning($"[{nameof(NightColorPostProcess)}] Vignette override not found in Volume profile.", this);
        }

        // Initialize the smoothed value to the current value to prevent a jump on start
        smoothedSunIntensity = weatherManager.sunIntensity;
    }

    void Update()
    {
        if (colorAdjustments == null && filmGrain == null && vignette == null)
            return; // Nothing to update if no overrides were found

        // Smoothly interpolate towards the target sun intensity from the WeatherManager
        if (weatherManager != null)
        {
            smoothedSunIntensity = Mathf.Lerp(smoothedSunIntensity, weatherManager.sunIntensity, transitionSmoothingSpeed * Time.deltaTime);
        }

        float sun = Mathf.Clamp01(smoothedSunIntensity); // Use the smoothed value
        float t = 1f - sun;  // t=0 at day, t=1 at night

        // Apply the interpolated values to the post-processing effects
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(dayColorFilter, nightColorFilter, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(dayPostExposure, nightPostExposure, t);
            colorAdjustments.saturation.value = Mathf.Lerp(daySaturation, nightSaturation, t);
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = Mathf.Lerp(dayFilmGrainIntensity, nightFilmGrainIntensity, t);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(dayVignetteIntensity, nightVignetteIntensity, t);
            vignette.smoothness.value = Mathf.Lerp(dayVignetteSmoothness, nightVignetteSmoothness, t);
        }
    }
}
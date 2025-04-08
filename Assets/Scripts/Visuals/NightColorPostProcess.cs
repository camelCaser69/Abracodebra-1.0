using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Ensure this script is attached to a GameObject in your scene
public class NightColorPostProcess : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Assign the WeatherManager controlling the day/night cycle.")]
    public WeatherManager weatherManager;
    [Tooltip("Assign the Global Post Processing Volume.")]
    public Volume globalVolume;

    // --- Private references to the Volume Overrides ---
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private Vignette vignette;
    // private Bloom bloom; // Example if you wanted to add Bloom later

    [Header("Color Adjustments")]
    [Tooltip("The color filter at full day (sunIntensity = 1).")]
    public Color dayColorFilter = Color.white;
    [Tooltip("The color filter at full night (sunIntensity ~ 0).")]
    public Color nightColorFilter = new Color(0.75f, 0.8f, 1f, 1f);
    [Tooltip("Daytime Post-Exposure (Higher = brighter).")]
    public float dayPostExposure = 0f;
    [Tooltip("Nighttime Post-Exposure (Lower = darker).")]
    public float nightPostExposure = -0.5f;
    [Tooltip("Saturation at full day (0 = no change, <0 desaturates, >0 saturates).")]
    public float daySaturation = 0f;
    [Tooltip("Saturation at full night (e.g., -50 for more desaturation).")]
    public float nightSaturation = -50f;

    [Header("Film Grain")]
    [Tooltip("Film grain intensity at full day (0 = none).")]
    [Range(0f, 1f)] public float dayFilmGrainIntensity = 0.1f;
    [Tooltip("Film grain intensity at full night (e.g., 0.5 for noticeable grain).")]
    [Range(0f, 1f)] public float nightFilmGrainIntensity = 0.5f;
    // Note: You could also control FilmGrain.response if desired

    [Header("Vignette")]
    [Tooltip("Vignette intensity at full day (0 = none, higher = stronger effect).")]
    [Range(0f, 1f)] public float dayVignetteIntensity = 0.2f;
    [Tooltip("Vignette intensity at full night (e.g., 0.5 for darker edges).")]
    [Range(0f, 1f)] public float nightVignetteIntensity = 0.5f;
    [Tooltip("Vignette smoothness at full day (higher = softer edge).")]
    [Range(0.01f, 1f)] public float dayVignetteSmoothness = 0.2f;
    [Tooltip("Vignette smoothness at full night.")]
    [Range(0.01f, 1f)] public float nightVignetteSmoothness = 0.3f;
    // Note: You could also control Vignette.color or Vignette.rounded if desired


    private void Start()
    {
        // --- Validate Core References ---
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

        // --- Attempt to Get Volume Overrides ---
        // It's okay if some aren't found, the Update loop will check for null
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
        // Example for Bloom:
        // if (!globalVolume.profile.TryGet<Bloom>(out bloom))
        // {
        //      Debug.LogWarning($"[{nameof(NightColorPostProcess)}] Bloom override not found in Volume profile.", this);
        // }

        // --- Ensure Overrides are Active ---
        // Make sure the overrides you intend to use are actually enabled on the Volume component itself.
        // You might need to manually check the boxes in the Inspector for ColorAdjustments, FilmGrain, and Vignette.
        // Alternatively, you could force them active here, but it's usually better to configure in the editor:
        // if (colorAdjustments != null) colorAdjustments.active = true;
        // if (filmGrain != null) filmGrain.active = true;
        // if (vignette != null) vignette.active = true;
    }

    private void Update()
    {
        // No need to check weatherManager, Start() already disables if null
        // Check if any overrides are available before proceeding
        if (colorAdjustments == null && filmGrain == null && vignette == null)
            return; // Nothing to update if no overrides were found

        // Get the sun intensity (0 = night, 1 = day) and calculate interpolation factor t
        float sun = Mathf.Clamp01(weatherManager.sunIntensity);
        float t = 1f - sun;  // t=0 at day, t=1 at night

        // --- Interpolate and Apply Color Adjustments ---
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(dayColorFilter, nightColorFilter, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(dayPostExposure, nightPostExposure, t);
            colorAdjustments.saturation.value = Mathf.Lerp(daySaturation, nightSaturation, t);
        }

        // --- Interpolate and Apply Film Grain ---
        if (filmGrain != null)
        {
            filmGrain.intensity.value = Mathf.Lerp(dayFilmGrainIntensity, nightFilmGrainIntensity, t);
            // You can add Lerp for filmGrain.response here if needed
        }

        // --- Interpolate and Apply Vignette ---
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(dayVignetteIntensity, nightVignetteIntensity, t);
            vignette.smoothness.value = Mathf.Lerp(dayVignetteSmoothness, nightVignetteSmoothness, t);
             // You can add Lerp for vignette.color or vignette.rounded here if needed
        }

        // --- Example for Bloom ---
        // if (bloom != null)
        // {
        //     bloom.intensity.value = Mathf.Lerp(dayBloomIntensity, nightBloomIntensity, t);
        // }
    }
}
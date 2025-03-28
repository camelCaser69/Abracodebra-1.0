using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NightColorPostProcess : MonoBehaviour
{
    [Header("References")]
    public WeatherManager weatherManager;        // Assign your existing WeatherManager
    public Volume globalVolume;                  // The Volume with Color Adjustments override
    
    private ColorAdjustments colorAdjustments;   // We'll read/write color filter and post exposure
    
    [Header("Color Settings")]
    [Tooltip("The color filter at full day (sunIntensity = 1).")]
    public Color dayColorFilter = Color.white;
    [Tooltip("The color filter at full night (sunIntensity ~ 0).")]
    public Color nightColorFilter = new Color(0.75f, 0.8f, 1f, 1f);
    
    [Tooltip("Daytime Post-Exposure (e.g. 0). Higher = brighter.")]
    public float dayPostExposure = 0f;
    [Tooltip("Nighttime Post-Exposure (e.g. -0.5). Lower = darker.")]
    public float nightPostExposure = -0.5f;
    
    private void Start()
    {
        if (!globalVolume)
        {
            Debug.LogWarning("[NightColorPostProcess] No globalVolume assigned!");
            return;
        }

        // Try to get the ColorAdjustments override from the volume's profile
        if (globalVolume.profile.TryGet<ColorAdjustments>(out var colorAdj))
        {
            colorAdjustments = colorAdj;
        }
        else
        {
            Debug.LogWarning("[NightColorPostProcess] No ColorAdjustments override found in the Volume profile!");
        }
    }

    private void Update()
    {
        if (!weatherManager || colorAdjustments == null)
            return;
        
        // sunIntensity goes 0..1 (0=night, 1=full day).
        // We invert that to get t = 1 - sunIntensity if we want 
        // more "night effect" at low intensities.
        
        float sun = Mathf.Clamp01(weatherManager.sunIntensity);  // ensure 0..1
        float t = 1f - sun;  // t=0 at day, t=1 at night
        
        // Lerp color filter
        Color finalFilter = Color.Lerp(dayColorFilter, nightColorFilter, t);
        colorAdjustments.colorFilter.value = finalFilter;
        
        // Lerp post exposure
        float finalExposure = Mathf.Lerp(dayPostExposure, nightPostExposure, t);
        colorAdjustments.postExposure.value = finalExposure;
    }
}

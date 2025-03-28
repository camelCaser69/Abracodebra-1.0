using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Sunlight Settings")]
    [Tooltip("Global sunlight intensity (0 = night, 1 = full day)")]
    [Range(0f, 1f)]
    public float sunIntensity = 1f;

    [Header("Day/Night Cycle")]
    [Tooltip("Speed multiplier for day/night cycle")]
    public float dayNightSpeed = 0.1f; // Adjust this for faster/slower cycles
    public float minSunIntensity = 0.2f; // Intensity at night

    [Header("Sunlight Visualization")]
    public SpriteRenderer fadeSprite;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Create a cyclic pattern using sine function.
        // Time.time * dayNightSpeed produces a repeating cycle.
        float cycle = (Mathf.Sin(Time.time * dayNightSpeed * Mathf.PI * 2f) + 1f) / 2f;
        // Map cycle to [minSunIntensity, 1]
        sunIntensity = Mathf.Lerp(minSunIntensity, 1f, cycle);

        if (fadeSprite != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, sunIntensity);
            Color c = fadeSprite.color;
            c.a = alpha;
            fadeSprite.color = c;
        }
    }
}
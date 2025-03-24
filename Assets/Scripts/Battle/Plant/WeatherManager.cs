using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Sunlight Settings")]
    [Tooltip("Global sunlight intensity in [0..1]. This affects plant photosynthesis.")]
    [Range(0f, 1f)]
    public float sunIntensity = 1f;

    [Header("Sunlight Visualization")]
    [Tooltip("Sprite whose opacity changes with sunIntensity.")]
    public SpriteRenderer fadeSprite;
    [Tooltip("Minimum alpha when sunIntensity = 0.")]
    public float minAlpha = 0f;
    [Tooltip("Maximum alpha when sunIntensity = 1.")]
    public float maxAlpha = 1f;

    private void Awake()
    {
        // Set up singleton instance.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
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
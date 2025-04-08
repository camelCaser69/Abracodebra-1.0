using UnityEngine;
using UnityEngine.Rendering.Universal; // Required if using Light2D

public class FireflyController : MonoBehaviour
{
    [Header("References (Optional)")]
    [Tooltip("Optional Light2D component for local glow.")]
    [SerializeField] private Light2D pointLight;
    [Tooltip("SpriteRenderer for flickering emission.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private Vector2 speedRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private float directionChangeInterval = 2.0f;
    [SerializeField] [Range(0f, 1f)] private float pauseChance = 0.2f;
    [SerializeField] private Vector2 pauseDurationRange = new Vector2(0.5f, 1.5f);

    [Header("Lifetime")]
    [SerializeField] private Vector2 lifetimeRange = new Vector2(5f, 15f);

    [Header("Glow Flicker")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField] private Vector2 intensityRange = new Vector2(1.5f, 3.0f); // For Emission / Light Intensity
    [SerializeField] private float flickerSpeed = 5.0f;

    // Internal State
    private FireflyManager manager;
    private Vector2 currentVelocity;
    private float currentSpeed;
    private float stateTimer; // Used for direction changes and pauses
    private bool isPaused;
    private float lifetime;
    private float flickerOffset; // Unique offset for Perlin noise or Sin wave
    private Material spriteMaterialInstance; // Instance for modifying emission

    // Movement Bounds
    private Vector2 minBounds;
    private Vector2 maxBounds;

    void Awake()
    {
        // Ensure renderer reference if flickering material
        if (enableFlicker && spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Create a material instance ONLY if flickering material emission
        if (enableFlicker && spriteRenderer != null && spriteRenderer.material != null)
        {
            // Create an instance so changing emission doesn't affect the original material asset
            spriteMaterialInstance = spriteRenderer.material; // This automatically instances if not already an instance
        }
        else if (enableFlicker && spriteRenderer != null && spriteRenderer.material == null)
        {
            Debug.LogWarning($"[{gameObject.name}] FireflyController: Cannot flicker material emission, SpriteRenderer has no material assigned.", gameObject);
            enableFlicker = false; // Disable flicker if no material
        }

        flickerOffset = Random.Range(0f, 100f); // Random start for flicker pattern
    }

    public void Initialize(FireflyManager owner, Vector2 minB, Vector2 maxB)
    {
        manager = owner;
        minBounds = minB;
        maxBounds = maxB;

        lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
        PickNewWanderState();
    }

    void Update()
    {
        // Lifetime Countdown
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Die();
            return; // Stop further updates after dying
        }

        // Movement State Machine
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            PickNewWanderState();
        }

        // Apply Movement
        if (!isPaused)
        {
            Vector2 newPos = (Vector2)transform.position + currentVelocity * Time.deltaTime;

            // Clamp position within bounds
            newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
            newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

            // If clamped, pick a new direction away from the boundary
            if (newPos.x <= minBounds.x || newPos.x >= maxBounds.x || newPos.y <= minBounds.y || newPos.y >= maxBounds.y)
            {
                // Reverse direction slightly and pick new state
                currentVelocity = -currentVelocity.normalized * 0.1f + Random.insideUnitCircle.normalized;
                currentVelocity = currentVelocity.normalized * currentSpeed; // Re-apply speed
                // Optionally force a direction change without pause:
                // isPaused = false;
                // stateTimer = directionChangeInterval * Random.Range(0.5f, 1.5f);
                // PickNewWanderState(true); // Force move state
            }

            transform.position = newPos;
        }

        // Glow Flicker Update
        HandleGlowFlicker();
    }

    void PickNewWanderState(bool forceMove = false)
    {
        if (!forceMove && Random.value < pauseChance)
        {
            // Start Pausing
            isPaused = true;
            currentVelocity = Vector2.zero;
            stateTimer = Random.Range(pauseDurationRange.x, pauseDurationRange.y);
        }
        else
        {
            // Start Moving
            isPaused = false;
            currentSpeed = Random.Range(speedRange.x, speedRange.y);
            currentVelocity = Random.insideUnitCircle.normalized * currentSpeed;
            stateTimer = directionChangeInterval * Random.Range(0.7f, 1.3f); // Add slight variation
        }
    }

    void HandleGlowFlicker()
    {
        if (!enableFlicker) return;

        // Use Perlin noise for smoother, more organic flicker
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset); // Value between ~0 and 1
        float targetIntensity = Mathf.Lerp(intensityRange.x, intensityRange.y, noise);

        // Apply to Light2D intensity
        if (pointLight != null)
        {
            pointLight.intensity = targetIntensity;
        }

        // Apply to Material Emission HDR Color Intensity
        if (spriteMaterialInstance != null)
        {
            // Get the current emission color (preserving hue/saturation)
            Color baseEmissionColor = spriteMaterialInstance.GetColor("_EmissionColor");

            // Calculate the final HDR color
            // Note: Color space (Gamma/Linear) can affect how intensity scales. This works well in Linear.
            Color finalEmissionColor = baseEmissionColor * Mathf.LinearToGammaSpace(targetIntensity); // Apply intensity

            spriteMaterialInstance.SetColor("_EmissionColor", finalEmissionColor);
        }
    }

    void Die()
    {
        if (manager != null)
        {
            manager.ReportFireflyDespawned(this);
        }
        // Optional: Add fade out effect here before destroying
        Destroy(gameObject);
    }

    // Optional: Clean up material instance if the object is destroyed unexpectedly
    void OnDestroy()
    {
        if (spriteMaterialInstance != null)
        {
            // Destroy the instanced material when the firefly is destroyed
            Destroy(spriteMaterialInstance);
        }
    }
}
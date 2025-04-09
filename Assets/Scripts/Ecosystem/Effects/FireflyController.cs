// FILE: Assets/Scripts/Ecosystem/Effects/FireflyController.cs
using UnityEngine;
using UnityEngine.Rendering.Universal; // Required if using Light2D
using System.Collections.Generic;

public class FireflyController : MonoBehaviour
{
    [Header("References (Optional)")]
    [Tooltip("Optional Light2D component for local glow.")]
    [SerializeField] private Light2D pointLight;
    [Tooltip("SpriteRenderer for flickering emission and alpha fade.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private Vector2 speedRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private float directionChangeInterval = 2.0f;
    [SerializeField] [Range(0f, 1f)] private float pauseChance = 0.2f;
    [SerializeField] private Vector2 pauseDurationRange = new Vector2(0.5f, 1.5f);

    [Header("Lifetime & Fade")]
    [SerializeField] private Vector2 lifetimeRange = new Vector2(8f, 18f); // Increased range slightly
    [SerializeField] private float fadeInDuration = 0.75f;
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Glow Flicker")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField] private Vector2 intensityRange = new Vector2(1.5f, 3.0f); // For Emission / Light Intensity
    [SerializeField] private float flickerSpeed = 5.0f;

    [Header("Spawn Flicker Effect")]
    [SerializeField] private bool enableSpawnFlicker = true;
    [SerializeField] private float spawnFlickerDuration = 0.5f; // Duration of initial intense flicker
    [SerializeField] private Vector2 spawnFlickerIntensityRange = new Vector2(0.5f, 4.0f); // Wider range for spawn
    [SerializeField] private float spawnFlickerSpeed = 15.0f; // Faster flicker at spawn

    [Header("Scent Attraction (Setup)")]
    [Tooltip("How often (in seconds) the firefly checks for nearby scent sources.")]
    [SerializeField] private float scentCheckInterval = 1.0f;
    [Tooltip("Radius within which scents are detected.")]
    [SerializeField] private float scentDetectionRadius = 4f;
    [Tooltip("How strongly the firefly steers towards the scent target.")]
    [SerializeField] private float attractionStrength = 2.0f;
    [Tooltip("Preferred distance to orbit the scent source.")]
    [SerializeField] private float orbitDistance = 0.8f;
    [Tooltip("How much randomness/wobble in the attracted movement.")]
    [SerializeField] private float attractionWobble = 0.5f;
    [Tooltip("Which scent types attract this firefly.")]
    [SerializeField] private List<ScentType> attractiveScentTypes = new List<ScentType> { ScentType.FloralSweet, ScentType.Fruity }; // Example


    // Internal State
    private FireflyManager manager;
    private Vector2 currentVelocity;
    private float currentSpeed;
    private float stateTimer; // Used for direction changes and pauses
    private bool isPaused;
    private float lifetime;
    private float age = 0f; // Track age for fade/spawn effects
    private float currentAlpha = 0f; // For fading
    private float flickerOffset; // Unique offset for Perlin noise or Sin wave
    private Material spriteMaterialInstance; // Instance for modifying emission & alpha

    // Scent State
    private Transform attractionTarget = null;
    private float scentCheckTimer;

    // Movement Bounds
    private Vector2 minBounds;
    private Vector2 maxBounds;

    void Awake()
    {
        // Renderer and Material Instance setup (as before)
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null && spriteRenderer.material != null)
        {
            spriteMaterialInstance = spriteRenderer.material; // Creates instance
        }
        else if (spriteRenderer == null || spriteRenderer.material == null)
        {
            Debug.LogWarning($"[{gameObject.name}] FireflyController: Cannot modify material properties (flicker/fade), SpriteRenderer or its material is missing.", gameObject);
            enableFlicker = false;
            enableSpawnFlicker = false; // Disable if no material
        }

        flickerOffset = Random.Range(0f, 100f);
    }

    public void Initialize(FireflyManager owner, Vector2 minB, Vector2 maxB)
    {
        manager = owner;
        minBounds = minB;
        maxBounds = maxB;

        lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
        age = 0f; // Reset age on initialize
        currentAlpha = 0f; // Start fully transparent
        ApplyAlphaAndIntensity(0f); // Apply initial transparency
        attractionTarget = null; // Ensure no initial target
        scentCheckTimer = Random.Range(0, scentCheckInterval); // Stagger initial check

        PickNewWanderState();
    }

    void Update()
    {
        age += Time.deltaTime;

        // --- Lifetime & Fading ---
        HandleLifetimeAndFade();
        if (currentAlpha <= 0f && age > fadeInDuration) // Fully faded out
        {
            Die();
            return; // Stop further updates
        }

        // --- Scent Detection ---
        HandleScentDetection();

        // --- Movement ---
        HandleMovement();

        // --- Glow Flicker ---
        HandleGlowAndFlicker(); // Now applies currentAlpha
    }

    void HandleLifetimeAndFade()
    {
        // Calculate target alpha based on age and lifetime
        if (age < fadeInDuration)
        {
            // Fade In
            currentAlpha = Mathf.Clamp01(age / fadeInDuration);
        }
        else if (lifetime - age < fadeOutDuration)
        {
            // Fade Out
            currentAlpha = Mathf.Clamp01((lifetime - age) / fadeOutDuration);
        }
        else
        {
            // Fully Visible
            currentAlpha = 1.0f;
        }

        // Check if lifetime naturally expired (independent of fade alpha)
        if (age >= lifetime && currentAlpha > 0)
        {
             // Ensure we start fading out if lifetime is reached but fade hasn't completed
             currentAlpha = Mathf.Clamp01((lifetime - age + fadeOutDuration) / fadeOutDuration);
             if(currentAlpha <= 0) Die(); // Trigger death if alpha hits zero after forced fade start
        }
    }

    void HandleScentDetection()
    {
        scentCheckTimer -= Time.deltaTime;
        if (scentCheckTimer <= 0f)
        {
            FindAttractionTarget();
            scentCheckTimer = scentCheckInterval;
        }
    }

     void FindAttractionTarget()
     {
         attractionTarget = null; // Assume no target initially
         float closestDistSq = scentDetectionRadius * scentDetectionRadius;

         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scentDetectionRadius); // Consider optimizing layers

         foreach (Collider2D hit in hits)
         {
             if (hit.TryGetComponent<ScentSource>(out ScentSource scent))
             {
                 // Check if the scent type is attractive
                 if (attractiveScentTypes.Contains(scent.type))
                 {
                     float distSq = (hit.transform.position - transform.position).sqrMagnitude;
                     if (distSq < closestDistSq)
                     {
                         closestDistSq = distSq;
                         attractionTarget = hit.transform;
                     }
                 }
             }
         }

         // If we found a target, potentially reset wander state to react immediately
         if (attractionTarget != null)
         {
             isPaused = false; // Ensure not paused when attracted
             // No need to call PickNewWanderState, HandleMovement will override
         }
     }

    void HandleMovement()
    {
        stateTimer -= Time.deltaTime;

        if (attractionTarget != null)
        {
            // --- Attracted Movement ---
            Vector2 directionToTarget = (attractionTarget.position - transform.position);
            float distanceToTarget = directionToTarget.magnitude;

            // Normalize carefully to avoid issues at zero distance
            if (distanceToTarget > 0.01f)
            {
                 directionToTarget /= distanceToTarget; // Normalize
            }

            // Calculate orbiting offset (perpendicular direction)
            Vector2 orbitOffsetDir = new Vector2(-directionToTarget.y, directionToTarget.x) * Mathf.Sign(currentSpeed + 0.1f); // Basic orbit direction

            // Combine direct attraction, orbit, and wobble
            Vector2 desiredDirection = directionToTarget * attractionStrength;
            if (distanceToTarget > orbitDistance)
            {
                 // Move towards if far
            }
            else
            {
                // Orbit when close enough
                desiredDirection += orbitOffsetDir * (currentSpeed * 0.5f); // Adjust orbiting speed factor
            }
            // Add random wobble
            desiredDirection += Random.insideUnitCircle * attractionWobble;

            // Smoothly steer towards desired direction
            currentVelocity = Vector2.Lerp(currentVelocity.normalized, desiredDirection.normalized, Time.deltaTime * 5f) * currentSpeed; // Adjust steering speed (5f)

            // Reset wander timer while attracted? Maybe not, let wander try to take over if target lost.
            // stateTimer = directionChangeInterval; // Optional: keeps wander state fresh

        }
        else
        {
            // --- Wander Movement ---
            if (stateTimer <= 0f)
            {
                PickNewWanderState();
            }
        }

        // Apply calculated velocity if not paused
        if (!isPaused)
        {
             Vector2 currentPos = transform.position;
             Vector2 newPos = currentPos + currentVelocity * Time.deltaTime;

             // Boundary clamping and reaction (as before)
             bool clampedX = false;
             bool clampedY = false;
             if (newPos.x <= minBounds.x || newPos.x >= maxBounds.x) { clampedX = true; newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x); }
             if (newPos.y <= minBounds.y || newPos.y >= maxBounds.y) { clampedY = true; newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y); }

             if (clampedX || clampedY)
             {
                 // Hit boundary - reflect or pick new direction
                 Vector2 reflectionNormal = Vector2.zero;
                 if(clampedX) reflectionNormal.x = -Mathf.Sign(currentVelocity.x);
                 if(clampedY) reflectionNormal.y = -Mathf.Sign(currentVelocity.y);
                 currentVelocity = Vector2.Reflect(currentVelocity, reflectionNormal.normalized + Random.insideUnitCircle * 0.1f).normalized * currentSpeed; // Reflect with slight randomness

                 // Ensure we don't get stuck if hitting a corner perfectly
                 if (currentVelocity.sqrMagnitude < 0.01f)
                 {
                      currentVelocity = Random.insideUnitCircle.normalized * currentSpeed;
                 }
                 PickNewWanderState(true); // Force a new move state after hitting wall
             }

            transform.position = newPos;
        }
    }


    void PickNewWanderState(bool forceMove = false)
    {
        // If currently attracted, wandering state changes are less relevant
        // but we might still want to update speed/direction if target is lost.
        if (attractionTarget != null && !forceMove) return;


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
            // If previously paused or forcing move, pick totally random direction
            if(currentVelocity.sqrMagnitude < 0.01f || forceMove)
                 currentVelocity = Random.insideUnitCircle.normalized * currentSpeed;
            else // Otherwise slightly adjust current direction
                 currentVelocity = (currentVelocity.normalized + Random.insideUnitCircle * 0.5f).normalized * currentSpeed;

            stateTimer = directionChangeInterval * Random.Range(0.7f, 1.3f); // Add slight variation
        }
    }

    void HandleGlowAndFlicker()
    {
        if (!enableFlicker && !enableSpawnFlicker) return; // Skip if both disabled

        float targetIntensity;

        if (enableSpawnFlicker && age < spawnFlickerDuration)
        {
            // --- Spawn Flicker Phase ---
            // Use Random.value for sharper, faster flicker
            targetIntensity = Mathf.Lerp(spawnFlickerIntensityRange.x, spawnFlickerIntensityRange.y, Random.value);
            // Could also rapidly change Perlin noise offset:
            // float noise = Mathf.PerlinNoise(Time.time * spawnFlickerSpeed, flickerOffset + Time.time * 10f);
            // targetIntensity = Mathf.Lerp(spawnFlickerIntensityRange.x, spawnFlickerIntensityRange.y, noise);
        }
        else if (enableFlicker)
        {
            // --- Normal Flicker Phase ---
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset); // Value between ~0 and 1
            targetIntensity = Mathf.Lerp(intensityRange.x, intensityRange.y, noise);
        }
        else
        {
             // Flicker disabled (after spawn), use base intensity
             targetIntensity = intensityRange.x; // Or average: (intensityRange.x + intensityRange.y) * 0.5f;
        }

        // Apply the calculated intensity, modulated by the current fade alpha
        ApplyAlphaAndIntensity(targetIntensity * currentAlpha);
    }

    void ApplyAlphaAndIntensity(float finalIntensity)
    {
        // Apply alpha to Sprite Material Tint Color
        if (spriteMaterialInstance != null)
        {
            Color baseColor = spriteMaterialInstance.GetColor("_Color"); // Get current base tint
            baseColor.a = currentAlpha; // Set alpha for fade
            spriteMaterialInstance.SetColor("_Color", baseColor);

            // Apply Intensity to Emission Color
            Color baseEmissionColor = spriteMaterialInstance.GetColor("_EmissionColor"); // Get HDR color structure
             // Calculate final HDR color based on intensity. Intensity is already modulated by alpha.
             // We assume baseEmissionColor defines the *color*, finalIntensity defines the *brightness*.
             // Note: Multiplying the HDR color directly scales its baked-in intensity.
             // We need the original color without intensity to apply the new intensity.
             // This requires knowing the 'pure' color, which is tricky.
             // A common workaround is to set emission to WHITE * intensity if the sprite tint handles the color.
             // Or, assume the emission color property is JUST the color, and intensity is separate.
             // Let's assume _EmissionColor stores base color+intensity, and we scale it:
             // *This might need adjustment based on how HDR colors behave in your Unity version.*
             // A safer bet is often: baseEmissionColor = Color.yellow; // Define pure color
             // finalEmissionColor = baseEmissionColor * finalIntensity;

             // Simpler approach: Scale the existing HDR color's brightness.
             // This assumes the base intensity was ~1 or is irrelevant compared to the flicker range.
             Color finalEmissionColor = baseEmissionColor * finalIntensity; // Scale existing HDR value


            spriteMaterialInstance.SetColor("_EmissionColor", finalEmissionColor);
        }

        // Apply Intensity to Light2D
        if (pointLight != null)
        {
            // We typically fade the light's intensity directly with alpha
            pointLight.intensity = finalIntensity; // Intensity already includes alpha modulation
        }
    }


    void Die()
    {
        if (manager != null)
        {
            manager.ReportFireflyDespawned(this);
        }
        // Ensure alpha is zero before destroying if fade wasn't complete
        currentAlpha = 0f;
        ApplyAlphaAndIntensity(0f); // Apply zero intensity/alpha

        // Destroy after a tiny delay to ensure final alpha state renders? Usually not needed.
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (spriteMaterialInstance != null)
        {
            Destroy(spriteMaterialInstance);
        }
    }
}
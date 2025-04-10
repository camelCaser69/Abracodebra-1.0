// FILE: Assets/Scripts/Ecosystem/Effects/FireflyController.cs
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] private Vector2 lifetimeRange = new Vector2(8f, 18f);
    [SerializeField] private float fadeInDuration = 0.75f;
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Glow Flicker")]
    [SerializeField] private bool enableFlicker = true;
    [Tooltip("Intensity range for Emission / Light2D during normal flicker.")]
    [SerializeField] private Vector2 intensityRange = new Vector2(1.5f, 3.0f);
    [SerializeField] private float flickerSpeed = 5.0f;

    [Header("Spawn Flicker Effect")]
    [SerializeField] private bool enableSpawnFlicker = true;
    [SerializeField] private float spawnFlickerDuration = 0.5f;
    [Tooltip("Intensity range for Emission / Light2D during SPAWN flicker.")]
    [SerializeField] private Vector2 spawnFlickerIntensityRange = new Vector2(0.5f, 4.0f);
    [SerializeField] private float spawnFlickerSpeed = 15.0f;

    [Header("Scent Attraction")]
    [Tooltip("How often (in seconds) the firefly checks for nearby scent sources.")]
    [SerializeField] private float scentCheckInterval = 1.0f;
    [Tooltip("Maximum distance squared the OverlapCircle will check.")]
    [SerializeField] private float scentOverlapCheckRadius = 10f;
    [Tooltip("How strongly the firefly steers towards the scent target.")]
    [SerializeField] private float attractionStrength = 2.0f;
    [Tooltip("Preferred distance to orbit the scent source.")]
    [SerializeField] private float orbitDistance = 0.8f;
    [Tooltip("How much randomness/wobble in the attracted movement.")]
    [SerializeField] private float attractionWobble = 0.5f;
    [Tooltip("Which Scent Definitions attract this firefly.")]
    [SerializeField] private List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();

    // --- Public Accessor ---
    public Transform AttractionTarget => attractionTarget;

    // --- Internal State ---
    private FireflyManager manager;
    private Vector2 currentVelocity;
    private float currentSpeed;
    private float stateTimer;
    private bool isPaused;
    private float lifetime;
    private float age = 0f;
    private float currentAlpha = 0f; // Overall transparency/fade progress
    private float flickerOffset;
    private Material spriteMaterialInstance; // Instanced material for modification

    // Scent State
    private Transform attractionTarget = null;
    private float scentCheckTimer;
    private ScentDefinition currentTargetScentDef = null;

    // Movement Bounds
    private Vector2 minBounds;
    private Vector2 maxBounds;

    // Store the original emission color *without* intensity scaling from the material asset
    private Color baseEmissionColor = Color.black; // Default to black if reading fails

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.material != null) {
            // Create instance BEFORE reading base emission
            spriteMaterialInstance = spriteRenderer.material;
            // Try to read the base emission color set in the material asset
            if (spriteMaterialInstance.HasProperty("_EmissionColor")) {
                 // Important: Get the color value directly. If it's HDR, this value
                 // might already have some intensity baked in, depending on how it was set.
                 // Ideally, set the material's emission color to the desired *hue/saturation*
                 // with an intensity of 1 in the editor, and control brightness purely via script.
                 baseEmissionColor = spriteMaterialInstance.GetColor("_EmissionColor");
                 // If the color read already has intensity > 1 baked in, we might need to normalize it.
                 // For simplicity, let's assume the user sets the base color correctly.
                 // Example normalization (if needed):
                 // float currentIntensity = Mathf.Max(baseEmissionColor.r, baseEmissionColor.g, baseEmissionColor.b);
                 // if (currentIntensity > 1.0f) baseEmissionColor /= currentIntensity;
            } else {
                 Debug.LogWarning($"[{gameObject.name}] Material '{spriteMaterialInstance.name}' does not have an '_EmissionColor' property.", gameObject);
            }
        } else if (spriteRenderer == null || spriteRenderer.material == null) {
            Debug.LogWarning($"[{gameObject.name}] FireflyController: Cannot modify material properties (flicker/fade), SpriteRenderer or its material is missing.", gameObject);
            enableFlicker = false;
            enableSpawnFlicker = false;
        }

        flickerOffset = Random.Range(0f, 100f);
    }

    public void Initialize(FireflyManager owner, Vector2 minB, Vector2 maxB)
    {
         manager = owner; minBounds = minB; maxBounds = maxB;
         lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
         age = 0f; currentAlpha = 0f;
         attractionTarget = null; currentTargetScentDef = null;
         scentCheckTimer = Random.Range(0, scentCheckInterval);
         PickNewWanderState();
         // Apply initial state (fully transparent, potentially zero intensity)
         ApplyVisualState(0f); // Pass initial intensity 0
    }

    void Update()
    {
        age += Time.deltaTime;

        HandleLifetimeAndFade(); // Calculates currentAlpha
        if (currentAlpha <= 0f && age > fadeInDuration) { Die(); return; }

        HandleScentDetection();
        HandleMovement();
        HandleGlowAndFlicker(); // Calculates target intensity & calls ApplyVisualState
    }

    void HandleLifetimeAndFade()
    {
        // Calculate target alpha based on age and lifetime
        if (age < fadeInDuration) {
            currentAlpha = Mathf.Clamp01(age / fadeInDuration); // Fade In
        } else if (lifetime - age < fadeOutDuration) {
            currentAlpha = Mathf.Clamp01((lifetime - age) / fadeOutDuration); // Fade Out
        } else {
            currentAlpha = 1.0f; // Fully Visible
        }

        // Check if lifetime naturally expired
        if (age >= lifetime && currentAlpha > 0) {
             currentAlpha = Mathf.Clamp01((lifetime - age + fadeOutDuration) / fadeOutDuration);
             if(currentAlpha <= 0) Die();
        }
    }

    // HandleScentDetection (No changes needed from previous version)
    void HandleScentDetection()
    {
        scentCheckTimer -= Time.deltaTime;
        if (scentCheckTimer <= 0f) { FindAttractionTarget(); scentCheckTimer = scentCheckInterval; }
        if (attractionTarget != null) { if (!attractionTarget.gameObject.activeInHierarchy || !attractionTarget.TryGetComponent<ScentSource>(out var currentScent) || currentScent.definition != currentTargetScentDef || (attractionTarget.position - transform.position).sqrMagnitude > (currentScent.EffectiveRadius * currentScent.EffectiveRadius) ) { attractionTarget = null; currentTargetScentDef = null; } }
    }

    // FindAttractionTarget (No changes needed from previous version)
     void FindAttractionTarget()
     {
         Transform bestTarget = null; ScentDefinition bestScentDef = null; float bestScore = -1f;
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scentOverlapCheckRadius);
         foreach (Collider2D hit in hits) {
             if (hit.TryGetComponent<ScentSource>(out ScentSource scent) && scent.definition != null) {
                 if (attractiveScentDefinitions.Contains(scent.definition)) {
                     float distSq = (hit.transform.position - transform.position).sqrMagnitude; float scentRadiusSq = scent.EffectiveRadius * scent.EffectiveRadius;
                     if (distSq <= scentRadiusSq) { float score = 1.0f / (distSq + 0.01f); if (score > bestScore) { bestScore = score; bestTarget = hit.transform; bestScentDef = scent.definition; } }
                 }
             }
         }
         if (bestTarget != attractionTarget) { attractionTarget = bestTarget; currentTargetScentDef = bestScentDef; if (attractionTarget != null) { isPaused = false; } }
     }

    // HandleMovement (No changes needed from previous version)
    void HandleMovement()
    {
        stateTimer -= Time.deltaTime;
        if (attractionTarget != null) {
            Vector2 directionToTarget = (attractionTarget.position - transform.position); float distanceToTarget = directionToTarget.magnitude; if (distanceToTarget > 0.01f) { directionToTarget /= distanceToTarget; }
            Vector2 orbitOffsetDir = new Vector2(-directionToTarget.y, directionToTarget.x) * Mathf.Sign(currentSpeed + 0.1f); Vector2 desiredDirection = directionToTarget * attractionStrength;
            if (distanceToTarget <= orbitDistance) { desiredDirection += orbitOffsetDir * (currentSpeed * 0.5f); } desiredDirection += Random.insideUnitCircle * attractionWobble;
            currentVelocity = Vector2.Lerp(currentVelocity.normalized, desiredDirection.normalized, Time.deltaTime * 5f) * currentSpeed;
        } else { if (stateTimer <= 0f) { PickNewWanderState(); } }
        if (!isPaused) {
             Vector2 currentPos = transform.position; Vector2 newPos = currentPos + currentVelocity * Time.deltaTime; bool clampedX = false; bool clampedY = false;
             if (newPos.x <= minBounds.x || newPos.x >= maxBounds.x) { clampedX = true; newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x); } if (newPos.y <= minBounds.y || newPos.y >= maxBounds.y) { clampedY = true; newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y); }
             if (clampedX || clampedY) { Vector2 reflectionNormal = Vector2.zero; if(clampedX) reflectionNormal.x = -Mathf.Sign(currentVelocity.x); if(clampedY) reflectionNormal.y = -Mathf.Sign(currentVelocity.y); currentVelocity = Vector2.Reflect(currentVelocity, reflectionNormal.normalized + Random.insideUnitCircle * 0.1f).normalized * currentSpeed; if (currentVelocity.sqrMagnitude < 0.01f) { currentVelocity = Random.insideUnitCircle.normalized * currentSpeed; } PickNewWanderState(true); }
            transform.position = newPos;
        }
    }

    // PickNewWanderState (No changes needed from previous version)
    void PickNewWanderState(bool forceMove = false)
    {
        if (attractionTarget != null && !forceMove) return;
        if (!forceMove && Random.value < pauseChance) { isPaused = true; currentVelocity = Vector2.zero; stateTimer = Random.Range(pauseDurationRange.x, pauseDurationRange.y); }
        else { isPaused = false; currentSpeed = Random.Range(speedRange.x, speedRange.y); if(currentVelocity.sqrMagnitude < 0.01f || forceMove) currentVelocity = Random.insideUnitCircle.normalized * currentSpeed; else currentVelocity = (currentVelocity.normalized + Random.insideUnitCircle * 0.5f).normalized * currentSpeed; stateTimer = directionChangeInterval * Random.Range(0.7f, 1.3f); }
    }


    // --- Modified Glow/Flicker Logic ---
    void HandleGlowAndFlicker()
    {
        // Calculate the target BRIGHTNESS intensity based on flicker state
        float targetFlickerIntensity;

        if (enableSpawnFlicker && age < spawnFlickerDuration) {
            targetFlickerIntensity = Mathf.Lerp(spawnFlickerIntensityRange.x, spawnFlickerIntensityRange.y, Random.value);
        } else if (enableFlicker) {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset);
            targetFlickerIntensity = Mathf.Lerp(intensityRange.x, intensityRange.y, noise);
        } else {
             targetFlickerIntensity = intensityRange.x; // Use base intensity if flicker disabled
        }

        // Apply the calculated intensity and the current fade alpha
        ApplyVisualState(targetFlickerIntensity);
    }

    /// <summary>
    /// Applies the visual state based on calculated flicker intensity and fade alpha.
    /// </summary>
    /// <param name="flickerIntensity">The target brightness intensity for emission/light.</param>
    void ApplyVisualState(float flickerIntensity)
    {
        // 1. Apply overall transparency (Alpha Fade) to the SpriteRenderer's base color alpha
        if (spriteMaterialInstance != null)
        {
            // Ensure _Color property exists before trying to set it
            if (spriteMaterialInstance.HasProperty("_Color"))
            {
                Color baseColor = spriteMaterialInstance.GetColor("_Color");
                baseColor.a = currentAlpha; // Apply fade alpha
                spriteMaterialInstance.SetColor("_Color", baseColor);
            }

            // 2. Apply Flicker Intensity to Emission Color's brightness
            if (spriteMaterialInstance.HasProperty("_EmissionColor"))
            {
                 // Apply intensity to the base emission color we stored in Awake
                 // Use LinearToGammaSpace if in Linear color space for more visually correct intensity scaling
                 Color finalEmissionColor = baseEmissionColor * Mathf.LinearToGammaSpace(flickerIntensity);
                 // Alternative if baseEmissionColor already has intensity: Multiply directly
                 // Color finalEmissionColor = baseEmissionColor * flickerIntensity;
                 spriteMaterialInstance.SetColor("_EmissionColor", finalEmissionColor);
            }
        }

        // 3. Apply Flicker Intensity (modulated by alpha fade) to Light2D
        if (pointLight != null)
        {
            // Light intensity should reflect both flicker and fade
            pointLight.intensity = flickerIntensity * currentAlpha;
        }
    }

    // Die (No changes needed)
    void Die()
    {
        if (manager != null) manager.ReportFireflyDespawned(this);
        currentAlpha = 0f; ApplyVisualState(0f); // Ensure visuals are off
        Destroy(gameObject);
    }

    // OnDestroy (No changes needed)
     void OnDestroy() { if (spriteMaterialInstance != null) Destroy(spriteMaterialInstance); }
}
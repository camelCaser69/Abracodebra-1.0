// FILE: Assets/Scripts/Ecosystem/Effects/FireflyController.cs
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// No longer requires LineRenderer directly on this GameObject
public class FireflyController : MonoBehaviour
{
    [Header("References (Optional)")]
    [SerializeField] private Light2D pointLight;
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
    [SerializeField] private Vector2 intensityRange = new Vector2(1.5f, 3.0f);
    [SerializeField] private float flickerSpeed = 5.0f;

    [Header("Spawn Flicker Effect")]
    [SerializeField] private bool enableSpawnFlicker = true;
    [SerializeField] private float spawnFlickerDuration = 0.5f;
    [SerializeField] private Vector2 spawnFlickerIntensityRange = new Vector2(0.5f, 4.0f);
    [SerializeField] private float spawnFlickerSpeed = 15.0f;

    [Header("Scent Attraction")]
    [Tooltip("How often (in seconds) the firefly checks for nearby scent sources.")]
    [SerializeField] private float scentCheckInterval = 1.0f;
    // REMOVED: [SerializeField] private float scentDetectionRadius = 4f;
    [Tooltip("Maximum distance squared the OverlapCircle will check. Should be generous enough to find relevant scents. Increase if scents have very large radii.")]
    [SerializeField] private float scentOverlapCheckRadius = 10f; // Radius for Physics Check
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
    private float currentAlpha = 0f;
    private float flickerOffset;
    private Material spriteMaterialInstance;

    // Scent State
    private Transform attractionTarget = null;
    private float scentCheckTimer;
    private ScentDefinition currentTargetScentDef = null;

    // Movement Bounds
    private Vector2 minBounds;
    private Vector2 maxBounds;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.material != null) { spriteMaterialInstance = spriteRenderer.material; }
        else if (spriteRenderer == null || spriteRenderer.material == null) { enableFlicker = false; enableSpawnFlicker = false; }
        flickerOffset = Random.Range(0f, 100f);
    }

    public void Initialize(FireflyManager owner, Vector2 minB, Vector2 maxB)
    {
         manager = owner; minBounds = minB; maxBounds = maxB;
         lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
         age = 0f; currentAlpha = 0f; ApplyAlphaAndIntensity(0f);
         attractionTarget = null; currentTargetScentDef = null;
         scentCheckTimer = Random.Range(0, scentCheckInterval);
         PickNewWanderState();
    }

    void Update()
    {
        age += Time.deltaTime;
        HandleLifetimeAndFade();
        if (currentAlpha <= 0f && age > fadeInDuration) { Die(); return; }
        HandleScentDetection();
        HandleMovement();
        HandleGlowAndFlicker();
    }

    void HandleLifetimeAndFade()
    {
        if (age < fadeInDuration) { currentAlpha = Mathf.Clamp01(age / fadeInDuration); }
        else if (lifetime - age < fadeOutDuration) { currentAlpha = Mathf.Clamp01((lifetime - age) / fadeOutDuration); }
        else { currentAlpha = 1.0f; }
        if (age >= lifetime && currentAlpha > 0) { currentAlpha = Mathf.Clamp01((lifetime - age + fadeOutDuration) / fadeOutDuration); if(currentAlpha <= 0) Die(); }
    }

    // --- Modified Scent Detection ---
    void HandleScentDetection()
    {
        scentCheckTimer -= Time.deltaTime;
        if (scentCheckTimer <= 0f)
        {
            FindAttractionTarget();
            scentCheckTimer = scentCheckInterval;
        }

        // Check if current target is still valid (exists and firefly is still within its radius)
        if (attractionTarget != null)
        {
            if (!attractionTarget.gameObject.activeInHierarchy ||
                !attractionTarget.TryGetComponent<ScentSource>(out var currentScent) ||
                currentScent.definition != currentTargetScentDef ||
                (attractionTarget.position - transform.position).sqrMagnitude > (currentScent.EffectiveRadius * currentScent.EffectiveRadius) // Check if outside radius
               )
            {
                 // Target lost, changed scent, or firefly moved out of range
                 attractionTarget = null;
                 currentTargetScentDef = null;
            }
        }
    }

     void FindAttractionTarget()
     {
         Transform bestTarget = null;
         ScentDefinition bestScentDef = null;
         // Use a scoring system, e.g., prioritize closer or stronger scents
         float bestScore = -1f;

         // Use a generous overlap check radius to find potential candidates nearby
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scentOverlapCheckRadius);

         foreach (Collider2D hit in hits)
         {
             if (hit.TryGetComponent<ScentSource>(out ScentSource scent) && scent.definition != null)
             {
                 // 1. Check if the scent definition is attractive
                 if (attractiveScentDefinitions.Contains(scent.definition))
                 {
                     // 2. Calculate distance squared
                     float distSq = (hit.transform.position - transform.position).sqrMagnitude;
                     float scentRadius = scent.EffectiveRadius;
                     float scentRadiusSq = scentRadius * scentRadius;

                     // 3. Check if the firefly is within the scent's actual radius
                     if (distSq <= scentRadiusSq)
                     {
                         // This scent is attractive and close enough to detect

                         // Calculate a score (e.g., inverse distance, could add strength factor later)
                         // Add a small epsilon to avoid division by zero if perfectly overlapping
                         float score = 1.0f / (distSq + 0.01f);

                         if (score > bestScore)
                         {
                             bestScore = score;
                             bestTarget = hit.transform;
                             bestScentDef = scent.definition;
                         }
                     }
                 }
             }
         }

         // Update target only if a new best target was found or if current is lost
         if (bestTarget != attractionTarget)
         {
             attractionTarget = bestTarget;
             currentTargetScentDef = bestScentDef;

             if (attractionTarget != null)
             {
                 isPaused = false; // Ensure not paused when attracted
             }
         }
         // If no attractive scent is found within its own radius, attractionTarget will become null
     }

    // HandleMovement (No changes needed, uses attractionTarget)
    void HandleMovement()
    {
        stateTimer -= Time.deltaTime;
        if (attractionTarget != null) {
            Vector2 directionToTarget = (attractionTarget.position - transform.position);
            float distanceToTarget = directionToTarget.magnitude;
            if (distanceToTarget > 0.01f) { directionToTarget /= distanceToTarget; }
            Vector2 orbitOffsetDir = new Vector2(-directionToTarget.y, directionToTarget.x) * Mathf.Sign(currentSpeed + 0.1f);
            Vector2 desiredDirection = directionToTarget * attractionStrength;
            if (distanceToTarget <= orbitDistance) { desiredDirection += orbitOffsetDir * (currentSpeed * 0.5f); }
            desiredDirection += Random.insideUnitCircle * attractionWobble;
            currentVelocity = Vector2.Lerp(currentVelocity.normalized, desiredDirection.normalized, Time.deltaTime * 5f) * currentSpeed;
        } else {
            if (stateTimer <= 0f) { PickNewWanderState(); }
        }
        if (!isPaused) {
             Vector2 currentPos = transform.position; Vector2 newPos = currentPos + currentVelocity * Time.deltaTime;
             bool clampedX = false; bool clampedY = false;
             if (newPos.x <= minBounds.x || newPos.x >= maxBounds.x) { clampedX = true; newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x); }
             if (newPos.y <= minBounds.y || newPos.y >= maxBounds.y) { clampedY = true; newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y); }
             if (clampedX || clampedY) {
                 Vector2 reflectionNormal = Vector2.zero; if(clampedX) reflectionNormal.x = -Mathf.Sign(currentVelocity.x); if(clampedY) reflectionNormal.y = -Mathf.Sign(currentVelocity.y);
                 currentVelocity = Vector2.Reflect(currentVelocity, reflectionNormal.normalized + Random.insideUnitCircle * 0.1f).normalized * currentSpeed;
                 if (currentVelocity.sqrMagnitude < 0.01f) { currentVelocity = Random.insideUnitCircle.normalized * currentSpeed; }
                 PickNewWanderState(true);
             }
            transform.position = newPos;
        }
    }

    // PickNewWanderState (No changes needed)
    void PickNewWanderState(bool forceMove = false)
    {
        if (attractionTarget != null && !forceMove) return;
        if (!forceMove && Random.value < pauseChance) { isPaused = true; currentVelocity = Vector2.zero; stateTimer = Random.Range(pauseDurationRange.x, pauseDurationRange.y); }
        else {
            isPaused = false; currentSpeed = Random.Range(speedRange.x, speedRange.y);
            if(currentVelocity.sqrMagnitude < 0.01f || forceMove) currentVelocity = Random.insideUnitCircle.normalized * currentSpeed;
            else currentVelocity = (currentVelocity.normalized + Random.insideUnitCircle * 0.5f).normalized * currentSpeed;
            stateTimer = directionChangeInterval * Random.Range(0.7f, 1.3f);
        }
    }

    // HandleGlowAndFlicker (No changes needed)
    void HandleGlowAndFlicker()
    {
        if (!enableFlicker && !enableSpawnFlicker) return; float targetIntensity;
        if (enableSpawnFlicker && age < spawnFlickerDuration) { targetIntensity = Mathf.Lerp(spawnFlickerIntensityRange.x, spawnFlickerIntensityRange.y, Random.value); }
        else if (enableFlicker) { float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset); targetIntensity = Mathf.Lerp(intensityRange.x, intensityRange.y, noise); }
        else { targetIntensity = intensityRange.x; } ApplyAlphaAndIntensity(targetIntensity * currentAlpha);
    }

    // ApplyAlphaAndIntensity (No changes needed)
    void ApplyAlphaAndIntensity(float finalIntensity)
    {
        if (spriteMaterialInstance != null) { Color baseColor = spriteMaterialInstance.GetColor("_Color"); baseColor.a = currentAlpha; spriteMaterialInstance.SetColor("_Color", baseColor); Color baseEmissionColor = spriteMaterialInstance.GetColor("_EmissionColor"); Color finalEmissionColor = baseEmissionColor * finalIntensity; spriteMaterialInstance.SetColor("_EmissionColor", finalEmissionColor); }
        if (pointLight != null) { pointLight.intensity = finalIntensity; }
    }

    // Die (No changes needed)
    void Die()
    {
        if (manager != null) manager.ReportFireflyDespawned(this);
        currentAlpha = 0f; ApplyAlphaAndIntensity(0f);
        Destroy(gameObject);
    }

    // OnDestroy (No changes needed)
     void OnDestroy() { if (spriteMaterialInstance != null) Destroy(spriteMaterialInstance); }
}
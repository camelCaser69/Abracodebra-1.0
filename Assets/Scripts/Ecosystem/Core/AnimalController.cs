using UnityEngine;
using System.Linq;
using System.Collections.Generic;


[RequireComponent(typeof(Rigidbody2D))]
public class AnimalController : MonoBehaviour
{
    // Assigned via FaunaManager on instantiation
    private AnimalDefinition definition;

    [Header("Thought/Dialogue Setup")]
    public AnimalThoughtLibrary thoughtLibrary;      // Assign GlobalAnimalThoughtLibrary asset
    public GameObject thoughtBubblePrefab;           // Assign your ThoughtBubble prefab

    [Header("Transforms for Visual Alignment")]
    public Transform mouthTransform;                 // For aligning mouth (for future pooping VFX)
    public Transform bubbleSpawnTransform;           // Where thought bubbles spawn

    [Header("Thought Bubble Settings")]
    public float thoughtCooldownTime = 5f;             // Minimum time between thought bubbles
    private float thoughtCooldownTimer = 0f;

    private string speciesName;                      // Derived from definition.animalName

    // Basic runtime stats
    private float currentHealth;
    private float currentHunger;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;           // For sprite flipping
    private Vector2 moveDirection;

    // Global movement bounds (set via FaunaManager; hidden in Inspector)
    [HideInInspector] private Vector2 minBounds;
    [HideInInspector] private Vector2 maxBounds;

    [Header("Eating Settings")]
    public float eatDuration = 2f;                    // Time spent eating
    [Tooltip("How close the animal must be to a leaf to eat it.")]
    public float eatDistance = 0.5f;
    private bool isEating = false;
    private float eatTimer = 0f;
    private GameObject currentTargetLeaf;

    [Header("Wander Behavior Settings")]
    [Tooltip("Intensity multiplier (0 to 1) controlling the chance to pause while wandering.")]
    [Range(0f, 1f)]
    public float wanderPauseIntensity = 0.5f;
    [Tooltip("Minimum duration for a moving phase while wandering.")]
    public float wanderMinMoveDuration = 1f;
    [Tooltip("Maximum duration for a moving phase while wandering.")]
    public float wanderMaxMoveDuration = 3f;
    [Tooltip("Minimum duration for a pause while wandering.")]
    public float wanderMinPauseDuration = 0.5f;
    [Tooltip("Maximum duration for a pause while wandering.")]
    public float wanderMaxPauseDuration = 2f;
    private bool isWanderPaused = false;
    private float wanderStateTimer = 0f;

    [Header("Pooping Settings")]
    [Tooltip("Minimum delay after eating before the animal poops.")]
    public float minPoopDelay = 5f;
    [Tooltip("Maximum delay after eating before the animal poops.")]
    public float maxPoopDelay = 10f;
    [Tooltip("Duration (in seconds) the animal spends pooping (not moving).")]
    public float poopDuration = 1f;

// Replace the single poopPrefab with a list:
    [Tooltip("List of poop prefabs for random selection.")]
    public List<GameObject> poopPrefabs;

// Variation to apply to the poop sprite's color channels (0 = no variation).
    [Tooltip("Maximum amount to vary each color channel (0-1) for the poop sprite.")]
    public float poopColorVariation = 0.1f;


    // New pooping state variables
    private bool isPooping = false;
    private float poopTimer = 0f;      // For pooping phase duration
    private float poopDelayTimer = 0f; // Delay before pooping after eating
    private bool hasPooped = false;    // True if the animal has already pooped after the last eating cycle

    public void Initialize(AnimalDefinition def)
    {
        definition = def;
        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        speciesName = definition.animalName; // e.g., "Bunny"
    
        // Start as if already pooped:
        hasPooped = true;
        // Set poopDelayTimer so the animal won't poop until after its next eating cycle.
        poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (definition == null)
        {
            Debug.LogWarning("[AnimalController] 'definition' is null. Did you call Initialize()?");
            return;
        }

        // Decrement thought bubble cooldown timer
        thoughtCooldownTimer -= Time.deltaTime;

        // Increase hunger over time
        currentHunger += definition.hungerDecayRate * Time.deltaTime;

        // Process pooping only if not eating and haven't pooped this cycle
        if (!isEating && !hasPooped)
        {
            poopDelayTimer -= Time.deltaTime;
            if (!isPooping && poopDelayTimer <= 0f)
            {
                // Start pooping phase
                isPooping = true;
                poopTimer = poopDuration;
                ShowThought(ThoughtTrigger.Pooping);
                moveDirection = Vector2.zero; // Stop moving during pooping
            }
            if (isPooping)
            {
                poopTimer -= Time.deltaTime;
                if (poopTimer <= 0f)
                {
                    SpawnPoop();
                    isPooping = false;
                    hasPooped = true; // Mark that we've pooped this cycle
                }
            }
        }

        // If in eating state, update timer and exit early
        if (isEating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0f)
            {
                isEating = false;
                FinishEatingLeaf();
            }
            return;
        }

        // Decide behavior based on hunger
        if (currentHunger >= definition.hungerThreshold)
        {
            Debug.Log($"{speciesName} is hungry! (Hunger: {currentHunger:0.00}/{definition.hungerThreshold})");
            if (thoughtCooldownTimer <= 0f)
            {
                ShowThought(ThoughtTrigger.Hungry);
                thoughtCooldownTimer = thoughtCooldownTime;
            }
            if (currentTargetLeaf == null)
                currentTargetLeaf = FindNearestLeaf();

            if (currentTargetLeaf != null)
                MoveTowardLeaf(currentTargetLeaf);
            else
                Wander();
        }
        else
        {
            Wander();
            currentTargetLeaf = null;
        }

        FlipSpriteBasedOnDirection(moveDirection);
    }

    private void FixedUpdate()
    {
        if (isEating || rb == null)
            return;
        Vector2 newPos = rb.position + moveDirection * definition.movementSpeed * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
        rb.MovePosition(newPos);
    }

    // Wander behavior: alternate between moving and pausing with random durations.
    private void Wander()
    {
        if (wanderStateTimer <= 0f)
        {
            if (isWanderPaused)
            {
                isWanderPaused = false;
                float angle = Random.Range(0f, 360f);
                moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration);
            }
            else
            {
                if (Random.value < wanderPauseIntensity)
                {
                    isWanderPaused = true;
                    moveDirection = Vector2.zero;
                    wanderStateTimer = Random.Range(wanderMinPauseDuration, wanderMaxPauseDuration);
                }
                else
                {
                    float angle = Random.Range(0f, 360f);
                    moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration);
                }
            }
        }
        else
        {
            wanderStateTimer -= Time.deltaTime;
        }
    }

    private void MoveTowardLeaf(GameObject leafObj)
    {
        if (isEating) return; // Prevent re-triggering if already eating
        if (!leafObj)
        {
            currentTargetLeaf = null;
            return;
        }
        Vector2 leafPos = leafObj.transform.position;
        Vector2 myPos = transform.position;
        float distance = Vector2.Distance(myPos, leafPos);
        if (distance <= eatDistance)
        {
            isEating = true;
            eatTimer = eatDuration;
            ShowThought(ThoughtTrigger.Eating);
        }
        else
        {
            moveDirection = (leafPos - myPos).normalized;
        }
    }

    private void FinishEatingLeaf()
    {
        if (currentTargetLeaf)
        {
            Destroy(currentTargetLeaf);
            currentTargetLeaf = null;
        }
        currentHunger -= definition.eatAmount;
        if (currentHunger < 0f)
            currentHunger = 0f;
        // Reset pooping state for this eating cycle.
        hasPooped = false;
        poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
    }

    private GameObject FindNearestLeaf()
    {
        GameObject[] leaves = GameObject.FindGameObjectsWithTag("Leaf");
        if (leaves.Length == 0)
            return null;
        Vector2 myPos = transform.position;
        GameObject nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var leaf in leaves)
        {
            float dist = Vector2.Distance(myPos, leaf.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = leaf;
            }
        }
        return nearest;
    }

    // Thought bubble logic: spawn a bubble as a child of bubbleSpawnTransform.
    private void ShowThought(ThoughtTrigger trigger)
    {
        if (!thoughtLibrary || !thoughtBubblePrefab)
        {
            Debug.LogWarning($"[{speciesName}] Missing thought library or bubble prefab!");
            return;
        }
        if (string.IsNullOrEmpty(speciesName))
            return;
        var matchingEntries = thoughtLibrary.allThoughts
            .Where(t => t.speciesName == speciesName && t.trigger == trigger)
            .ToList();
        if (matchingEntries.Count == 0)
        {
            Debug.Log($"[{speciesName}] No thought lines for trigger {trigger}.");
            return;
        }
        var chosenEntry = matchingEntries[Random.Range(0, matchingEntries.Count)];
        if (chosenEntry.lines == null || chosenEntry.lines.Count == 0)
        {
            Debug.Log($"[{speciesName}] Thought entry for trigger {trigger} has no lines.");
            return;
        }
        string randomLine = chosenEntry.lines[Random.Range(0, chosenEntry.lines.Count)];
        Debug.Log($"[{speciesName}] Spawning thought bubble: {randomLine}");
        Transform spawnParent = bubbleSpawnTransform ? bubbleSpawnTransform : transform;
        GameObject bubbleObj = Instantiate(thoughtBubblePrefab, spawnParent.position, Quaternion.identity, spawnParent);
        bubbleObj.transform.localPosition = Vector3.zero;
        ThoughtBubbleController bubble = bubbleObj.GetComponent<ThoughtBubbleController>();
        if (bubble != null)
            bubble.Initialize(randomLine, spawnParent, 2f);
        else
            Debug.LogWarning($"[{speciesName}] ThoughtBubblePrefab is missing ThoughtBubbleController!");
    }

    // Spawn a poop object at the mouthTransform (or fallback to self)
    private void SpawnPoop()
    {
        if (poopPrefabs != null && poopPrefabs.Count > 0)
        {
            // Randomly choose one poop prefab using the integer overload.
            int index = Random.Range(0, (int)poopPrefabs.Count);
            GameObject selectedPrefab = poopPrefabs[index];

            // Use the mouthTransform as the spawn point if available, else fallback to self.
            Transform spawnPoint = mouthTransform ? mouthTransform : transform;
            GameObject poopObj = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);

            // Randomly flip the poop horizontally.
            SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = (Random.value > 0.5f);

                // Adjust color randomly within the specified variation range.
                Color originalColor = sr.color;
                float variation = poopColorVariation;
                float newR = Mathf.Clamp01(originalColor.r + Random.Range(-variation, variation));
                float newG = Mathf.Clamp01(originalColor.g + Random.Range(-variation, variation));
                float newB = Mathf.Clamp01(originalColor.b + Random.Range(-variation, variation));
                sr.color = new Color(newR, newG, newB, originalColor.a);
            }
        }
        else
        {
            Debug.LogWarning($"[{speciesName}] No poopPrefabs assigned!");
        }
    }



    // Sprite flipping based on horizontal movement direction.
    private void FlipSpriteBasedOnDirection(Vector2 direction)
    {
        if (!spriteRenderer)
            return;
        if (direction.x < -0.01f)
            spriteRenderer.flipX = true;
        else if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
    }
    
    // Public setter for movement bounds (called from FaunaManager)
    public void SetMovementBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }

    // Public method to compare species name (used by FaunaManager)
    public bool SpeciesNameEquals(string other)
    {
        return speciesName == other;
    }
}

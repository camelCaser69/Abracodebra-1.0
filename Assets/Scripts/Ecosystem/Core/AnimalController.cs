using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Keep for potential future use, but not strictly needed for current FindBestFood

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SortableEntity))]
public class AnimalController : MonoBehaviour
{
    // References set by FaunaManager or Initialize
    private AnimalDefinition definition;
    private AnimalDiet animalDiet;

    // Inspector Assigned References (Optional Features)
    [Header("Optional Features")]
    public AnimalThoughtLibrary thoughtLibrary;
    public GameObject thoughtBubblePrefab;
    public Transform bubbleSpawnTransform;
    public Transform poopSpawnPoint;
    public List<GameObject> poopPrefabs;
    public Animator animator; // Assign if using animations

    [Header("Behavior Tuning")]
    public float searchRadius = 5f;
    public float eatDistance = 0.5f;
    public float eatDuration = 1.5f;
    [Range(0f, 1f)] public float wanderPauseChance = 0.3f;
    public float wanderMinMoveDuration = 1f;
    public float wanderMaxMoveDuration = 3f;
    public float wanderMinPauseDuration = 0.5f;
    public float wanderMaxPauseDuration = 2f;
    public float minPoopDelay = 5f;
    public float maxPoopDelay = 10f;
    public float poopDuration = 1f;
    public float poopColorVariation = 0.1f;
    public float thoughtCooldownTime = 5f;


    // Internal State
    private float currentHealth;
    private float currentHunger;
    private GameObject currentTargetFood = null;
    private Vector2 moveDirection = Vector2.zero;
    private bool isEating = false;
    private float eatTimer = 0f;
    private bool isWanderPaused = false;
    private float wanderStateTimer = 0f;
    private bool isPooping = false;
    private float poopTimer = 0f;
    private float poopDelayTimer = 0f;
    private bool hasPooped = true;
    private float thoughtCooldownTimer = 0f;

    // Component References
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Movement Bounds
    private Vector2 minBounds;
    private Vector2 maxBounds;

    // Public Accessors
    public float CurrentHealth => currentHealth;
    public string SpeciesName => definition ? definition.animalName : "Uninitialized";

    /// <summary>
    /// Initializes the Animal Controller. Called by FaunaManager.
    /// </summary>
    public void Initialize(AnimalDefinition def, Vector2 minB, Vector2 maxB)
    {
        definition = def;
        if (definition == null) {
            Debug.LogError($"[{gameObject.name}] Null definition provided!", gameObject);
            Destroy(gameObject); return;
        }

        animalDiet = def.diet;
        if (animalDiet == null) {
            Debug.LogError($"[{gameObject.name}] AnimalDefinition '{def.name}' missing required Diet!", gameObject);
            enabled = false; // Disable controller
            return;
        }

        // Get Components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Allows sprite to be child
        // Animator is assigned via inspector if used

        // Set Initial State
        currentHealth = definition.maxHealth;
        currentHunger = 0f; // Start not hungry
        hasPooped = true;
        poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
        minBounds = minB;
        maxBounds = maxB;

        if (spriteRenderer == null) {
             Debug.LogWarning($"[{gameObject.name}] No SpriteRenderer found in children.", gameObject);
        }
    }

    void Update()
    {
        if (!enabled) return; // Do nothing if not initialized correctly

        UpdateHunger();
        HandlePooping();
        UpdateThoughts();

        if (isEating) {
            HandleEating();
            moveDirection = Vector2.zero;
        } else if (isPooping) {
            // Pooping state/timer handled in HandlePooping
            moveDirection = Vector2.zero;
        } else {
            DecideNextAction(); // Decide whether to wander or seek food
        }

        FlipSpriteBasedOnDirection();
        UpdateAnimationState();
    }

    void FixedUpdate()
    {
        // Apply movement if applicable
        if (rb != null && !isEating && !isPooping && moveDirection != Vector2.zero)
        {
            Vector2 currentPos = rb.position;
            Vector2 desiredMove = moveDirection.normalized * definition.movementSpeed * Time.fixedDeltaTime;
            Vector2 newPos = currentPos + desiredMove;

            // Clamp position
            newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
            newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

            rb.MovePosition(newPos);
        }
    }

    void UpdateHunger()
    {
        currentHunger += animalDiet.hungerIncreaseRate * Time.deltaTime;
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger); // Clamp

        // Removed starvation logic for simplicity
        // if (currentHunger >= animalDiet.maxHunger) { /* ApplyStarvationDamage(); */ }
    }

    void HandlePooping()
    {
        if (!isEating && !hasPooped) {
            poopDelayTimer -= Time.deltaTime;
            if (!isPooping && poopDelayTimer <= 0f) { StartPooping(); }
            if (isPooping) {
                poopTimer -= Time.deltaTime;
                if (poopTimer <= 0f) { FinishPooping(); }
            }
        }
    }

     void UpdateThoughts() {
         if (thoughtCooldownTimer > 0) { thoughtCooldownTimer -= Time.deltaTime; }
     }

    void HandleEating()
    {
        eatTimer -= Time.deltaTime;
        if (eatTimer <= 0f) {
            isEating = false;
            FinishEatingAction(); // Renamed for clarity
        }
    }

    void DecideNextAction()
    {
        if (currentHunger >= animalDiet.hungerThreshold) {
            SeekFood();
        } else {
            Wander();
            currentTargetFood = null; // Lose target if not hungry
        }
    }

    void SeekFood()
    {
        if (CanShowThought()) ShowThought(ThoughtTrigger.Hungry);

        // Check if target is still valid (exists and has FoodItem)
        bool targetValid = currentTargetFood != null && currentTargetFood.activeInHierarchy && currentTargetFood.GetComponent<FoodItem>() != null;

        if (!targetValid) {
            currentTargetFood = FindNearestFood();
        }

        if (currentTargetFood != null) {
            MoveTowardFood(currentTargetFood);
        } else {
            Wander(); // Can't find food
        }
    }

    GameObject FindNearestFood()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        return animalDiet.FindBestFood(colliders, transform.position); // Use Diet's logic
    }

    void MoveTowardFood(GameObject foodObj)
    {
        if (foodObj == null) return;
        float distance = Vector2.Distance(transform.position, foodObj.transform.position);

        if (distance <= eatDistance) {
            StartEating();
        } else {
            moveDirection = (foodObj.transform.position - transform.position).normalized;
            isWanderPaused = false; // Ensure not paused while seeking food
            wanderStateTimer = 0f;
        }
    }

    void StartEating()
    {
        isEating = true;
        eatTimer = eatDuration;
        moveDirection = Vector2.zero;
        if (CanShowThought()) ShowThought(ThoughtTrigger.Eating);
    }

    // Called when the eat timer finishes
    void FinishEatingAction()
    {
        if (currentTargetFood == null) return; // Target disappeared mid-eat

        FoodItem foodItem = currentTargetFood.GetComponent<FoodItem>();
        if (foodItem != null && foodItem.foodType != null)
        {
            // 1. Get Satiation
            float satiationGain = animalDiet.GetSatiationValue(foodItem.foodType);

            // 2. Apply Satiation
            currentHunger -= satiationGain;
            currentHunger = Mathf.Max(0f, currentHunger);

            // 3. Destroy the Food GameObject *** THIS IS THE KEY CHANGE ***
            Destroy(currentTargetFood);

            // 4. Reset Poop Timer
            hasPooped = false;
            poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);

            // 5. Clear Target Reference
            currentTargetFood = null;
        }
         else {
             // Target was invalid (missing FoodItem/FoodType), clear it
             Debug.LogWarning($"[{gameObject.name}] Tried to finish eating invalid target '{currentTargetFood?.name}'. Clearing target.", currentTargetFood);
             currentTargetFood = null;
         }
    }

    void StartPooping()
    {
        isPooping = true;
        poopTimer = poopDuration;
        moveDirection = Vector2.zero;
        if (CanShowThought()) ShowThought(ThoughtTrigger.Pooping);
    }

    void FinishPooping()
    {
        SpawnPoop();
        isPooping = false;
        hasPooped = true;
    }

    void SpawnPoop()
    {
        if (poopPrefabs == null || poopPrefabs.Count == 0) return;

        int index = Random.Range(0, poopPrefabs.Count);
        GameObject prefab = poopPrefabs[index];
        if (prefab == null) return;

        Transform spawnT = poopSpawnPoint ? poopSpawnPoint : transform;
        GameObject poopObj = Instantiate(prefab, spawnT.position, Quaternion.identity);

        // Apply visual variations
        SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.flipX = Random.value > 0.5f;
            Color c = sr.color;
            float v = poopColorVariation;
            sr.color = new Color(
                Mathf.Clamp01(c.r + Random.Range(-v, v)),
                Mathf.Clamp01(c.g + Random.Range(-v, v)),
                Mathf.Clamp01(c.b + Random.Range(-v, v)),
                c.a);
        }

        // Ensure PoopController exists and initialize
        PoopController pc = poopObj.GetComponent<PoopController>() ?? poopObj.AddComponent<PoopController>();
        pc.Initialize();
    }

    void Wander()
    {
        if (wanderStateTimer <= 0f) {
            if (isWanderPaused) {
                // Finish pause, start moving
                isWanderPaused = false;
                moveDirection = Random.insideUnitCircle.normalized;
                wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration);
            } else {
                // Finish moving, decide to pause or change direction
                if (Random.value < wanderPauseChance) {
                    isWanderPaused = true;
                    moveDirection = Vector2.zero;
                    wanderStateTimer = Random.Range(wanderMinPauseDuration, wanderMaxPauseDuration);
                } else {
                    moveDirection = Random.insideUnitCircle.normalized;
                    wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration);
                }
            }
        } else {
            wanderStateTimer -= Time.deltaTime;
        }
    }

    void FlipSpriteBasedOnDirection()
    {
        if (spriteRenderer != null && Mathf.Abs(moveDirection.x) > 0.01f) {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    void UpdateAnimationState()
    {
        if (animator == null) return;
        bool isMoving = !isEating && !isPooping && moveDirection.sqrMagnitude > 0.01f;
        // Use parameter names matching your Animator controller
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsEating", isEating);
        // animator.SetBool("IsPooping", isPooping); // Add if needed
    }

    bool CanShowThought() {
        return thoughtLibrary != null && thoughtBubblePrefab != null && thoughtCooldownTimer <= 0f;
    }

    void ShowThought(ThoughtTrigger trigger)
    {
        // Simplified - assumes CanShowThought() was checked
        var entry = thoughtLibrary.allThoughts.FirstOrDefault(t => t.speciesName == SpeciesName && t.trigger == trigger);
        if (entry != null && entry.lines != null && entry.lines.Count > 0) {
            string line = entry.lines[Random.Range(0, entry.lines.Count)];
            Transform spawnT = bubbleSpawnTransform ? bubbleSpawnTransform : transform;
            GameObject bubbleGO = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity, spawnT);
            bubbleGO.transform.localPosition = Vector3.zero;
            ThoughtBubbleController bubble = bubbleGO.GetComponent<ThoughtBubbleController>();
            if (bubble) {
                bubble.Initialize(line, spawnT, 2f); // 2s default life
                thoughtCooldownTimer = thoughtCooldownTime;
            } else {
                Destroy(bubbleGO);
            }
        }
    }

     // Removed Die() and starvation logic for simplicity

     public bool SpeciesNameEquals(string otherSpeciesName) {
         return definition != null && definition.animalName == otherSpeciesName;
     }
}
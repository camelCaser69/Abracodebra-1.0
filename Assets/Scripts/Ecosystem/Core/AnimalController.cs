using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SortableEntity))]
[RequireComponent(typeof(Collider2D))]
public class AnimalController : MonoBehaviour
{
    // --- Fields ---
    private AnimalDefinition definition;
    private AnimalDiet animalDiet;
    
    [Header("Optional Features")]
    public AnimalThoughtLibrary thoughtLibrary;
    public GameObject thoughtBubblePrefab;
    public Transform bubbleSpawnTransform;
    public Transform poopSpawnPoint;
    public List<GameObject> poopPrefabs;
    public Animator animator;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI hungerText; // NEW: Hunger text reference

    [Header("UI Settings")] // NEW HEADER
    [Tooltip("Which key to hold to display HP and Hunger texts")]
    [SerializeField] private KeyCode showStatsKey = KeyCode.LeftAlt; // NEW: Configurable key for showing stats
    
    [Header("Behavior Tuning")]
    public float searchRadius = 5f;
    public float eatDistance = 0.5f;
    public float eatDuration = 1.5f;
    [Range(0f, 1f)]
    public float wanderPauseChance = 0.3f;
    public float wanderMinMoveDuration = 1f;
    public float wanderMaxMoveDuration = 3f;
    public float wanderMinPauseDuration = 0.5f;
    public float wanderMaxPauseDuration = 2f;
    public float minPoopDelay = 5f;
    public float maxPoopDelay = 10f;
    public float poopDuration = 1f;
    public float poopColorVariation = 0.1f;
    public float thoughtCooldownTime = 5f;
    [SerializeField] private List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    [SerializeField] private List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();

    // --- NEW: Food reassessment timer fields ---
    [Header("Food Seeking Improvements")]
    [Tooltip("How often (in seconds) to reassess nearby food even when current target is valid")]
    public float foodReassessmentInterval = 0.5f;
    private float foodReassessmentTimer = 0f;

    // --- NEW: Starvation damage settings ---
    [Header("Starvation Settings")]
    [Tooltip("Damage per second when hunger is at maximum")]
    public float starvationDamageRate = 2f;

    // --- Internal State ---
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
    private bool isSeekingScreenCenter = false;
    private Vector2 screenCenterTarget;

    // --- Component References ---
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D animalCollider;

    // --- Movement Bounds (Shifted Padded Screen Bounds) ---
    private Vector2 minBounds;
    private Vector2 maxBounds;

    // --- Public Accessors ---
    public float CurrentHealth => currentHealth;
    public string SpeciesName => definition ? definition.animalName : "Uninitialized";

    // --- NEW: Fields for slowdown system ---
    private float baseMovementSpeed;
    private List<float> activeSpeedMultipliers = new List<float>();

    // --- Initialize ---
    public void Initialize(AnimalDefinition def, Vector2 shiftedMinBounds, Vector2 shiftedMaxBounds, bool spawnedOffscreen = false)
    {
        definition = def; 
        if (definition == null) { Destroy(gameObject); return; }
        
        animalDiet = def.diet; 
        if (animalDiet == null) { enabled = false; return; }
        
        rb = GetComponent<Rigidbody2D>(); 
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); 
        animalCollider = GetComponent<Collider2D>();
        
        if (animalCollider == null) 
        { 
            Debug.LogError($"[{gameObject.name}] Missing Collider2D!", gameObject); 
            enabled = false; 
            return; 
        }

        // Store base movement speed - NEW
        baseMovementSpeed = definition.movementSpeed;
        activeSpeedMultipliers.Clear(); // NEW

        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        hasPooped = true;
        poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
        foodReassessmentTimer = Random.Range(0f, foodReassessmentInterval); // Randomize initial timer

        // Store the SHIFTED bounds received from FaunaManager
        minBounds = shiftedMinBounds;
        maxBounds = shiftedMaxBounds;
        
        // Calculate the target center based on the SHIFTED bounds
        screenCenterTarget = (minBounds + maxBounds) / 2f;

        isSeekingScreenCenter = spawnedOffscreen;
        if (isSeekingScreenCenter)
        {
            if(Debug.isDebugBuild) 
                Debug.Log($"[{gameObject.name} Initialize] Offscreen spawn. Seeking SHIFTED center ({screenCenterTarget}). SHIFTED Bounds: min{minBounds}, max{maxBounds}", gameObject);
            
            moveDirection = (screenCenterTarget - (Vector2)transform.position).normalized;
            if (moveDirection == Vector2.zero) 
                moveDirection = Random.insideUnitCircle.normalized;
        }
    
        EnsureUITextReferences();
    
        // Hide text elements initially
        SetStatsTextVisibility(false);
    
        UpdateHpText(); 
        UpdateHungerText();
    
        if (spriteRenderer == null) { /* Warning */ }
    }
    
    private void EnsureUITextReferences() 
    { 
        if (hpText == null) 
        {
            hpText = GetComponentInChildren<TextMeshProUGUI>(true);
            // If we found a TMP_Text but it should be for HP, don't assign it to both
            if (hpText != null && hpText.gameObject.name.Contains("Hunger"))
            {
                hungerText = hpText;
                hpText = null;
            }
        }
    
        if (hungerText == null)
        {
            // Try to find any TextMeshProUGUI component that's not the HP text
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text != hpText)
                {
                    hungerText = text;
                    break;
                }
            }
        }
    }

    // --- Update ---
    void Update()
    {
        if (!enabled || rb == null) return;

        // Check for ALT key (or configured key) press to show/hide stats
        bool showStats = Input.GetKey(showStatsKey);
        SetStatsTextVisibility(showStats);

        if (isSeekingScreenCenter)
        {
            Vector2 currentPos = rb.position;
            // Check if center is within the SHIFTED padded bounds
            bool centerWithinBounds =
                currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
                currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;

            if (centerWithinBounds)
            {
                if(Debug.isDebugBuild) 
                    Debug.Log($"[{gameObject.name} Update] Center reached SHIFTED bounds! Pos: {currentPos}. MinB: {minBounds}, MaxB: {maxBounds}. Switching to normal AI.", gameObject);
                
                isSeekingScreenCenter = false;
                moveDirection = Vector2.zero;
            }
            else
            {
                // Seek the SHIFTED center target
                moveDirection = (screenCenterTarget - currentPos).normalized;
                if (moveDirection == Vector2.zero) 
                    moveDirection = Random.insideUnitCircle.normalized;
                
                FlipSpriteBasedOnDirection();
                UpdateAnimationState();
                return; // Skip normal AI
            }
        }

        // Normal AI Logic
        UpdateHunger();
        HandlePooping();
        UpdateThoughts();
        
        if (isEating) 
        { 
            HandleEating(); 
            moveDirection = Vector2.zero; 
        }
        else if (isPooping) 
        { 
            moveDirection = Vector2.zero; 
        }
        else 
        { 
            DecideNextAction(); 
        }

        FlipSpriteBasedOnDirection();
        UpdateAnimationState();
    }
    
    private void SetStatsTextVisibility(bool visible)
    {
        if (hpText != null)
        {
            hpText.gameObject.SetActive(visible);
        }
    
        if (hungerText != null)
        {
            hungerText.gameObject.SetActive(visible);
        }
    }

    // --- FixedUpdate ---
    void FixedUpdate()
    {
        if (rb == null) return;
        
        if (!isEating && !isPooping && moveDirection != Vector2.zero)
        {
            Vector2 currentPos = rb.position;
            Vector2 desiredMove = moveDirection.normalized * definition.movementSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = currentPos + desiredMove;

            if (!isSeekingScreenCenter) // Clamp only when NOT seeking
            {
                // Clamp the CENTER position using the SHIFTED bounds
                nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
                nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y, maxBounds.y);
            }
            rb.MovePosition(nextPos);
        }
    }

    void UpdateHunger() 
    { 
        currentHunger += animalDiet.hungerIncreaseRate * Time.deltaTime; 
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger);
        
        // Check for starvation damage when hunger reaches maximum
        if (currentHunger >= animalDiet.maxHunger)
        {
            ApplyStarvationDamage();
        }
        
        // Update hunger text when hunger changes
        UpdateHungerText();
    }
    
    void ApplyStarvationDamage() 
    { 
        // Apply damage over time when starving
        float damage = starvationDamageRate * Time.deltaTime;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        UpdateHpText();
        
        // Die if health reaches zero
        if (currentHealth <= 0f)
        {
            Die(CauseOfDeath.Starvation);
        }
    }
    
    void HandlePooping() 
    { 
        if (!isEating && !hasPooped) 
        { 
            poopDelayTimer -= Time.deltaTime; 
            if (!isPooping && poopDelayTimer <= 0f) 
            { 
                StartPooping(); 
            } 
            if (isPooping) 
            { 
                poopTimer -= Time.deltaTime; 
                if (poopTimer <= 0f) 
                { 
                    FinishPooping(); 
                } 
            } 
        } 
    }
    
    void UpdateThoughts() 
    { 
        if (thoughtCooldownTimer > 0) 
        { 
            thoughtCooldownTimer -= Time.deltaTime; 
        } 
    }
    
    void DecideNextAction() 
    { 
        if (currentHunger >= animalDiet.hungerThreshold) 
        { 
            SeekFood(); 
        } 
        else 
        { 
            Wander(); 
            currentTargetFood = null; 
        } 
    }
    
    // --- Food seeking with periodic reassessment ---
    void SeekFood() 
    { 
        if (CanShowThought()) ShowThought(ThoughtTrigger.Hungry);
        
        // Reassess food periodically even if current target is valid
        foodReassessmentTimer -= Time.deltaTime;
        bool shouldReassess = foodReassessmentTimer <= 0f;
        bool targetValid = currentTargetFood != null && currentTargetFood.activeInHierarchy && 
                          currentTargetFood.GetComponent<FoodItem>() != null;
        
        // Reassess when timer expires or if target is invalid
        if (shouldReassess || !targetValid) 
        {
            // Store position of old target for comparison
            Vector3 oldTargetPosition = targetValid ? currentTargetFood.transform.position : Vector3.zero;
            
            // Find potentially better food
            GameObject potentialBetterFood = FindNearestFood();
            
            if (potentialBetterFood != null) 
            {
                // Always switch to new target if no current target
                if (!targetValid) 
                {
                    currentTargetFood = potentialBetterFood;
                    if(Debug.isDebugBuild) 
                        Debug.Log($"[{gameObject.name} SeekFood] Found new food target (no previous): {potentialBetterFood.name}");
                }
                // Switch to new target if it's better than current
                else if (potentialBetterFood != currentTargetFood) 
                {
                    // Get preferences for comparison
                    FoodItem currentFoodItem = currentTargetFood.GetComponent<FoodItem>();
                    FoodItem newFoodItem = potentialBetterFood.GetComponent<FoodItem>();
                    
                    if (currentFoodItem != null && newFoodItem != null) 
                    {
                        float currentPriority = animalDiet.GetPreference(currentFoodItem.foodType)?.preferencePriority ?? 0f;
                        float newPriority = animalDiet.GetPreference(newFoodItem.foodType)?.preferencePriority ?? 0f;
                        
                        // Switch if new food has higher priority
                        if (newPriority > currentPriority) 
                        {
                            if(Debug.isDebugBuild) 
                                Debug.Log($"[{gameObject.name} SeekFood] Switching to higher priority food: {newFoodItem.foodType.foodName} (priority: {newPriority}) from {currentFoodItem.foodType.foodName} (priority: {currentPriority})");
                            
                            currentTargetFood = potentialBetterFood;
                        }
                    }
                }
            }
            
            // Reset timer
            foodReassessmentTimer = foodReassessmentInterval;
        }
        
        // Proceed with target as before
        if (currentTargetFood != null) 
        {
            MoveTowardFood(currentTargetFood);
        } 
        else 
        {
            Wander();
        }
    }

    GameObject FindNearestFood() 
    { 
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius); 
        return animalDiet.FindBestFood(colliders, transform.position); 
    }
    
    void MoveTowardFood(GameObject foodObj) 
    { 
        if (foodObj == null) return; 
        
        float distance = Vector2.Distance(transform.position, foodObj.transform.position); 
        if (distance <= eatDistance) 
        { 
            StartEating(); 
        } 
        else 
        { 
            moveDirection = (foodObj.transform.position - transform.position).normalized; 
            isWanderPaused = false; 
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
    
    void HandleEating() 
    { 
        eatTimer -= Time.deltaTime; 
        if (eatTimer <= 0f) 
        { 
            isEating = false; 
            FinishEatingAction(); 
        } 
    }
    
    void FinishEatingAction() 
    { 
        if (currentTargetFood == null) return; 
        
        FoodItem foodItem = currentTargetFood.GetComponent<FoodItem>(); 
        if (foodItem != null && foodItem.foodType != null) 
        { 
            float satiationGain = animalDiet.GetSatiationValue(foodItem.foodType); 
            currentHunger -= satiationGain; 
            currentHunger = Mathf.Max(0f, currentHunger); 
            Destroy(currentTargetFood); 
            hasPooped = false; 
            poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay); 
            currentTargetFood = null;
            // Update hunger text
            UpdateHungerText();
        } 
        else 
        { 
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
        
        SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>(); 
        if (sr != null) 
        { 
            sr.flipX = Random.value > 0.5f; 
            Color c = sr.color; 
            float v = poopColorVariation; 
            sr.color = new Color(
                Mathf.Clamp01(c.r + Random.Range(-v, v)),
                Mathf.Clamp01(c.g + Random.Range(-v, v)),
                Mathf.Clamp01(c.b + Random.Range(-v, v)),
                c.a
            ); 
        } 
        
        PoopController pc = poopObj.GetComponent<PoopController>() ?? poopObj.AddComponent<PoopController>(); 
        pc.Initialize(); 
    }
    
    void Wander() 
    { 
        if (wanderStateTimer <= 0f) 
        { 
            if (isWanderPaused) 
            { 
                isWanderPaused = false; 
                moveDirection = Random.insideUnitCircle.normalized; 
                wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration); 
            } 
            else 
            { 
                if (Random.value < wanderPauseChance) 
                { 
                    isWanderPaused = true; 
                    moveDirection = Vector2.zero; 
                    wanderStateTimer = Random.Range(wanderMinPauseDuration, wanderMaxPauseDuration); 
                } 
                else 
                { 
                    moveDirection = Random.insideUnitCircle.normalized; 
                    wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration); 
                } 
            } 
        } 
        else 
        { 
            wanderStateTimer -= Time.deltaTime; 
        } 
    }
    
    void FlipSpriteBasedOnDirection() 
    { 
        if (spriteRenderer != null && Mathf.Abs(moveDirection.x) > 0.01f) 
        { 
            spriteRenderer.flipX = moveDirection.x < 0; 
        } 
    }
    
    void UpdateAnimationState() 
    { 
        if (animator == null) return; 
        bool isMoving = !isEating && !isPooping && moveDirection.sqrMagnitude > 0.01f; 
        animator.SetBool("IsMoving", isMoving); 
        animator.SetBool("IsEating", isEating); 
    }
    
    bool CanShowThought() 
    { 
        return thoughtLibrary != null && thoughtBubblePrefab != null && thoughtCooldownTimer <= 0f; 
    }
    
    void ShowThought(ThoughtTrigger trigger) 
    { 
        if (thoughtLibrary == null || thoughtLibrary.allThoughts == null) return; 
        
        var entry = thoughtLibrary.allThoughts.FirstOrDefault(t => 
            t != null && t.speciesName == SpeciesName && t.trigger == trigger
        ); 
        
        if (entry != null && entry.lines != null && entry.lines.Count > 0) 
        { 
            string line = entry.lines[Random.Range(0, entry.lines.Count)]; 
            Transform spawnT = bubbleSpawnTransform ? bubbleSpawnTransform : transform; 
            GameObject bubbleGO = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity, spawnT); 
            bubbleGO.transform.localPosition = Vector3.zero; 
            
            ThoughtBubbleController bubble = bubbleGO.GetComponent<ThoughtBubbleController>(); 
            if (bubble) 
            { 
                bubble.Initialize(line, spawnT, 2f); 
                thoughtCooldownTimer = thoughtCooldownTime; 
            } 
            else 
            { 
                Destroy(bubbleGO); 
            } 
        } 
    }
    
    public enum CauseOfDeath { Unknown, Starvation, EatenByPredator }
    
    private void Die(CauseOfDeath cause) 
    { 
        Debug.Log($"[{SpeciesName} died: {cause}]", gameObject); 
        Destroy(gameObject); 
    }
    
    public bool SpeciesNameEquals(string other) 
    { 
        return definition != null && definition.animalName == other; 
    }
    
    private void UpdateHpText() 
    { 
        if (hpText == null || definition == null) return; 
        hpText.text = $"HP: {Mathf.FloorToInt(currentHealth)}/{Mathf.FloorToInt(definition.maxHealth)}"; 
    }
    
    private void UpdateHungerText()
    {
        if (hungerText == null || animalDiet == null) return;
        hungerText.text = $"Hunger: {Mathf.FloorToInt(currentHunger)}/{Mathf.FloorToInt(animalDiet.maxHunger)}";
    }

    // --- NEW METHODS FOR SLOWDOWN SYSTEM ---
    
    // Method to apply speed multiplier from SlowdownZone
    public void ApplySpeedMultiplier(float multiplier)
    {
        if (!activeSpeedMultipliers.Contains(multiplier))
        {
            activeSpeedMultipliers.Add(multiplier);
            UpdateMovementSpeed();
            
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{gameObject.name}] Applied speed multiplier: {multiplier}. New speed: {definition.movementSpeed}");
            }
        }
    }

    // Method to remove speed multiplier when leaving SlowdownZone
    public void RemoveSpeedMultiplier(float multiplier)
    {
        if (activeSpeedMultipliers.Contains(multiplier))
        {
            activeSpeedMultipliers.Remove(multiplier);
            UpdateMovementSpeed();
            
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{gameObject.name}] Removed speed multiplier: {multiplier}. New speed: {definition.movementSpeed}");
            }
        }
    }

    // Method to recalculate movement speed based on active multipliers
    private void UpdateMovementSpeed()
    {
        // Start with base speed (original speed from definition)
        float newSpeed = baseMovementSpeed;
        
        // Apply all active multipliers
        if (activeSpeedMultipliers.Count > 0)
        {
            // Use the most restrictive (lowest) multiplier
            float lowestMultiplier = 1.0f;
            foreach (float multiplier in activeSpeedMultipliers)
            {
                if (multiplier < lowestMultiplier)
                {
                    lowestMultiplier = multiplier;
                }
            }
            
            newSpeed *= lowestMultiplier;
        }
        
        // Update the definition's movement speed (which is used in FixedUpdate)
        definition.movementSpeed = newSpeed;
        
        // Optionally update animation speed to match movement
        if (animator != null)
        {
            float speedRatio = newSpeed / baseMovementSpeed;
            animator.speed = Mathf.Max(0.5f, speedRatio); // Don't go below half speed
        }
    }
}

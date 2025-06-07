// FILE: Assets/Scripts/Ecosystem/Animals/AnimalController.cs

using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// CHANGED: Inherits from SpeedModifiable now
public class AnimalController : SpeedModifiable
{
    // --- Member variables from original script ---
    AnimalDefinition definition;
    AnimalDiet animalDiet;

    public AnimalThoughtLibrary thoughtLibrary;
    public GameObject thoughtBubblePrefab;
    public Transform bubbleSpawnTransform;
    public Transform poopSpawnPoint;
    public List<GameObject> poopPrefabs;
    public Animator animator;

    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;

    [Header("UI Settings")]
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;

    public float searchRadius = 5f;
    public float eatDistance = 0.5f;
    public float eatDuration = 1.5f;
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
    [SerializeField] List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    [SerializeField] List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();

    public float foodReassessmentInterval = 0.5f;
    float foodReassessmentTimer = 0f;

    public float starvationDamageRate = 2f;

    public float damageFlashDuration = 0.2f;
    public float deathFadeDuration = 1.5f;
    public Color damageFlashColor = Color.red;

    float currentHealth;
    float currentHunger;
    GameObject currentTargetFood = null;
    Vector2 moveDirection = Vector2.zero;
    bool isEating = false;
    float eatTimer = 0f;
    bool isWanderPaused = false;
    float wanderStateTimer = 0f;
    bool isPooping = false;
    float poopTimer = 0f;
    float poopDelayTimer = 0f;
    bool hasPooped = true;
    float thoughtCooldownTimer = 0f;
    bool isSeekingScreenCenter = false;
    Vector2 screenCenterTarget;

    bool isDying = false;
    Color originalColor;
    bool isFlashing = false;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Collider2D animalCollider;

    Vector2 minBounds;
    Vector2 maxBounds;

    public float CurrentHealth => currentHealth;
    public string SpeciesName => definition ? definition.animalName : "Uninitialized";

    // REMOVED: baseMovementSpeed and activeSpeedMultipliers are now handled by SpeedModifiable base class.

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

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // CHANGED: Set baseSpeed and currentSpeed from the new base class
        baseSpeed = definition.movementSpeed;
        currentSpeed = baseSpeed;

        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        hasPooped = true;
        poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
        foodReassessmentTimer = Random.Range(0f, foodReassessmentInterval);

        minBounds = shiftedMinBounds;
        maxBounds = shiftedMaxBounds;

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
        SetStatsTextVisibility(false);
        UpdateHpText();
        UpdateHungerText();
    }

    void EnsureUITextReferences()
    {
        if (hpText == null)
        {
            hpText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (hpText != null && hpText.gameObject.name.Contains("Hunger"))
            {
                hungerText = hpText;
                hpText = null;
            }
        }

        if (hungerText == null)
        {
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

    void Update()
    {
        if (!enabled || rb == null || isDying) return;

        bool showStats = Input.GetKey(showStatsKey);
        SetStatsTextVisibility(showStats);

        if (isSeekingScreenCenter)
        {
            Vector2 currentPos = rb.position;
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
                moveDirection = (screenCenterTarget - currentPos).normalized;
                if (moveDirection == Vector2.zero)
                    moveDirection = Random.insideUnitCircle.normalized;

                FlipSpriteBasedOnDirection();
                UpdateAnimationState();
                return; // Skip normal AI
            }
        }

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

    void SetStatsTextVisibility(bool visible)
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

    void FixedUpdate()
    {
        if (rb == null || isDying) return;

        if (!isEating && !isPooping && moveDirection != Vector2.zero)
        {
            Vector2 currentPos = rb.position;
            // CHANGED: Use `currentSpeed` from SpeedModifiable base class
            Vector2 desiredMove = moveDirection.normalized * currentSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = currentPos + desiredMove;

            if (!isSeekingScreenCenter) // Clamp only when NOT seeking
            {
                nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
                nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y, maxBounds.y);
            }
            rb.MovePosition(nextPos);
        }
    }
    
    // --- The rest of the methods are unchanged, except for removing speed multiplier logic ---

    void UpdateHunger()
    {
        currentHunger += animalDiet.hungerIncreaseRate * Time.deltaTime;
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger);

        if (currentHunger >= animalDiet.maxHunger)
        {
            ApplyStarvationDamage();
        }

        UpdateHungerText();
    }

    void ApplyStarvationDamage()
    {
        float damage = starvationDamageRate * Time.deltaTime;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        if (!isFlashing)
        {
            StartCoroutine(FlashDamage());
        }

        UpdateHpText();

        if (currentHealth <= 0f)
        {
            Die(CauseOfDeath.Starvation);
        }
    }

    IEnumerator FlashDamage()
    {
        if (spriteRenderer == null) yield break;

        isFlashing = true;
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    IEnumerator FadeOutAndDestroy()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        isDying = true;
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsedTime < deathFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / deathFadeDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
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

    void SeekFood()
    {
        if (CanShowThought()) ShowThought(ThoughtTrigger.Hungry);

        foodReassessmentTimer -= Time.deltaTime;
        bool shouldReassess = foodReassessmentTimer <= 0f;
        bool targetValid = currentTargetFood != null && currentTargetFood.activeInHierarchy &&
                           currentTargetFood.GetComponent<FoodItem>() != null;

        if (shouldReassess || !targetValid)
        {
            Vector3 oldTargetPosition = targetValid ? currentTargetFood.transform.position : Vector3.zero;

            GameObject potentialBetterFood = FindNearestFood();

            if (potentialBetterFood != null)
            {
                if (!targetValid)
                {
                    currentTargetFood = potentialBetterFood;
                    if(Debug.isDebugBuild)
                        Debug.Log($"[{gameObject.name} SeekFood] Found new food target (no previous): {potentialBetterFood.name}");
                }
                else if (potentialBetterFood != currentTargetFood)
                {
                    FoodItem currentFoodItem = currentTargetFood.GetComponent<FoodItem>();
                    FoodItem newFoodItem = potentialBetterFood.GetComponent<FoodItem>();

                    if (currentFoodItem != null && newFoodItem != null)
                    {
                        float currentPriority = animalDiet.GetPreference(currentFoodItem.foodType)?.preferencePriority ?? 0f;
                        float newPriority = animalDiet.GetPreference(newFoodItem.foodType)?.preferencePriority ?? 0f;

                        if (newPriority > currentPriority)
                        {
                            if(Debug.isDebugBuild)
                                Debug.Log($"[{gameObject.name} SeekFood] Switching to higher priority food: {newFoodItem.foodType.foodName} (priority: {newPriority}) from {currentFoodItem.foodType.foodName} (priority: {currentPriority})");
                            
                            currentTargetFood = potentialBetterFood;
                        }
                    }
                }
            }

            foodReassessmentTimer = foodReassessmentInterval;
        }

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

    void Die(CauseOfDeath cause)
    {
        Debug.Log($"[{SpeciesName} died: {cause}]", gameObject);
        StartCoroutine(FadeOutAndDestroy());
    }

    public bool SpeciesNameEquals(string other)
    {
        return definition != null && definition.animalName == other;
    }

    void UpdateHpText()
    {
        if (hpText == null || definition == null) return;
        hpText.text = $"HP: {Mathf.FloorToInt(currentHealth)}/{Mathf.FloorToInt(definition.maxHealth)}";
    }

    void UpdateHungerText()
    {
        if (hungerText == null || animalDiet == null) return;
        hungerText.text = $"Hunger: {Mathf.FloorToInt(currentHunger)}/{Mathf.FloorToInt(animalDiet.maxHunger)}";
    }
    
    // REMOVED: ApplySpeedMultiplier, RemoveSpeedMultiplier, and UpdateMovementSpeed are now in the base class.

    // ADDED: Override OnSpeedChanged to handle side-effects like animation speed.
    protected override void OnSpeedChanged(float newSpeed)
    {
        if (animator != null)
        {
            // baseSpeed is inherited from SpeedModifiable and set in Initialize
            float speedRatio = (baseSpeed > 0f) ? newSpeed / baseSpeed : 1f;
            animator.speed = Mathf.Max(0.5f, speedRatio); // Don't go below half speed
        }
    }
}
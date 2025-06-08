using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WegoSystem;
using System.Linq;

public class AnimalController : SpeedModifiable, ITickUpdateable {
    [SerializeField] bool useWegoMovement = true;
    [SerializeField] int thinkingTickInterval = 3; // How often to make decisions

    AnimalDefinition definition;
    AnimalDiet animalDiet;
    GridEntity gridEntity;

    public AnimalThoughtLibrary thoughtLibrary;
    public GameObject thoughtBubblePrefab;
    public Transform bubbleSpawnTransform;
    public Transform poopSpawnPoint;
    public List<GameObject> poopPrefabs;
    public Animator animator;

    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;

    GridPosition targetPosition;
    GameObject currentTargetFood = null;
    bool hasPlannedAction = false;
    int lastThinkTick = 0;

    int hungerTick = 0;
    int poopTick = 0;
    int thoughtCooldownTick = 0;

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
    public float foodReassessmentInterval = 0.5f;
    public float starvationDamageRate = 2f;
    public float damageFlashDuration = 0.2f;
    public float deathFadeDuration = 1.5f;
    public Color damageFlashColor = Color.red;

    [SerializeField] List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    [SerializeField] List<ScentDefinition> repellentScentDefinitions = new List<ScentDefinition>();

    float currentHealth;
    float currentHunger;
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
    float foodReassessmentTimer = 0f;

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

    public void Initialize(AnimalDefinition def, Vector2 shiftedMinBounds, Vector2 shiftedMaxBounds, bool spawnedOffscreen = false) {
        definition = def;
        if (definition == null) { Destroy(gameObject); return; }

        animalDiet = def.diet;
        if (animalDiet == null) { enabled = false; return; }

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animalCollider = GetComponent<Collider2D>();
        gridEntity = GetComponent<GridEntity>();

        if (animalCollider == null) {
            Debug.LogError($"[{gameObject.name}] Missing Collider2D!", gameObject);
            enabled = false;
            return;
        }

        if (useWegoMovement && gridEntity == null) {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }

        if (spriteRenderer != null) {
            originalColor = spriteRenderer.color;
        }

        baseSpeed = definition.movementSpeed;
        currentSpeed = baseSpeed;

        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        hasPooped = true;

        if (TickManager.Instance?.Config != null) {
            var config = TickManager.Instance.Config;
            poopTick = Random.Range(
                config.ConvertSecondsToTicks(minPoopDelay),
                config.ConvertSecondsToTicks(maxPoopDelay)
            );
            thoughtCooldownTick = config.ConvertSecondsToTicks(thoughtCooldownTime);
        }

        poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
        foodReassessmentTimer = Random.Range(0f, foodReassessmentInterval);

        minBounds = shiftedMinBounds;
        maxBounds = shiftedMaxBounds;

        screenCenterTarget = (minBounds + maxBounds) / 2f;

        isSeekingScreenCenter = spawnedOffscreen;
        if (isSeekingScreenCenter) {
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

        if (useWegoMovement && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoMovement || isDying) return;

        hungerTick++;
        if (poopTick > 0) poopTick--;
        if (thoughtCooldownTick > 0) thoughtCooldownTick--;

        if (TickManager.Instance?.Config != null) {
            var config = TickManager.Instance.Config;
            if (hungerTick >= config.animalHungerTickInterval) {
                UpdateHungerTick();
                hungerTick = 0;
            }
        }

        if (currentTick - lastThinkTick >= thinkingTickInterval) {
            MakeDecision();
            lastThinkTick = currentTick;
        }

        if (hasPlannedAction) {
            ExecutePlannedAction();
        }

        if (!hasPooped && poopTick <= 0 && !isEating) {
            TryPoop();
        }
    }

    void MakeDecision() {
        if (isSeekingScreenCenter) {
            HandleScreenCenterSeeking();
            return;
        }

        if (currentHunger >= animalDiet.hungerThreshold) {
            PlanFoodSeeking();
        } else {
            PlanWandering();
        }
    }

    void HandleScreenCenterSeeking() {
        if (gridEntity == null) return;

        Vector2 currentPos = (Vector2)transform.position;
        bool centerWithinBounds =
            currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
            currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;

        if (centerWithinBounds) {
            if(Debug.isDebugBuild)
                Debug.Log($"[{gameObject.name}] Center reached! Switching to normal AI.", gameObject);
            isSeekingScreenCenter = false;
            hasPlannedAction = false;
        } else {
            GridPosition currentGridPos = gridEntity.Position;
            GridPosition targetGridPos = GridPositionManager.Instance.WorldToGrid(screenCenterTarget);
            PlanMovementToward(targetGridPos);
        }
    }

    void PlanFoodSeeking() {
        if (CanShowThought()) ShowThought(ThoughtTrigger.Hungry);

        GameObject nearestFood = FindNearestFoodInGrid();
        if (nearestFood != null) {
            currentTargetFood = nearestFood;
            GridPosition foodGridPos = GridPositionManager.Instance.WorldToGrid(nearestFood.transform.position);

            GridPosition myPos = gridEntity.Position;
            int distance = myPos.ManhattanDistance(foodGridPos);

            if (distance <= 1) {
                hasPlannedAction = true;
                targetPosition = myPos; // Stay in place to eat
            } else {
                PlanMovementToward(foodGridPos);
            }
        } else {
            PlanWandering();
        }
    }

    void PlanWandering() {
        if (gridEntity == null) return;

        GridPosition currentPos = gridEntity.Position;
        GridPosition[] directions = {
            GridPosition.Up, GridPosition.Down,
            GridPosition.Left, GridPosition.Right
        };

        for (int i = 0; i < 3; i++) {
            GridPosition randomDir = directions[Random.Range(0, directions.Length)];
            GridPosition targetPos = currentPos + randomDir;

            if (GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsPositionOccupied(targetPos)) {
                targetPosition = targetPos;
                hasPlannedAction = true;
                break;
            }
        }

        if (!hasPlannedAction) {
            targetPosition = currentPos;
            hasPlannedAction = true;
        }
    }

    void PlanMovementToward(GridPosition target) {
        if (gridEntity == null) return;

        GridPosition currentPos = gridEntity.Position;
        GridPosition direction = new GridPosition(
            Mathf.Clamp(target.x - currentPos.x, -1, 1),
            Mathf.Clamp(target.y - currentPos.y, -1, 1)
        );

        GridPosition targetPos = currentPos + direction;

        if (GridPositionManager.Instance.IsPositionValid(targetPos) &&
            !GridPositionManager.Instance.IsPositionOccupied(targetPos)) {
            targetPosition = targetPos;
            hasPlannedAction = true;
        } else {
            GridPosition[] alternatives = {
                currentPos + new GridPosition(direction.x, 0),
                currentPos + new GridPosition(0, direction.y),
                currentPos + GridPosition.Up,
                currentPos + GridPosition.Down,
                currentPos + GridPosition.Left,
                currentPos + GridPosition.Right
            };

            foreach (var alt in alternatives) {
                if (GridPositionManager.Instance.IsPositionValid(alt) &&
                    !GridPositionManager.Instance.IsPositionOccupied(alt)) {
                    targetPosition = alt;
                    hasPlannedAction = true;
                    break;
                }
            }

            if (!hasPlannedAction) {
                targetPosition = currentPos; // Stay in place
                hasPlannedAction = true;
            }
        }
    }

    void ExecutePlannedAction() {
        if (gridEntity == null) return;

        GridPosition currentPos = gridEntity.Position;

        if (currentTargetFood != null && targetPosition == currentPos) {
            Vector3 foodWorldPos = currentTargetFood.transform.position;
            Vector3 myWorldPos = transform.position;

            if (Vector3.Distance(myWorldPos, foodWorldPos) <= 2f) { // Grid-adjusted eat distance
                StartEating();
                return;
            }
        }

        if (targetPosition != currentPos) {
            gridEntity.SetPosition(targetPosition);
        }

        hasPlannedAction = false;
    }

    GameObject FindNearestFoodInGrid() {
        if (animalDiet == null) return null;

        int gridRadius = Mathf.RoundToInt(searchRadius);
        List<GridEntity> nearbyEntities = GridPositionManager.Instance.GetEntitiesInRadius(
            gridEntity.Position, gridRadius, true);

        GameObject bestFood = null;
        float bestScore = -1f;

        foreach (var entity in nearbyEntities) {
            if (entity == null || entity.gameObject == this.gameObject) continue;

            FoodItem foodItem = entity.GetComponent<FoodItem>();
            if (foodItem != null && foodItem.foodType != null && animalDiet.CanEat(foodItem.foodType)) {
                var pref = animalDiet.GetPreference(foodItem.foodType);
                if (pref == null) continue;

                float distance = entity.Position.ManhattanDistance(gridEntity.Position);
                float score = pref.preferencePriority / (1f + distance);

                if (score > bestScore) {
                    bestScore = score;
                    bestFood = entity.gameObject;
                }
            }
        }

        return bestFood;
    }

    void UpdateHungerTick() {
        if (animalDiet == null) return;

        currentHunger += animalDiet.hungerIncreaseRate;
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger);

        if (currentHunger >= animalDiet.maxHunger) {
            ApplyStarvationDamage();
        }

        UpdateHungerText();
    }

    void TryPoop() {
        if (isEating || isPooping) return;

        isPooping = true;
        SpawnPoop();

        if (TickManager.Instance?.Config != null) {
            var config = TickManager.Instance.Config;
            poopTick = Random.Range(
                config.ConvertSecondsToTicks(minPoopDelay),
                config.ConvertSecondsToTicks(maxPoopDelay)
            );
        }

        hasPooped = true;
        isPooping = false;

        if (CanShowThought()) ShowThought(ThoughtTrigger.Pooping);
    }

    void StartEating() {
        isEating = true;
        eatTimer = eatDuration;
        if (CanShowThought()) ShowThought(ThoughtTrigger.Eating);
    }

    void Update() {
        if (!enabled || isDying) return;

        bool showStats = Input.GetKey(showStatsKey);
        SetStatsTextVisibility(showStats);

        if (!useWegoMovement) {
            HandleRealtimeUpdate();
        } else {
            if (isEating) {
                eatTimer -= Time.deltaTime;
                if (eatTimer <= 0f) {
                    FinishEating();
                }
            }
        }

        FlipSpriteBasedOnDirection();
        UpdateAnimationState();
    }

    void HandleRealtimeUpdate() {
        if (isSeekingScreenCenter) {
            Vector2 currentPos = rb.position;
            bool centerWithinBounds =
                currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
                currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;

            if (centerWithinBounds) {
                if(Debug.isDebugBuild)
                    Debug.Log($"[{gameObject.name} Update] Center reached SHIFTED bounds! Pos: {currentPos}. MinB: {minBounds}, MaxB: {maxBounds}. Switching to normal AI.", gameObject);

                isSeekingScreenCenter = false;
                moveDirection = Vector2.zero;
            }
            else {
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

        if (isEating) {
            HandleEating();
            moveDirection = Vector2.zero;
        }
        else if (isPooping) {
            moveDirection = Vector2.zero;
        }
        else {
            DecideNextAction();
        }
    }

    void FinishEating() {
        isEating = false;

        if (currentTargetFood == null) return;

        FoodItem foodItem = currentTargetFood.GetComponent<FoodItem>();
        if (foodItem != null && foodItem.foodType != null) {
            float satiationGain = animalDiet.GetSatiationValue(foodItem.foodType);
            currentHunger -= satiationGain;
            currentHunger = Mathf.Max(0f, currentHunger);

            Destroy(currentTargetFood);
            hasPooped = false;
            currentTargetFood = null;
            UpdateHungerText();
        }
        else {
            currentTargetFood = null;
        }
    }

    void UpdateHunger() {
        currentHunger += animalDiet.hungerIncreaseRate * Time.deltaTime;
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger);

        if (currentHunger >= animalDiet.maxHunger) {
            ApplyStarvationDamage();
        }

        UpdateHungerText();
    }

    void HandlePooping() {
        if (!isEating && !hasPooped) {
            poopDelayTimer -= Time.deltaTime;
            if (!isPooping && poopDelayTimer <= 0f) {
                StartPooping();
            }
            if (isPooping) {
                poopTimer -= Time.deltaTime;
                if (poopTimer <= 0f) {
                    FinishPooping();
                }
            }
        }
    }

    void StartPooping() {
        isPooping = true;
        poopTimer = poopDuration;
        moveDirection = Vector2.zero;
        if (CanShowThought()) ShowThought(ThoughtTrigger.Pooping);
    }

    void FinishPooping() {
        SpawnPoop();
        isPooping = false;
        hasPooped = true;
    }

    void UpdateThoughts() {
        if (thoughtCooldownTimer > 0) {
            thoughtCooldownTimer -= Time.deltaTime;
        }
    }

    void DecideNextAction() {
        if (currentHunger >= animalDiet.hungerThreshold) {
            SeekFood();
        }
        else {
            Wander();
            currentTargetFood = null;
        }
    }

    void SeekFood() {
        if (CanShowThought()) ShowThought(ThoughtTrigger.Hungry);

        foodReassessmentTimer -= Time.deltaTime;
        bool shouldReassess = foodReassessmentTimer <= 0f;
        bool targetValid = currentTargetFood != null && currentTargetFood.activeInHierarchy &&
            currentTargetFood.GetComponent<FoodItem>() != null;

        if (shouldReassess || !targetValid) {
            GameObject potentialBetterFood = FindNearestFood();

            if (potentialBetterFood != null) {
                if (!targetValid) {
                    currentTargetFood = potentialBetterFood;
                    if(Debug.isDebugBuild)
                        Debug.Log($"[{gameObject.name} SeekFood] Found new food target (no previous): {potentialBetterFood.name}");
                }
                else if (potentialBetterFood != currentTargetFood) {
                    FoodItem currentFoodItem = currentTargetFood.GetComponent<FoodItem>();
                    FoodItem newFoodItem = potentialBetterFood.GetComponent<FoodItem>();

                    if (currentFoodItem != null && newFoodItem != null) {
                        float currentPriority = animalDiet.GetPreference(currentFoodItem.foodType)?.preferencePriority ?? 0f;
                        float newPriority = animalDiet.GetPreference(newFoodItem.foodType)?.preferencePriority ?? 0f;

                        if (newPriority > currentPriority) {
                            if(Debug.isDebugBuild)
                                Debug.Log($"[{gameObject.name} SeekFood] Switching to higher priority food: {newFoodItem.foodType.foodName} (priority: {newPriority}) from {currentFoodItem.foodType.foodName} (priority: {currentPriority})");

                            currentTargetFood = potentialBetterFood;
                        }
                    }
                }
            }

            foodReassessmentTimer = foodReassessmentInterval;
        }

        if (currentTargetFood != null) {
            MoveTowardFood(currentTargetFood);
        }
        else {
            Wander();
        }
    }

    GameObject FindNearestFood() {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        return animalDiet.FindBestFood(colliders, transform.position);
    }

    void MoveTowardFood(GameObject foodObj) {
        if (foodObj == null) return;

        float distance = Vector2.Distance(transform.position, foodObj.transform.position);
        if (distance <= eatDistance) {
            StartEating();
        }
        else {
            moveDirection = (foodObj.transform.position - transform.position).normalized;
            isWanderPaused = false;
            wanderStateTimer = 0f;
        }
    }

    void HandleEating() {
        eatTimer -= Time.deltaTime;
        if (eatTimer <= 0f) {
            isEating = false;
            FinishEatingAction();
        }
    }

    void FinishEatingAction() {
        if (currentTargetFood == null) return;

        FoodItem foodItem = currentTargetFood.GetComponent<FoodItem>();
        if (foodItem != null && foodItem.foodType != null) {
            float satiationGain = animalDiet.GetSatiationValue(foodItem.foodType);
            currentHunger -= satiationGain;
            currentHunger = Mathf.Max(0f, currentHunger);
            Destroy(currentTargetFood);
            hasPooped = false;
            poopDelayTimer = Random.Range(minPoopDelay, maxPoopDelay);
            currentTargetFood = null;
            UpdateHungerText();
        }
        else {
            currentTargetFood = null;
        }
    }

    void Wander() {
        if (wanderStateTimer <= 0f) {
            if (isWanderPaused) {
                isWanderPaused = false;
                moveDirection = Random.insideUnitCircle.normalized;
                wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration);
            }
            else {
                if (Random.value < wanderPauseChance) {
                    isWanderPaused = true;
                    moveDirection = Vector2.zero;
                    wanderStateTimer = Random.Range(wanderMinPauseDuration, wanderMaxPauseDuration);
                }
                else {
                    moveDirection = Random.insideUnitCircle.normalized;
                    wanderStateTimer = Random.Range(wanderMinMoveDuration, wanderMaxMoveDuration);
                }
            }
        }
        else {
            wanderStateTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate() {
        if (rb == null || isDying || useWegoMovement) return;

        if (!isEating && !isPooping && moveDirection != Vector2.zero) {
            Vector2 currentPos = rb.position;
            Vector2 desiredMove = moveDirection.normalized * currentSpeed * Time.fixedDeltaTime;
            Vector2 nextPos = currentPos + desiredMove;

            if (!isSeekingScreenCenter) {
                nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
                nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y, maxBounds.y);
            }
            rb.MovePosition(nextPos);
        }
    }

    void ApplyStarvationDamage() {
        float damage = starvationDamageRate * Time.deltaTime;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        if (!isFlashing) {
            StartCoroutine(FlashDamage());
        }

        UpdateHpText();

        if (currentHealth <= 0f) {
            Die(CauseOfDeath.Starvation);
        }
    }

    IEnumerator FlashDamage() {
        if (spriteRenderer == null) yield break;

        isFlashing = true;
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    IEnumerator FadeOutAndDestroy() {
        if (spriteRenderer == null) {
            Destroy(gameObject);
            yield break;
        }

        isDying = true;
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsedTime < deathFadeDuration) {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / deathFadeDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    void SpawnPoop() {
        if (poopPrefabs == null || poopPrefabs.Count == 0) return;

        int index = Random.Range(0, poopPrefabs.Count);
        GameObject prefab = poopPrefabs[index];
        if (prefab == null) return;

        Transform spawnT = poopSpawnPoint ? poopSpawnPoint : transform;
        GameObject poopObj = Instantiate(prefab, spawnT.position, Quaternion.identity);

        SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>();
        if (sr != null) {
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

    void FlipSpriteBasedOnDirection() {
        if (spriteRenderer != null && Mathf.Abs(moveDirection.x) > 0.01f) {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    void UpdateAnimationState() {
        if (animator == null) return;
        bool isMoving = !isEating && !isPooping && moveDirection.sqrMagnitude > 0.01f;
        if (useWegoMovement && gridEntity != null) {
            isMoving = gridEntity.IsMoving && !isEating && !isPooping;
        }
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsEating", isEating);
    }

    bool CanShowThought() {
        if (useWegoMovement) {
            return thoughtLibrary != null && thoughtBubblePrefab != null && thoughtCooldownTick <= 0;
        } else {
            return thoughtLibrary != null && thoughtBubblePrefab != null && thoughtCooldownTimer <= 0f;
        }
    }

    void ShowThought(ThoughtTrigger trigger) {
        if (thoughtLibrary == null || thoughtLibrary.allThoughts == null) return;

        var entry = thoughtLibrary.allThoughts.FirstOrDefault(t =>
            t != null && t.speciesName == SpeciesName && t.trigger == trigger
        );

        if (entry != null && entry.lines != null && entry.lines.Count > 0) {
            string line = entry.lines[Random.Range(0, entry.lines.Count)];
            Transform spawnT = bubbleSpawnTransform ? bubbleSpawnTransform : transform;
            GameObject bubbleGO = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity, spawnT);
            bubbleGO.transform.localPosition = Vector3.zero;

            ThoughtBubbleController bubble = bubbleGO.GetComponent<ThoughtBubbleController>();
            if (bubble) {
                bubble.Initialize(line, spawnT, 2f);
                if (useWegoMovement) {
                    thoughtCooldownTick = TickManager.Instance?.Config?.ConvertSecondsToTicks(thoughtCooldownTime) ?? thoughtCooldownTick;
                } else {
                    thoughtCooldownTimer = thoughtCooldownTime;
                }
            }
            else {
                Destroy(bubbleGO);
            }
        }
    }

    public enum CauseOfDeath { Unknown, Starvation, EatenByPredator }

    void Die(CauseOfDeath cause) {
        Debug.Log($"[{SpeciesName} died: {cause}]", gameObject);
        StartCoroutine(FadeOutAndDestroy());
    }

    public bool SpeciesNameEquals(string other) {
        return definition != null && definition.animalName == other;
    }

    void UpdateHpText() {
        if (hpText == null || definition == null) return;
        hpText.text = $"HP: {Mathf.FloorToInt(currentHealth)}/{Mathf.FloorToInt(definition.maxHealth)}";
    }

    void UpdateHungerText() {
        if (hungerText == null || animalDiet == null) return;
        hungerText.text = $"Hunger: {Mathf.FloorToInt(currentHunger)}/{Mathf.FloorToInt(animalDiet.maxHunger)}";
    }

    void EnsureUITextReferences() {
        if (hpText == null) {
            hpText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (hpText != null && hpText.gameObject.name.Contains("Hunger")) {
                hungerText = hpText;
                hpText = null;
            }
        }

        if (hungerText == null) {
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts) {
                if (text != hpText) {
                    hungerText = text;
                    break;
                }
            }
        }
    }

    void SetStatsTextVisibility(bool visible) {
        if (hpText != null) {
            hpText.gameObject.SetActive(visible);
        }

        if (hungerText != null) {
            hungerText.gameObject.SetActive(visible);
        }
    }

    protected override void OnSpeedChanged(float newSpeed) {
        if (animator != null) {
            float speedRatio = (baseSpeed > 0f) ? newSpeed / baseSpeed : 1f;
            animator.speed = Mathf.Max(0.5f, speedRatio);
        }
    }

    public void SetWegoMovement(bool enabled) {
        useWegoMovement = enabled;

        if (enabled && gridEntity == null) {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }

        if (enabled && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        } else if (!enabled && TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public GridPosition GetCurrentGridPosition() {
        return gridEntity?.Position ?? GridPosition.Zero;
    }
}
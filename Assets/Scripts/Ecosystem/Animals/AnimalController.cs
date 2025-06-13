using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using WegoSystem;

public class AnimalController : MonoBehaviour, ITickUpdateable {
    [Header("References")]
    [SerializeField] public AnimalDefinition definition;
    [SerializeField] GameObject thoughtBubblePrefab;
    [SerializeField] Transform bubbleSpawnTransform;
    [SerializeField] Transform poopSpawnPoint;
    [SerializeField] List<GameObject> poopPrefabs;
    [SerializeField] Animator animator;
    
    [Header("UI")]
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;
    
    // Core Components
    AnimalDiet animalDiet;
    GridEntity gridEntity;
    AnimalThoughtLibrary thoughtLibrary;
    
    public int thinkingTickInterval = 3;  // Add this field
    
    // Grid Movement
    GridPosition targetPosition;
    GameObject currentTargetFood = null;
    bool hasPlannedAction = false;
    int lastThinkTick = 0;
    List<GridPosition> currentPath = new List<GridPosition>();
    int currentPathIndex = 0;
    
    // Tick Counters
    int hungerTick = 0;
    int poopDelayTick = 0;
    int currentPoopCooldownTick = 0;
    int thoughtCooldownTick = 0;
    int eatRemainingTicks = 0;
    int starvationTick = 0;
    int deathFadeRemainingTicks = 0;
    int flashRemainingTicks = 0;
    int wanderPauseTicks = 0;
    
    // State
    float currentHealth;
    float currentHunger;
    bool isEating = false;
    bool isPooping = false;
    bool hasPooped = true;
    bool isDying = false;
    bool isWanderPaused = false;
    bool isFlashing = false;
    
    // Spawn Behavior
    bool isSeekingScreenCenter = false;
    Vector2 screenCenterTarget;
    Vector2 minBounds;
    Vector2 maxBounds;
    
    // Cached References
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Collider2D animalCollider;
    Color originalColor;
    
    public float CurrentHealth => currentHealth;
    public string SpeciesName => definition ? definition.animalName : "Uninitialized";
    
    void Awake() {
        ValidateComponents();
        CacheComponents();
    }
    
    void Start() {
        if (GridPositionManager.Instance != null) {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[AnimalController] {gameObject.name} snapped to grid position {gridEntity.Position}");
        }
        
        if (TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }
    
    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }
    
    void ValidateComponents() {
        if (definition == null) {
            Debug.LogError($"[{gameObject.name}] Missing AnimalDefinition!", this);
            enabled = false;
            return;
        }
        
        animalDiet = definition.diet;
        if (animalDiet == null) {
            Debug.LogError($"[{gameObject.name}] AnimalDefinition missing diet!", this);
            enabled = false;
            return;
        }
    }
    
    void CacheComponents() {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animalCollider = GetComponent<Collider2D>();
        gridEntity = GetComponent<GridEntity>();
        
        if (gridEntity == null) {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
        
        if (spriteRenderer != null) {
            originalColor = spriteRenderer.color;
        }
        
        if (animalCollider == null) {
            Debug.LogError($"[{gameObject.name}] Missing Collider2D!", this);
            enabled = false;
        }
    }
    
    public void Initialize(AnimalDefinition def, Vector2 shiftedMinBounds, Vector2 shiftedMaxBounds, bool spawnedOffscreen = false) {
        definition = def;
        if (definition == null) {
            Destroy(gameObject);
            return;
        }
        
        ValidateComponents();
        
        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        hasPooped = true;
        
        // Initialize tick counters
        poopDelayTick = Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);
        thoughtCooldownTick = 0;
        
        minBounds = shiftedMinBounds;
        maxBounds = shiftedMaxBounds;
        screenCenterTarget = (minBounds + maxBounds) / 2f;
        
        isSeekingScreenCenter = spawnedOffscreen;
        if (isSeekingScreenCenter && gridEntity != null) {
            GridPosition targetGridPos = GridPositionManager.Instance.WorldToGrid(screenCenterTarget);
            hasPlannedAction = true;
            targetPosition = targetGridPos;
        }
        
        EnsureUITextReferences();
        SetStatsTextVisibility(false);
        UpdateUI();
    }
    
    public void OnTickUpdate(int currentTick) {
        if (isDying) return;
        
        // Update tick counters
        hungerTick++;
        if (poopDelayTick > 0) poopDelayTick--;
        if (thoughtCooldownTick > 0) thoughtCooldownTick--;
        if (currentPoopCooldownTick > 0) currentPoopCooldownTick--;
        if (eatRemainingTicks > 0) eatRemainingTicks--;
        if (wanderPauseTicks > 0) wanderPauseTicks--;
        if (flashRemainingTicks > 0) flashRemainingTicks--;
        
        // Process hunger
        if (TickManager.Instance?.Config != null) {
            if (hungerTick >= TickManager.Instance.Config.animalHungerTickInterval) {
                UpdateHungerTick();
                hungerTick = 0;
            }
        }
        
        // Process starvation damage
        if (currentHunger >= animalDiet.maxHunger) {
            starvationTick++;
            if (starvationTick >= definition.starvationDamageTickInterval) {
                ApplyStarvationDamage();
                starvationTick = 0;
            }
        }
        
        // Process eating
        if (isEating && eatRemainingTicks <= 0) {
            FinishEating();
        }
        
        // Process pooping
        if (!hasPooped && poopDelayTick <= 0 && currentPoopCooldownTick <= 0 && !isEating) {
            TryPoop();
        }
        
        // Make decisions
        if (currentTick - lastThinkTick >= definition.thinkingTickInterval) {
            MakeDecision();
            lastThinkTick = currentTick;
        }
        
        // Execute movement
        if (hasPlannedAction && !isEating && !isPooping && wanderPauseTicks <= 0) {
            ExecutePlannedAction();
        }
        
        // Process death fade
        if (deathFadeRemainingTicks > 0) {
            deathFadeRemainingTicks--;
            UpdateDeathFade();
            if (deathFadeRemainingTicks <= 0) {
                Destroy(gameObject);
            }
        }
        
        // Update visuals
        UpdateAnimations();
        UpdateFlashEffect();
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
        
        Vector2 currentPos = transform.position;
        bool centerWithinBounds = currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
                                 currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;
        
        if (centerWithinBounds) {
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
            
            currentPath = GridPositionManager.Instance.GetPath(gridEntity.Position, foodGridPos);
            currentPathIndex = 0;
            
            if (currentPath.Count > 0) {
                hasPlannedAction = true;
                targetPosition = currentPath[0];
            } else {
                PlanMovementToward(foodGridPos);
            }
        } else {
            PlanWandering();
        }
    }
    
    void PlanWandering() {
        if (gridEntity == null) return;
        
        // Check for wander pause
        if (Random.Range(0, 100) < definition.wanderPauseTickChance) {
            isWanderPaused = true;
            wanderPauseTicks = Random.Range(definition.minWanderPauseTicks, definition.maxWanderPauseTicks);
            hasPlannedAction = false;
            return;
        }
        
        // Normal wander movement
        GridPosition currentPos = gridEntity.Position;
        GridPosition[] directions = {
            GridPosition.Up, GridPosition.Down,
            GridPosition.Left, GridPosition.Right
        };
        
        for (int i = 0; i < 3; i++) {
            GridPosition randomDir = directions[Random.Range(0, directions.Length)];
            GridPosition targetPos = currentPos + randomDir;
            
            if (IsValidMove(targetPos)) {
                targetPosition = targetPos;
                hasPlannedAction = true;
                wanderPauseTicks = Random.Range(definition.minWanderMoveTicks, definition.maxWanderMoveTicks);
                break;
            }
        }
    }
    
    void PlanMovementToward(GridPosition target) {
        if (gridEntity == null) return;
        
        GridPosition currentPos = gridEntity.Position;
        int dx = Mathf.Clamp(target.x - currentPos.x, -1, 1);
        int dy = Mathf.Clamp(target.y - currentPos.y, -1, 1);
        
        // Try diagonal first
        if (dx != 0 && dy != 0) {
            GridPosition diagonalTarget = currentPos + new GridPosition(dx, dy);
            if (IsValidMove(diagonalTarget)) {
                targetPosition = diagonalTarget;
                hasPlannedAction = true;
                return;
            }
        }
        
        // Try horizontal
        if (dx != 0) {
            GridPosition horizontalTarget = currentPos + new GridPosition(dx, 0);
            if (IsValidMove(horizontalTarget)) {
                targetPosition = horizontalTarget;
                hasPlannedAction = true;
                return;
            }
        }
        
        // Try vertical
        if (dy != 0) {
            GridPosition verticalTarget = currentPos + new GridPosition(0, dy);
            if (IsValidMove(verticalTarget)) {
                targetPosition = verticalTarget;
                hasPlannedAction = true;
                return;
            }
        }
        
        // Can't move closer, try wandering
        PlanWandering();
    }
    
    bool IsValidMove(GridPosition pos) {
        return GridPositionManager.Instance.IsPositionValid(pos) &&
               !GridPositionManager.Instance.IsPositionOccupied(pos);
    }
    
    void ExecutePlannedAction() {
        if (gridEntity == null) return;
        
        // Check if we're at food
        if (currentTargetFood != null && targetPosition == gridEntity.Position) {
            GridPosition foodPos = GridPositionManager.Instance.WorldToGrid(currentTargetFood.transform.position);
            int distance = gridEntity.Position.ManhattanDistance(foodPos);
            
            if (distance <= definition.eatDistanceTiles) {
                StartEating();
                return;
            }
        }
        
        // Move to target
        if (targetPosition != gridEntity.Position) {
            gridEntity.SetPosition(targetPosition);
            
            // Continue path if we have one
            if (currentPath.Count > 0 && currentPathIndex < currentPath.Count - 1) {
                currentPathIndex++;
                targetPosition = currentPath[currentPathIndex];
                hasPlannedAction = true;
                return;
            }
        }
        
        hasPlannedAction = false;
    }
    
    GameObject FindNearestFoodInGrid() {
        if (animalDiet == null) return null;
    
        // Get all tiles within search radius using circle approximation
        var tilesInRange = GridRadiusUtility.GetTilesInCircle(gridEntity.Position, definition.searchRadiusTiles);
    
        GameObject bestFood = null;
        float bestScore = -1f;
    
        // Debug visualization
        if (GridDebugVisualizer.Instance != null && Debug.isDebugBuild) {
            GridDebugVisualizer.Instance.VisualizeAnimalSearchRadius(this, gridEntity.Position, definition.searchRadiusTiles);
        }
    
        foreach (var tilePos in tilesInRange) {
            // Check all entities at this tile position
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tilePos);
        
            foreach (var entity in entitiesAtTile) {
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
        }
    
        return bestFood;
    }
    
    void StartEating() {
        isEating = true;
        eatRemainingTicks = definition.eatDurationTicks;
        if (CanShowThought()) ShowThought(ThoughtTrigger.Eating);
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
            poopDelayTick = Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);
            currentTargetFood = null;
            UpdateUI();
        }
    }
    
    void UpdateHungerTick() {
        if (animalDiet == null) return;
        
        currentHunger += animalDiet.hungerIncreaseRate;
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger);
        UpdateUI();
    }
    
    void TryPoop() {
        if (isEating || isPooping) return;
        
        isPooping = true;
        currentPoopCooldownTick = definition.poopCooldownTicks;
        SpawnPoop();
        hasPooped = true;
        isPooping = false;
        
        if (CanShowThought()) ShowThought(ThoughtTrigger.Pooping);
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
            float v = definition.poopColorVariation;
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
    
    void ApplyStarvationDamage() {
        currentHealth -= definition.damagePerStarvationTick;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        if (!isFlashing) {
            flashRemainingTicks = definition.damageFlashTicks;
            isFlashing = true;
        }
        
        UpdateUI();
        
        if (currentHealth <= 0f) {
            Die(CauseOfDeath.Starvation);
        }
    }
    
    void UpdateFlashEffect() {
        if (flashRemainingTicks > 0 && spriteRenderer != null) {
            spriteRenderer.color = definition.damageFlashColor;
        } else if (isFlashing && spriteRenderer != null) {
            spriteRenderer.color = originalColor;
            isFlashing = false;
        }
    }
    
    public enum CauseOfDeath { Unknown, Starvation, EatenByPredator }
    
    void Die(CauseOfDeath cause) {
        Debug.Log($"[{SpeciesName}] died: {cause}", gameObject);
        isDying = true;
        deathFadeRemainingTicks = definition.deathFadeTicks;
    }
    
    void UpdateDeathFade() {
        if (spriteRenderer != null && definition.deathFadeTicks > 0) {
            float alpha = (float)deathFadeRemainingTicks / definition.deathFadeTicks;
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
    
    bool CanShowThought() {
        return thoughtLibrary != null && thoughtBubblePrefab != null && thoughtCooldownTick <= 0;
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
            
            ThoughtBubbleController bubble = bubbleGO.GetComponent<ThoughtBubbleController>();
            if (bubble) {
                bubble.Initialize(line, spawnT, 2f);
                thoughtCooldownTick = definition.thoughtCooldownTicks;
            } else {
                Destroy(bubbleGO);
            }
        }
    }
    
    void Update() {
        if (!enabled || isDying) return;
        
        bool showStats = Input.GetKey(showStatsKey);
        SetStatsTextVisibility(showStats);
        
        UpdateSpriteFlipping();
    }
    
    void UpdateSpriteFlipping() {
        if (spriteRenderer != null && gridEntity != null && gridEntity.IsMoving) {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = GridPositionManager.Instance.GridToWorld(targetPosition);
            Vector2 moveDirection = (targetPos - currentPos).normalized;
            
            if (Mathf.Abs(moveDirection.x) > 0.01f) {
                spriteRenderer.flipX = moveDirection.x < 0;
            }
        }
    }
    
    void UpdateAnimations() {
        if (animator == null) return;
        bool isMoving = gridEntity != null && gridEntity.IsMoving && !isEating && !isPooping;
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsEating", isEating);
    }
    
    void UpdateUI() {
        UpdateHpText();
        UpdateHungerText();
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
        if (hpText != null) hpText.gameObject.SetActive(visible);
        if (hungerText != null) hungerText.gameObject.SetActive(visible);
    }
    
    public GridPosition GetCurrentGridPosition() {
        return gridEntity?.Position ?? GridPosition.Zero;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using WegoSystem;

public class AnimalController : MonoBehaviour, ITickUpdateable
{
    #region Fields

    [SerializeField] public AnimalDefinition definition;
    [SerializeField] GameObject thoughtBubblePrefab;
    [SerializeField] Transform bubbleSpawnTransform;
    [SerializeField] Transform poopSpawnPoint;
    [SerializeField] List<GameObject> poopPrefabs;
    [SerializeField] Animator animator;

    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;
    
    [Header("Debugging")]
    [SerializeField] bool showPathfindingDebugLine = false;

    AnimalDiet animalDiet;
    GridEntity gridEntity;
    AnimalThoughtLibrary thoughtLibrary;
    LineRenderer pathDebugLine;

    GridPosition targetPosition;
    GameObject currentTargetFood = null;
    bool hasPlannedAction = false;
    int lastThinkTick = 0;
    List<GridPosition> currentPath = new List<GridPosition>();
    int currentPathIndex = 0;

    int hungerTick = 0;
    int poopDelayTick = 0;
    int currentPoopCooldownTick = 0;
    int thoughtCooldownTick = 0;
    int eatRemainingTicks = 0;
    int starvationTick = 0;
    int deathFadeRemainingTicks = 0;
    int wanderPauseTicks = 0;

    float flashRemainingTime = 0f;
    float flashDurationSeconds = 0.2f;

    float currentHealth;
    float currentHunger;
    bool isEating = false;
    bool isPooping = false;
    bool hasPooped = true;
    bool isDying = false;
    bool isFlashing = false;
    bool isHungryAndSearching = false;

    bool isSeekingScreenCenter = false;
    Vector2 screenCenterTarget;
    Vector2 minBounds;
    Vector2 maxBounds;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Collider2D animalCollider;
    Color originalColor;

    public float CurrentHealth => currentHealth;
    public string SpeciesName => definition ? definition.animalName : "Uninitialized";

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        CacheComponents();
        SetupDebugLineRenderer();
    }

    void Start()
    {
        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[AnimalController] {gameObject.name} snapped to grid position {gridEntity.Position}");
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
    }

    void Update()
    {
        if (!enabled || isDying) return;

        bool showStats = Input.GetKey(showStatsKey);
        SetStatsTextVisibility(showStats);

        UpdateSpriteFlipping();
        UpdateFlashEffect();

        UpdatePathDebugLine();
    }

    #endregion

    #region Initialization & Setup

    void ValidateComponents()
    {
        if (definition == null)
        {
            Debug.LogError($"[{gameObject.name}] Missing AnimalDefinition!", this);
            enabled = false;
            return;
        }

        animalDiet = definition.diet;
        if (animalDiet == null)
        {
            Debug.LogError($"[{gameObject.name}] AnimalDefinition missing diet!", this);
            enabled = false;
            return;
        }

        if (definition != null && TickManager.Instance?.Config != null)
        {
            flashDurationSeconds = definition.damageFlashTicks / TickManager.Instance.Config.ticksPerRealSecond;
        }
    }

    void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animalCollider = GetComponent<Collider2D>();
        gridEntity = GetComponent<GridEntity>();

        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (animalCollider == null)
        {
            Debug.LogError($"[{gameObject.name}] Missing Collider2D!", this);
            enabled = false;
        }
    }
    
    void SetupDebugLineRenderer()
    {
        pathDebugLine = gameObject.GetComponent<LineRenderer>();
        if (showPathfindingDebugLine)
        {
            if (pathDebugLine == null)
            {
                pathDebugLine = gameObject.AddComponent<LineRenderer>();
            }
            pathDebugLine.enabled = true;
            pathDebugLine.startWidth = 0.05f;
            pathDebugLine.endWidth = 0.05f;
            pathDebugLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            pathDebugLine.startColor = Color.cyan;
            pathDebugLine.endColor = Color.magenta;
            pathDebugLine.positionCount = 0;
            pathDebugLine.sortingLayerName = "UI";
            pathDebugLine.sortingOrder = 100;
        }
        else if(pathDebugLine != null)
        {
            pathDebugLine.enabled = false;
        }
    }

    public void Initialize(AnimalDefinition def, Vector2 shiftedMinBounds, Vector2 shiftedMaxBounds, bool spawnedOffscreen = false)
    {
        definition = def;
        if (definition == null)
        {
            Destroy(gameObject);
            return;
        }

        ValidateComponents();

        currentHealth = definition.maxHealth;
        currentHunger = 0f;
        hasPooped = true;

        poopDelayTick = UnityEngine.Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);
        thoughtCooldownTick = 0;

        minBounds = shiftedMinBounds;
        maxBounds = shiftedMaxBounds;
        screenCenterTarget = (minBounds + maxBounds) / 2f;

        isSeekingScreenCenter = spawnedOffscreen;
        if (isSeekingScreenCenter && gridEntity != null)
        {
            GridPosition targetGridPos = GridPositionManager.Instance.WorldToGrid(screenCenterTarget);
            hasPlannedAction = true;
            targetPosition = targetGridPos;
        }

        if (thoughtLibrary == null && definition.thoughtLibrary != null)
        {
            thoughtLibrary = definition.thoughtLibrary;
        }

        EnsureUITextReferences();
        SetStatsTextVisibility(false);
        UpdateUI();
    }

    #endregion

    #region Tick Update and Decisions

    public void OnTickUpdate(int currentTick)
    {
        if (isDying) return;

        hungerTick++;
        if (poopDelayTick > 0) poopDelayTick--;
        if (thoughtCooldownTick > 0) thoughtCooldownTick--;
        if (currentPoopCooldownTick > 0) currentPoopCooldownTick--;
        if (eatRemainingTicks > 0) eatRemainingTicks--;
        if (wanderPauseTicks > 0) wanderPauseTicks--;

        if (TickManager.Instance?.Config != null)
        {
            if (hungerTick >= TickManager.Instance.Config.animalHungerTickInterval)
            {
                UpdateHungerTick();
                hungerTick = 0;

                if (!isHungryAndSearching && currentHunger >= animalDiet.hungerThreshold)
                {
                    isHungryAndSearching = true;
                    // Force an immediate decision when becoming hungry
                    MakeDecision();
                }
            }
        }

        if (currentHunger >= animalDiet.maxHunger)
        {
            starvationTick++;
            if (starvationTick >= definition.starvationDamageTickInterval)
            {
                ApplyStarvationDamage();
                starvationTick = 0;
            }
        }

        if (isEating && eatRemainingTicks <= 0)
        {
            FinishEating();
        }

        if (!hasPooped && poopDelayTick <= 0 && currentPoopCooldownTick <= 0 && !isEating)
        {
            TryPoop();
        }

        if (currentTick - lastThinkTick >= definition.thinkingTickInterval)
        {
            MakeDecision();
            lastThinkTick = currentTick;
        }

        if (hasPlannedAction && !isEating && !isPooping)
        {
            ExecutePlannedAction();
        }

        if (deathFadeRemainingTicks > 0)
        {
            deathFadeRemainingTicks--;
            UpdateDeathFade();
            if (deathFadeRemainingTicks <= 0)
            {
                Destroy(gameObject);
            }
        }

        UpdateAnimations();
    }

    void MakeDecision()
    {
        if (isEating || isDying || gridEntity.IsMoving) return;
        
        if (isSeekingScreenCenter)
        {
            HandleScreenCenterSeeking();
            return;
        }
        
        if (currentHunger >= animalDiet.hungerThreshold)
        {
            isHungryAndSearching = true;
            PlanFoodSeeking();
        }
        else
        {
            isHungryAndSearching = false;
            PlanWandering();
        }
    }

    // <<< FIX: Completely rewritten for robust path execution.
    void ExecutePlannedAction()
    {
        if (gridEntity == null || !hasPlannedAction || gridEntity.IsMoving) return;

        // Step 1: Check for the goal condition (eating).
        if (currentTargetFood != null && gridEntity.Position.ManhattanDistance(GridPositionManager.Instance.WorldToGrid(currentTargetFood.transform.position)) <= definition.eatDistanceTiles)
        {
            StartEating();
            return;
        }

        // Step 2: If we have a path, get the next waypoint.
        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            targetPosition = currentPath[currentPathIndex];
        }
        else
        {
            // If there's no path, this must be a single-step wander.
            // 'targetPosition' is already set from PlanWandering.
        }

        // Step 3: Execute the move if we aren't at our immediate target.
        if (gridEntity.Position != targetPosition)
        {
            gridEntity.SetPosition(targetPosition);
            
            // Step 4: After *initiating* the move, prepare for the next tick.
            if (currentPath != null && currentPath.Count > 0)
            {
                // Advance the path index so next tick we aim for the next waypoint.
                currentPathIndex++;
            }
            else
            {
                // It was a single-step move, so the plan is now complete.
                hasPlannedAction = false;
            }
        }
        else
        {
            // We are already at our target position. This can happen if the path is blocked
            // or if it was a single point. In any case, this plan is done.
            hasPlannedAction = false;
            currentPath.Clear();
            ClearPathDebugLine();
        }
    }

    #endregion

    #region Planning and Movement

    void HandleScreenCenterSeeking()
    {
        if (gridEntity == null) return;

        Vector2 currentPos = transform.position;
        bool centerWithinBounds = currentPos.x >= minBounds.x && currentPos.x <= maxBounds.x &&
                                  currentPos.y >= minBounds.y && currentPos.y <= maxBounds.y;

        if (centerWithinBounds)
        {
            isSeekingScreenCenter = false;
            hasPlannedAction = false;
        }
        else
        {
            GridPosition currentGridPos = gridEntity.Position;
            GridPosition targetGridPos = GridPositionManager.Instance.WorldToGrid(screenCenterTarget);
            PlanMovementToward(targetGridPos);
        }
    }

    void PlanFoodSeeking()
    {
        // <<< FIX: Turn on the search radius visual.
        if (GridDebugVisualizer.Instance != null && Debug.isDebugBuild)
        {
            GridDebugVisualizer.Instance.VisualizeAnimalSearchRadius(this, gridEntity.Position, definition.searchRadiusTiles);
        }

        if (CanShowThought()) ShowThought(ThoughtTrigger.Hungry);

        GameObject nearestFood = FindNearestFoodInGrid();
        if (nearestFood != null)
        {
            currentTargetFood = nearestFood;
            GridPosition foodGridPos = GridPositionManager.Instance.WorldToGrid(nearestFood.transform.position);

            currentPath = GridPositionManager.Instance.GetPath(gridEntity.Position, foodGridPos);
            currentPathIndex = 0;

            if (currentPath.Count > 0)
            {
                hasPlannedAction = true;
            }
            else
            {
                PlanMovementToward(foodGridPos);
                currentPath.Clear();
            }
        }
        else
        {
            PlanWandering();
        }
    }

    void PlanWandering()
    {
        // <<< FIX: Turn off the search radius and path visuals when not seeking food.
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
        ClearPathDebugLine();

        if (gridEntity == null) return;
        
        currentPath.Clear();
        currentTargetFood = null;

        if (wanderPauseTicks > 0) return;

        if (UnityEngine.Random.Range(0, 100) < definition.wanderPauseTickChance)
        {
            wanderPauseTicks = UnityEngine.Random.Range(definition.minWanderPauseTicks, definition.maxWanderPauseTicks);
            hasPlannedAction = false;
            return;
        }

        GridPosition currentPos = gridEntity.Position;
        GridPosition[] directions = {
            GridPosition.Up, GridPosition.Down,
            GridPosition.Left, GridPosition.Right
        };

        for (int i = 0; i < 4; i++)
        {
            GridPosition randomDir = directions[UnityEngine.Random.Range(0, directions.Length)];
            GridPosition targetPos = currentPos + randomDir;

            if (IsValidMove(targetPos))
            {
                targetPosition = targetPos;
                hasPlannedAction = true;
                return;
            }
        }
        
        hasPlannedAction = false;
    }

    void PlanMovementToward(GridPosition target)
    {
        if (gridEntity == null) return;

        GridPosition currentPos = gridEntity.Position;
        int dx = Mathf.Clamp(target.x - currentPos.x, -1, 1);
        int dy = Mathf.Clamp(target.y - currentPos.y, -1, 1);

        if (dx != 0 && dy != 0)
        {
            GridPosition diagonalTarget = currentPos + new GridPosition(dx, dy);
            if (IsValidMove(diagonalTarget))
            {
                targetPosition = diagonalTarget;
                hasPlannedAction = true;
                return;
            }
        }

        if (dx != 0)
        {
            GridPosition horizontalTarget = currentPos + new GridPosition(dx, 0);
            if (IsValidMove(horizontalTarget))
            {
                targetPosition = horizontalTarget;
                hasPlannedAction = true;
                return;
            }
        }

        if (dy != 0)
        {
            GridPosition verticalTarget = currentPos + new GridPosition(0, dy);
            if (IsValidMove(verticalTarget))
            {
                targetPosition = verticalTarget;
                hasPlannedAction = true;
                return;
            }
        }

        PlanWandering();
    }

    bool IsValidMove(GridPosition pos)
    {
        if (GridPositionManager.Instance == null) return false;
        
        if (GridPositionManager.Instance.IsPositionOccupied(pos))
        {
            if (currentTargetFood != null)
            {
                var foodGridPos = GridPositionManager.Instance.WorldToGrid(currentTargetFood.transform.position);
                if (pos == foodGridPos) return true;
            }
            return false;
        }

        return GridPositionManager.Instance.IsPositionValid(pos);
    }

    GameObject FindNearestFoodInGrid()
    {
        if (animalDiet == null) return null;

        var tilesInRange = GridRadiusUtility.GetTilesInCircle(gridEntity.Position, definition.searchRadiusTiles);

        GameObject bestFood = null;
        float bestScore = -1f;

        foreach (var tilePos in tilesInRange)
        {
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tilePos);

            foreach (var entity in entitiesAtTile)
            {
                if (entity == null || entity.gameObject == this.gameObject) continue;

                FoodItem foodItem = entity.GetComponent<FoodItem>();
                if (foodItem != null && foodItem.foodType != null && animalDiet.CanEat(foodItem.foodType))
                {
                    var pref = animalDiet.GetPreference(foodItem.foodType);
                    if (pref == null) continue;

                    float distance = entity.Position.ManhattanDistance(gridEntity.Position);
                    float score = pref.preferencePriority / (1f + distance);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFood = entity.gameObject;
                    }
                }
            }
        }

        return bestFood;
    }

    #endregion

    #region Actions (Eat, Poop, Die)

    void StartEating()
    {
        // <<< FIX: Turn off all debug visuals when eating.
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
        ClearPathDebugLine();
        currentPath.Clear();
        hasPlannedAction = false;

        isEating = true;
        eatRemainingTicks = definition.eatDurationTicks;
        if (CanShowThought()) ShowThought(ThoughtTrigger.Eating);
    }

    void FinishEating()
    {
        isEating = false;

        if (currentTargetFood == null) return;

        FoodItem foodItem = currentTargetFood.GetComponent<FoodItem>();
        if (foodItem != null && foodItem.foodType != null)
        {
            float satiationGain = animalDiet.GetSatiationValue(foodItem.foodType);
            currentHunger -= satiationGain;
            currentHunger = Mathf.Max(0f, currentHunger);

            Destroy(currentTargetFood);
            hasPooped = false;
            poopDelayTick = UnityEngine.Random.Range(definition.minPoopDelayTicks, definition.maxPoopDelayTicks);
            currentTargetFood = null;
            
            if (currentHunger < animalDiet.hungerThreshold)
            {
                isHungryAndSearching = false;
            }

            UpdateUI();
        }
    }

    void UpdateHungerTick()
    {
        if (animalDiet == null) return;

        currentHunger += animalDiet.hungerIncreaseRate;
        currentHunger = Mathf.Min(currentHunger, animalDiet.maxHunger);
        UpdateUI();
    }

    void TryPoop()
    {
        if (isEating || isPooping) return;

        isPooping = true;
        currentPoopCooldownTick = definition.poopCooldownTicks;
        SpawnPoop();
        hasPooped = true;
        isPooping = false;

        if (CanShowThought()) ShowThought(ThoughtTrigger.Pooping);
    }

    void SpawnPoop()
    {
        if (poopPrefabs == null || poopPrefabs.Count == 0) return;

        int index = UnityEngine.Random.Range(0, poopPrefabs.Count);
        GameObject prefab = poopPrefabs[index];
        if (prefab == null) return;

        Transform spawnT = poopSpawnPoint ? poopSpawnPoint : transform;
        GameObject poopObj = Instantiate(prefab, spawnT.position, Quaternion.identity);

        SpriteRenderer sr = poopObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = UnityEngine.Random.value > 0.5f;
            Color c = sr.color;
            float v = definition.poopColorVariation;
            sr.color = new Color(
                Mathf.Clamp01(c.r + UnityEngine.Random.Range(-v, v)),
                Mathf.Clamp01(c.g + UnityEngine.Random.Range(-v, v)),
                Mathf.Clamp01(c.b + UnityEngine.Random.Range(-v, v)),
                c.a
            );
        }

        PoopController pc = poopObj.GetComponent<PoopController>() ?? poopObj.AddComponent<PoopController>();
        pc.Initialize();
    }

    void ApplyStarvationDamage()
    {
        currentHealth -= definition.damagePerStarvationTick;
        currentHealth = Mathf.Max(0f, currentHealth);

        if (!isFlashing)
        {
            flashRemainingTime = flashDurationSeconds;
            isFlashing = true;
        }

        UpdateUI();

        if (currentHealth <= 0f)
        {
            Die(CauseOfDeath.Starvation);
        }
    }

    public enum CauseOfDeath { Unknown, Starvation, EatenByPredator }

    void Die(CauseOfDeath cause)
    {
        Debug.Log($"[{SpeciesName}] died: {cause}", gameObject);
        isDying = true;
        deathFadeRemainingTicks = definition.deathFadeTicks;
    }

    #endregion

    #region Visuals and UI

    void UpdatePathDebugLine()
    {
        if (pathDebugLine == null || !showPathfindingDebugLine) return;
        
        // <<< FIX: Simplified and corrected logic.
        if (isHungryAndSearching && currentPath != null && currentPath.Count > 0 && currentTargetFood != null)
        {
            // Number of points is the animal's current position + remaining waypoints.
            int remainingWaypoints = currentPath.Count - currentPathIndex;
            pathDebugLine.positionCount = 1 + remainingWaypoints;

            // First point is always the animal's current ground position.
            pathDebugLine.SetPosition(0, gridEntity.GroundWorldPosition);

            // Add the rest of the waypoints from the path.
            for (int i = 0; i < remainingWaypoints; i++)
            {
                Vector3 worldPos = GridPositionManager.Instance.GridToWorld(currentPath[i + currentPathIndex]);
                pathDebugLine.SetPosition(i + 1, worldPos);
            }
        }
        else
        {
            // If not actively pathing to food, clear the line.
            pathDebugLine.positionCount = 0;
        }
    }

    void ClearPathDebugLine()
    {
        if (pathDebugLine != null)
        {
            pathDebugLine.positionCount = 0;
        }
    }
    
    void UpdateFlashEffect()
    {
        if (flashRemainingTime > 0 && spriteRenderer != null)
        {
            spriteRenderer.color = definition.damageFlashColor;
            flashRemainingTime -= Time.deltaTime;
        }
        else if (isFlashing && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            isFlashing = false;
            flashRemainingTime = 0f;
        }
    }

    void UpdateDeathFade()
    {
        if (spriteRenderer != null && definition.deathFadeTicks > 0)
        {
            float alpha = (float)deathFadeRemainingTicks / definition.deathFadeTicks;
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }

    bool CanShowThought()
    {
        return thoughtLibrary != null && thoughtBubblePrefab != null && thoughtCooldownTick <= 0;
    }

    void ShowThought(ThoughtTrigger trigger)
    {
        if (thoughtLibrary == null || thoughtLibrary.allThoughts == null) return;

        var entry = thoughtLibrary.allThoughts.FirstOrDefault(t =>
            t != null && t.speciesName == SpeciesName && t.trigger == trigger
        );

        if (entry != null && entry.lines != null && entry.lines.Count > 0)
        {
            string line = entry.lines[UnityEngine.Random.Range(0, entry.lines.Count)];
            Transform spawnT = bubbleSpawnTransform ? bubbleSpawnTransform : transform;
            GameObject bubbleGO = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity, spawnT);

            ThoughtBubbleController bubble = bubbleGO.GetComponent<ThoughtBubbleController>();
            if (bubble)
            {
                float durationInTicks = TickManager.Instance?.Config != null ?
                    2f * TickManager.Instance.Config.ticksPerRealSecond : 4f;
                bubble.Initialize(line, spawnT, durationInTicks);
                thoughtCooldownTick = definition.thoughtCooldownTicks;
            }
            else
            {
                Destroy(bubbleGO);
            }
        }
    }

    void UpdateSpriteFlipping()
    {
        if (spriteRenderer != null && gridEntity != null && gridEntity.IsMoving)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = GridPositionManager.Instance.GridToWorld(targetPosition);
            Vector2 moveDirection = (targetPos - currentPos).normalized;

            if (Mathf.Abs(moveDirection.x) > 0.01f)
            {
                spriteRenderer.flipX = moveDirection.x < 0;
            }
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;
        bool isMoving = gridEntity != null && gridEntity.IsMoving && !isEating && !isPooping;
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsEating", isEating);
    }

    void UpdateUI()
    {
        UpdateHpText();
        UpdateHungerText();
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

    void SetStatsTextVisibility(bool visible)
    {
        if (hpText != null) hpText.gameObject.SetActive(visible);
        if (hungerText != null) hungerText.gameObject.SetActive(visible);
    }

    #endregion

    #region Public Getters

    public GridPosition GetCurrentGridPosition()
    {
        return gridEntity?.Position ?? GridPosition.Zero;
    }

    #endregion
} 
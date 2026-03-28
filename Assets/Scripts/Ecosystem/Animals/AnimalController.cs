// FILE: Assets/Scripts/Ecosystem/Animals/AnimalController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using WegoSystem;
using Abracodabra.Genes;

public class AnimalController : MonoBehaviour, ITickUpdateable, IStatusEffectable, ITriggerTarget
{
    [Header("Configuration")]
    [SerializeField] public AnimalDefinition definition;

    [Header("UI & Visuals")]
    [SerializeField] GameObject thoughtBubblePrefab;
    [SerializeField] Transform bubbleSpawnTransform;
    [SerializeField] Animator animator;

    [Header("Debug")]
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;

    AnimalMovement movement;
    AnimalNeeds needs;
    AnimalBehavior behavior;
    GridEntity gridEntity;
    SpriteRenderer spriteRenderer;
    StatusEffectManager statusManager;
    StatusEffectUIManager statusEffectUI;

    bool isDying = false;
    float deathFadeTimer = 0f;
    float deathFadeDuration = 1f;
    int thoughtCooldownTick = 0;

    PlantGrowth currentEatingPlant;
    Vector2Int targetLeafCoord;
    int eatProgressTicks = 0;
    int eatRequiredTicks = 0;

    // ═══════════════════════════════════════════════════════
    //  FEAR STATE (Task 3)
    // ═══════════════════════════════════════════════════════
    bool isFeared = false;
    Vector3 fearSourcePosition;
    int fearTicksRemaining = 0;

    // ═══════════════════════════════════════════════════════
    //  IMMOBILIZE STATE (Task 7 — Traps)
    // ═══════════════════════════════════════════════════════
    int immobilizedTicksRemaining = 0;

    public bool IsFeared => isFeared;
    public bool IsImmobilized => immobilizedTicksRemaining > 0;

    /// <summary>True if any freeze-type status effect has reached max stacks (Task 4).</summary>
    public bool IsFrozen => statusManager != null && statusManager.IsFrozen;

    /// <summary>True if the creature cannot act (frozen, immobilized, or dying).</summary>
    public bool IsIncapacitated => IsFrozen || IsImmobilized || isDying;

    public GridEntity GridEntity => gridEntity;
    public StatusEffectManager StatusManager => statusManager;
    public AnimalDefinition Definition => definition;
    public AnimalMovement Movement => movement;
    public AnimalNeeds Needs => needs;
    public AnimalBehavior Behavior => behavior;
    public bool IsDying => isDying;
    public string SpeciesName => definition != null ? definition.animalName : "Uninitialized";

    void Awake()
    {
        CacheComponents();
        ValidateComponents();
    }

    void Start()
    {
        InitializeAnimal();

        if (TickManager.Instance == null)
        {
            Debug.LogError($"[{GetType().Name}] TickManager not found! Disabling component.", this);
            enabled = false;
            return;
        }

        TickManager.Instance.RegisterTickUpdateable(this);

        if (gridEntity != null)
        {
            gridEntity.OnPositionChanged += OnGridPositionChanged;
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

        if (gridEntity != null)
        {
            gridEntity.OnPositionChanged -= OnGridPositionChanged;
        }
    }

    void Update()
    {
        if (!enabled) return;

        if (isDying && deathFadeTimer > 0)
        {
            deathFadeTimer -= Time.deltaTime;
            UpdateDeathFade();
            if (deathFadeTimer <= 0) Destroy(gameObject);
            return;
        }

        bool showStats = Input.GetKey(showStatsKey);
        SetStatsTextVisibility(showStats);
        UpdateSpriteFlipping();
        movement.UpdateVisuals();
    }

    void LateUpdate()
    {
        transform.position = PixelGridSnapper.SnapToGrid(transform.position);
    }

    public void OnTickUpdate(int currentTick)
    {
        if (!enabled || definition == null) return;

        if (!isDying && needs != null && needs.CurrentHealth <= 0)
        {
            StartDying();
            return;
        }

        if (isDying) return;

        // Status effects always tick (freeze decay, poison damage, etc.)
        statusManager.OnTickUpdate(currentTick);

        // ── FROZEN or IMMOBILIZED: creature cannot act ──
        if (IsFrozen || IsImmobilized)
        {
            // Tick down immobilize
            if (immobilizedTicksRemaining > 0)
            {
                immobilizedTicksRemaining--;
                if (immobilizedTicksRemaining <= 0)
                {
                    Debug.Log($"[Immobilize] '{SpeciesName}' freed from immobilize.");
                }
            }

            // Cancel any active behavior
            if (behavior.IsEating) behavior.CancelCurrentAction();
            if (currentEatingPlant != null) ResetEatingState();

            // Needs still tick (hunger, starvation damage)
            needs.OnTickUpdate(currentTick);

            if (gridEntity != null && statusManager != null)
            {
                gridEntity.SetSpeedMultiplier(statusManager.VisualInterpolationSpeedMultiplier);
            }

            if (thoughtCooldownTick > 0) thoughtCooldownTick--;
            UpdateAnimations();
            return; // Skip movement, behavior, pest attack, fear
        }

        needs.OnTickUpdate(currentTick);
        behavior.OnTickUpdate(currentTick);

        // Tick fear state BEFORE movement so flee direction is set
        TickFear();

        movement.OnTickUpdate(currentTick);

        if (gridEntity != null && statusManager != null)
        {
            gridEntity.SetSpeedMultiplier(statusManager.VisualInterpolationSpeedMultiplier);
        }

        if (thoughtCooldownTick > 0) thoughtCooldownTick--;

        // Pests don't attack while feared
        if (definition.isPest && !isDying && !isFeared)
        {
            HandlePestAttack();
        }

        UpdateAnimations();
    }

    // ═══════════════════════════════════════════════════════
    //  IMMOBILIZE — Public API (Task 7)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Immobilize (root) the creature for the given number of ticks.
    /// Used by TrapWorldEffect. Refreshes duration if already immobilized.
    /// </summary>
    public void ApplyImmobilize(int durationTicks)
    {
        if (isDying) return;

        immobilizedTicksRemaining = Mathf.Max(immobilizedTicksRemaining, durationTicks);

        // Cancel current actions
        ResetEatingState();
        behavior.CancelCurrentAction();
        movement.ClearMovementPlan();

        // Clear fear — can't flee while rooted
        if (isFeared)
        {
            isFeared = false;
            fearTicksRemaining = 0;
            movement.StopFleeing();
        }

        Debug.Log($"[Immobilize] '{SpeciesName}' immobilized for {durationTicks} ticks.");
    }

    // ═══════════════════════════════════════════════════════
    //  FEAR — Public API (Task 3)
    // ═══════════════════════════════════════════════════════

    public void ApplyFear(Vector3 sourcePosition, int durationTicks)
    {
        if (isDying) return;
        if (IsFrozen || IsImmobilized) return; // Can't be feared while incapacitated

        if (definition != null && definition.immuneToFear) return;

        fearSourcePosition = sourcePosition;
        fearTicksRemaining = durationTicks;

        if (!isFeared)
        {
            isFeared = true;
            ResetEatingState();
            behavior.CancelCurrentAction();
            movement.ClearMovementPlan();
            movement.StartFleeing(fearSourcePosition);

            if (CanShowThought())
            {
                ShowThought(ThoughtTrigger.Fleeing);
            }

            Debug.Log($"[Fear] '{SpeciesName}' feared! Fleeing from {sourcePosition} for {durationTicks} ticks.");
        }
        else
        {
            movement.UpdateFleeSource(fearSourcePosition);
            Debug.Log($"[Fear] '{SpeciesName}' fear refreshed. {durationTicks} ticks remaining.");
        }
    }

    void TickFear()
    {
        if (!isFeared) return;

        fearTicksRemaining--;

        if (fearTicksRemaining <= 0)
        {
            isFeared = false;
            movement.StopFleeing();
            Debug.Log($"[Fear] '{SpeciesName}' recovered from fear. Resuming normal behavior.");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PEST ATTACK
    // ═══════════════════════════════════════════════════════

    void HandlePestAttack()
    {
        if (currentEatingPlant != null)
        {
            bool targetInvalid =
                currentEatingPlant.CurrentState == PlantState.Dead ||
                currentEatingPlant.CurrentState == PlantState.Withering ||
                currentEatingPlant.ActiveLeafCount <= 0;

            if (!targetInvalid && !currentEatingPlant.CellManager.HasCellAt(targetLeafCoord))
            {
                targetInvalid = true;
            }

            if (targetInvalid)
            {
                ResetEatingState();
            }
        }

        if (currentEatingPlant == null)
        {
            PlantGrowth plant = FindAttackablePlant();
            if (plant == null) return;

            var activeLeaves = plant.CellManager.LeafDataList.Where(l => l.IsActive).ToList();
            if (activeLeaves.Count == 0) return;

            var chosenLeaf = activeLeaves[Random.Range(0, activeLeaves.Count)];

            currentEatingPlant = plant;
            targetLeafCoord = chosenLeaf.GridCoord;
            eatProgressTicks = 0;
            eatRequiredTicks = Mathf.CeilToInt(definition.baseEatSpeedTicks * plant.leafDurabilityMultiplier);

            Debug.Log($"[AnimalController] Pest '{SpeciesName}' targeting leaf at {targetLeafCoord} on '{plant.name}'. Eat time: {eatRequiredTicks} ticks.");
        }

        // Freeze slows eating: at 3+ stacks, eating speed is doubled
        int freezeStacks = statusManager != null ? statusManager.GetStackCount("freeze") : 0;
        int eatSpeedMultiplier = freezeStacks >= 3 ? 2 : 1;

        eatProgressTicks++;

        if (eatProgressTicks >= eatRequiredTicks * eatSpeedMultiplier)
        {
            GameObject cellObj = currentEatingPlant.GetCellGameObjectAt(targetLeafCoord);
            if (cellObj != null)
            {
                PlantCell plantCell = cellObj.GetComponent<PlantCell>();
                if (plantCell != null)
                {
                    PlantGrowth eatenPlant = currentEatingPlant;
                    currentEatingPlant.HandleBeingEaten(this, plantCell);
                    Debug.Log($"[AnimalController] Pest '{SpeciesName}' ate a leaf from '{eatenPlant.name}'. Remaining: {eatenPlant.ActiveLeafCount}");
                }
            }

            ResetEatingState();
        }
    }

    void ResetEatingState()
    {
        currentEatingPlant = null;
        targetLeafCoord = Vector2Int.zero;
        eatProgressTicks = 0;
        eatRequiredTicks = 0;
    }

    PlantGrowth FindAttackablePlant()
    {
        float rangeSqr = definition.attackRangeTiles * definition.attackRangeTiles;

        PlantGrowth closest = null;
        float closestDistSqr = float.MaxValue;

        foreach (var plant in PlantGrowth.AllActivePlants)
        {
            if (plant == null) continue;
            if (plant.CurrentState == PlantState.Dead) continue;
            if (plant.CurrentState == PlantState.Withering) continue;
            if (plant.ActiveLeafCount <= 0) continue;

            float distSqr = (plant.transform.position - transform.position).sqrMagnitude;
            if (distSqr <= rangeSqr && distSqr < closestDistSqr)
            {
                closest = plant;
                closestDistSqr = distSqr;
            }
        }

        return closest;
    }

    // ═══════════════════════════════════════════════════════
    //  INTERFACE IMPLEMENTATIONS
    // ═══════════════════════════════════════════════════════

    void OnGridPositionChanged(GridPosition oldPos, GridPosition newPos) { }

    public string GetDisplayName() { return SpeciesName; }
    public void Heal(float amount) { if (needs != null) needs.Heal(amount); }
    public void ModifyHunger(float amount) { if (needs != null) needs.ModifyHunger(amount); }

    public void TakeDamage(float amount)
    {
        if (isDying) return;
        float finalDamage = amount;
        if (statusManager != null)
        {
            finalDamage *= statusManager.DamageResistanceMultiplier;
        }
        needs.TakeDamage(finalDamage);
    }

    // ═══════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═══════════════════════════════════════════════════════

    void CacheComponents()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null) gridEntity = gameObject.AddComponent<GridEntity>();

        movement = GetComponent<AnimalMovement>();
        if (movement == null) movement = gameObject.AddComponent<AnimalMovement>();

        needs = GetComponent<AnimalNeeds>();
        if (needs == null) needs = gameObject.AddComponent<AnimalNeeds>();

        behavior = GetComponent<AnimalBehavior>();
        if (behavior == null) behavior = gameObject.AddComponent<AnimalBehavior>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        statusManager = GetComponent<StatusEffectManager>();
        if (statusManager == null) statusManager = gameObject.AddComponent<StatusEffectManager>();

        statusEffectUI = GetComponentInChildren<StatusEffectUIManager>(true);
        if (statusEffectUI == null)
            Debug.LogWarning($"StatusEffectUIManager not found on a child of {gameObject.name}. Icons will not display.", this);
    }

    void ValidateComponents()
    {
        if (definition == null) { Debug.LogError($"[{gameObject.name}] Missing AnimalDefinition!", this); enabled = false; return; }
        if (definition.diet == null) { Debug.LogError($"[{gameObject.name}] AnimalDefinition missing diet!", this); enabled = false; return; }
    }

    void InitializeAnimal()
    {
        movement.Initialize(this, definition);
        needs.Initialize(this, definition);
        behavior.Initialize(this, definition);
        statusManager.Initialize(this);

        if (statusEffectUI != null)
        {
            statusEffectUI.Initialize(statusManager);
        }

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[AnimalController] {gameObject.name} snapped to grid position {gridEntity.Position}");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  DEATH
    // ═══════════════════════════════════════════════════════

    void StartDying()
    {
        if (isDying) return;
        isDying = true;

        isFeared = false;
        fearTicksRemaining = 0;
        immobilizedTicksRemaining = 0;

        if (TickManager.Instance?.Config != null)
        {
            deathFadeDuration = definition.deathFadeTicks / TickManager.Instance.Config.ticksPerRealSecond;
        }
        else
        {
            deathFadeDuration = definition.deathFadeTicks * 0.5f;
        }
        deathFadeTimer = deathFadeDuration;

        Debug.Log($"[AnimalController] {SpeciesName} is dying! Duration: {deathFadeDuration}s");

        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }

        movement.StopAllMovement();
        behavior.CancelCurrentAction();

        if (movement != null) movement.enabled = false;
        if (behavior != null) behavior.enabled = false;
        if (needs != null) needs.enabled = false;
    }

    void UpdateDeathFade()
    {
        if (spriteRenderer == null) return;

        float fadeProgress = 1f - (deathFadeTimer / deathFadeDuration);
        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(1f, 0f, fadeProgress);
        spriteRenderer.color = color;
    }

    // ═══════════════════════════════════════════════════════
    //  THOUGHTS & UI
    // ═══════════════════════════════════════════════════════

    public void ShowThought(ThoughtTrigger trigger)
    {
        if (!CanShowThought()) return;

        thoughtCooldownTick = definition.thoughtCooldownTicks;
        if (definition.thoughtLibrary == null || thoughtBubblePrefab == null) return;

        string message = "";
        switch (trigger)
        {
            case ThoughtTrigger.Hungry:
                message = definition.thoughtLibrary?.hungryThoughts?.Length > 0
                    ? definition.thoughtLibrary.hungryThoughts[Random.Range(0, definition.thoughtLibrary.hungryThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Eating:
                message = definition.thoughtLibrary?.eatingThoughts?.Length > 0
                    ? definition.thoughtLibrary.eatingThoughts[Random.Range(0, definition.thoughtLibrary.eatingThoughts.Length)] : "";
                break;
            case ThoughtTrigger.HealthLow:
                message = definition.thoughtLibrary?.healthLowThoughts?.Length > 0
                    ? definition.thoughtLibrary.healthLowThoughts[Random.Range(0, definition.thoughtLibrary.healthLowThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Fleeing:
                message = definition.thoughtLibrary?.fleeingThoughts?.Length > 0
                    ? definition.thoughtLibrary.fleeingThoughts[Random.Range(0, definition.thoughtLibrary.fleeingThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Pooping:
                message = definition.thoughtLibrary?.poopingThoughts?.Length > 0
                    ? definition.thoughtLibrary.poopingThoughts[Random.Range(0, definition.thoughtLibrary.poopingThoughts.Length)] : "";
                break;
        }

        if (!string.IsNullOrEmpty(message))
        {
            Transform spawnT = bubbleSpawnTransform != null ? bubbleSpawnTransform : transform;
            GameObject bubble = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity);
            ThoughtBubbleController bubbleCtrl = bubble.GetComponent<ThoughtBubbleController>();
            if (bubbleCtrl != null)
            {
                bubbleCtrl.Initialize(message, spawnT, 3f);
            }
        }
    }

    public bool CanShowThought()
    {
        return thoughtCooldownTick <= 0 && !isDying;
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = gridEntity != null && gridEntity.IsMoving;
        bool isEating = behavior != null && behavior.IsEating;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isEating", isEating);
        animator.SetBool("isDying", isDying);
    }

    void UpdateSpriteFlipping()
    {
        if (spriteRenderer == null || movement == null) return;

        Vector2 moveDirection = movement.GetLastMoveDirection();
        if (Mathf.Abs(moveDirection.x) > 0.01f)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    void SetStatsTextVisibility(bool visible)
    {
        if (hpText != null) hpText.gameObject.SetActive(visible);
        if (hungerText != null) hungerText.gameObject.SetActive(visible);

        if (visible) UpdateUI();
    }

    public void UpdateUI()
    {
        if (needs == null) return;

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(needs.CurrentHealth)}/{Mathf.CeilToInt(definition.maxHealth)}";
        }
        if (hungerText != null)
        {
            hungerText.text = $"{Mathf.CeilToInt(needs.CurrentHunger)}/{Mathf.CeilToInt(definition.diet.maxHunger)}";
        }
    }

    public void SetSeekingScreenCenter(Vector2 target, Vector2 minBounds, Vector2 maxBounds)
    {
        movement.SetSeekingScreenCenter(target, minBounds, maxBounds);
    }
}
// Assets/Scripts/Ecosystem/Animals/AnimalController.cs
using System;
using System.Collections.Generic;
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

    // TASK 8: attack cooldown tracker
    int attackCooldownRemaining = 0;

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

        needs.OnTickUpdate(currentTick);
        behavior.OnTickUpdate(currentTick);
        movement.OnTickUpdate(currentTick);
        statusManager.OnTickUpdate(currentTick);

        if (gridEntity != null && statusManager != null)
        {
            gridEntity.SetSpeedMultiplier(statusManager.VisualInterpolationSpeedMultiplier);
        }

        if (thoughtCooldownTick > 0) thoughtCooldownTick--;

        // ── TASK 8: Pest deals damage to adjacent plants ──
        if (definition.isPest && !isDying)
        {
            HandlePestAttack();
        }

        UpdateAnimations();
    }

    // ──────────────────────────────────────────────────────────
    // TASK 8: Pest Attack Logic
    // ──────────────────────────────────────────────────────────

    void HandlePestAttack()
    {
        if (attackCooldownRemaining > 0)
        {
            attackCooldownRemaining--;
            return;
        }

        PlantGrowth targetPlant = FindAttackablePlant();
        if (targetPlant == null) return;

        targetPlant.TakeDamage(definition.attackDamage);
        attackCooldownRemaining = definition.attackCooldownTicks;

        Debug.Log($"[AnimalController] Pest '{SpeciesName}' attacked plant '{targetPlant.name}' for {definition.attackDamage} damage. Plant HP: {targetPlant.currentHP:F0}");
    }

    PlantGrowth FindAttackablePlant()
    {
        float rangeSqr = definition.attackRangeTiles * definition.attackRangeTiles;

        foreach (var plant in PlantGrowth.AllActivePlants)
        {
            if (plant == null) continue;
            if (plant.CurrentState == PlantState.Dead) continue;

            float distSqr = (plant.transform.position - transform.position).sqrMagnitude;
            if (distSqr <= rangeSqr)
            {
                return plant;
            }
        }

        return null;
    }

    // ──────────────────────────────────────────────────────────
    // Existing methods (unchanged)
    // ──────────────────────────────────────────────────────────

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

    void StartDying()
    {
        if (isDying) return;
        isDying = true;

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

    public void ShowThought(ThoughtTrigger trigger) {
        if (!CanShowThought()) return;

        thoughtCooldownTick = definition.thoughtCooldownTicks;
        if (definition.thoughtLibrary == null || thoughtBubblePrefab == null) return;

        string message = "";
        switch (trigger) {
            case ThoughtTrigger.Hungry:
                message = definition.thoughtLibrary?.hungryThoughts?.Length > 0
                    ? definition.thoughtLibrary.hungryThoughts[UnityEngine.Random.Range(0, definition.thoughtLibrary.hungryThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Eating:
                message = definition.thoughtLibrary?.eatingThoughts?.Length > 0
                    ? definition.thoughtLibrary.eatingThoughts[UnityEngine.Random.Range(0, definition.thoughtLibrary.eatingThoughts.Length)] : "";
                break;
            case ThoughtTrigger.HealthLow:
                message = definition.thoughtLibrary?.healthLowThoughts?.Length > 0
                    ? definition.thoughtLibrary.healthLowThoughts[UnityEngine.Random.Range(0, definition.thoughtLibrary.healthLowThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Fleeing:
                message = definition.thoughtLibrary?.fleeingThoughts?.Length > 0
                    ? definition.thoughtLibrary.fleeingThoughts[UnityEngine.Random.Range(0, definition.thoughtLibrary.fleeingThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Pooping:
                message = definition.thoughtLibrary?.poopingThoughts?.Length > 0
                    ? definition.thoughtLibrary.poopingThoughts[UnityEngine.Random.Range(0, definition.thoughtLibrary.poopingThoughts.Length)] : "";
                break;
        }

        if (!string.IsNullOrEmpty(message)) {
            Transform spawnT = bubbleSpawnTransform != null ? bubbleSpawnTransform : transform;
            GameObject bubble = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity);
            ThoughtBubbleController bubbleCtrl = bubble.GetComponent<ThoughtBubbleController>();
            if (bubbleCtrl != null) {
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

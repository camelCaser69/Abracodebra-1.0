// Assets/Scripts/Ecosystem/Animals/AnimalController.cs
using TMPro;
using UnityEngine;
using WegoSystem;

// CORRECTED: Implemented the new ITriggerTarget marker interface.
public class AnimalController : MonoBehaviour, ITickUpdateable, IStatusEffectable, ITriggerTarget
{
    [Header("Configuration")]
    [SerializeField] public AnimalDefinition definition;

    [Header("UI & Visuals")]
    [SerializeField] private GameObject thoughtBubblePrefab;
    [SerializeField] private Transform bubbleSpawnTransform;
    [SerializeField] private Animator animator;

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private KeyCode showStatsKey = KeyCode.LeftAlt;

    // Component References
    private AnimalMovement movement;
    private AnimalNeeds needs;
    private AnimalBehavior behavior;
    private GridEntity gridEntity;
    private SpriteRenderer spriteRenderer;
    private StatusEffectManager statusManager;
    private StatusEffectUIManager statusEffectUI;

    // State
    private bool isDying = false;
    private float deathFadeTimer = 0f;
    private float deathFadeDuration = 1f;
    private int thoughtCooldownTick = 0;

    #region Properties
    public GridEntity GridEntity => gridEntity;
    public StatusEffectManager StatusManager => statusManager;
    public AnimalDefinition Definition => definition;
    public AnimalMovement Movement => movement;
    public AnimalNeeds Needs => needs;
    public AnimalBehavior Behavior => behavior;
    public bool IsDying => isDying;
    public string SpeciesName => definition != null ? definition.animalName : "Uninitialized";
    #endregion

    #region Unity Lifecycle
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
    #endregion

    private void LateUpdate()
    {
        // Snap final visual position to the pixel grid after all movement calculations.
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
            gridEntity.SetSpeedMultiplier(statusManager.VisualSpeedMultiplier);
        }

        if (thoughtCooldownTick > 0) thoughtCooldownTick--;
        UpdateAnimations();
    }

    private void OnGridPositionChanged(GridPosition oldPos, GridPosition newPos)
    {
        // Future logic can go here
    }

    #region IStatusEffectable Implementation
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
    #endregion

    private void CacheComponents()
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
        if (statusEffectUI == null) Debug.LogWarning($"StatusEffectUIManager not found on a child of {gameObject.name}. Icons will not display.", this);
    }

    private void ValidateComponents()
    {
        if (definition == null) { Debug.LogError($"[{gameObject.name}] Missing AnimalDefinition!", this); enabled = false; return; }
        if (definition.diet == null) { Debug.LogError($"[{gameObject.name}] AnimalDefinition missing diet!", this); enabled = false; return; }
    }

    private void InitializeAnimal()
    {
        movement.Initialize(this, definition);
        needs.Initialize(this, definition);
        behavior.Initialize(this, definition);
        statusManager.Initialize(this);
        
        // This line is now restored and placed correctly.
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

    private void StartDying()
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

    private void UpdateDeathFade()
    {
        if (spriteRenderer == null) return;

        float fadeProgress = 1f - (deathFadeTimer / deathFadeDuration);
        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(1f, 0f, fadeProgress);
        spriteRenderer.color = color;
    }

    public void ShowThought(ThoughtTrigger trigger)
    {
        if (!CanShowThought()) return;

        thoughtCooldownTick = definition.thoughtCooldownTicks;
        if (definition.thoughtLibrary == null || thoughtBubblePrefab == null) return;

        string message = "";
        switch (trigger)
        {
            case ThoughtTrigger.Hungry:
                message = definition.thoughtLibrary?.hungryThoughts?.Length > 0 ? definition.thoughtLibrary.hungryThoughts[Random.Range(0, definition.thoughtLibrary.hungryThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Eating:
                message = definition.thoughtLibrary?.eatingThoughts?.Length > 0 ? definition.thoughtLibrary.eatingThoughts[Random.Range(0, definition.thoughtLibrary.eatingThoughts.Length)] : "";
                break;
            case ThoughtTrigger.HealthLow:
                message = definition.thoughtLibrary?.healthLowThoughts?.Length > 0 ? definition.thoughtLibrary.healthLowThoughts[Random.Range(0, definition.thoughtLibrary.healthLowThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Fleeing:
                message = definition.thoughtLibrary?.fleeingThoughts?.Length > 0 ? definition.thoughtLibrary.fleeingThoughts[Random.Range(0, definition.thoughtLibrary.fleeingThoughts.Length)] : "";
                break;
            case ThoughtTrigger.Pooping:
                message = definition.thoughtLibrary?.poopingThoughts?.Length > 0 ? definition.thoughtLibrary.poopingThoughts[Random.Range(0, definition.thoughtLibrary.poopingThoughts.Length)] : "";
                break;
        }

        if (!string.IsNullOrEmpty(message))
        {
            Transform spawnT = bubbleSpawnTransform != null ? bubbleSpawnTransform : transform;
            GameObject bubble = Instantiate(thoughtBubblePrefab, spawnT.position, Quaternion.identity);
            ThoughtBubbleController controller = bubble.GetComponent<ThoughtBubbleController>();
            if (controller != null)
            {
                controller.Initialize(message, spawnT, 3f);
            }
        }
    }

    public bool CanShowThought()
    {
        return thoughtCooldownTick <= 0 && !isDying;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = gridEntity != null && gridEntity.IsMoving;
        bool isEating = behavior != null && behavior.IsEating;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isEating", isEating);
        animator.SetBool("isDying", isDying);
    }

    private void UpdateSpriteFlipping()
    {
        if (spriteRenderer == null || movement == null) return;

        Vector2 moveDirection = movement.GetLastMoveDirection();
        if (Mathf.Abs(moveDirection.x) > 0.01f)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    private void SetStatsTextVisibility(bool visible)
    {
        if (hpText != null) hpText.gameObject.SetActive(visible);
        if (hungerText != null) hungerText.gameObject.SetActive(visible);

        if (visible)
        {
            UpdateUI();
        }
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
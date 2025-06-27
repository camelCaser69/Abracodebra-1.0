using System.Collections.Generic;
using UnityEngine;
using TMPro;
using WegoSystem;

[RequireComponent(typeof(AnimalMovement))]
[RequireComponent(typeof(AnimalNeeds))]
[RequireComponent(typeof(AnimalBehavior))]
public class AnimalController : MonoBehaviour, ITickUpdateable
{
    [Header("Configuration")]
    [SerializeField] public AnimalDefinition definition;
    
    [Header("Visual Components")]
    [SerializeField] GameObject thoughtBubblePrefab;
    [SerializeField] Transform bubbleSpawnTransform;
    [SerializeField] Animator animator;
    
    [Header("UI")]
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;
    
    // Component References
    private AnimalMovement movement;
    private AnimalNeeds needs;
    private AnimalBehavior behavior;
    private GridEntity gridEntity;
    private SpriteRenderer spriteRenderer;
    
    // State
    private bool isDying = false;
    private int deathFadeRemainingTicks = 0;
    private int thoughtCooldownTick = 0;
    
    // Properties
    public AnimalDefinition Definition => definition;
    public GridEntity GridEntity => gridEntity;
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
        movement.UpdateVisuals();
    }
    
    public void OnTickUpdate(int currentTick)
    {
        if (!enabled || definition == null || isDying) return;
        
        // Update components
        needs.OnTickUpdate(currentTick);
        behavior.OnTickUpdate(currentTick);
        movement.OnTickUpdate(currentTick);
        
        // Handle death fade
        if (deathFadeRemainingTicks > 0)
        {
            deathFadeRemainingTicks--;
            UpdateDeathFade();
            if (deathFadeRemainingTicks <= 0)
            {
                Destroy(gameObject);
            }
        }
        
        // Update thought cooldown
        if (thoughtCooldownTick > 0)
        {
            thoughtCooldownTick--;
        }
        
        UpdateAnimations();
    }
    
    private void CacheComponents()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
        
        movement = GetComponent<AnimalMovement>();
        if (movement == null)
        {
            movement = gameObject.AddComponent<AnimalMovement>();
        }
        
        needs = GetComponent<AnimalNeeds>();
        if (needs == null)
        {
            needs = gameObject.AddComponent<AnimalNeeds>();
        }
        
        behavior = GetComponent<AnimalBehavior>();
        if (behavior == null)
        {
            behavior = gameObject.AddComponent<AnimalBehavior>();
        }
        
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    
    private void ValidateComponents()
    {
        if (definition == null)
        {
            Debug.LogError($"[{gameObject.name}] Missing AnimalDefinition!", this);
            enabled = false;
            return;
        }
        
        if (definition.diet == null)
        {
            Debug.LogError($"[{gameObject.name}] AnimalDefinition missing diet!", this);
            enabled = false;
            return;
        }
    }
    
    private void InitializeAnimal()
    {
        // Initialize components with definition
        movement.Initialize(this, definition);
        needs.Initialize(this, definition);
        behavior.Initialize(this, definition);
        
        // Snap to grid
        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[AnimalController] {gameObject.name} snapped to grid position {gridEntity.Position}");
        }
    }
    
    public void TakeDamage(float amount)
    {
        needs.TakeDamage(amount);
        
        if (needs.CurrentHealth <= 0 && !isDying)
        {
            StartDying();
        }
    }
    
    private void StartDying()
    {
        isDying = true;
        deathFadeRemainingTicks = definition.deathFadeTicks;
        
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius(this);
        }
        
        movement.StopAllMovement();
    }
    
    private void UpdateDeathFade()
    {
        if (spriteRenderer == null || deathFadeRemainingTicks <= 0) return;
        
        float fadeProgress = 1f - (deathFadeRemainingTicks / (float)definition.deathFadeTicks);
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
        
        // Get thought message based on trigger
        switch (trigger)
        {
            case ThoughtTrigger.Hungry:
                message = definition.thoughtLibrary?.hungryThoughts?.Length > 0 
                    ? definition.thoughtLibrary.hungryThoughts[Random.Range(0, definition.thoughtLibrary.hungryThoughts.Length)]
                    : "";
                break;
            case ThoughtTrigger.Eating:
                message = definition.thoughtLibrary?.eatingThoughts?.Length > 0
                    ? definition.thoughtLibrary.eatingThoughts[Random.Range(0, definition.thoughtLibrary.eatingThoughts.Length)]
                    : "";
                break;
            case ThoughtTrigger.HealthLow:
                message = definition.thoughtLibrary?.healthLowThoughts?.Length > 0
                    ? definition.thoughtLibrary.healthLowThoughts[Random.Range(0, definition.thoughtLibrary.healthLowThoughts.Length)]
                    : "";
                break;
            case ThoughtTrigger.Fleeing:
                message = definition.thoughtLibrary?.fleeingThoughts?.Length > 0
                    ? definition.thoughtLibrary.fleeingThoughts[Random.Range(0, definition.thoughtLibrary.fleeingThoughts.Length)]
                    : "";
                break;
            case ThoughtTrigger.Pooping:
                message = definition.thoughtLibrary?.poopingThoughts?.Length > 0
                    ? definition.thoughtLibrary.poopingThoughts[Random.Range(0, definition.thoughtLibrary.poopingThoughts.Length)]
                    : "";
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
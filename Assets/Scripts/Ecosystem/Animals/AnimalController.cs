// Assets/Scripts/Ecosystem/Animals/AnimalController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using WegoSystem;

// Implement the new interface here
public class AnimalController : MonoBehaviour, ITickUpdateable, IStatusEffectable 
{
    [SerializeField] public AnimalDefinition definition;

    [Header("Status Effect Integration")] 
    [SerializeField] private StatusEffect wetStatusEffect; 
    [SerializeField] private TileDefinition waterTileDefinition; 

    [Header("Component References")]
    [SerializeField] GameObject thoughtBubblePrefab;
    [SerializeField] Transform bubbleSpawnTransform;
    [SerializeField] Animator animator;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hungerText;
    [SerializeField] KeyCode showStatsKey = KeyCode.LeftAlt;

    AnimalMovement movement;
    AnimalNeeds needs;
    AnimalBehavior behavior;
    GridEntity gridEntity;
    SpriteRenderer spriteRenderer;
    StatusEffectManager statusManager; // Renamed from AnimalStatusEffectManager
    StatusEffectUIManager statusEffectUI;


    bool isDying = false;
    float deathFadeTimer = 0f;
    float deathFadeDuration = 1f; 
    int thoughtCooldownTick = 0;

    // --- Interface Properties ---
    public GridEntity GridEntity => gridEntity;
    public StatusEffectManager StatusManager => statusManager;
    
    // --- Public Properties ---
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

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        
        if (gridEntity != null)
        {
            gridEntity.OnPositionChanged += CheckTileForStatusEffect;
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
            gridEntity.OnPositionChanged -= CheckTileForStatusEffect;
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
            gridEntity.SetSpeedMultiplier(statusManager.MovementSpeedMultiplier);
        }

        if (thoughtCooldownTick > 0) thoughtCooldownTick--;

        UpdateAnimations();
    }
    
    // --- Interface Methods ---
    public string GetDisplayName()
    {
        return SpeciesName;
    }

    public void Heal(float amount)
    {
        if (needs != null) needs.Heal(amount);
    }
    
    public void ModifyHunger(float amount)
    {
        if (needs != null) needs.ModifyHunger(amount);
    }
    // --- End Interface Methods ---
    
    private void CheckTileForStatusEffect(GridPosition oldPos, GridPosition newPos)
    {
        if (wetStatusEffect == null || waterTileDefinition == null || TileInteractionManager.Instance == null) return;
        TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(newPos.ToVector3Int());
        if (currentTile == waterTileDefinition)
        {
            statusManager.ApplyStatusEffect(wetStatusEffect);
        }
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
        if (statusEffectUI == null) Debug.LogWarning($"StatusEffectUIManager not found on a child of {gameObject.name}. Icons will not display.", this);
    }

    void ValidateComponents()
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

    void InitializeAnimal()
    {
        movement.Initialize(this, definition);
        needs.Initialize(this, definition);
        behavior.Initialize(this, definition);
        statusManager.Initialize(this); // Pass 'this' as the IStatusEffectable
        
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

    public void TakeDamage(float amount)
    {
        if (isDying) return;

        // Calculate the final damage here, applying the resistance multiplier.
        float finalDamage = amount;
        if (statusManager != null)
        {
            finalDamage *= statusManager.DamageResistanceMultiplier;
        }
    
        // Pass the final, calculated damage to the Needs component.
        needs.TakeDamage(finalDamage);
    }

    void StartDying()
    {
        if (isDying) return;
        isDying = true;

        if (TickManager.Instance?.Config != null) deathFadeDuration = definition.deathFadeTicks / TickManager.Instance.Config.ticksPerRealSecond;
        else deathFadeDuration = definition.deathFadeTicks * 0.5f;

        deathFadeTimer = deathFadeDuration;
        Debug.Log($"[AnimalController] {SpeciesName} is dying! Duration: {deathFadeDuration}s");
        
        if (GridDebugVisualizer.Instance != null) GridDebugVisualizer.Instance.HideContinuousRadius(this);

        movement.StopAllMovement();
        behavior.CancelCurrentAction();

        if (movement != null) movement.enabled = false;
        if (behavior != null) behavior.enabled = false;
        if (needs != null) needs.enabled = false;
    }
    
    // ... (All other methods like UpdateDeathFade, ShowThought, UpdateAnimations, etc. remain the same) ...
    // Note: I'm omitting them here for brevity but they are included in the copy-paste block
    void UpdateDeathFade(){if(spriteRenderer==null)return;float fadeProgress=1f-(deathFadeTimer/deathFadeDuration);Color color=spriteRenderer.color;color.a=Mathf.Lerp(1f,0f,fadeProgress);spriteRenderer.color=color;}
    public void ShowThought(ThoughtTrigger trigger){if(!CanShowThought())return;thoughtCooldownTick=definition.thoughtCooldownTicks;if(definition.thoughtLibrary==null||thoughtBubblePrefab==null)return;string message="";switch(trigger){case ThoughtTrigger.Hungry:message=definition.thoughtLibrary?.hungryThoughts?.Length>0?definition.thoughtLibrary.hungryThoughts[Random.Range(0,definition.thoughtLibrary.hungryThoughts.Length)]:"";break;case ThoughtTrigger.Eating:message=definition.thoughtLibrary?.eatingThoughts?.Length>0?definition.thoughtLibrary.eatingThoughts[Random.Range(0,definition.thoughtLibrary.eatingThoughts.Length)]:"";break;case ThoughtTrigger.HealthLow:message=definition.thoughtLibrary?.healthLowThoughts?.Length>0?definition.thoughtLibrary.healthLowThoughts[Random.Range(0,definition.thoughtLibrary.healthLowThoughts.Length)]:"";break;case ThoughtTrigger.Fleeing:message=definition.thoughtLibrary?.fleeingThoughts?.Length>0?definition.thoughtLibrary.fleeingThoughts[Random.Range(0,definition.thoughtLibrary.fleeingThoughts.Length)]:"";break;case ThoughtTrigger.Pooping:message=definition.thoughtLibrary?.poopingThoughts?.Length>0?definition.thoughtLibrary.poopingThoughts[Random.Range(0,definition.thoughtLibrary.poopingThoughts.Length)]:"";break;}
    if(!string.IsNullOrEmpty(message)){Transform spawnT=bubbleSpawnTransform!=null?bubbleSpawnTransform:transform;GameObject bubble=Instantiate(thoughtBubblePrefab,spawnT.position,Quaternion.identity);ThoughtBubbleController controller=bubble.GetComponent<ThoughtBubbleController>();if(controller!=null){controller.Initialize(message,spawnT,3f);}}}
    public bool CanShowThought(){return thoughtCooldownTick<=0&&!isDying;}
    void UpdateAnimations(){if(animator==null)return;bool isMoving=gridEntity!=null&&gridEntity.IsMoving;bool isEating=behavior!=null&&behavior.IsEating;animator.SetBool("isMoving",isMoving);animator.SetBool("isEating",isEating);animator.SetBool("isDying",isDying);}
    void UpdateSpriteFlipping(){if(spriteRenderer==null||movement==null)return;Vector2 moveDirection=movement.GetLastMoveDirection();if(Mathf.Abs(moveDirection.x)>0.01f){spriteRenderer.flipX=moveDirection.x<0;}}
    void SetStatsTextVisibility(bool visible){if(hpText!=null)hpText.gameObject.SetActive(visible);if(hungerText!=null)hungerText.gameObject.SetActive(visible);if(visible){UpdateUI();}}
    public void UpdateUI(){if(needs==null)return;if(hpText!=null){hpText.text=$"{Mathf.CeilToInt(needs.CurrentHealth)}/{Mathf.CeilToInt(definition.maxHealth)}";}
    if(hungerText!=null){hungerText.text=$"{Mathf.CeilToInt(needs.CurrentHunger)}/{Mathf.CeilToInt(definition.diet.maxHunger)}";}}
    public void SetSeekingScreenCenter(Vector2 target,Vector2 minBounds,Vector2 maxBounds){movement.SetSeekingScreenCenter(target,minBounds,maxBounds);}
}
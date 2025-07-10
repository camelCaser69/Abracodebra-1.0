// Assets/Scripts/WorldInteraction/Player/GardenerController.cs
using System.Collections;
using UnityEngine;
using WegoSystem;

public class GardenerController : MonoBehaviour, IStatusEffectable, ITickUpdateable
{
    [Header("Configuration")]
    [SerializeField] float multiTickDelay = 0.5f;

    // References have been removed to centralize the logic
    // [SerializeField] private StatusEffect wetStatusEffect;
    // [SerializeField] private TileDefinition waterTileDefinition;

    [Header("Animation")]
    [SerializeField] bool useAnimations = true;
    [SerializeField] Animator animator;
    [SerializeField] string runningParameterName = "isRunning";
    [SerializeField] string plantingTriggerName = "plant";

    [Header("Visuals")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool flipSpriteWhenMovingLeft = true;
    [SerializeField] bool flipHorizontalDirection = true;

    GridEntity gridEntity;
    StatusEffectManager statusManager;
    StatusEffectUIManager statusEffectUI;

    GridPosition currentTargetPosition;
    bool isProcessingMovement = false;

    public GridEntity GridEntity => gridEntity;
    public StatusEffectManager StatusManager => statusManager;

    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null) gridEntity = gameObject.AddComponent<GridEntity>();
        
        statusManager = GetComponent<StatusEffectManager>();
        if (statusManager == null) statusManager = gameObject.AddComponent<StatusEffectManager>();

        statusEffectUI = GetComponentInChildren<StatusEffectUIManager>(true);
        if (statusEffectUI == null) Debug.LogWarning("[GardenerController] StatusEffectUIManager not found in children. Icons won't display.", this);

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) Debug.LogWarning("[GardenerController] SpriteRenderer not found.", gameObject);
        if (animator == null && useAnimations) Debug.LogWarning("[GardenerController] Animator not found.", gameObject);
    }
    
    void Start()
    {
        statusManager.Initialize(this);
        if (statusEffectUI != null)
        {
            statusEffectUI.Initialize(statusManager);
        }
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
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
        if (gridEntity != null)
        {
            gridEntity.OnPositionChanged -= OnGridPositionChanged;
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (statusManager != null)
        {
            statusManager.OnTickUpdate(currentTick);
        }
    }

    void Update()
    {
        if (RunManager.Instance != null && RunManager.Instance.CurrentState == RunState.GrowthAndThreat)
        {
            HandlePlayerInput();
        }
        
        if(gridEntity != null && statusManager != null)
        {
            gridEntity.SetSpeedMultiplier(statusManager.MovementSpeedMultiplier);
        }

        UpdateAnimations();
        UpdateSpriteDirection();
    }

    private void OnGridPositionChanged(GridPosition oldPos, GridPosition newPos)
    {
        // The central system now handles this every tick, so this call is redundant.
        // EnvironmentalStatusEffectSystem.Instance?.CheckAndApplyEffects(this);
    }

    public string GetDisplayName() { return "Gardener"; }
    public void TakeDamage(float amount) { Debug.Log($"Gardener took {amount} damage!"); }
    public void Heal(float amount) { Debug.Log($"Gardener was healed for {amount}!"); }
    public void ModifyHunger(float amount) { /* Player doesn't have hunger. */ }
    
    void HandlePlayerInput()
    {
        if (gridEntity == null || gridEntity.IsMoving || isProcessingMovement) return;

        GridPosition moveDir = GridPosition.Zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) moveDir = GridPosition.Up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) moveDir = GridPosition.Down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) moveDir = GridPosition.Left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) moveDir = GridPosition.Right;

        if (moveDir != GridPosition.Zero)
        {
            TryMove(moveDir);
        }
    }

    void TryMove(GridPosition direction)
    {
        if (gridEntity == null) return;
        GridPosition targetPos = gridEntity.Position + direction;

        if (GridPositionManager.Instance != null && PlayerActionManager.Instance != null && TickManager.Instance != null &&
            GridPositionManager.Instance.IsPositionValid(targetPos) &&
            !GridPositionManager.Instance.IsPositionOccupied(targetPos))
        {
            Vector3 targetWorldPos = GridPositionManager.Instance.GridToWorld(targetPos);
            int moveCost = PlayerActionManager.Instance.GetMovementTickCost(targetWorldPos, this);

            if (moveCost > 1)
            {
                StartCoroutine(ProcessMultiTickMovement(targetPos, moveCost));
            }
            else
            {
                gridEntity.SetPosition(targetPos);
                currentTargetPosition = targetPos;
                TickManager.Instance.AdvanceTick();
            }
        }
        else
        {
            Debug.Log($"[GardenerController] Move to {targetPos} is invalid or blocked.");
        }
    }

    IEnumerator ProcessMultiTickMovement(GridPosition targetPos, int tickCost)
    {
        isProcessingMovement = true;
        for (int i = 0; i < tickCost - 1; i++)
        {
            TickManager.Instance.AdvanceTick();
            yield return new WaitForSeconds(multiTickDelay);
        }
        gridEntity.SetPosition(targetPos);
        currentTargetPosition = targetPos;
        TickManager.Instance.AdvanceTick();
        isProcessingMovement = false;
    }

    void UpdateAnimations()
    {
        if (!useAnimations || animator == null) return;
        bool isMoving = gridEntity != null && gridEntity.IsMoving;
        animator.SetBool(runningParameterName, isMoving);
    }

    void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft || gridEntity == null || !gridEntity.IsMoving) return;
        Vector3 worldTarget = GridPositionManager.Instance.GridToWorld(currentTargetPosition);
        Vector3 currentWorld = transform.position;
        Vector2 directionToCheck = (worldTarget - currentWorld).normalized;
        if (Mathf.Abs(directionToCheck.x) > 0.01f)
        {
            bool shouldFlip = directionToCheck.x < 0;
            spriteRenderer.flipX = flipHorizontalDirection ? shouldFlip : !shouldFlip;
        }
    }

    public void Plant()
    {
        if (useAnimations && animator != null)
        {
            animator.SetTrigger(plantingTriggerName);
        }
    }

    public GridPosition GetCurrentGridPosition()
    {
        return gridEntity?.Position ?? GridPosition.Zero;
    }
}
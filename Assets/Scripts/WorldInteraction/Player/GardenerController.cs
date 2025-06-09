using UnityEngine;
using WegoSystem;

public class GardenerController : MonoBehaviour, ITickUpdateable
{
    [SerializeField] private bool useWegoMovement = true;

    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f);
    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;

    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingParameterName = "isPlanting";
    public float plantingDuration = 0.25f;

    private GridEntity gridEntity;
    private GridPosition currentTargetPosition;

    private SortableEntity sortableEntity;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool isPlanting = false;
    private float plantingTimer = 0f;

    void Awake()
    {
        sortableEntity = GetComponent<SortableEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        gridEntity = GetComponent<GridEntity>();

        if (sortableEntity == null)
            sortableEntity = gameObject.AddComponent<SortableEntity>();

        if (spriteRenderer == null)
            Debug.LogWarning("[GardenerController Awake] Main SpriteRenderer component not found.", gameObject);

        if (animator == null && useAnimations)
            Debug.LogWarning("[GardenerController Awake] Animator component not found but useAnimations is true.", gameObject);

        if (gridEntity == null)
        {
            Debug.Log("[GardenerController] No GridEntity found. Adding one.", gameObject);
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }

    void Start()
    {
        if (TileInteractionManager.Instance != null)
        {
            Vector3Int cellPos = TileInteractionManager.Instance.WorldToCell(transform.position);
            Vector3 snappedPos = TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(cellPos);
            transform.position = snappedPos;

            if (gridEntity != null && GridPositionManager.Instance != null)
            {
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(snappedPos);
                gridEntity.SetPosition(gridPos, true);
                currentTargetPosition = gridPos;
            }

            Debug.Log($"[GardenerController] Snapped to tile grid position {cellPos} at world {snappedPos}");
        }

        // We still register for OnTickUpdate for potential future non-movement tick-based actions
        if (useWegoMovement && TickManager.Instance != null)
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
    }

    // This method is now much simpler. It can be used for other tick-based logic if needed.
    public void OnTickUpdate(int currentTick)
    {
        if (!useWegoMovement) return;
        // The movement logic has been moved to Update(), as it is now player-input-driven.
    }

    void Update()
    {
        if (useWegoMovement)
        {
            // Only allow direct movement when not in the planning phase.
            if (RunManager.Instance != null && RunManager.Instance.CurrentState != RunState.Planning)
            {
                HandleImmediateWegoMovement();
            }
        }
        else
        {
            // Non-wego movement can be handled here if you need a separate real-time mode.
        }

        HandlePlanting();
        UpdateAnimations();
        UpdateSpriteDirection();
    }

    private void HandleImmediateWegoMovement()
    {
        if (isPlanting || gridEntity == null) return;

        GridPosition moveDir = GridPosition.Zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) moveDir = GridPosition.Up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) moveDir = GridPosition.Down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) moveDir = GridPosition.Left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) moveDir = GridPosition.Right;

        if (moveDir != GridPosition.Zero)
        {
            GridPosition targetPos = gridEntity.Position + moveDir;

            // Validate the move before executing
            if (GridPositionManager.Instance != null && PlayerActionManager.Instance != null && TickManager.Instance != null &&
                GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsPositionOccupied(targetPos))
            {
                // Execute the move on the grid
                gridEntity.SetPosition(targetPos);
                currentTargetPosition = targetPos; // For sprite flipping

                // Calculate tick cost for the move
                int moveCost = PlayerActionManager.Instance.GetMovementTickCost(
                    GridPositionManager.Instance.GridToWorld(targetPos)
                );

                // Advance the game ticks
                TickManager.Instance.AdvanceMultipleTicks(moveCost);
                Debug.Log($"[GardenerController] Moved to {targetPos}. Advanced game by {moveCost} tick(s).");
            }
            else
            {
                 Debug.Log($"[GardenerController] Move to {targetPos} is invalid or blocked.");
            }
        }
    }
    
    private void HandlePlanting()
    {
        if (isPlanting)
        {
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0) EndPlantingAnimation();
        }
    }

    private void UpdateAnimations()
    {
        if (!useAnimations || animator == null) return;

        bool isMoving = gridEntity != null && gridEntity.IsMoving;
        animator.SetBool(runningParameterName, isMoving && !isPlanting);
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft) return;

        Vector2 directionToCheck = Vector2.zero;
        if (gridEntity != null && GridPositionManager.Instance != null)
        {
            Vector3 worldTarget = GridPositionManager.Instance.GridToWorld(currentTargetPosition);
            Vector3 currentWorld = transform.position;

            if (Vector3.Distance(worldTarget, currentWorld) > 0.01f)
            {
                directionToCheck = (worldTarget - currentWorld).normalized;
            }
        }

        if (directionToCheck.x != 0)
        {
            bool shouldFlip = (directionToCheck.x < 0);
            if (flipHorizontalDirection)
            {
                spriteRenderer.flipX = shouldFlip;
            }
            else
            {
                Vector3 scale = transform.localScale;
                scale.x = shouldFlip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }

    public void StartPlantingAnimation()
    {
        if (!useAnimations || isPlanting) return;
        isPlanting = true;
        plantingTimer = plantingDuration;
        if (animator != null)
        {
            animator.SetBool(plantingParameterName, true);
            animator.SetBool(runningParameterName, false);
        }
    }

    private void EndPlantingAnimation()
    {
        isPlanting = false;
        if (animator != null)
        {
            animator.SetBool(plantingParameterName, false);
        }
    }

    public Vector2 GetPlantingPosition()
    {
        return (Vector2)transform.position + seedPlantingOffset;
    }

    public void Plant()
    {
        StartPlantingAnimation();
    }

    public GridPosition GetCurrentGridPosition()
    {
        return gridEntity?.Position ?? GridPosition.Zero;
    }

    public void SetWegoMovement(bool enabled)
    {
        useWegoMovement = enabled;
        if (enabled && TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else if (!enabled && TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }
}
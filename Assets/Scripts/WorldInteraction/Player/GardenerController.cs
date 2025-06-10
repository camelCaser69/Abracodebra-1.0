// Assets\Scripts\WorldInteraction\Player\GardenerController.cs

using UnityEngine;
using WegoSystem;

public class GardenerController : MonoBehaviour, ITickUpdateable
{
    // --- REMOVED: useWegoMovement, seedPlantingOffset, plantingDuration ---

    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;

    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
// --- MODIFIED: Use a trigger for a one-shot animation ---
    public string plantingTriggerName = "plantTrigger"; // Changed from a bool parameter name

    private GridEntity gridEntity;
    private GridPosition currentTargetPosition;

    private SortableEntity sortableEntity;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // --- REMOVED: isPlanting, plantingTimer ---

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

        // --- MODIFIED: Unconditional registration with TickManager ---
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        // --- MODIFIED: Unconditional un-registration ---
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        // This can be used for future tick-based gardener logic if needed.
    }

    void Update()
    {
        // --- MODIFIED: Simplified Update loop ---
        if (RunManager.Instance != null && RunManager.Instance.CurrentState != RunState.Planning)
        {
            HandleImmediateWegoMovement();
        }

        UpdateAnimations();
        UpdateSpriteDirection();
    }

    void HandleImmediateWegoMovement()
    {
        // --- REMOVED: isPlanting check, as planting is now an instantaneous action. ---
        if (gridEntity == null) return;

        GridPosition moveDir = GridPosition.Zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) moveDir = GridPosition.Up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) moveDir = GridPosition.Down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) moveDir = GridPosition.Left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) moveDir = GridPosition.Right;

        if (moveDir != GridPosition.Zero)
        {
            GridPosition targetPos = gridEntity.Position + moveDir;

            if (GridPositionManager.Instance != null && PlayerActionManager.Instance != null && TickManager.Instance != null &&
                GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsPositionOccupied(targetPos))
            {
                int moveCost = PlayerActionManager.Instance.GetMovementTickCost(transform.position, this);

                gridEntity.SetPosition(targetPos);
                currentTargetPosition = targetPos;

                TickManager.Instance.AdvanceMultipleTicks(moveCost);
                Debug.Log($"[GardenerController] Moved to {targetPos}. Advanced game by {moveCost} tick(s).");
            }
            else
            {
                Debug.Log($"[GardenerController] Move to {targetPos} is invalid or blocked.");
            }
        }
    }

    // --- REMOVED: HandlePlanting() method ---

    void UpdateAnimations()
    {
        if (!useAnimations || animator == null) return;

        bool isMoving = gridEntity != null && gridEntity.IsMoving;
        // --- MODIFIED: Removed !isPlanting check ---
        animator.SetBool(runningParameterName, isMoving);
    }

    void UpdateSpriteDirection()
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

    // --- REMOVED: StartPlantingAnimation() and EndPlantingAnimation() ---

    // --- REMOVED: GetPlantingPosition() method ---

    /// <summary>
    /// Called by external managers when a planting action occurs. Triggers a one-shot animation.
    /// </summary>
    public void Plant()
    {
        if (useAnimations && animator != null)
        {
            // --- MODIFIED: Use SetTrigger instead of SetBool ---
            animator.SetTrigger(plantingTriggerName);
        }
    }

    public GridPosition GetCurrentGridPosition()
    {
        return gridEntity?.Position ?? GridPosition.Zero;
    }

    // --- REMOVED: SetWegoMovement() method ---
}
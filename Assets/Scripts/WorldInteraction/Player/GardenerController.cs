using UnityEngine;
using WegoSystem;

public class GardenerController : MonoBehaviour
{
    [Header("Visuals & Animation")]
    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;
    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingTriggerName = "plantTrigger";

    // Component References
    private GridEntity gridEntity;
    private SortableEntity sortableEntity;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // State
    private GridPosition currentTargetPosition;

    void Awake()
    {
        // Cache all necessary components
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

    // Input and visual updates should be handled every frame in Update()
    void Update()
    {
        // Check for player input only during the 'action' phase
        if (RunManager.Instance != null && RunManager.Instance.CurrentState == RunState.GrowthAndThreat)
        {
            HandlePlayerInput();
        }

        // Update visuals every frame for smooth animation and movement
        UpdateAnimations();
        UpdateSpriteDirection();
    }

    /// <summary>
    /// Checks for WASD/Arrow key input and triggers movement if valid.
    /// This is the main driver for player-initiated, tick-costing actions.
    /// </summary>
    private void HandlePlayerInput()
    {
        if (gridEntity == null || gridEntity.IsMoving) return;

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

    /// <summary>
    /// Attempts to move the player one tile in the given direction, advancing the game ticks if successful.
    /// </summary>
    private void TryMove(GridPosition direction)
    {
        GridPosition targetPos = gridEntity.Position + direction;

        if (GridPositionManager.Instance != null && PlayerActionManager.Instance != null && TickManager.Instance != null &&
            GridPositionManager.Instance.IsPositionValid(targetPos) &&
            !GridPositionManager.Instance.IsPositionOccupied(targetPos))
        {
            // Calculate the cost of the move before performing it
            int moveCost = PlayerActionManager.Instance.GetMovementTickCost(transform.position, this);

            // Set the new position for the GridEntity to handle visual interpolation
            gridEntity.SetPosition(targetPos);
            currentTargetPosition = targetPos; // Update target for sprite flipping

            // Advance the game state by the cost of the action
            TickManager.Instance.AdvanceMultipleTicks(moveCost);
            Debug.Log($"[GardenerController] Moved to {targetPos}. Advanced game by {moveCost} tick(s).");
        }
        else
        {
            Debug.Log($"[GardenerController] Move to {targetPos} is invalid or blocked.");
        }
    }

    private void UpdateAnimations()
    {
        if (!useAnimations || animator == null) return;

        bool isMoving = gridEntity != null && gridEntity.IsMoving;
        animator.SetBool(runningParameterName, isMoving);
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft || gridEntity == null || !gridEntity.IsMoving) return;

        Vector3 worldTarget = GridPositionManager.Instance.GridToWorld(currentTargetPosition);
        Vector3 currentWorld = transform.position;
        Vector2 directionToCheck = (worldTarget - currentWorld).normalized;

        if (Mathf.Abs(directionToCheck.x) > 0.01f)
        {
            bool shouldFlip = directionToCheck.x < 0;
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

    /// <summary>
    /// Triggers the planting animation. Called externally.
    /// </summary>
    public void Plant()
    {
        if (useAnimations && animator != null)
        {
            animator.SetTrigger(plantingTriggerName);
        }
    }

    /// <summary>
    /// Gets the gardener's current grid position.
    /// </summary>
    public GridPosition GetCurrentGridPosition()
    {
        return gridEntity?.Position ?? GridPosition.Zero;
    }
}
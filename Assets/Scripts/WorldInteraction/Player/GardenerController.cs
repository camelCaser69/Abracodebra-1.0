// Assets\Scripts\WorldInteraction\Player\GardenerController.cs

using UnityEngine;
using WegoSystem;
using System.Collections.Generic;

public class GardenerController : SpeedModifiable, ITickUpdateable {
    [SerializeField] bool useWegoMovement = true;

    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f);
    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;

    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingParameterName = "isPlanting";
    public float plantingDuration = 0.25f;

    GridEntity gridEntity;
    Queue<GridPosition> plannedMoves = new Queue<GridPosition>();
    GridPosition currentTargetPosition;
    bool hasMoveQueued = false;

    // Remove Rigidbody2D references - no longer needed for grid movement
    // Rigidbody2D rb;
    // Vector2 movement;

    SortableEntity sortableEntity;
    SpriteRenderer spriteRenderer;
    Animator animator;

    bool isPlanting = false;
    float plantingTimer = 0f;

    protected override void Awake() {
        base.Awake();

        // Remove rigidbody reference
        // rb = GetComponent<Rigidbody2D>();
        
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

        if (gridEntity == null) {
            Debug.Log("[GardenerController] No GridEntity found. Adding one.", gameObject);
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }

    void Start() {
        // CRITICAL: Force grid snapping on start
        if (GridPositionManager.Instance != null) {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            currentTargetPosition = gridEntity.Position;
            Debug.Log($"[GardenerController] Snapped to grid position {gridEntity.Position} on start");
        }

        if (useWegoMovement && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);

            if (TurnPhaseManager.Instance != null) {
                TurnPhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            }
        }
    }

    void OnDestroy() {
        if (TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }

        if (TurnPhaseManager.Instance != null) {
            TurnPhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    void OnPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase) {
        if (newPhase == TurnPhase.Planning) {
            plannedMoves.Clear();
            hasMoveQueued = false;
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoMovement || isPlanting) return;

        if (plannedMoves.Count > 0) {
            GridPosition nextMove = plannedMoves.Dequeue();

            if (gridEntity != null && GridPositionManager.Instance != null) {
                if (GridPositionManager.Instance.IsPositionValid(nextMove) &&
                    !GridPositionManager.Instance.IsPositionOccupied(nextMove)) {
                    gridEntity.SetPosition(nextMove);
                    currentTargetPosition = nextMove;
                }
            }
        }

        hasMoveQueued = plannedMoves.Count > 0;
    }

    void Update() {
        if (useWegoMovement) {
            HandleWegoInput();
        } else {
            HandleImmediateInput();
        }

        HandlePlanting();
        UpdateAnimations();
        UpdateSpriteDirection();
    }

    void HandleWegoInput() {
        if (TurnPhaseManager.Instance?.IsInPlanningPhase != true) return;
        if (isPlanting) return;

        Vector2 input = Vector2.zero;
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        if (input.sqrMagnitude > 0.01f && gridEntity != null) {
            GridPosition currentPos = gridEntity.Position;
            GridPosition targetPos = currentPos;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y)) {
                targetPos = currentPos + (input.x > 0 ? GridPosition.Right : GridPosition.Left);
            } else {
                targetPos = currentPos + (input.y > 0 ? GridPosition.Up : GridPosition.Down);
            }

            if (PlayerActionManager.Instance != null) {
                PlayerActionManager.Instance.ExecutePlayerMove(this, currentPos, targetPos);
            } else {
                Debug.LogError("[GardenerController] PlayerActionManager not found!");
            }
        }
    }

    void HandleImmediateInput() {
        if (isPlanting) return;

        // Discrete grid-based movement
        bool shouldMove = false;
        GridPosition moveDir = GridPosition.Zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
            moveDir = GridPosition.Up;
            shouldMove = true;
        } else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
            moveDir = GridPosition.Down;
            shouldMove = true;
        } else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
            moveDir = GridPosition.Left;
            shouldMove = true;
        } else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
            moveDir = GridPosition.Right;
            shouldMove = true;
        }

        if (shouldMove && gridEntity != null && PlayerActionManager.Instance != null) {
            GridPosition currentPos = gridEntity.Position;
            GridPosition targetPos = currentPos + moveDir;
            PlayerActionManager.Instance.ExecutePlayerMove(this, currentPos, targetPos);
        }
    }

    void HandlePlanting() {
        if (isPlanting) {
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0) EndPlantingAnimation();
        }
    }

    // Remove FixedUpdate entirely - no physics-based movement
    // void FixedUpdate() {
    //     if (!useWegoMovement && !isPlanting && rb != null) {
    //         rb.MovePosition(rb.position + movement.normalized * currentSpeed * Time.fixedDeltaTime);
    //     }
    // }

    void UpdateAnimations() {
        if (!useAnimations || animator == null) return;

        bool isMoving;
        if (useWegoMovement) {
            isMoving = gridEntity != null && gridEntity.IsMoving;
        } else {
            isMoving = gridEntity != null && gridEntity.IsMoving;
        }

        animator.SetBool(runningParameterName, isMoving && !isPlanting);
    }

    void UpdateSpriteDirection() {
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft) return;

        Vector2 directionToCheck = Vector2.zero;
        if (gridEntity != null && GridPositionManager.Instance != null) {
            Vector3 worldTarget = GridPositionManager.Instance.GridToWorld(currentTargetPosition);
            Vector3 currentWorld = transform.position;
            
            if (Vector3.Distance(worldTarget, currentWorld) > 0.01f) {
                directionToCheck = (worldTarget - currentWorld).normalized;
            }
        }

        if (directionToCheck.x != 0) {
            bool shouldFlip = (directionToCheck.x < 0);
            if (flipHorizontalDirection) {
                spriteRenderer.flipX = shouldFlip;
            }
            else {
                Vector3 scale = transform.localScale;
                scale.x = shouldFlip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }

    public void StartPlantingAnimation() {
        if (!useAnimations || isPlanting) return;
        isPlanting = true;
        plantingTimer = plantingDuration;
        if (animator != null) {
            animator.SetBool(plantingParameterName, true);
            animator.SetBool(runningParameterName, false);
        }
    }

    void EndPlantingAnimation() {
        isPlanting = false;
        if (animator != null) {
            animator.SetBool(plantingParameterName, false);
        }
    }

    public Vector2 GetPlantingPosition() {
        return (Vector2)transform.position + seedPlantingOffset;
    }

    public void SetPlantingDuration(float duration) {
        plantingDuration = Mathf.Max(0.1f, duration);
    }

    public void Plant() {
        StartPlantingAnimation();
    }

    public void QueueMovement(GridPosition targetPosition) {
        if (useWegoMovement && TurnPhaseManager.Instance?.IsInPlanningPhase == true) {
            plannedMoves.Enqueue(targetPosition);
        }
    }

    public void ClearQueuedMoves() {
        plannedMoves.Clear();
        hasMoveQueued = false;
    }

    public int GetQueuedMoveCount() {
        return plannedMoves.Count;
    }

    public GridPosition GetCurrentGridPosition() {
        return gridEntity?.Position ?? GridPosition.Zero;
    }

    public void SetWegoMovement(bool enabled) {
        useWegoMovement = enabled;

        if (enabled && gridEntity == null) {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }

        if (enabled && TickManager.Instance != null) {
            TickManager.Instance.RegisterTickUpdateable(this);
        } else if (!enabled && TickManager.Instance != null) {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }
}
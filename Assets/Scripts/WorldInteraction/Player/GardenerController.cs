using UnityEngine;
using WegoSystem;
using System.Collections.Generic;

public class GardenerController : SpeedModifiable, ITickUpdateable {
    [Header("Wego System")]
    [SerializeField] bool useWegoMovement = true;
    
    [Header("Movement")]
    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f);
    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;

    [Header("Animation")]
    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingParameterName = "isPlanting";
    public float plantingDuration = 0.25f;

    // Wego System Components
    GridEntity gridEntity;
    Queue<GridPosition> plannedMoves = new Queue<GridPosition>();
    GridPosition currentTargetPosition;
    bool hasMoveQueued = false;

    // Real-time movement (fallback)
    Rigidbody2D rb;
    Vector2 movement;
    
    // Visual & Animation
    SortableEntity sortableEntity;
    SpriteRenderer spriteRenderer;
    Animator animator;

    bool isPlanting = false;
    float plantingTimer = 0f;
    bool wasMovingBeforePlanting = false;

    protected override void Awake() {
        base.Awake(); // This is crucial: it sets currentSpeed = baseSpeed

        rb = GetComponent<Rigidbody2D>();
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

        if (useWegoMovement && gridEntity == null) {
            Debug.LogWarning("[GardenerController] Wego movement enabled but no GridEntity found. Adding one.", gameObject);
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }

    void Start() {
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
            // Clear any pending moves when entering planning
            plannedMoves.Clear();
            hasMoveQueued = false;
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoMovement || isPlanting) return;

        // Execute queued movement
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
            HandleRealtimeMovement();
        }

        HandlePlanting();
        UpdateAnimations();
        UpdateSpriteDirection();
    }

    void HandleWegoInput() {
        if (TurnPhaseManager.Instance?.IsInPlanningPhase != true) return;
        if (isPlanting) return;

        // Queue movement inputs during planning phase
        Vector2 input = Vector2.zero;
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        if (input.sqrMagnitude > 0.01f && gridEntity != null) {
            GridPosition currentPos = gridEntity.Position;
            GridPosition targetPos = currentPos;

            // Convert input to grid movement (one tile at a time)
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y)) {
                targetPos = currentPos + (input.x > 0 ? GridPosition.Right : GridPosition.Left);
            } else {
                targetPos = currentPos + (input.y > 0 ? GridPosition.Up : GridPosition.Down);
            }

            // Queue the move if valid and not already queued
            if (GridPositionManager.Instance != null &&
                GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsPositionOccupied(targetPos) &&
                !plannedMoves.Contains(targetPos)) {
                
                plannedMoves.Enqueue(targetPos);
                
                if (Debug.isDebugBuild) {
                    Debug.Log($"[GardenerController] Queued move to {targetPos}");
                }
            }
        }
    }

    void HandleRealtimeMovement() {
        if (!isPlanting) {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            bool isMoving = movement.sqrMagnitude > 0.01f;
            if (isMoving) wasMovingBeforePlanting = true;
        }
        else {
            movement = Vector2.zero;
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0) EndPlantingAnimation();
        }
    }

    void HandlePlanting() {
        if (isPlanting) {
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0) EndPlantingAnimation();
        }
    }

    void FixedUpdate() {
        if (!useWegoMovement && !isPlanting && rb != null) {
            rb.MovePosition(rb.position + movement.normalized * currentSpeed * Time.fixedDeltaTime);
        }
    }

    void UpdateAnimations() {
        if (!useAnimations || animator == null) return;
        
        bool isMoving;
        if (useWegoMovement) {
            isMoving = gridEntity != null && gridEntity.IsMoving;
        } else {
            isMoving = movement.sqrMagnitude > 0.01f;
        }
        
        animator.SetBool(runningParameterName, isMoving && !isPlanting);
    }

    void UpdateSpriteDirection() {
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft) return;
        
        Vector2 directionToCheck = movement;
        if (useWegoMovement && gridEntity != null) {
            // Use the direction the grid entity is moving
            Vector3 worldTarget = GridPositionManager.Instance?.GridToWorld(currentTargetPosition) ?? transform.position;
            directionToCheck = (worldTarget - transform.position).normalized;
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
        wasMovingBeforePlanting = movement.sqrMagnitude > 0.01f;
        if (animator != null) {
            animator.SetBool(plantingParameterName, true);
            animator.SetBool(runningParameterName, false);
        }
    }

    void EndPlantingAnimation() {
        isPlanting = false;
        if (animator != null) {
            animator.SetBool(plantingParameterName, false);
            if (!useWegoMovement) {
                movement.x = Input.GetAxisRaw("Horizontal");
                movement.y = Input.GetAxisRaw("Vertical");
                bool shouldResumeRunning = movement.sqrMagnitude > 0.01f;
                animator.SetBool(runningParameterName, shouldResumeRunning);
            }
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

    // Wego-specific methods
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
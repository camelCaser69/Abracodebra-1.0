using UnityEngine;
using System.Collections.Generic;
using WegoSystem;

public class GardenerController : MonoBehaviour, ITickUpdateable {
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

    SortableEntity sortableEntity;
    SpriteRenderer spriteRenderer;
    Animator animator;

    bool isPlanting = false;
    float plantingTimer = 0f;
    
    // Track if we're currently processing a move to prevent multiple moves per tick
    bool isProcessingMove = false;
    
    // Add input tracking to prevent multiple queues per key press
    bool upPressed = false;
    bool downPressed = false;
    bool leftPressed = false;
    bool rightPressed = false;

    void Awake() {
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
        // Snap to tile grid instead of generic grid
        if (TileInteractionManager.Instance != null) {
            Vector3Int cellPos = TileInteractionManager.Instance.WorldToCell(transform.position);
            Vector3 snappedPos = TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(cellPos);
            transform.position = snappedPos;
            
            // Update grid entity position
            if (gridEntity != null && GridPositionManager.Instance != null) {
                GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(snappedPos);
                gridEntity.SetPosition(gridPos, true);
                currentTargetPosition = gridPos;
            }
            
            Debug.Log($"[GardenerController] Snapped to tile grid position {cellPos} at world {snappedPos}");
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
            // Clear any remaining moves when returning to planning
            plannedMoves.Clear();
            hasMoveQueued = false;
            isProcessingMove = false;
            
            // Ensure we're snapped to our current position
            if (gridEntity != null) {
                currentTargetPosition = gridEntity.Position;
            }
        }
    }

    public void OnTickUpdate(int currentTick) {
        if (!useWegoMovement || isPlanting) return;
        
        // Only process moves during execution phase
        if (TurnPhaseManager.Instance?.CurrentPhase == TurnPhase.Execution) {
            // Process one move per tick
            if (!isProcessingMove && plannedMoves.Count > 0) {
                ProcessNextMove();
            }
        }
        
        hasMoveQueued = plannedMoves.Count > 0;
    }
    
    void ProcessNextMove() {
        if (plannedMoves.Count == 0) return;
        
        isProcessingMove = true;
        GridPosition nextMove = plannedMoves.Dequeue();
        
        if (gridEntity != null && GridPositionManager.Instance != null) {
            if (GridPositionManager.Instance.IsPositionValid(nextMove) &&
                !GridPositionManager.Instance.IsPositionOccupied(nextMove)) {
                
                // Calculate tick cost before moving
                Vector3 worldPos = GridPositionManager.Instance.GridToWorld(nextMove);
                int moveCost = 1; // Default cost
                
                // Check for slowdown zones
                if (PlayerActionManager.Instance != null) {
                    moveCost = PlayerActionManager.Instance.GetMovementTickCost(worldPos);
                }
                
                // Execute the move
                gridEntity.SetPosition(nextMove);
                currentTargetPosition = nextMove;
                
                Debug.Log($"[GardenerController] Executed move to {nextMove}, cost {moveCost} ticks");
                
                // Schedule next tick for movement cost
                if (TickManager.Instance != null) {
                    // Don't advance ticks here - let TurnPhaseManager handle it
                    // Just wait for the next natural tick
                }
            } else {
                Debug.Log($"[GardenerController] Move to {nextMove} blocked");
            }
        }
        
        isProcessingMove = false;
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

        // Use GetKeyDown for single press detection
        bool shouldMove = false;
        GridPosition moveDir = GridPosition.Zero;
        
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && !upPressed) {
            moveDir = GridPosition.Up;
            shouldMove = true;
            upPressed = true;
        } else if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.UpArrow)) {
            upPressed = false;
        }
        
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !downPressed) {
            moveDir = GridPosition.Down;
            shouldMove = true;
            downPressed = true;
        } else if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.DownArrow)) {
            downPressed = false;
        }
        
        if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) && !leftPressed) {
            moveDir = GridPosition.Left;
            shouldMove = true;
            leftPressed = true;
        } else if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.LeftArrow)) {
            leftPressed = false;
        }
        
        if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) && !rightPressed) {
            moveDir = GridPosition.Right;
            shouldMove = true;
            rightPressed = true;
        } else if (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.RightArrow)) {
            rightPressed = false;
        }

        if (shouldMove && gridEntity != null) {
            GridPosition currentPos = gridEntity.Position;
            
            // If we have queued moves, use the last queued position as our starting point
            if (plannedMoves.Count > 0) {
                var moves = plannedMoves.ToArray();
                currentPos = moves[moves.Length - 1];
            }
            
            GridPosition targetPos = currentPos + moveDir;

            if (PlayerActionManager.Instance != null) {
                PlayerActionManager.Instance.ExecutePlayerMove(this, currentPos, targetPos);
            } else {
                Debug.LogError("[GardenerController] PlayerActionManager not found!");
            }
        }
    }

    void HandleImmediateInput() {
        if (isPlanting) return;

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

        if (shouldMove && gridEntity != null) {
            GridPosition currentPos = gridEntity.Position;
            GridPosition targetPos = currentPos + moveDir;
            
            // In immediate mode, execute the move directly
            if (GridPositionManager.Instance != null &&
                GridPositionManager.Instance.IsPositionValid(targetPos) &&
                !GridPositionManager.Instance.IsPositionOccupied(targetPos)) {
                
                gridEntity.SetPosition(targetPos);
                currentTargetPosition = targetPos;
                
                // Advance tick in immediate mode
                if (TickManager.Instance != null) {
                    Vector3 worldPos = GridPositionManager.Instance.GridToWorld(targetPos);
                    int moveCost = PlayerActionManager.Instance?.GetMovementTickCost(worldPos) ?? 1;
                    TickManager.Instance.AdvanceMultipleTicks(moveCost);
                }
            }
        }
    }

    void HandlePlanting() {
        if (isPlanting) {
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0) EndPlantingAnimation();
        }
    }

    void UpdateAnimations() {
        if (!useAnimations || animator == null) return;

        bool isMoving = gridEntity != null && gridEntity.IsMoving;
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
            hasMoveQueued = true;
            Debug.Log($"[GardenerController] Queued move to {targetPosition}. Total queued: {plannedMoves.Count}");
        }
    }

    public void ClearQueuedMoves() {
        plannedMoves.Clear();
        hasMoveQueued = false;
        isProcessingMove = false;
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
        
        // Clear any queued moves when switching modes
        ClearQueuedMoves();
    }
}
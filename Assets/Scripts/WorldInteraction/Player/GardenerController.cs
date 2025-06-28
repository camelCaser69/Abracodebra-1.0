using System.Collections;
using UnityEngine;
using WegoSystem;
using TMPro;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(GridEntity))]
public class GardenerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float multiTickDelay = 0.5f; // Delay between ticks for multi-tick actions
    
    [Header("Animation")]
    [SerializeField] bool useAnimations = true;
    [SerializeField] Animator animator;
    [SerializeField] string runningParameterName = "isRunning";
    [SerializeField] string plantingTriggerName = "plant";
    
    [Header("Sprite")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool flipSpriteWhenMovingLeft = true;
    [SerializeField] bool flipHorizontalDirection = true;
    
    GridEntity gridEntity;
    GridPosition currentTargetPosition;
    private bool isProcessingMovement = false;
    
    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            Debug.LogError("[GardenerController Awake] GridEntity component is required!", gameObject);
        }
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer == null)
            Debug.LogWarning("[GardenerController Awake] SpriteRenderer not found. Sprite flipping disabled.", gameObject);
        
        if (animator == null && useAnimations)
            Debug.LogWarning("[GardenerController Awake] Animator component not found but useAnimations is true.", gameObject);
        
        if (gridEntity == null)
        {
            Debug.Log("[GardenerController] No GridEntity found. Adding one.", gameObject);
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }
    
    void Update()
    {
        if (RunManager.Instance != null && RunManager.Instance.CurrentState == RunState.GrowthAndThreat)
        {
            HandlePlayerInput();
        }
        
        UpdateAnimations();
        UpdateSpriteDirection();
    }
    
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
        GridPosition targetPos = gridEntity.Position + direction;
        
        if (GridPositionManager.Instance != null && PlayerActionManager.Instance != null && TickManager.Instance != null &&
            GridPositionManager.Instance.IsPositionValid(targetPos) &&
            !GridPositionManager.Instance.IsPositionOccupied(targetPos))
        {
            // Get movement cost BEFORE moving
            Vector3 targetWorldPos = GridPositionManager.Instance.GridToWorld(targetPos);
            int moveCost = PlayerActionManager.Instance.GetMovementTickCost(targetWorldPos, this);
            
            if (moveCost > 1)
            {
                // Multi-tick movement with delay
                StartCoroutine(ProcessMultiTickMovement(targetPos, moveCost));
            }
            else
            {
                // Single tick movement - immediate
                gridEntity.SetPosition(targetPos);
                currentTargetPosition = targetPos;
                TickManager.Instance.AdvanceTick();
                Debug.Log($"[GardenerController] Moved to {targetPos}. Advanced game by 1 tick.");
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
        
        Debug.Log($"[GardenerController] Starting multi-tick movement. Cost: {tickCost} ticks");
        
        // Process all ticks except the last one
        for (int i = 0; i < tickCost - 1; i++)
        {
            Debug.Log($"[GardenerController] Processing tick {i + 1}/{tickCost} (delay tick)");
            TickManager.Instance.AdvanceTick();
            
            // Wait for the specified delay
            yield return new WaitForSeconds(multiTickDelay);
        }
        
        // On the final tick, actually move the player
        gridEntity.SetPosition(targetPos);
        currentTargetPosition = targetPos;
        TickManager.Instance.AdvanceTick();
        
        Debug.Log($"[GardenerController] Completed movement to {targetPos}. Total ticks: {tickCost}");
        
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
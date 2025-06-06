// FILE: Assets/Scripts/Player/GardenerController.cs
using UnityEngine;
using System.Collections.Generic; // Keep for activeSpeedMultipliers

// No longer requires ToolSwitcher on the same GameObject
public class GardenerController : MonoBehaviour
{
    // --- Fields ---
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Speed Modifiers")]
    [Tooltip("Current speed including all active modifiers (read-only)")]
    [SerializeField] private float currentMoveSpeed;
    private float baseMoveSpeed;
    private List<float> activeSpeedMultipliers = new List<float>();
    
    [Header("Planting Settings")]
    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f);
    
    [Header("Visual Settings")]
    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;

    [Header("Animation Settings")]
    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingParameterName = "isPlanting";
    public float plantingDuration = 0.25f;

    // Private references
    private Rigidbody2D rb;
    private Vector2 movement;
    private SortableEntity sortableEntity;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // Private variables
    private bool isPlanting = false;
    private float plantingTimer = 0f;
    private bool wasMovingBeforePlanting = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sortableEntity = GetComponent<SortableEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        baseMoveSpeed = moveSpeed;
        currentMoveSpeed = moveSpeed;
        activeSpeedMultipliers = new List<float>();

        if (sortableEntity == null)
            sortableEntity = gameObject.AddComponent<SortableEntity>();

        if (spriteRenderer == null)
            Debug.LogWarning("[GardenerController Awake] Main SpriteRenderer component not found.", gameObject);
    
        if (animator == null && useAnimations)
            Debug.LogWarning("[GardenerController Awake] Animator component not found but useAnimations is true.", gameObject);
    }

    private void Start()
    {
        // Subscription to InventoryBarController.OnSelectionChanged removed
    }

    private void OnDestroy()
    {
        activeSpeedMultipliers.Clear();
        // Unsubscription from InventoryBarController.OnSelectionChanged removed
    }

    private void Update()
    {
        if (!isPlanting)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            bool isMoving = movement.sqrMagnitude > 0.01f;
            if (isMoving) wasMovingBeforePlanting = true;
        }
        else
        {
            movement = Vector2.zero;
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0) EndPlantingAnimation();
        }
        UpdateAnimations();
        UpdateSpriteDirection();
    }
    
    public void ApplySpeedMultiplier(float multiplier)
    {
        if (!activeSpeedMultipliers.Contains(multiplier))
        {
            activeSpeedMultipliers.Add(multiplier);
            UpdateMovementSpeed();
        
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{gameObject.name}] Applied speed multiplier: {multiplier}. New speed: {currentMoveSpeed}");
            }
        }
    }
    
    private void UpdateMovementSpeed()
    {
        float newSpeed = baseMoveSpeed;
        if (activeSpeedMultipliers.Count > 0)
        {
            float lowestMultiplier = 1.0f;
            foreach (float multiplier in activeSpeedMultipliers)
            {
                if (multiplier < lowestMultiplier)
                {
                    lowestMultiplier = multiplier;
                }
            }
            newSpeed *= lowestMultiplier;
        }
        currentMoveSpeed = newSpeed;
    }
    
    public void RemoveSpeedMultiplier(float multiplier)
    {
        if (activeSpeedMultipliers.Contains(multiplier))
        {
            activeSpeedMultipliers.Remove(multiplier);
            UpdateMovementSpeed();
        
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{gameObject.name}] Removed speed multiplier: {multiplier}. New speed: {currentMoveSpeed}");
            }
        }
    }
        
    void FixedUpdate()
    {
        if (!isPlanting)
        {
            rb.MovePosition(rb.position + movement.normalized * currentMoveSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimations() 
    { 
        if (!useAnimations || animator == null) return; 
        bool isMoving = movement.sqrMagnitude > 0.01f; 
        animator.SetBool(runningParameterName, isMoving); 
    }

    private void UpdateSpriteDirection() 
    { 
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft) return; 
        if (movement.x != 0) 
        { 
            bool shouldFlip = (movement.x < 0); 
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
        wasMovingBeforePlanting = movement.sqrMagnitude > 0.01f; 
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
            movement.x = Input.GetAxisRaw("Horizontal"); 
            movement.y = Input.GetAxisRaw("Vertical"); 
            bool shouldResumeRunning = movement.sqrMagnitude > 0.01f; 
            animator.SetBool(runningParameterName, shouldResumeRunning); 
        } 
    }

    public Vector2 GetPlantingPosition() 
    { 
        return (Vector2)transform.position + seedPlantingOffset; 
    }
    
    public void SetPlantingDuration(float duration) 
    { 
        plantingDuration = Mathf.Max(0.1f, duration); 
    }

    public void Plant() 
    { 
        StartPlantingAnimation(); 
    }
}
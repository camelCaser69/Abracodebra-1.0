using UnityEngine;

public class GardenerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Planting Settings")]
    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f); // Configurable offset for seed planting

    [Header("Visual Settings")]
    [Tooltip("If true, the sprite will be flipped when moving left")]
    public bool flipSpriteWhenMovingLeft = true;
    [Tooltip("If true, the character will face the opposite direction when flipped")]
    public bool flipHorizontalDirection = true;

    [Header("Animation Settings")]
    [Tooltip("Set to false to disable animations")]
    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingParameterName = "isPlanting";
    [Tooltip("Should match your planting animation length exactly")]
    public float plantingDuration = 0.25f; // UPDATED to match your 0.25s animation


    
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

    private void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        sortableEntity = GetComponent<SortableEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // Add SortableEntity if not already present
        if (sortableEntity == null)
            sortableEntity = gameObject.AddComponent<SortableEntity>();
            
        // Warn if sprite renderer is missing
        if (spriteRenderer == null)
            Debug.LogWarning("GardenerController: SpriteRenderer component not found. Sprite flipping won't work.");
            
        // Warn if animator is missing but animations are enabled
        if (animator == null && useAnimations)
            Debug.LogWarning("GardenerController: Animator component not found but useAnimations is true.");
    }

    private void Update()
    {
        // Handle movement input and planting action
        if (!isPlanting)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        
            // Check if we're moving (store for later)
            bool isMoving = movement.sqrMagnitude > 0.01f;
        
            // Handle planting action
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Store movement state before planting
                wasMovingBeforePlanting = isMoving;
                StartPlantingAnimation();
            }
        }
        else
        {
            // When planting, we don't accept movement input
            movement = Vector2.zero;
        
            // Handle planting timer
            plantingTimer -= Time.deltaTime;
            if (plantingTimer <= 0)
            {
                EndPlantingAnimation();
            }
        }
    
        // Update animations after all state changes are processed
        UpdateAnimations();
    
        // Handle sprite flipping based on movement direction
        UpdateSpriteDirection();
    }

    private void FixedUpdate()
    {
        // Only move if not planting
        if (!isPlanting)
        {
            rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }
    
    // Update character animations based on state
    private void UpdateAnimations()
    {
        if (!useAnimations || animator == null) return;
    
        // Set running animation parameter
        bool isMoving = movement.sqrMagnitude > 0.01f;
        animator.SetBool(runningParameterName, isMoving);
    }
    
    // Update sprite direction based on movement
    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || !flipSpriteWhenMovingLeft) return;
        
        if (movement.x != 0)
        {
            // Only flip if moving horizontally
            bool shouldFlip = (movement.x < 0);
            
            // Apply flipping logic based on settings
            if (flipHorizontalDirection)
            {
                spriteRenderer.flipX = shouldFlip;
            }
            else
            {
                // Alternative approach: flip the entire transform
                // This is useful if the sprite is already facing left initially
                Vector3 scale = transform.localScale;
                scale.x = shouldFlip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }
    
    // Start planting animation and process
    public void StartPlantingAnimation()
    {
        if (!useAnimations || isPlanting) return;
    
        isPlanting = true;
        plantingTimer = plantingDuration;
    
        // Set animation parameters
        if (animator != null)
        {
            // Set planting to true and ensure running is false
            animator.SetBool(plantingParameterName, true);
            animator.SetBool(runningParameterName, false);
        }
    }
    
    // End planting animation and resume normal control
    
    private void EndPlantingAnimation()
    {
        // Reset planting state
        isPlanting = false;
    
        // Reset animation parameters
        if (animator != null)
        {
            animator.SetBool(plantingParameterName, false);
        
            // Important: don't immediately set isRunning based on current movement
            // because movement is zero during planting. Instead:
            if (wasMovingBeforePlanting && (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0))
            {
                // Only resume running if we were running before AND still have directional input
                animator.SetBool(runningParameterName, true);
            }
        }
    
        // Now update movement based on current input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
    }
    
    // Returns the position used for planting seeds, now with configurable offset
    public Vector2 GetPlantingPosition()
    {
        return (Vector2)transform.position + seedPlantingOffset;
    }
    
    // Public method to set planting animation duration
    public void SetPlantingDuration(float duration)
    {
        plantingDuration = Mathf.Max(0.1f, duration); // Ensure minimum duration
    }
    
    // Public method to trigger planting animation from other scripts
    public void Plant()
    {
        StartPlantingAnimation();
    }
}
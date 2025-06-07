using UnityEngine;
using System.Collections.Generic;

// CHANGED: Inherits from SpeedModifiable now
public class GardenerController : SpeedModifiable
{
    // REMOVED: moveSpeed, currentMoveSpeed, baseMoveSpeed, and activeSpeedMultipliers
    // These are now handled by the SpeedModifiable base class.
    // The public 'moveSpeed' field is replaced by the protected 'baseSpeed' field from the base class,
    // which is serialized and will appear in the Inspector.

    [Header("Behavior")]
    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f);
    public bool flipSpriteWhenMovingLeft = true;
    public bool flipHorizontalDirection = true;
    
    [Header("Animation")]
    public bool useAnimations = true;
    public string runningParameterName = "isRunning";
    public string plantingParameterName = "isPlanting";
    public float plantingDuration = 0.25f;

    Rigidbody2D rb;
    Vector2 movement;
    SortableEntity sortableEntity;
    SpriteRenderer spriteRenderer;
    Animator animator;

    bool isPlanting = false;
    float plantingTimer = 0f;
    bool wasMovingBeforePlanting = false;

    // CHANGED: Awake now calls base.Awake() to initialize speed variables.
    protected override void Awake()
    {
        base.Awake(); // This is crucial: it sets currentSpeed = baseSpeed

        rb = GetComponent<Rigidbody2D>();
        sortableEntity = GetComponent<SortableEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (sortableEntity == null)
            sortableEntity = gameObject.AddComponent<SortableEntity>();

        if (spriteRenderer == null)
            Debug.LogWarning("[GardenerController Awake] Main SpriteRenderer component not found.", gameObject);

        if (animator == null && useAnimations)
            Debug.LogWarning("[GardenerController Awake] Animator component not found but useAnimations is true.", gameObject);
    }
    
    void Update()
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
    
    void FixedUpdate()
    {
        if (!isPlanting)
        {
            // CHANGED: Use `currentSpeed` from SpeedModifiable base class
            rb.MovePosition(rb.position + movement.normalized * currentSpeed * Time.fixedDeltaTime);
        }
    }
    
    // REMOVED: ApplySpeedMultiplier, RemoveSpeedMultiplier, and UpdateMovementSpeed are now in the base class.

    void UpdateAnimations()
    {
        if (!useAnimations || animator == null) return;
        bool isMoving = movement.sqrMagnitude > 0.01f;
        animator.SetBool(runningParameterName, isMoving);
    }

    void UpdateSpriteDirection()
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

    void EndPlantingAnimation()
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
// FILE: Assets/Scripts/Player/GardenerController.cs
using UnityEngine;
using System;

// No longer requires ToolSwitcher on the same GameObject
public class GardenerController : MonoBehaviour
{
    // --- Fields ---
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    [Header("Planting Settings")]
    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f);

    [Header("Tool References")] // <<< NEW HEADER
    [Tooltip("Assign the SpriteRenderer used to display the current tool's icon.")]
    [SerializeField] private SpriteRenderer toolIconRenderer;
    [Tooltip("Assign the GameObject or Component containing the ToolSwitcher script.")] // <<< NEW TOOLTIP
    [SerializeField] private ToolSwitcher toolSwitcherInstance; // <<< CHANGED: Reference field

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
    // ToolSwitcher reference is now toolSwitcherInstance (assigned via inspector)

    // Private variables
    private bool isPlanting = false;
    private float plantingTimer = 0f;
    private bool wasMovingBeforePlanting = false;

    private void Awake()
    {
        // --- Get Components (excluding ToolSwitcher) ---
        rb = GetComponent<Rigidbody2D>();
        sortableEntity = GetComponent<SortableEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (sortableEntity == null) sortableEntity = gameObject.AddComponent<SortableEntity>();

        // --- VALIDATIONS (Focus on assigned references) ---
        if (spriteRenderer == null) Debug.LogWarning("[GardenerController Awake] Main SpriteRenderer component not found.", gameObject);
        if (animator == null && useAnimations) Debug.LogWarning("[GardenerController Awake] Animator component not found but useAnimations is true.", gameObject);

        // Validate the explicitly assigned ToolSwitcher reference
        if (toolSwitcherInstance == null)
        {
            // This is now an error because it MUST be assigned in the inspector
            Debug.LogError("[GardenerController Awake] Tool Switcher Instance is not assigned in the Inspector! Tool switching and icon display will not function.", gameObject);
        }

        if (toolIconRenderer == null)
            Debug.LogError("[GardenerController Awake] Tool Icon Renderer is not assigned in the Inspector! Tool icons will not display.", gameObject);
        else
        {
            Debug.Log("[GardenerController Awake] Tool Icon Renderer found. Initializing as hidden.", gameObject);
            toolIconRenderer.enabled = false;
        }
    }

    private void Start()
    {
        // Subscribe ONLY if toolSwitcherInstance was assigned in the inspector
        if (toolSwitcherInstance != null)
        {
            Debug.Log("[GardenerController Start] ToolSwitcherInstance assigned. Subscribing to ToolSwitcher.OnToolChanged.", gameObject);
            toolSwitcherInstance.OnToolChanged += HandleToolChanged;

            // Manually trigger the handler once at the start
            Debug.Log("[GardenerController Start] Manually calling HandleToolChanged for initial tool.", gameObject);
            HandleToolChanged(toolSwitcherInstance.CurrentTool);
        }
        else
        {
             // Error logged in Awake, no need for more logs here.
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe ONLY if toolSwitcherInstance was assigned and we subscribed
        if (toolSwitcherInstance != null)
        {
            Debug.Log("[GardenerController OnDestroy] Unsubscribing from ToolSwitcher.OnToolChanged.", gameObject);
            toolSwitcherInstance.OnToolChanged -= HandleToolChanged;
        }
    }

    // --- Update, FixedUpdate, UpdateAnimations, UpdateSpriteDirection, Start/EndPlantingAnimation, GetPlantingPosition, SetPlantingDuration, Plant methods remain the same ---
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
    private void FixedUpdate() { if (!isPlanting) { rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime); } }
    private void UpdateAnimations() { if (!useAnimations || animator == null) return; bool isMoving = movement.sqrMagnitude > 0.01f; animator.SetBool(runningParameterName, isMoving); }
    private void UpdateSpriteDirection() { if (spriteRenderer == null || !flipSpriteWhenMovingLeft) return; if (movement.x != 0) { bool shouldFlip = (movement.x < 0); if (flipHorizontalDirection) { spriteRenderer.flipX = shouldFlip; } else { Vector3 scale = transform.localScale; scale.x = shouldFlip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x); transform.localScale = scale; } } }
    public void StartPlantingAnimation() { if (!useAnimations || isPlanting) return; isPlanting = true; plantingTimer = plantingDuration; wasMovingBeforePlanting = movement.sqrMagnitude > 0.01f; if (animator != null) { animator.SetBool(plantingParameterName, true); animator.SetBool(runningParameterName, false); } }
    private void EndPlantingAnimation() { isPlanting = false; if (animator != null) { animator.SetBool(plantingParameterName, false); movement.x = Input.GetAxisRaw("Horizontal"); movement.y = Input.GetAxisRaw("Vertical"); bool shouldResumeRunning = movement.sqrMagnitude > 0.01f; animator.SetBool(runningParameterName, shouldResumeRunning); } }
    public Vector2 GetPlantingPosition() { return (Vector2)transform.position + seedPlantingOffset; }
    public void SetPlantingDuration(float duration) { plantingDuration = Mathf.Max(0.1f, duration); }
    public void Plant() { StartPlantingAnimation(); }
    // ---------------------------------

    // --- HandleToolChanged remains the same (uses toolSwitcherInstance implicitly via Start/OnDestroy) ---
    private void HandleToolChanged(ToolDefinition newTool)
    {
        string toolName = newTool != null ? newTool.displayName : "NULL";
        Debug.Log($"[HandleToolChanged] Received tool: {toolName}", gameObject);

        if (toolIconRenderer == null)
        {
            Debug.LogError("[HandleToolChanged] toolIconRenderer is NULL. Cannot update icon.", gameObject);
            return;
        }

        if (newTool != null)
        {
            string iconName = newTool.icon != null ? newTool.icon.name : "NULL";
            Debug.Log($"[HandleToolChanged] Tool '{toolName}' has icon: {iconName}", gameObject);

            if (newTool.icon != null)
            {
                Debug.Log($"[HandleToolChanged] Assigning sprite '{newTool.icon.name}' and color '{newTool.iconTint}' to toolIconRenderer.", gameObject);
                toolIconRenderer.sprite = newTool.icon;
                toolIconRenderer.color = newTool.iconTint;
                Debug.Log($"[HandleToolChanged] Enabling toolIconRenderer. Current state before: {toolIconRenderer.enabled}", gameObject);
                toolIconRenderer.enabled = true;
                Debug.Log($"[HandleToolChanged] toolIconRenderer.enabled is now: {toolIconRenderer.enabled}", gameObject);
                Debug.Log($"[HandleToolChanged] toolIconRenderer.sprite is now: {toolIconRenderer.sprite?.name ?? "NULL"}", gameObject);
            }
            else
            {
                Debug.LogWarning($"[HandleToolChanged] Tool '{toolName}' has a NULL icon. Hiding renderer.", gameObject);
                toolIconRenderer.enabled = false;
                toolIconRenderer.sprite = null;
            }
        }
        else
        {
            Debug.Log("[HandleToolChanged] newTool is NULL. Hiding renderer.", gameObject);
            toolIconRenderer.enabled = false;
            toolIconRenderer.sprite = null;
        }
    }
}
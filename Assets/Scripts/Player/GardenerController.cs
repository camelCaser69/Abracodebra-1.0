// FILE: Assets/Scripts/Player/GardenerController.cs
using UnityEngine;
using System;
using System.Collections.Generic;

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

    [Header("Tool References")] // <<< NEW HEADER
    [Tooltip("Assign the SpriteRenderer used to display the current tool's icon.")]
    [SerializeField] private SpriteRenderer toolIconRenderer;
    

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

    void Awake()
    {
        // Store original components and references
        rb = GetComponent<Rigidbody2D>();
        sortableEntity = GetComponent<SortableEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Store base movement speed for multiplier system
        baseMoveSpeed = moveSpeed;
        currentMoveSpeed = moveSpeed;
    
        // Initialize speed multipliers list
        activeSpeedMultipliers = new List<float>();

        if (sortableEntity == null)
            sortableEntity = gameObject.AddComponent<SortableEntity>();

        // Validations for other components
        if (spriteRenderer == null)
            Debug.LogWarning("[GardenerController Awake] Main SpriteRenderer component not found.", gameObject);
    
        if (animator == null && useAnimations)
            Debug.LogWarning("[GardenerController Awake] Animator component not found but useAnimations is true.", gameObject);

        
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
        if (InventoryBarController.Instance != null)
        {
            InventoryBarController.Instance.OnSelectionChanged += HandleInventorySelectionChanged;
        }
    }

    private void OnDestroy()
    {
        activeSpeedMultipliers.Clear();
        if (InventoryBarController.Instance != null)
        {
            InventoryBarController.Instance.OnSelectionChanged -= HandleInventorySelectionChanged;
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
    
    private void HandleInventorySelectionChanged(InventoryBarItem selectedItem)
    {
        Debug.Log($"[GardenerController] HandleInventorySelectionChanged: {selectedItem?.GetDisplayName() ?? "NULL"}");
    
        if (selectedItem != null && selectedItem.Type == InventoryBarItem.ItemType.Tool)
        {
            // Just update the visual icon
            HandleToolChanged(selectedItem.ToolDefinition);
        }
        else
        {
            // Hide tool icon when not a tool
            HandleToolChanged(null);
        }
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
        // Start with base speed
        float newSpeed = baseMoveSpeed;
    
        // Apply all active multipliers
        if (activeSpeedMultipliers.Count > 0)
        {
            // Use the most restrictive (lowest) multiplier
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
    
        // Update the current move speed
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
            // Use currentMoveSpeed (with modifiers) instead of moveSpeed
            rb.MovePosition(rb.position + movement.normalized * currentMoveSpeed * Time.fixedDeltaTime);
        }
    }
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
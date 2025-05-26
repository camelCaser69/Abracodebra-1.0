// FILE: Assets/Scripts/Visuals/WaterReflection.cs
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Creates a water reflection effect by duplicating and flipping the sprite.
/// Simply attach to any GameObject with a SpriteRenderer to create its reflection.
/// </summary>
public class WaterReflection : MonoBehaviour
{
    [Header("Reflection Settings")]
    [Tooltip("Vertical offset of the reflection (negative values place it below the original)")]
    [SerializeField] private float yOffset = -1f;
    
    [Tooltip("Opacity of the reflection (0 = invisible, 1 = fully opaque)")]
    [SerializeField] [Range(0f, 1f)] private float reflectionOpacity = 0.5f;
    
    [Tooltip("Additional tint color for the reflection")]
    [SerializeField] private Color reflectionTint = Color.white;
    
    [Header("Distance Fade")]
    [Tooltip("Enable fading reflection based on distance from original")]
    [SerializeField] private bool enableDistanceFade = false;
    
    [Tooltip("Distance where fade starts (0 = no fade at origin)")]
    [SerializeField] private float fadeStartDistance = 0.5f;
    
    [Tooltip("Distance where reflection becomes fully transparent")]
    [SerializeField] private float fadeEndDistance = 2.0f;
    
    [Tooltip("Minimum alpha even when fully faded (0 = completely invisible)")]
    [SerializeField] [Range(0f, 1f)] private float minFadeAlpha = 0.0f;
    
    [Tooltip("Custom material with gradient fade shader (leave empty to auto-create)")]
    [SerializeField] private Material gradientFadeMaterial;
    
    [Header("Sorting")]
    [Tooltip("Sorting order offset for the reflection (usually negative to render behind)")]
    [SerializeField] private int sortingOrderOffset = -1;
    
    [Header("Water Masking")]
    [Tooltip("If enabled, reflection will only be visible over water tiles")]
    [SerializeField] private bool useWaterMasking = true;
    
    [Tooltip("Tag used to identify the water tilemap (default: 'Water')")]
    [SerializeField] private string waterTilemapTag = "Water";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // --- Internal References ---
    private SpriteRenderer originalRenderer;
    private Animator originalAnimator;
    private GameObject reflectionObject;
    private SpriteRenderer reflectionRenderer;
    private Animator reflectionAnimator;
    private SpriteMask waterMask;
    private Material originalReflectionMaterial;
    private Material currentGradientMaterial;
    
    // --- Cached Values ---
    private Sprite lastSprite;
    private Color lastColor;
    private bool lastFlipX;
    private bool lastEnabled;
    private Vector3 lastScale;

    void Awake()
    {
        // Get the original components
        originalRenderer = GetComponent<SpriteRenderer>();
        originalAnimator = GetComponent<Animator>();
        
        if (originalRenderer == null)
        {
            Debug.LogError($"[WaterReflection] No SpriteRenderer found on {gameObject.name}! Component disabled.", this);
            enabled = false;
            return;
        }
        
        CreateReflection();
        
        // Setup water masking if enabled
        if (useWaterMasking)
        {
            SetupWaterMasking();
        }
    }

    void Start()
    {
        // Initial sync after all components are initialized
        UpdateReflection();
    }

    void LateUpdate()
    {
        // Only update if something has changed (performance optimization)
        if (HasVisualStateChanged())
        {
            UpdateReflection();
            CacheCurrentState();
        }
        
        // Always update position (in case the parent moves)
        UpdateReflectionPosition();
    }

    private void CreateReflection()
    {
        // Create the reflection GameObject
        reflectionObject = new GameObject($"{gameObject.name}_Reflection");
        reflectionObject.transform.SetParent(transform.parent, false); // Same parent as original
        
        // Add SpriteRenderer
        reflectionRenderer = reflectionObject.AddComponent<SpriteRenderer>();
        
        // Copy sorting layer settings
        reflectionRenderer.sortingLayerName = originalRenderer.sortingLayerName;
        reflectionRenderer.sortingOrder = originalRenderer.sortingOrder + sortingOrderOffset;
        
        // Copy other basic properties
        reflectionRenderer.drawMode = originalRenderer.drawMode;
        reflectionRenderer.size = originalRenderer.size;
        reflectionRenderer.tileMode = originalRenderer.tileMode;
        
        // Store original material for later restoration
        originalReflectionMaterial = reflectionRenderer.material;
        
        // Setup distance fade material if enabled
        if (enableDistanceFade)
        {
            SetupGradientMaterial();
        }
        
        // Add Animator if original has one
        if (originalAnimator != null)
        {
            reflectionAnimator = reflectionObject.AddComponent<Animator>();
            reflectionAnimator.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;
            reflectionAnimator.avatar = originalAnimator.avatar;
            reflectionAnimator.applyRootMotion = originalAnimator.applyRootMotion;
            reflectionAnimator.updateMode = originalAnimator.updateMode;
            reflectionAnimator.cullingMode = originalAnimator.cullingMode;
        }
        
        // Add SortableEntity if the original has one (following your project's pattern)
        SortableEntity originalSortable = GetComponent<SortableEntity>();
        if (originalSortable != null)
        {
            SortableEntity reflectionSortable = reflectionObject.AddComponent<SortableEntity>();
            // Copy sortable settings if needed
        }
        
        if (showDebugInfo)
            Debug.Log($"[WaterReflection] Created reflection for {gameObject.name}", this);
    }

    private void SetupWaterMasking()
    {
        // Find water tilemap by tag
        GameObject waterTilemapGO = FindWaterTilemapByTag();
        
        if (waterTilemapGO == null)
        {
            Debug.LogWarning($"[WaterReflection] Could not find water tilemap with tag '{waterTilemapTag}' for masking on {gameObject.name}. Reflection will be visible everywhere.", this);
            return;
        }

        // Validate it has a Tilemap component
        if (waterTilemapGO.GetComponent<Tilemap>() == null)
        {
            Debug.LogError($"[WaterReflection] GameObject '{waterTilemapGO.name}' with tag '{waterTilemapTag}' is not a valid tilemap! Please ensure the tagged GameObject has a Tilemap component.", this);
            return;
        }

        // Get or create SpriteMask on the water tilemap
        waterMask = waterTilemapGO.GetComponent<SpriteMask>();
        if (waterMask == null)
        {
            waterMask = waterTilemapGO.AddComponent<SpriteMask>();
            waterMask.sprite = null; // Will use the tilemap itself as the mask
            waterMask.alphaCutoff = 0.1f;
            waterMask.isCustomRangeActive = false;
            
            if (showDebugInfo)
                Debug.Log($"[WaterReflection] Added SpriteMask to water tilemap: {waterTilemapGO.name}", this);
        }

        // Set the reflection to be masked by water
        if (reflectionRenderer != null)
        {
            reflectionRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            
            if (showDebugInfo)
                Debug.Log($"[WaterReflection] Applied water masking to reflection of {gameObject.name} using tilemap: {waterTilemapGO.name}", this);
        }
    }

    private GameObject FindWaterTilemapByTag()
    {
        // Try to find by tag first
        if (!string.IsNullOrEmpty(waterTilemapTag))
        {
            GameObject taggedWater = GameObject.FindGameObjectWithTag(waterTilemapTag);
            if (taggedWater != null)
            {
                // Validate it has a Tilemap component
                if (taggedWater.GetComponent<Tilemap>() != null)
                {
                    if (showDebugInfo)
                        Debug.Log($"[WaterReflection] Found water tilemap by tag '{waterTilemapTag}': {taggedWater.name}", this);
                    return taggedWater;
                }
                else
                {
                    Debug.LogWarning($"[WaterReflection] GameObject '{taggedWater.name}' has tag '{waterTilemapTag}' but no Tilemap component!", this);
                }
            }
        }

        // Fallback to auto-detection if tag search failed
        return FindWaterTilemapFallback();
    }

    private GameObject FindWaterTilemapFallback()
    {
        // Try to find water tilemap through TileInteractionManager (following your project structure)
        if (TileInteractionManager.Instance != null)
        {
            // Look through the tile definition mappings for water tiles
            var mappings = TileInteractionManager.Instance.tileDefinitionMappings;
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (mapping?.tileDef != null && mapping.tilemapModule != null)
                    {
                        // Check if this tile definition is marked as water
                        if (mapping.tileDef.isWaterTile)
                        {
                            // Find the RenderTilemap child (which actually renders the water)
                            Transform renderTilemap = mapping.tilemapModule.transform.Find("RenderTilemap");
                            if (renderTilemap != null)
                            {
                                if (showDebugInfo)
                                    Debug.Log($"[WaterReflection] Auto-detected water tilemap via TileInteractionManager: {renderTilemap.name}", this);
                                return renderTilemap.gameObject;
                            }
                        }
                    }
                }
            }
        }

        // Last resort: search for common water tilemap names
        string[] commonWaterNames = { "Water", "WaterTilemap", "RenderTilemap" };
        foreach (string name in commonWaterNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null && found.GetComponent<Tilemap>() != null)
            {
                // Double-check if it's actually water by checking parent names or components
                if (found.name.ToLower().Contains("water") || 
                    (found.transform.parent != null && found.transform.parent.name.ToLower().Contains("water")))
                {
                    if (showDebugInfo)
                        Debug.Log($"[WaterReflection] Auto-detected water tilemap by name search: {found.name}", this);
                    return found;
                }
            }
        }

        return null;
    }

    private void UpdateReflection()
    {
        if (reflectionRenderer == null) return;
        
        // Sync sprite
        reflectionRenderer.sprite = originalRenderer.sprite;
        
        // Apply reflection transformations
        reflectionRenderer.flipX = originalRenderer.flipX; // Keep same X flip as original
        reflectionRenderer.flipY = !originalRenderer.flipY; // Flip vertically for reflection
        
        // Apply color with opacity and tint
        Color originalColor = originalRenderer.color;
        Color finalColor = originalColor * reflectionTint;
        
        // Apply base opacity
        finalColor.a = originalColor.a * reflectionOpacity;
        
        reflectionRenderer.color = finalColor;
        
        // Update gradient fade if enabled
        if (enableDistanceFade && currentGradientMaterial != null)
        {
            UpdateGradientMaterial();
        }
        
        // Sync enabled state
        reflectionRenderer.enabled = originalRenderer.enabled;
        
        // Sync scale
        reflectionObject.transform.localScale = transform.localScale;
        
        // Sync animator state if present
        if (reflectionAnimator != null && originalAnimator != null)
        {
            reflectionAnimator.enabled = originalAnimator.enabled;
            
            // Sync animation parameters (basic sync)
            if (originalAnimator.parameterCount > 0)
            {
                foreach (AnimatorControllerParameter param in originalAnimator.parameters)
                {
                    try
                    {
                        switch (param.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                reflectionAnimator.SetBool(param.name, originalAnimator.GetBool(param.name));
                                break;
                            case AnimatorControllerParameterType.Float:
                                reflectionAnimator.SetFloat(param.name, originalAnimator.GetFloat(param.name));
                                break;
                            case AnimatorControllerParameterType.Int:
                                reflectionAnimator.SetInteger(param.name, originalAnimator.GetInteger(param.name));
                                break;
                            case AnimatorControllerParameterType.Trigger:
                                // Triggers are tricky to sync, skip for simplicity
                                break;
                        }
                    }
                    catch
                    {
                        // Ignore parameter sync errors (parameter might not exist in reflection)
                    }
                }
            }
        }
    }

    private void UpdateReflectionPosition()
    {
        if (reflectionObject == null) return;
        
        // Position the reflection
        Vector3 reflectionPos = transform.position;
        reflectionPos.y += yOffset;
        reflectionObject.transform.position = reflectionPos;
        
        // Keep the same rotation as original
        reflectionObject.transform.rotation = transform.rotation;
    }

    private bool HasVisualStateChanged()
    {
        if (originalRenderer == null) return false;
        
        return lastSprite != originalRenderer.sprite ||
               lastColor != originalRenderer.color ||
               lastFlipX != originalRenderer.flipX ||
               lastEnabled != originalRenderer.enabled ||
               lastScale != transform.localScale;
    }

    private void CacheCurrentState()
    {
        if (originalRenderer == null) return;
        
        lastSprite = originalRenderer.sprite;
        lastColor = originalRenderer.color;
        lastFlipX = originalRenderer.flipX;
        lastEnabled = originalRenderer.enabled;
        lastScale = transform.localScale;
    }

    private float CalculateDistanceFadeAlpha(float distance)
    {
        if (distance <= fadeStartDistance)
        {
            return 1.0f; // No fade yet
        }
        else if (distance >= fadeEndDistance)
        {
            return minFadeAlpha; // Fully faded
        }
        else
        {
            // Interpolate between full alpha and min alpha
            float t = Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, distance);
            return Mathf.Lerp(1.0f, minFadeAlpha, t);
        }
    }

    private void SetupGradientMaterial()
    {
        if (reflectionRenderer == null) return;

        // Use provided material or create one with the gradient shader
        if (gradientFadeMaterial != null)
        {
            currentGradientMaterial = new Material(gradientFadeMaterial);
        }
        else
        {
            // Try to find the gradient shader
            Shader gradientShader = Shader.Find("Custom/WaterReflectionGradient");
            if (gradientShader != null)
            {
                currentGradientMaterial = new Material(gradientShader);
            }
            else
            {
                Debug.LogWarning($"[WaterReflection] Custom gradient shader not found. Create 'WaterReflectionGradient.shader' for proper gradient fading. Using simple fade instead.", this);
                currentGradientMaterial = new Material(originalReflectionMaterial);
            }
        }

        reflectionRenderer.material = currentGradientMaterial;
        UpdateGradientMaterial();
    }

    private void UpdateGradientMaterial()
    {
        if (currentGradientMaterial == null || reflectionRenderer == null) return;

        // Update shader properties for gradient fade
        if (currentGradientMaterial.HasProperty("_FadeStart"))
        {
            currentGradientMaterial.SetFloat("_FadeStart", fadeStartDistance);
        }
        
        if (currentGradientMaterial.HasProperty("_FadeEnd"))
        {
            currentGradientMaterial.SetFloat("_FadeEnd", fadeEndDistance);
        }
        
        if (currentGradientMaterial.HasProperty("_MinAlpha"))
        {
            currentGradientMaterial.SetFloat("_MinAlpha", minFadeAlpha);
        }
        
        if (currentGradientMaterial.HasProperty("_OriginalY"))
        {
            // Pass the original object's world Y position to the shader
            currentGradientMaterial.SetFloat("_OriginalY", transform.position.y);
        }
    }

    private void RestoreOriginalMaterial()
    {
        if (reflectionRenderer != null && originalReflectionMaterial != null)
        {
            reflectionRenderer.material = originalReflectionMaterial;
        }
        
        if (currentGradientMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(currentGradientMaterial);
            }
            else
            {
                DestroyImmediate(currentGradientMaterial);
            }
            currentGradientMaterial = null;
        }
    }

    void OnDestroy()
    {
        // Clean up the reflection object
        if (reflectionObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(reflectionObject);
            }
            else
            {
                DestroyImmediate(reflectionObject);
            }
        }
        
        // Clean up gradient material
        RestoreOriginalMaterial();
    }

    void OnValidate()
    {
        // Update reflection in editor when values change
        if (reflectionRenderer != null && originalRenderer != null)
        {
            UpdateReflection();
            UpdateReflectionPosition();
        }
    }

    // --- Public Methods for Runtime Control ---
    
    /// <summary>
    /// Set the reflection opacity at runtime
    /// </summary>
    public void SetReflectionOpacity(float opacity)
    {
        reflectionOpacity = Mathf.Clamp01(opacity);
        UpdateReflection();
    }
    
    /// <summary>
    /// Set the Y offset of the reflection at runtime
    /// </summary>
    public void SetYOffset(float offset)
    {
        yOffset = offset;
        UpdateReflectionPosition();
    }
    
    /// <summary>
    /// Set the reflection tint color at runtime
    /// </summary>
    public void SetReflectionTint(Color tint)
    {
        reflectionTint = tint;
        UpdateReflection();
    }
    
    /// <summary>
    /// Enable or disable the reflection
    /// </summary>
    public void SetReflectionEnabled(bool enabled)
    {
        if (reflectionObject != null)
        {
            reflectionObject.SetActive(enabled);
        }
    }
    
    /// <summary>
    /// Enable or disable water masking at runtime
    /// </summary>
    public void SetWaterMasking(bool enabled)
    {
        useWaterMasking = enabled;
        
        if (reflectionRenderer != null)
        {
            if (enabled)
            {
                SetupWaterMasking();
            }
            else
            {
                reflectionRenderer.maskInteraction = SpriteMaskInteraction.None;
                if (showDebugInfo)
                    Debug.Log($"[WaterReflection] Disabled water masking for {gameObject.name}", this);
            }
        }
    }
    
    /// <summary>
    /// Enable or disable distance fade at runtime
    /// </summary>
    public void SetDistanceFade(bool enabled)
    {
        enableDistanceFade = enabled;
        
        if (enabled)
        {
            SetupGradientMaterial();
        }
        else
        {
            RestoreOriginalMaterial();
        }
        
        UpdateReflection(); // Refresh to apply/remove fade
    }
    
    /// <summary>
    /// Set distance fade parameters at runtime
    /// </summary>
    public void SetDistanceFadeParams(float startDistance, float endDistance, float minAlpha = 0f)
    {
        fadeStartDistance = startDistance;
        fadeEndDistance = Mathf.Max(startDistance, endDistance); // Ensure end >= start
        minFadeAlpha = Mathf.Clamp01(minAlpha);
        
        if (enableDistanceFade)
        {
            UpdateReflection(); // Refresh to apply new parameters
        }
    }
}
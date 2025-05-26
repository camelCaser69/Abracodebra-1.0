// FILE: Assets/Scripts/Visuals/WaterReflection.cs
using UnityEngine;
using UnityEngine.Tilemaps; // Keep for water masking logic

/// <summary>
/// Creates a water reflection effect by duplicating and flipping the sprite.
/// Attach to any GameObject with a SpriteRenderer to create its reflection.
/// Can optionally use parent's transform for offset and fade calculations.
/// </summary>
public class WaterReflection : MonoBehaviour
{
    [Header("Reflection Source")]
    [Tooltip("If true, Y Offset and Distance Fade calculations will be relative to this GameObject's parent. If false (default), relative to this GameObject.")]
    [SerializeField] private bool useParentAsReference = false;

    [Header("Reflection Settings")]
    [Tooltip("Vertical offset of the reflection. Interpretation depends on 'Use Parent As Reference'.")]
    [SerializeField] private float yOffset = -1f;

    [Tooltip("Opacity of the reflection (0 = invisible, 1 = fully opaque)")]
    [SerializeField] [Range(0f, 1f)] private float reflectionOpacity = 0.5f;

    [Tooltip("Additional tint color for the reflection")]
    [SerializeField] private Color reflectionTint = Color.white;

    [Header("Distance Fade (Shader Controlled)")]
    [Tooltip("Enable fading reflection. Requires 'Gradient Fade Material' to be assigned.")]
    [SerializeField] private bool enableDistanceFade = true; // True by default if using the shader
    [Tooltip("Vertical distance from the reference Y (self or parent) where fade starts (shader's _FadeStart).")]
    [SerializeField] private float fadeStartDistance = 0.0f;
    [Tooltip("Vertical distance from the reference Y (self or parent) where reflection becomes min alpha (shader's _FadeEnd).")]
    [SerializeField] private float fadeEndDistance = 1.0f;
    [Tooltip("Minimum alpha when fully faded (shader's _MinAlpha).")]
    [SerializeField] [Range(0f, 1f)] private float minFadeAlpha = 0.0f;
    [Tooltip("Assign the 'Custom/WaterReflectionGradient' material, or one using that shader.")]
    [SerializeField] private Material gradientFadeBaseMaterial; // Base material to instance from

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
    private Animator originalAnimator; // Keep for potential animator sync
    private GameObject reflectionObject;
    private SpriteRenderer reflectionRenderer;
    private Animator reflectionAnimator; // Keep for potential animator sync
    private Material reflectionMaterialInstance; // Instanced material for this reflection

    // --- Cached Values for Optimization ---
    private Sprite lastSprite;
    private Color lastOriginalColor;
    private bool lastFlipX, lastFlipY;
    private bool lastEnabled;
    private Vector3 lastScale; // Original's scale
    private Vector3 lastPosition; // Original's position
    private float lastParentY; // For parent-relative mode

    void Awake()
    {
        originalRenderer = GetComponent<SpriteRenderer>();
        originalAnimator = GetComponent<Animator>(); // Get animator if present

        if (originalRenderer == null)
        {
            Debug.LogError($"[WaterReflection] No SpriteRenderer found on {gameObject.name}! Component disabled.", this);
            enabled = false;
            return;
        }

        if (useParentAsReference && transform.parent == null)
        {
            if (showDebugInfo) Debug.LogWarning($"[WaterReflection] 'Use Parent As Reference' is true on {gameObject.name}, but it has no parent. Will use self as reference.", this);
            useParentAsReference = false; // Fallback to self if no parent
        }

        if (enableDistanceFade && gradientFadeBaseMaterial == null)
        {
            Debug.LogWarning($"[WaterReflection] 'Enable Distance Fade' is true, but 'Gradient Fade Base Material' is not assigned on {gameObject.name}. Fading will use simple alpha if shader is unavailable. Assign a material using 'Custom/WaterReflectionGradient'.", this);
            // Don't disable fade entirely, can fallback to CPU alpha if shader params can't be set
        }

        CreateReflectionObject();

        if (useWaterMasking)
        {
            SetupWaterMaskingInteraction();
        }
    }

    void Start()
    {
        UpdateReflectionVisuals(); // Initial full update
        UpdateReflectionTransform();
        CacheCurrentState();
    }

    void LateUpdate()
    {
        if (originalRenderer == null || reflectionObject == null)
        {
            if (reflectionObject != null) reflectionObject.SetActive(false);
            return;
        }

        // Always update transform in case original or parent moves
        UpdateReflectionTransform();

        if (HasVisualStateChanged())
        {
            UpdateReflectionVisuals();
            CacheCurrentState();
        }
    }

    private void CreateReflectionObject()
    {
        reflectionObject = new GameObject($"{gameObject.name}_Reflection");
        // Parent the reflection to the same parent as the original object
        reflectionObject.transform.SetParent(transform.parent, false);
        // Try to place it after original in hierarchy for neatness
        reflectionObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);


        reflectionRenderer = reflectionObject.AddComponent<SpriteRenderer>();
        reflectionRenderer.sortingLayerName = originalRenderer.sortingLayerName;
        reflectionRenderer.sortingOrder = originalRenderer.sortingOrder + sortingOrderOffset;
        reflectionRenderer.drawMode = originalRenderer.drawMode;
        // Size, tilemode, etc., will be synced from originalRenderer

        if (enableDistanceFade && gradientFadeBaseMaterial != null)
        {
            reflectionMaterialInstance = new Material(gradientFadeBaseMaterial);
            reflectionRenderer.material = reflectionMaterialInstance;
        }
        else
        {
            reflectionRenderer.sharedMaterial = originalRenderer.sharedMaterial; // Use shared material if not fading
        }

        // Animator (basic copy, same as your original script)
        if (originalAnimator != null)
        {
            reflectionAnimator = reflectionObject.AddComponent<Animator>();
            reflectionAnimator.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;
            // Copy other animator properties if needed, e.g., avatar, updateMode etc.
        }
        
        // Add SortableEntity if the original has one (following your project's pattern)
        SortableEntity originalSortable = GetComponent<SortableEntity>();
        if (originalSortable != null)
        {
            SortableEntity reflectionSortable = reflectionObject.AddComponent<SortableEntity>();
            // Optionally copy properties from originalSortable to reflectionSortable
            // For example, if useParentYCoordinate is relevant for the reflection itself:
            // reflectionSortable.SetUseParentYCoordinate(originalSortable.GetUseParentYCoordinate());
            // However, reflection's Y sorting might need custom logic or to follow the original closely.
        }


        if (showDebugInfo) Debug.Log($"[WaterReflection] Created reflection for {gameObject.name}", this);
    }

    private void UpdateReflectionTransform()
    {
        if (reflectionObject == null || originalRenderer == null) return;

        Transform referenceTransform = (useParentAsReference && transform.parent != null) ? transform.parent : transform;

        // --- Position ---
        // Reflection's world position: Start with original's world position,
        // then apply yOffset relative to the *referenceTransform's* Y.
        // The reflection object is a sibling, so its local position needs to achieve this.
        Vector3 originalWorldPos = transform.position;
        Vector3 reflectionWorldPos = originalWorldPos;

        // If using parent as reference, the yOffset is from the parent's Y.
        // Otherwise, it's from the original object's Y.
        // This is subtly different from just parenting and setting local offset.
        // We calculate desired world Y then convert to local Y for reflectionObject.
        
        float referenceYForOffset = referenceTransform.position.y;
        reflectionWorldPos.y = referenceYForOffset + yOffset - (originalWorldPos.y - referenceYForOffset);

        reflectionObject.transform.position = reflectionWorldPos;


        // --- Rotation ---
        // Reflection should match original's world rotation.
        reflectionObject.transform.rotation = transform.rotation;

        // --- Scale ---
        // Reflection scale Y is flipped relative to its *own local space* after matching original's world scale.
        reflectionObject.transform.localScale = transform.localScale; // Match world scale first
        Vector3 currentLocalScale = reflectionObject.transform.localScale;
        currentLocalScale.y *= -1; // Flip Y locally
        reflectionObject.transform.localScale = currentLocalScale;
    }


    private void UpdateReflectionVisuals()
    {
        if (reflectionRenderer == null || originalRenderer == null) return;

        // Sync sprite and basic properties
        reflectionRenderer.sprite = originalRenderer.sprite;
        reflectionRenderer.flipX = originalRenderer.flipX;
        // Y-flip for the sprite itself is handled by the reflectionObject's scale.y *= -1;
        // So, reflectionRenderer.flipY should match originalRenderer.flipY
        reflectionRenderer.flipY = originalRenderer.flipY;


        // --- Color, Tint, Opacity ---
        Color baseOriginalSpriteColor = originalRenderer.color;
        Color finalReflectionTintedColor = baseOriginalSpriteColor * reflectionTint;
        float finalCombinedAlpha = baseOriginalSpriteColor.a * reflectionOpacity;

        // Apply to SpriteRenderer color
        reflectionRenderer.color = new Color(finalReflectionTintedColor.r, finalReflectionTintedColor.g, finalReflectionTintedColor.b, finalCombinedAlpha);

        // --- Shader Parameters for Distance Fade ---
        if (enableDistanceFade && reflectionMaterialInstance != null)
        {
            // _OriginalY for the shader should be the Y of the "water surface"
            // This is the Y of the reference transform (self or parent)
            Transform referenceTransform = (useParentAsReference && transform.parent != null) ? transform.parent : transform;
            float waterSurfaceY = referenceTransform.position.y;

            reflectionMaterialInstance.SetFloat("_FadeStart", fadeStartDistance);
            reflectionMaterialInstance.SetFloat("_FadeEnd", fadeEndDistance);
            reflectionMaterialInstance.SetFloat("_MinAlpha", minFadeAlpha);
            reflectionMaterialInstance.SetFloat("_OriginalY", waterSurfaceY);

            // The shader's _Color property (tint) should incorporate our global tint and opacity settings
            // The SpriteRenderer.color already has this, so we can pass it or reconstruct it.
            // Let's pass a color to the material that reflects the desired tint *before* the shader's own fade.
            Color materialBaseColor = reflectionTint; // Start with the script's global tint
            materialBaseColor.a = reflectionOpacity * baseOriginalSpriteColor.a; // Combine global opacity with original sprite alpha
            reflectionMaterialInstance.SetColor("_Color", materialBaseColor);
        }
        else if (!enableDistanceFade && reflectionMaterialInstance != null)
        {
            // If fade was disabled but we have an instanced material, revert to shared
            reflectionRenderer.sharedMaterial = originalRenderer.sharedMaterial;
            Destroy(reflectionMaterialInstance);
            reflectionMaterialInstance = null;
        }


        // Sync enabled state
        reflectionRenderer.enabled = originalRenderer.enabled && originalRenderer.gameObject.activeInHierarchy;

        // Sync animator state (basic, if present)
        if (reflectionAnimator != null && originalAnimator != null)
        {
            reflectionAnimator.enabled = originalAnimator.enabled;
            if (originalAnimator.runtimeAnimatorController != null && originalAnimator.parameterCount > 0)
            {
                foreach (AnimatorControllerParameter param in originalAnimator.parameters)
                {
                    try { // Defensive coding for parameter sync
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
                        }
                    } catch (System.Exception e) {
                        if(showDebugInfo) Debug.LogWarning($"Failed to sync animator param '{param.name}': {e.Message}", reflectionAnimator);
                    }
                }
            }
        }
    }

    private bool HasVisualStateChanged()
    {
        if (originalRenderer == null) return false;

        bool parentYChanged = false;
        if (useParentAsReference && transform.parent != null)
        {
            parentYChanged = !Mathf.Approximately(lastParentY, transform.parent.position.y);
        }

        return lastSprite != originalRenderer.sprite ||
               !ColorsApproximatelyEqual(lastOriginalColor, originalRenderer.color) ||
               lastFlipX != originalRenderer.flipX ||
               lastFlipY != originalRenderer.flipY ||
               lastEnabled != (originalRenderer.enabled && originalRenderer.gameObject.activeInHierarchy) ||
               lastScale != transform.localScale || // Check original's scale
               lastPosition != transform.position || // Check original's position
               parentYChanged; // Check parent's Y if relevant for fade
    }

    private void CacheCurrentState()
    {
        if (originalRenderer == null) return;
        lastSprite = originalRenderer.sprite;
        lastOriginalColor = originalRenderer.color;
        lastFlipX = originalRenderer.flipX;
        lastFlipY = originalRenderer.flipY;
        lastEnabled = originalRenderer.enabled && originalRenderer.gameObject.activeInHierarchy;
        lastScale = transform.localScale;
        lastPosition = transform.position;
        if (useParentAsReference && transform.parent != null)
        {
            lastParentY = transform.parent.position.y;
        }
    }

    private bool ColorsApproximatelyEqual(Color c1, Color c2, float tolerance = 0.001f)
    {
        return Mathf.Abs(c1.r - c2.r) < tolerance &&
               Mathf.Abs(c1.g - c2.g) < tolerance &&
               Mathf.Abs(c1.b - c2.b) < tolerance &&
               Mathf.Abs(c1.a - c2.a) < tolerance;
    }

    private void SetupWaterMaskingInteraction() // Renamed from SetupWaterMasking
    {
        if (!useWaterMasking || reflectionRenderer == null) return;

        GameObject waterTilemapGO = FindWaterTilemapByTag();
        if (waterTilemapGO == null)
        {
            if (showDebugInfo) Debug.LogWarning($"[WaterReflection] Water tilemap for masking not found on {gameObject.name} via tag '{waterTilemapTag}'. Masking disabled.", this);
            useWaterMasking = false; // Disable if not found, to prevent errors
            return;
        }

        // Ensure the found water tilemap object has a SpriteMask component.
        // The reflection renderer will interact with this mask.
        SpriteMask maskComponent = waterTilemapGO.GetComponent<SpriteMask>();
        if (maskComponent == null)
        {
            maskComponent = waterTilemapGO.AddComponent<SpriteMask>();
            maskComponent.sprite = null; // Use the Tilemap's shape for the mask
            // Configure other SpriteMask properties if needed (e.g., sorting)
            if (showDebugInfo) Debug.Log($"[WaterReflection] Added SpriteMask to water tilemap '{waterTilemapGO.name}' for object '{gameObject.name}'.", waterTilemapGO);
        }

        reflectionRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        if (showDebugInfo) Debug.Log($"[WaterReflection] Reflection of '{gameObject.name}' will be masked by '{waterTilemapGO.name}'.", this);
    }

    // FindWaterTilemapByTag and FindWaterTilemapFallback:
    // These can remain mostly the same as in your original script or my previous hierarchical version.
    // The key is that they reliably find the GameObject that has the SpriteMask for the water.
    private GameObject FindWaterTilemapByTag()
    {
        if (string.IsNullOrEmpty(waterTilemapTag)) return FindWaterTilemapFallback();
        GameObject taggedWater = GameObject.FindGameObjectWithTag(waterTilemapTag);
        if (taggedWater != null && taggedWater.GetComponent<Tilemap>() != null)
        {
            if (showDebugInfo) Debug.Log($"[WaterReflection] Found water tilemap by tag '{waterTilemapTag}': {taggedWater.name} for {gameObject.name}", this);
            return taggedWater;
        }
        if (taggedWater != null && taggedWater.GetComponent<Tilemap>() == null && showDebugInfo)
        {
            Debug.LogWarning($"[WaterReflection] GameObject '{taggedWater.name}' (tag '{waterTilemapTag}') has no Tilemap component!", this);
        }
        return FindWaterTilemapFallback();
    }

    private GameObject FindWaterTilemapFallback()
    {
        if (TileInteractionManager.Instance != null)
        {
            var mappings = TileInteractionManager.Instance.tileDefinitionMappings;
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (mapping?.tileDef != null && mapping.tilemapModule != null && mapping.tileDef.isWaterTile)
                    {
                        // The mask should be on the object that has the Tilemap for rendering water.
                        // This is likely the "RenderTilemap" child of the DualGridTilemapModule.
                        Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                        if (renderTilemapTransform != null && renderTilemapTransform.GetComponent<Tilemap>() != null)
                        {
                            if (showDebugInfo) Debug.Log($"[WaterReflection] Auto-detected water tilemap via TIM: {renderTilemapTransform.name} for {gameObject.name}", this);
                            return renderTilemapTransform.gameObject;
                        }
                    }
                }
            }
        }
        if (showDebugInfo) Debug.LogWarning($"[WaterReflection] Could not auto-detect water tilemap via TileInteractionManager for {gameObject.name}.", this);
        return null;
    }


    void OnDestroy()
    {
        if (reflectionObject != null)
        {
            if (Application.isPlaying) Destroy(reflectionObject);
            else DestroyImmediate(reflectionObject);
        }
        if (reflectionMaterialInstance != null)
        {
            if (Application.isPlaying) Destroy(reflectionMaterialInstance);
            else DestroyImmediate(reflectionMaterialInstance);
        }
    }

    void OnValidate()
    {
        if (gradientFadeBaseMaterial == null && enableDistanceFade && Application.isEditor && !Application.isPlaying)
        {
             Debug.LogWarning($"[WaterReflection] OnValidate: 'Enable Distance Fade' is true, but 'Gradient Fade Base Material' is not assigned on {gameObject.name}. Fading will not work as intended in Play mode unless assigned.", this);
        }

        if (Application.isPlaying) return; // Avoid frequent updates in play mode from OnValidate

        // Update reflection in editor when values change
        // Need to be careful with instantiating materials in editor via OnValidate.
        // For simplicity, we'll just ensure basic visual properties that don't involve material instancing are updated.
        if (reflectionRenderer != null && originalRenderer != null)
        {
            // Basic visual sync that's safe for editor
            reflectionRenderer.sprite = originalRenderer.sprite;
            reflectionRenderer.flipX = originalRenderer.flipX;
            reflectionRenderer.flipY = originalRenderer.flipY;
            reflectionRenderer.sortingOrder = originalRenderer.sortingOrder + sortingOrderOffset;

            Color baseOriginalSpriteColor = originalRenderer.color;
            Color finalReflectionTintedColor = baseOriginalSpriteColor * reflectionTint;
            float finalCombinedAlpha = baseOriginalSpriteColor.a * reflectionOpacity;
            reflectionRenderer.color = new Color(finalReflectionTintedColor.r, finalReflectionTintedColor.g, finalReflectionTintedColor.b, finalCombinedAlpha);

            UpdateReflectionTransform(); // Keep transform updated
        }
    }

    // --- Public Methods for Runtime Control ---
    public void SetReflectionOpacity(float opacity)
    {
        reflectionOpacity = Mathf.Clamp01(opacity);
        if (Application.isPlaying) UpdateReflectionVisuals();
    }
    public void SetYOffset(float offset)
    {
        yOffset = offset;
        if (Application.isPlaying) UpdateReflectionTransform();
    }
    public void SetReflectionTint(Color tint)
    {
        reflectionTint = tint;
        if (Application.isPlaying) UpdateReflectionVisuals();
    }
    public void SetReflectionEnabled(bool enabled)
    {
        if (reflectionObject != null) reflectionObject.SetActive(enabled);
    }
    // Add setters for fade parameters if needed
} 
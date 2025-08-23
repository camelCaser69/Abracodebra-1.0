using UnityEngine;
using UnityEngine.Tilemaps;
using WegoSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaterReflection : MonoBehaviour
{
    // --- SECTION: Override Toggles ---
    [System.Serializable]
    public class OverrideSettings
    {
        [Tooltip("If checked, the local 'Reflection Opacity' value will be used instead of the global manager's default.")]
        public bool reflectionOpacity = false;
        [Tooltip("If checked, the local 'Reflection Tint' value will be used instead of the global manager's default.")]
        public bool reflectionTint = false;
        [Tooltip("If checked, the local 'Gradient Fade Base Material' will be used. Otherwise, manager's default is used.")]
        public bool gradientFadeBaseMaterial = false;
        [Tooltip("If checked, the local 'Sorting Order Offset' value will be used instead of the global manager's default.")]
        public bool sortingOrderOffset = false;
        [Tooltip("If checked, the local 'Use Water Masking' value will be used instead of the global manager's default.")]
        public bool useWaterMasking = false;
        [Tooltip("If checked, the local 'Water Tilemap Tag' value will be used instead of the global manager's default.")]
        public bool waterTilemapTag = false;
        [Tooltip("If checked, the local 'Show Debug Info' value will be used instead of the global manager's default.")]
        public bool showDebugInfo = false;
    }
    [Header("Overrides (Global Defaults from WaterReflectionManager)")]
    [SerializeField] private OverrideSettings overrides;


    // --- SECTION: Local Settings (Used if Overridden) ---
    [Header("Reflection Source")]
    [Tooltip("If true, Y Offset and Distance Fade calculations will be relative to this GameObject's parent. If false (default), relative to this GameObject.")]
    [SerializeField] private bool useParentAsReference = false; // This remains a local setting

    [Header("Local Reflection Settings (If Overridden)")]
    [Tooltip("Vertical offset of the reflection. Interpretation depends on 'Use Parent As Reference'.")]
    [SerializeField] private float yOffset = -1f; // This remains a local setting

    [Tooltip("Local opacity of the reflection (0 = invisible, 1 = fully opaque)")]
    [SerializeField] [Range(0f, 1f)] private float localReflectionOpacity = 0.5f;

    [Tooltip("Local additional tint color for the reflection")]
    [SerializeField] private Color localReflectionTint = Color.white;

    [Header("Local Distance Fade (If Overridden)")]
    [Tooltip("Enable fading reflection. Requires 'Gradient Fade Base Material' (local or global) to be assigned.")]
    [SerializeField] private bool enableDistanceFade = true; // This remains local as it depends on material
    [Tooltip("Vertical distance from the reference Y where fade starts.")]
    [SerializeField] private float fadeStartDistance = 0.0f; // Local
    [Tooltip("Vertical distance from the reference Y where reflection becomes min alpha.")]
    [SerializeField] private float fadeEndDistance = 1.0f; // Local
    [Tooltip("Minimum alpha when fully faded.")]
    [SerializeField] [Range(0f, 1f)] private float minFadeAlpha = 0.0f; // Local
    [Tooltip("Local override for the gradient fade material. If unassigned and override is false, uses manager's default.")]
    [SerializeField] private Material localGradientFadeBaseMaterial;

    [Header("Local Sorting (If Overridden)")]
    [Tooltip("Local sorting order offset for the reflection")]
    [SerializeField] private int localSortingOrderOffset = -1;

    [Header("Local Water Masking (If Overridden)")]
    [Tooltip("Local override for using water masking")]
    [SerializeField] private bool localUseWaterMasking = true;
    [Tooltip("Local override for the water tilemap tag")]
    [SerializeField] private string localWaterTilemapTag = "Water";

    [Header("Local Debug (If Overridden)")]
    [SerializeField] private bool localShowDebugInfo = false;


    // --- Internal References ---
    private SpriteRenderer originalRenderer;
    private Animator originalAnimator;
    private GameObject reflectionObject;
    private SpriteRenderer reflectionRenderer;
    private Animator reflectionAnimator;
    private Material reflectionMaterialInstance; // Instanced material for this reflection

    // --- Resolved Settings (from Manager or Local) ---
    private float _actualReflectionOpacity;
    private Color _actualReflectionTint;
    private Material _actualGradientFadeBaseMaterial;
    private int _actualSortingOrderOffset;
    private bool _actualUseWaterMasking;
    private string _actualWaterTilemapTag;
    private bool _actualShowDebugInfo;


    // --- Cached Values for Optimization ---
    private Sprite lastSprite;
    private Color lastOriginalColor;
    private bool lastFlipX, lastFlipY;
    private bool lastEnabled;
    private Vector3 lastScale;
    private Vector3 lastPosition;
    private float lastParentY;
    
    #region Unity Lifecycle

    void Awake()
    {
        // Initialize with local settings first, will be updated in Start if manager exists
        _actualReflectionOpacity = localReflectionOpacity;
        _actualReflectionTint = localReflectionTint;
        _actualGradientFadeBaseMaterial = localGradientFadeBaseMaterial;
        _actualSortingOrderOffset = localSortingOrderOffset;
        _actualUseWaterMasking = localUseWaterMasking;
        _actualWaterTilemapTag = localWaterTilemapTag;
        _actualShowDebugInfo = localShowDebugInfo;

        originalRenderer = GetComponent<SpriteRenderer>();
        originalAnimator = GetComponent<Animator>();

        if (originalRenderer == null)
        {
            if (_actualShowDebugInfo) Debug.LogError($"[WaterReflection] No SpriteRenderer found on {gameObject.name}! Component disabled.", this);
            enabled = false;
            return;
        }

        if (useParentAsReference && transform.parent == null)
        {
            if (_actualShowDebugInfo) Debug.LogWarning($"[WaterReflection] 'Use Parent As Reference' is true on {gameObject.name}, but it has no parent. Will use self as reference.", this);
            useParentAsReference = false;
        }

        if (Application.isPlaying)
        {
            if (enableDistanceFade && _actualGradientFadeBaseMaterial == null)
            {
                 if (_actualShowDebugInfo) Debug.LogWarning($"[WaterReflection Awake] '{gameObject.name}': 'Enable Distance Fade' is true, but no 'Gradient Fade Base Material' (local or global) is assigned/found. Distance fade will not use the custom shader.", this);
            }
        }

        CreateReflectionObject();

        if (_actualUseWaterMasking)
        {
            SetupWaterMaskingInteraction();
        }
    }
    
    void ResolveSettings()
    {
        if (WaterReflectionManager.Instance != null)
        {
            _actualReflectionOpacity = overrides.reflectionOpacity ? localReflectionOpacity : WaterReflectionManager.Instance.defaultReflectionOpacity;
            _actualReflectionTint = overrides.reflectionTint ? localReflectionTint : WaterReflectionManager.Instance.defaultReflectionTint;
            _actualGradientFadeBaseMaterial = overrides.gradientFadeBaseMaterial ? localGradientFadeBaseMaterial : WaterReflectionManager.Instance.defaultGradientFadeMaterial;
            _actualSortingOrderOffset = overrides.sortingOrderOffset ? localSortingOrderOffset : WaterReflectionManager.Instance.defaultSortingOrderOffset;
            _actualUseWaterMasking = overrides.useWaterMasking ? localUseWaterMasking : WaterReflectionManager.Instance.defaultUseWaterMasking;
            _actualWaterTilemapTag = overrides.waterTilemapTag && !string.IsNullOrEmpty(localWaterTilemapTag) ? localWaterTilemapTag : WaterReflectionManager.Instance.defaultWaterTilemapTag;
            _actualShowDebugInfo = overrides.showDebugInfo ? localShowDebugInfo : WaterReflectionManager.Instance.globalShowDebugInfo;
        }
        else // Fallback if no manager in scene
        {
            _actualReflectionOpacity = localReflectionOpacity;
            _actualReflectionTint = localReflectionTint;
            _actualGradientFadeBaseMaterial = localGradientFadeBaseMaterial;
            _actualSortingOrderOffset = localSortingOrderOffset;
            _actualUseWaterMasking = localUseWaterMasking;
            _actualWaterTilemapTag = localWaterTilemapTag;
            _actualShowDebugInfo = localShowDebugInfo;
            if (Application.isPlaying) Debug.LogWarning("[WaterReflection] WaterReflectionManager not found in scene. Using local settings for all reflections.", this);
        }
    }


    void Start()
    {
        ResolveSettings(); // Now resolve settings after all Awake() calls are completed
        UpdateReflectionVisuals();
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
        reflectionObject.transform.SetParent(transform.parent, false);
        reflectionObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);

        reflectionRenderer = reflectionObject.AddComponent<SpriteRenderer>();
        reflectionRenderer.sortingLayerName = originalRenderer.sortingLayerName;
        reflectionRenderer.sortingOrder = originalRenderer.sortingOrder + _actualSortingOrderOffset; // Use resolved
        reflectionRenderer.drawMode = originalRenderer.drawMode;

        if (enableDistanceFade && _actualGradientFadeBaseMaterial != null) // Use resolved
        {
            reflectionMaterialInstance = new Material(_actualGradientFadeBaseMaterial);
            reflectionRenderer.material = reflectionMaterialInstance;
            if (_actualShowDebugInfo && Application.isPlaying) Debug.Log($"[{gameObject.name}] Instantiated gradient material for reflection using '{_actualGradientFadeBaseMaterial.name}'.", this);
        }
        else
        {
            reflectionRenderer.sharedMaterial = originalRenderer.sharedMaterial;
            if (enableDistanceFade && _actualGradientFadeBaseMaterial == null && _actualShowDebugInfo && Application.isPlaying)
            {
                 Debug.Log($"[{gameObject.name}] Using sharedMaterial for reflection as no gradientFadeBaseMaterial (local or global) was resolved during CreateReflectionObject.", this);
            }
        }

        if (originalAnimator != null)
        {
            reflectionAnimator = reflectionObject.AddComponent<Animator>();
            reflectionAnimator.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;
        }
        SortableEntity originalSortable = GetComponent<SortableEntity>();
        if (originalSortable != null)
        {
            reflectionObject.AddComponent<SortableEntity>();
        }
        if (_actualShowDebugInfo) Debug.Log($"[WaterReflection] Created reflection for {gameObject.name}", this);
    }

    private void UpdateReflectionTransform()
    {
        if (reflectionObject == null || originalRenderer == null) return;
        Transform referenceTransform = (useParentAsReference && transform.parent != null) ? transform.parent : transform;
        Vector3 originalWorldPos = transform.position;
        Vector3 reflectionWorldPos = originalWorldPos;
        float referenceYForOffset = referenceTransform.position.y;
        reflectionWorldPos.y = referenceYForOffset + yOffset - (originalWorldPos.y - referenceYForOffset);
        reflectionObject.transform.position = reflectionWorldPos;
        reflectionObject.transform.rotation = transform.rotation;
        reflectionObject.transform.localScale = transform.localScale;
        Vector3 currentLocalScale = reflectionObject.transform.localScale;
        currentLocalScale.y *= -1;
        reflectionObject.transform.localScale = currentLocalScale;
    }

    private void UpdateReflectionVisuals()
    {
        if (reflectionRenderer == null || originalRenderer == null) return;

        reflectionRenderer.sprite = originalRenderer.sprite;
        reflectionRenderer.flipX = originalRenderer.flipX;
        reflectionRenderer.flipY = originalRenderer.flipY;

        Color baseOriginalSpriteColor = originalRenderer.color;
        Color finalReflectionTintedColor = baseOriginalSpriteColor * _actualReflectionTint; // Use resolved
        float finalCombinedAlpha = baseOriginalSpriteColor.a * _actualReflectionOpacity; // Use resolved
        reflectionRenderer.color = new Color(finalReflectionTintedColor.r, finalReflectionTintedColor.g, finalReflectionTintedColor.b, finalCombinedAlpha);

        if (enableDistanceFade && reflectionMaterialInstance != null)
        {
            Transform referenceTransform = (useParentAsReference && transform.parent != null) ? transform.parent : transform;
            float waterSurfaceY = referenceTransform.position.y;
            reflectionMaterialInstance.SetFloat("_FadeStart", fadeStartDistance);
            reflectionMaterialInstance.SetFloat("_FadeEnd", fadeEndDistance);
            reflectionMaterialInstance.SetFloat("_MinAlpha", minFadeAlpha);
            reflectionMaterialInstance.SetFloat("_OriginalY", waterSurfaceY);
            Color materialBaseColor = _actualReflectionTint; // Use resolved
            materialBaseColor.a = _actualReflectionOpacity * baseOriginalSpriteColor.a; // Use resolved
            reflectionMaterialInstance.SetColor("_Color", materialBaseColor);
        }
        else if (!enableDistanceFade && reflectionMaterialInstance != null)
        {
            reflectionRenderer.sharedMaterial = originalRenderer.sharedMaterial;
            Destroy(reflectionMaterialInstance);
            reflectionMaterialInstance = null;
        }

        reflectionRenderer.enabled = originalRenderer.enabled && originalRenderer.gameObject.activeInHierarchy;

        if (reflectionAnimator != null && originalAnimator != null)
        {
            reflectionAnimator.enabled = originalAnimator.enabled;
            if (originalAnimator.runtimeAnimatorController != null && originalAnimator.parameterCount > 0)
            {
                foreach (AnimatorControllerParameter param in originalAnimator.parameters)
                {
                    try {
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
                        if(_actualShowDebugInfo) Debug.LogWarning($"Failed to sync animator param '{param.name}': {e.Message}", reflectionAnimator);
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
               lastScale != transform.localScale ||
               lastPosition != transform.position ||
               parentYChanged;
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

    private void SetupWaterMaskingInteraction()
    {
        if (!_actualUseWaterMasking || reflectionRenderer == null) return; // Use resolved
        GameObject waterTilemapGO = FindWaterTilemapByTag(); // FindWaterTilemapByTag will use resolved tag
        if (waterTilemapGO == null)
        {
            if (_actualShowDebugInfo) Debug.LogWarning($"[WaterReflection] Water tilemap for masking not found on {gameObject.name} using tag '{_actualWaterTilemapTag}'. Masking disabled.", this);
            // _actualUseWaterMasking = false; // Don't change resolved setting here, just don't apply mask
            return;
        }
        SpriteMask maskComponent = waterTilemapGO.GetComponent<SpriteMask>();
        if (maskComponent == null)
        {
            maskComponent = waterTilemapGO.AddComponent<SpriteMask>();
            maskComponent.sprite = null;
            if (_actualShowDebugInfo) Debug.Log($"[WaterReflection] Added SpriteMask to water tilemap '{waterTilemapGO.name}' for object '{gameObject.name}'.", waterTilemapGO);
        }
        reflectionRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        if (_actualShowDebugInfo) Debug.Log($"[WaterReflection] Reflection of '{gameObject.name}' will be masked by '{waterTilemapGO.name}'.", this);
    }

    private GameObject FindWaterTilemapByTag()
    {
        // Uses _actualWaterTilemapTag which is resolved in Awake
        if (string.IsNullOrEmpty(_actualWaterTilemapTag)) return FindWaterTilemapFallback();
        GameObject taggedWater = GameObject.FindGameObjectWithTag(_actualWaterTilemapTag);
        if (taggedWater != null && taggedWater.GetComponent<Tilemap>() != null)
        {
            if (_actualShowDebugInfo) Debug.Log($"[WaterReflection] Found water tilemap by tag '{_actualWaterTilemapTag}': {taggedWater.name} for {gameObject.name}", this);
            return taggedWater;
        }
        if (taggedWater != null && taggedWater.GetComponent<Tilemap>() == null && _actualShowDebugInfo)
        {
            Debug.LogWarning($"[WaterReflection] GameObject '{taggedWater.name}' (tag '{_actualWaterTilemapTag}') has no Tilemap component!", this);
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
                        Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                        if (renderTilemapTransform != null && renderTilemapTransform.GetComponent<Tilemap>() != null)
                        {
                            if (_actualShowDebugInfo) Debug.Log($"[WaterReflection] Auto-detected water tilemap via TIM: {renderTilemapTransform.name} for {gameObject.name}", this);
                            return renderTilemapTransform.gameObject;
                        }
                    }
                }
            }
        }
        if (_actualShowDebugInfo) Debug.LogWarning($"[WaterReflection] Could not auto-detect water tilemap via TileInteractionManager for {gameObject.name}.", this);
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
        // OnValidate is called when the script is loaded or a value is changed in the Inspector.
        // We need to wrap our logic in an editor check.
        #if UNITY_EDITOR
        // Use delayCall to prevent the "TransientArtifact" errors.
        // This defers the execution of our preview update until after the Inspector's
        // current update cycle is complete, breaking the feedback loop.
        EditorApplication.delayCall -= EditorUpdatePreview; // Remove previous requests to avoid stacking
        EditorApplication.delayCall += EditorUpdatePreview; // Add a new request
        #endif
    }

    // Assets/Scripts/Visual/Effects/WaterReflection.cs

#if UNITY_EDITOR
	void EditorUpdatePreview()
	{
		if (this == null || gameObject == null) // The object could be destroyed before the call
		{
			return;
		}

		if (Application.isEditor && !Application.isPlaying)
		{
			// Only show validation logs if the local debug flag is enabled.
			if (localShowDebugInfo)
			{
				bool localMaterialNeeded = enableDistanceFade && (!overrides.gradientFadeBaseMaterial || localGradientFadeBaseMaterial == null);
				bool globalMaterialMightBeUsed = enableDistanceFade && !overrides.gradientFadeBaseMaterial && localGradientFadeBaseMaterial == null;

				if (localMaterialNeeded && !globalMaterialMightBeUsed) // Warn if local ovr is on but local material missing
				{
					Debug.LogWarning($"[WaterReflection OnValidate] '{gameObject.name}': 'Enable Distance Fade' is true and 'Override Gradient Material' is true, but 'Local Gradient Fade Base Material' is not assigned. Assign local material or uncheck override.", this);
				}
				else if (globalMaterialMightBeUsed) // Inform that global will be used if local isn't set
				{
					Debug.Log($"[WaterReflection OnValidate] '{gameObject.name}': 'Enable Distance Fade' is true. If 'Local Gradient Fade Base Material' remains unassigned and ovr is false, the global default from WaterReflectionManager will be used in Play mode.", this);
				}
			}
			
			if (reflectionRenderer != null && originalRenderer != null)
			{
				Color previewTint = overrides.reflectionTint ? localReflectionTint : Color.white; // Default to white if no manager
				float previewOpacity = overrides.reflectionOpacity ? localReflectionOpacity : 0.5f;
				int previewSortOffset = overrides.sortingOrderOffset ? localSortingOrderOffset : -1;

				reflectionRenderer.sprite = originalRenderer.sprite;
				reflectionRenderer.flipX = originalRenderer.flipX;
				reflectionRenderer.flipY = originalRenderer.flipY;
				reflectionRenderer.sortingOrder = originalRenderer.sortingOrder + previewSortOffset;
				Color baseOriginalSpriteColor = originalRenderer.color;
				Color finalReflectionTintedColor = baseOriginalSpriteColor * previewTint;
				float finalCombinedAlpha = baseOriginalSpriteColor.a * previewOpacity;
				reflectionRenderer.color = new Color(finalReflectionTintedColor.r, finalReflectionTintedColor.g, finalReflectionTintedColor.b, finalCombinedAlpha);
				UpdateReflectionTransform(); // Keep transform updated
			}
		}
	}
#endif


    // --- Public Methods for Runtime Control (Could be removed if not needed, or kept for dynamic changes) ---
    public void SetLocalReflectionOpacity(float opacity) // Example of changing a local-only value
    {
        localReflectionOpacity = Mathf.Clamp01(opacity);
        if (overrides.reflectionOpacity) // Only re-resolve and update if this local value is being used
        {
            ResolveSettings();
            if (Application.isPlaying && originalRenderer != null) UpdateReflectionVisuals();
        }
    }
    // Add more setters if you need to programmatically change local override values and have them take effect.
    
    #endregion
}

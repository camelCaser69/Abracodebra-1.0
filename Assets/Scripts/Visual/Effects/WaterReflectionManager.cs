using UnityEngine;

public class WaterReflectionManager : MonoBehaviour
{
    public static WaterReflectionManager Instance { get; private set; }

    [Header("Global Default Reflection Settings")]
    [Tooltip("Default material to use for reflections if 'Enable Distance Fade' is true and no specific material is assigned on the WaterReflection component. Assign your 'Custom/WaterReflectionGradient' material asset here.")]
    public Material defaultGradientFadeMaterial;

    [Tooltip("Default opacity for all reflections (0 = invisible, 1 = fully opaque). Can be overridden per instance.")]
    [Range(0f, 1f)] public float defaultReflectionOpacity = 0.5f;

    [Tooltip("Default additional tint color for all reflections. Can be overridden per instance.")]
    public Color defaultReflectionTint = Color.white;

    [Tooltip("Default sorting order offset for reflections (usually negative). Can be overridden per instance.")]
    public int defaultSortingOrderOffset = -1;

    [Header("Global Default Water Masking Settings")]
    [Tooltip("Default setting for whether to use water masking. Can be overridden per instance.")]
    public bool defaultUseWaterMasking = true;

    [Tooltip("Default tag used to identify the water tilemap. Can be overridden per instance.")]
    public string defaultWaterTilemapTag = "Water";

    [Header("Global Debug Settings")]
    [Tooltip("Enable debug logs for all WaterReflection instances that don't override this.")]
    public bool globalShowDebugInfo = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[WaterReflectionManager] Duplicate instance found on {gameObject.name}. Destroying self.", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (defaultGradientFadeMaterial == null)
        {
            Debug.LogWarning("[WaterReflectionManager] Default Gradient Fade Material is not assigned. Distance fade may not work correctly for reflections that don't have their own material specified.", this);
        }
    }
}
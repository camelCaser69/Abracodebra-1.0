using UnityEngine;
using System.Collections.Generic;

public class PlantShadowController : MonoBehaviour
{
    [Header("Global Shadow Settings")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] [Range(0.1f, 2f)] [Tooltip("Vertical squash factor (1 = none, <1 = flatter, >1 = taller)")]
    private float squashFactor = 0.6f;
    [SerializeField] [Range(0f, 360f)] [Tooltip("Rotation angle of the shadow around the plant's base (0 = right, 90 = up, 180 = left, 270 = down)")]
    private float shadowAngleDegrees = 270f; // Default to directly downwards
    [SerializeField] [Tooltip("Flip the shadow horizontally?")]
    private bool flipShadow = false;
    // Removed: lightSourceDirection
    // Removed: shadowDistance
    // Removed: skewAngleDegrees (replaced by shadowAngleDegrees for direction)
    
    [Header("Distance Fade")]
    [Tooltip("Enable fading parts based on distance from the root.")]
    [SerializeField] private bool enableDistanceFade = true;
    [Tooltip("Distance from the root where the shadow part starts fading.")]
    [SerializeField] private float fadeStartDistance = 1.5f;
    [Tooltip("Distance from the root where the shadow part is fully faded (alpha 0).")]
    [SerializeField] private float fadeEndDistance = 3.0f;
    [Tooltip("Minimum alpha value even when fully faded (e.g., 0.1 for slight visibility).")]
    [SerializeField] [Range(0f, 1f)] private float minFadeAlpha = 0.0f;

    [Header("Sorting")]
    [SerializeField] [Tooltip("Name of the Sorting Layer for shadows (e.g., 'Shadows')")]
    private string shadowSortingLayerName = "Default";
    [SerializeField] [Tooltip("Sorting Order within the layer (lower values are rendered first)")]
    private int shadowSortingOrder = -1;

    // --- Internal ID ---
    private int shadowSortingLayerID;

    // Public accessors for ShadowPartController
    public Color ShadowColor => shadowColor;
    public int ShadowSortingLayer => shadowSortingLayerID;
    public int ShadowSortingOrder => shadowSortingOrder;

    // <<< NEW ACCESSORS for Fade >>>
    public bool EnableDistanceFade => enableDistanceFade;
    public float FadeStartDistance => fadeStartDistance;
    public float FadeEndDistance => fadeEndDistance;
    public float MinFadeAlpha => minFadeAlpha;
    
    // Cached base transform values
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalPosition;

    // Dictionary to manage shadow parts (unchanged)
    private Dictionary<SpriteRenderer, ShadowPartController> shadowPartMap = new Dictionary<SpriteRenderer, ShadowPartController>();

    void Awake()
    {
        // Convert layer name to ID (unchanged)
        shadowSortingLayerID = SortingLayer.NameToID(shadowSortingLayerName);
        if (shadowSortingLayerID == 0 && shadowSortingLayerName != "Default")
        {
            Debug.LogWarning($"Sorting Layer '{shadowSortingLayerName}' not found. Shadow will use 'Default'.", this);
            shadowSortingLayerID = SortingLayer.NameToID("Default");
        }

        // Cache initial transform state (unchanged)
        baseLocalScale = transform.localScale;
        baseLocalRotation = transform.localRotation;
        // --- IMPORTANT: Ensure _ShadowRoot starts at local position (0,0,0) relative to the Plant root ---
        baseLocalPosition = transform.localPosition;
        if (baseLocalPosition != Vector3.zero) {
             Debug.LogWarning($"'{gameObject.name}' initial localPosition is not zero ({baseLocalPosition}). Shadow origin might be slightly offset from plant root.", gameObject);
        }
    }

    void LateUpdate()
    {
        // 1. Set Position: Keep the shadow root at the plant's origin (relative to parent)
        // We don't apply any offset anymore.
        transform.localPosition = baseLocalPosition; // Should typically be Vector3.zero

        // 2. Set Rotation based on the angle slider
        // Apply the rotation relative to the initial orientation
        Quaternion angleRotation = Quaternion.Euler(0, 0, shadowAngleDegrees);
        transform.localRotation = baseLocalRotation * angleRotation;

        // 3. Calculate and Apply Scale (Squash + Flip)
        Vector3 finalScale = baseLocalScale; // Start with original scale
        // Apply squash factor (typically affects the Y-axis before rotation)
        // To apply squash along the *rotated* Y-axis is more complex.
        // Let's keep the simpler approach: squash the local Y scale.
        finalScale.y *= squashFactor;

        // Apply horizontal flip if checked (affects the X-axis)
        if (flipShadow)
        {
            finalScale.x *= -1f;
        }
        // Apply the calculated scale
        transform.localScale = finalScale;

    }

    // --- Methods for PlantGrowth Integration (Unchanged) ---

    public void RegisterPlantPart(SpriteRenderer plantPartRenderer, GameObject shadowPartPrefab)
    {
        if (plantPartRenderer == null || shadowPartPrefab == null) return;
        if (shadowPartMap.ContainsKey(plantPartRenderer)) return;
        GameObject shadowInstance = Instantiate(shadowPartPrefab, transform);
        ShadowPartController shadowController = shadowInstance.GetComponent<ShadowPartController>();
        if (shadowController != null) { shadowController.Initialize(plantPartRenderer, this); shadowPartMap.Add(plantPartRenderer, shadowController); }
        else { Debug.LogError($"Shadow Part Prefab '{shadowPartPrefab.name}' missing ShadowPartController.", shadowPartPrefab); Destroy(shadowInstance); }
    }

    public void UnregisterPlantPart(SpriteRenderer plantPartRenderer)
    {
        if (plantPartRenderer != null && shadowPartMap.TryGetValue(plantPartRenderer, out ShadowPartController shadowController)) {
            if (shadowController != null) { shadowController.OnPlantPartDestroyed(); }
            shadowPartMap.Remove(plantPartRenderer);
        }
    }

    void OnDestroy()
    {
        shadowPartMap.Clear(); // Prevent memory leaks
    }
}
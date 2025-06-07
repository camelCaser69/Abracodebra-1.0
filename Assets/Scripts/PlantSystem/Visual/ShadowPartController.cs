// FILE: Assets/Scripts/Visuals/ShadowPartController.cs
using UnityEngine;
// using UnityEngine.Rendering; // Not needed for this simple fade

[RequireComponent(typeof(SpriteRenderer))]
public class ShadowPartController : MonoBehaviour
{
    private SpriteRenderer shadowRenderer;
    private SpriteRenderer plantPartRenderer;
    private Transform plantPartTransform;
    private Transform shadowRootTransform;
    private PlantShadowController mainShadowController;
    // Removed: initialLocalPosition - not needed for distance calc

    void Awake()
    {
        shadowRenderer = GetComponent<SpriteRenderer>();
        shadowRenderer.drawMode = SpriteDrawMode.Simple;
        shadowRenderer.enabled = false;
    }

    public void Initialize(SpriteRenderer targetPlantPartRenderer, PlantShadowController controller)
    {
        if (targetPlantPartRenderer == null || controller == null) { Destroy(gameObject); return; }
        plantPartRenderer = targetPlantPartRenderer;
        plantPartTransform = targetPlantPartRenderer.transform;
        mainShadowController = controller;
        shadowRootTransform = controller.transform;

        shadowRenderer.sortingLayerID = mainShadowController.ShadowSortingLayer;
        shadowRenderer.sortingOrder = mainShadowController.ShadowSortingOrder;
        // Color is now set in LateUpdate to include fade alpha

        transform.SetParent(shadowRootTransform, true); // Parent first

        shadowRenderer.enabled = plantPartRenderer.enabled && plantPartRenderer.sprite != null;
        // Initialize color with potentially full alpha
        UpdateColorAndFade();
    }

    void LateUpdate()
    {
        if (plantPartRenderer == null || !plantPartRenderer.enabled || plantPartRenderer.sprite == null || shadowRenderer == null || mainShadowController == null)
        {
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            return;
        }

        shadowRenderer.enabled = true;

        // 1. Sync Sprite
        shadowRenderer.sprite = plantPartRenderer.sprite;

        // 2. Sync Position & Rotation (Relative to Shadow Root) - Unchanged
        Vector3 plantPartPosRelativeToPlantRoot = plantPartTransform.parent.InverseTransformPoint(plantPartTransform.position);
        transform.localPosition = plantPartPosRelativeToPlantRoot;
        transform.localRotation = plantPartTransform.localRotation;

        // 3. Sync Flip - Unchanged
        shadowRenderer.flipX = plantPartRenderer.flipX;
        shadowRenderer.flipY = plantPartRenderer.flipY;

        // 4. Sync Scale (relative to parent) - Unchanged
        transform.localScale = plantPartTransform.localScale;

        // 5. Update Color & Apply Distance Fade
        UpdateColorAndFade();
    }

    // <<< NEW METHOD >>>
    private void UpdateColorAndFade()
    {
        if(mainShadowController == null || shadowRenderer == null) return;

        Color baseShadowColor = mainShadowController.ShadowColor;
        float finalAlpha = baseShadowColor.a; // Start with the controller's base alpha

        // Apply distance fade if enabled
        if (mainShadowController.EnableDistanceFade)
        {
            float distance = Vector3.Distance(transform.position, shadowRootTransform.position); // Distance from shadow part to shadow root

            float fadeStart = mainShadowController.FadeStartDistance;
            float fadeEnd = mainShadowController.FadeEndDistance;
            float minAlpha = mainShadowController.MinFadeAlpha;

            if (distance >= fadeEnd)
            {
                finalAlpha *= minAlpha; // Apply min alpha
            }
            else if (distance > fadeStart)
            {
                // Calculate interpolation factor (0 at start, 1 at end)
                float t = Mathf.InverseLerp(fadeStart, fadeEnd, distance);
                // Lerp between 1 (full alpha multiplier) and minAlpha
                float distanceAlphaMultiplier = Mathf.Lerp(1f, minAlpha, t);
                finalAlpha *= distanceAlphaMultiplier; // Modulate base alpha
            }
            // Else (distance <= fadeStart), finalAlpha remains baseShadowColor.a
        }

        // Set the final color with calculated alpha
        shadowRenderer.color = new Color(baseShadowColor.r, baseShadowColor.g, baseShadowColor.b, finalAlpha);
    }
    // <<< END NEW METHOD >>>


    public void OnPlantPartDestroyed() // Unchanged
    {
         if (this != null && gameObject != null) { if (Application.isPlaying) { Destroy(gameObject); } else { DestroyImmediate(gameObject); } }
    }
}
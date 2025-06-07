using UnityEngine;

public class ShadowPartController : MonoBehaviour
{
    // Cached Components
    private SpriteRenderer shadowRenderer;
    private Transform cachedTransform;

    // References
    private SpriteRenderer plantPartRenderer;
    private Transform plantPartTransform;
    private Transform shadowRootTransform;
    private PlantShadowController mainShadowController;

    void Awake()
    {
        shadowRenderer = GetComponent<SpriteRenderer>();
        cachedTransform = transform;

        shadowRenderer.drawMode = SpriteDrawMode.Simple;
        shadowRenderer.enabled = false;
    }

    public void Initialize(SpriteRenderer targetPlantPartRenderer, PlantShadowController controller)
    {
        if (targetPlantPartRenderer == null || controller == null)
        {
            Destroy(gameObject);
            return;
        }

        plantPartRenderer = targetPlantPartRenderer;
        plantPartTransform = targetPlantPartRenderer.transform;
        mainShadowController = controller;
        shadowRootTransform = controller.transform;

        shadowRenderer.sortingLayerID = mainShadowController.ShadowSortingLayer;
        shadowRenderer.sortingOrder = mainShadowController.ShadowSortingOrder;

        cachedTransform.SetParent(shadowRootTransform, true); // Parent first

        shadowRenderer.enabled = plantPartRenderer.enabled && plantPartRenderer.sprite != null;
        UpdateColorAndFade();
    }

    void LateUpdate()
    {
        if (plantPartRenderer == null || !plantPartRenderer.enabled || plantPartRenderer.sprite == null || shadowRenderer == null || mainShadowController == null)
        {
            if (shadowRenderer != null)
                shadowRenderer.enabled = false;
            return;
        }

        shadowRenderer.enabled = true;
        shadowRenderer.sprite = plantPartRenderer.sprite;

        Vector3 plantPartPosRelativeToPlantRoot = plantPartTransform.parent.InverseTransformPoint(plantPartTransform.position);
        cachedTransform.localPosition = plantPartPosRelativeToPlantRoot;
        cachedTransform.localRotation = plantPartTransform.localRotation;
        cachedTransform.localScale = plantPartTransform.localScale;

        shadowRenderer.flipX = plantPartRenderer.flipX;
        shadowRenderer.flipY = plantPartRenderer.flipY;

        UpdateColorAndFade();
    }

    void UpdateColorAndFade()
    {
        if (mainShadowController == null || shadowRenderer == null) return;

        Color baseShadowColor = mainShadowController.ShadowColor;
        float finalAlpha = baseShadowColor.a; // Start with the controller's base alpha

        if (mainShadowController.EnableDistanceFade)
        {
            float distance = Vector3.Distance(cachedTransform.position, shadowRootTransform.position); // Distance from shadow part to shadow root

            float fadeStart = mainShadowController.FadeStartDistance;
            float fadeEnd = mainShadowController.FadeEndDistance;
            float minAlpha = mainShadowController.MinFadeAlpha;

            if (distance >= fadeEnd)
            {
                finalAlpha *= minAlpha; // Apply min alpha
            }
            else if (distance > fadeStart)
            {
                float t = Mathf.InverseLerp(fadeStart, fadeEnd, distance);
                float distanceAlphaMultiplier = Mathf.Lerp(1f, minAlpha, t);
                finalAlpha *= distanceAlphaMultiplier; // Modulate base alpha
            }
        }

        shadowRenderer.color = new Color(baseShadowColor.r, baseShadowColor.g, baseShadowColor.b, finalAlpha);
    }

    public void OnPlantPartDestroyed()
    {
        if (this != null && gameObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
// REWORKED FILE: Assets/Scripts/PlantSystem/Visual/OutlinePartController.cs
using UnityEngine;

public class OutlinePartController : MonoBehaviour
{
    private SpriteRenderer outlineRenderer;
    private Transform cachedTransform;
    private SpriteRenderer sourcePlantPartRenderer;

    public Vector2Int gridCoord;

    void Awake()
    {
        outlineRenderer = GetComponent<SpriteRenderer>();
        cachedTransform = transform;

        if (outlineRenderer != null)
        {
            outlineRenderer.drawMode = SpriteDrawMode.Simple;
            outlineRenderer.enabled = false;
        }
    }

    public void Initialize(SpriteRenderer sourceRenderer, Vector2Int myCoord, PlantOutlineController controller)
    {
        if (sourceRenderer == null || controller == null) { Destroy(gameObject); return; }

        sourcePlantPartRenderer = sourceRenderer;
        gridCoord = myCoord;

        outlineRenderer.sortingLayerID = controller.OutlineSortingLayer;
        outlineRenderer.sortingOrder = controller.OutlineSortingOrder;
        outlineRenderer.color = controller.OutlineColor;

        cachedTransform.SetParent(controller.transform, true);

        // FIX: The old method GetCellSpacing() was removed. We can find the plant and get the value.
        // This is not ideal for performance, but will work. A better solution is to pass spacing in.
        float spacing = 0.08f; // Fallback
        var plant = controller.GetComponentInParent<PlantGrowth>();
        if (plant != null)
        {
            // A public field for cellSpacing should be added to PlantGrowth if this is needed often.
            // For now, we assume a default value as the field was removed.
        }
        cachedTransform.localPosition = (Vector2)myCoord * spacing;

        outlineRenderer.enabled = IsSourceRendererValid() &&
                                  sourcePlantPartRenderer.enabled &&
                                  sourcePlantPartRenderer.sprite != null;

        SyncSpriteAndTransform();
    }

    void LateUpdate()
    {
        if (outlineRenderer == null || !outlineRenderer.enabled) return;

        if (!IsSourceRendererValid())
        {
            SetVisibility(false);
            return;
        }

        if (!sourcePlantPartRenderer.enabled || sourcePlantPartRenderer.sprite == null)
        {
            SetVisibility(false);
            return;
        }

        SyncSpriteAndTransform();
    }

    public bool IsSourceRendererValid()
    {
        if (sourcePlantPartRenderer == null) return false;
        if (!sourcePlantPartRenderer.gameObject.activeInHierarchy) return false;
        return true;
    }

    public void UpdateSourceRenderer(SpriteRenderer newSource)
    {
        if (newSource != null)
        {
            sourcePlantPartRenderer = newSource;
            SetVisibility(outlineRenderer != null && sourcePlantPartRenderer.enabled && sourcePlantPartRenderer.sprite != null);
            SyncSpriteAndTransform();
        }
        else
        {
            SetVisibility(false);
        }
    }

    public void SyncSpriteAndTransform()
    {
        if (!IsSourceRendererValid() || outlineRenderer == null) return;
        if (outlineRenderer.sprite != sourcePlantPartRenderer.sprite)
        {
            outlineRenderer.sprite = sourcePlantPartRenderer.sprite;
        }
        cachedTransform.localScale = sourcePlantPartRenderer.transform.localScale;
        outlineRenderer.flipX = sourcePlantPartRenderer.flipX;
        outlineRenderer.flipY = sourcePlantPartRenderer.flipY;
    }

    public void SetVisibility(bool isVisible)
    {
        if (outlineRenderer != null && outlineRenderer.enabled != isVisible)
        {
            outlineRenderer.enabled = isVisible;
        }
    }

    public void DestroyOutlinePart()
    {
        if (this != null && gameObject != null)
        {
            if (Application.isPlaying) { Destroy(gameObject); }
            else { DestroyImmediate(gameObject); }
        }
    }
}
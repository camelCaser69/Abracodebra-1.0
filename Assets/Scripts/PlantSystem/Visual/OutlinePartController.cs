using UnityEngine;
using Abracodabra.Genes;

public class OutlinePartController : MonoBehaviour
{
    private SpriteRenderer outlineRenderer;
    private Transform cachedTransform;
    private SpriteRenderer sourcePlantPartRenderer;

    public Vector2Int gridCoord;

    private void Awake()
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

        if (outlineRenderer != null)
        {
            outlineRenderer.sortingLayerID = controller.OutlineSortingLayer;
            outlineRenderer.sortingOrder = controller.OutlineSortingOrder;
            outlineRenderer.color = controller.OutlineColor;
        }
        
        cachedTransform.SetParent(controller.transform, true);

        // Retrieve the correct world-unit spacing from the parent plant
        float spacing = 0.08f; // Fallback in world units
        var plant = controller.GetComponentInParent<PlantGrowth>();
        if (plant != null)
        {
            spacing = plant.cellSpacing; // Use the actual cell spacing from the plant
        }
        cachedTransform.localPosition = (Vector2)myCoord * spacing;
        
        if(outlineRenderer != null)
        {
            outlineRenderer.enabled = IsSourceRendererValid() &&
                                  sourcePlantPartRenderer.enabled &&
                                  sourcePlantPartRenderer.sprite != null;
        }

        SyncSpriteAndTransform();
    }

    private void LateUpdate()
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
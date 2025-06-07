using UnityEngine;

public class OutlinePartController : MonoBehaviour
{
    // Cached Components
    private SpriteRenderer outlineRenderer;
    private Transform cachedTransform;

    // References
    private SpriteRenderer sourcePlantPartRenderer; // The plant part this outline mimics

    [HideInInspector]
    public Vector2Int gridCoord; // The coordinate *this outline* lives at

    void Awake()
    {
        outlineRenderer = GetComponent<SpriteRenderer>();
        cachedTransform = transform;

        if (outlineRenderer != null)
        {
            outlineRenderer.drawMode = SpriteDrawMode.Simple;
            outlineRenderer.enabled = false;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] OutlinePartController is missing SpriteRenderer component!", this);
        }
    }

    public void Initialize(SpriteRenderer sourceRenderer, Vector2Int myCoord, PlantOutlineController controller)
    {
        if (sourceRenderer == null || controller == null)
        {
            Debug.LogError($"[{gameObject.name}] Initialization failed! Source renderer or controller is null!", this);
            Destroy(gameObject);
            return;
        }

        sourcePlantPartRenderer = sourceRenderer; // Store initial source
        gridCoord = myCoord;

        if (outlineRenderer == null)
        {
            outlineRenderer = GetComponent<SpriteRenderer>();
            if (outlineRenderer == null)
            {
                Debug.LogError($"[{gameObject.name}] Initialize: Cannot find SpriteRenderer component!", this);
                Destroy(gameObject);
                return;
            }
        }

        outlineRenderer.sortingLayerID = controller.OutlineSortingLayer;
        outlineRenderer.sortingOrder = controller.OutlineSortingOrder;
        outlineRenderer.color = controller.OutlineColor;

        cachedTransform.SetParent(controller.transform, true); // Parent to controller's transform

        float spacing = controller.GetComponentInParent<PlantGrowth>()?.GetCellSpacing() ?? 0.08f; // Get spacing
        cachedTransform.localPosition = (Vector2)myCoord * spacing;

        outlineRenderer.enabled = IsSourceRendererValid() &&
                                  sourcePlantPartRenderer.enabled &&
                                  sourcePlantPartRenderer.sprite != null;

        SyncSpriteAndTransform(); // Initial sync
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
        if (sourcePlantPartRenderer == null)
            return false;

        if (!sourcePlantPartRenderer.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    public void UpdateSourceRenderer(SpriteRenderer newSource)
    {
        if (newSource != null)
        {
            sourcePlantPartRenderer = newSource;

            SetVisibility(
                outlineRenderer != null &&
                sourcePlantPartRenderer.enabled &&
                sourcePlantPartRenderer.sprite != null
            );

            SyncSpriteAndTransform();
        }
        else
        {
            Debug.LogWarning($"Attempted to update source renderer for outline at {gridCoord} with null.", gameObject);
            SetVisibility(false);
        }
    }

    public void SyncSpriteAndTransform()
    {
        if (!IsSourceRendererValid() || outlineRenderer == null)
            return;

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
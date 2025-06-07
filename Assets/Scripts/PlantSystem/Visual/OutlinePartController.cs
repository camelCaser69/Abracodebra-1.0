using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class OutlinePartController : MonoBehaviour
{
    private SpriteRenderer outlineRenderer;
    private SpriteRenderer sourcePlantPartRenderer; // The plant part this outline mimics

    [HideInInspector] public Vector2Int gridCoord; // The coordinate *this outline* lives at

    void Awake()
    {
        outlineRenderer = GetComponent<SpriteRenderer>();
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
        
        // Set layer & color properties
        outlineRenderer.sortingLayerID = controller.OutlineSortingLayer;
        outlineRenderer.sortingOrder = controller.OutlineSortingOrder;
        outlineRenderer.color = controller.OutlineColor;
        
        // Set parent & position
        transform.SetParent(controller.transform, true); // Parent to controller's transform
        
        // Set initial position based on grid coordinate (controller handles spacing)
        float spacing = controller.GetComponentInParent<PlantGrowth>()?.GetCellSpacing() ?? 0.08f; // Get spacing
        transform.localPosition = (Vector2)myCoord * spacing;

        // Set visibility based on source renderer's state
        outlineRenderer.enabled = IsSourceRendererValid() && 
                                  sourcePlantPartRenderer.enabled && 
                                  sourcePlantPartRenderer.sprite != null;
        
        SyncSpriteAndTransform(); // Initial sync
    }

    void LateUpdate()
    {
        // Skip update if not visible
        if (outlineRenderer == null || !outlineRenderer.enabled) return;

        // Check if source is still valid
        if (!IsSourceRendererValid())
        {
            SetVisibility(false);
            return;
        }
        
        // Check if source is still enabled and has a sprite
        if (!sourcePlantPartRenderer.enabled || sourcePlantPartRenderer.sprite == null)
        {
            SetVisibility(false);
            return;
        }

        // If we are enabled and source is valid, sync visuals
        SyncSpriteAndTransform();
    }

    // Public method to check if the source renderer still exists
    public bool IsSourceRendererValid()
    {
        // Unity overloads null check for destroyed objects
        if (sourcePlantPartRenderer == null)
            return false;
            
        // Additional check: is the gameObject actually active/valid
        if (!sourcePlantPartRenderer.gameObject.activeInHierarchy)
            return false;
            
        return true;
    }

    // Public method to update the source renderer if the original was destroyed
    public void UpdateSourceRenderer(SpriteRenderer newSource)
    {
        if (newSource != null)
        {
            sourcePlantPartRenderer = newSource;
            
            // Ensure visibility state is correct after source update
            SetVisibility(
                outlineRenderer != null && 
                sourcePlantPartRenderer.enabled && 
                sourcePlantPartRenderer.sprite != null
            );
            
            // Immediately sync sprite/transform after updating source
            SyncSpriteAndTransform();
            
            // Debug.Log($"Updated outline at {gridCoord} with new source renderer: {newSource.gameObject.name}");
        } 
        else 
        {
            Debug.LogWarning($"Attempted to update source renderer for outline at {gridCoord} with null.", gameObject);
            // If no valid new source, hide this outline
            SetVisibility(false);
        }
    }

    // Sync sprite and transform data
    public void SyncSpriteAndTransform()
    {
        // Safety checks
        if (!IsSourceRendererValid() || outlineRenderer == null) 
            return;

        // Sync sprite
        if (outlineRenderer.sprite != sourcePlantPartRenderer.sprite)
        {
            outlineRenderer.sprite = sourcePlantPartRenderer.sprite;
        }
        
        // Sync other visual properties
        transform.localScale = sourcePlantPartRenderer.transform.localScale;
        outlineRenderer.flipX = sourcePlantPartRenderer.flipX;
        outlineRenderer.flipY = sourcePlantPartRenderer.flipY;
        
        // Note: Position is set in Initialize and doesn't need to track source renderer's parent offset
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
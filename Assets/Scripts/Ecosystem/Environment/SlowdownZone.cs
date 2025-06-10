// Assets/Scripts/Ecosystem/Environment/SlowdownZone.cs

using UnityEngine;
using WegoSystem;

public class SlowdownZone : MonoBehaviour 
{
    [SerializeField] private int additionalTickCost = 1;
    [SerializeField] private bool affectsAnimals = true;
    [SerializeField] private bool affectsPlayer = true;

    public bool AffectsPlayer => affectsPlayer;
    public bool AffectsAnimals => affectsAnimals;
    
    [Header("Debugging & Visuals")]
    [Tooltip("If checked, a colored sprite will be drawn over the collider area in-game to visualize the zone.")]
    [SerializeField] private bool showVisualIndicator = true;
    [Tooltip("This only works if the attached collider is a BoxCollider2D.")]
    [SerializeField] private float colliderShrinkAmount = 0.2f;
    [SerializeField] private Color zoneColor = new Color(0.5f, 0.5f, 1f, 0.3f);
    [SerializeField] private bool showDebugMessages = false;

    private Collider2D zoneCollider;
    private BoxCollider2D boxCollider; // Kept for shrink logic compatibility
    private Vector2 originalSize;
    private SpriteRenderer visualRenderer;
    private const float SHRINK_EPSILON = 0.001f;

    void Awake() 
    {
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null) {
            Debug.LogError($"SlowdownZone on '{gameObject.name}' requires a Collider2D component!", gameObject);
            enabled = false;
            return;
        }

        if (!zoneCollider.isTrigger) {
            zoneCollider.isTrigger = true;
        }

        boxCollider = zoneCollider as BoxCollider2D;
        if (boxCollider != null) {
            originalSize = boxCollider.size;
            if (colliderShrinkAmount > SHRINK_EPSILON) {
                ShrinkCollider();
            }
        }
        
        SetupVisualIndicator();
    }

    public bool IsPositionInZone(Vector3 worldPosition) {
        if (zoneCollider != null) {
            return zoneCollider.OverlapPoint(worldPosition);
        }
        return false;
    }

    public int GetAdditionalTickCost() {
        return additionalTickCost;
    }

    private void SetupVisualIndicator() {
        // Find the visual's renderer if it already exists
        if (visualRenderer == null)
        {
            Transform existingVisual = transform.Find("ZoneVisual");
            if (existingVisual != null) {
                visualRenderer = existingVisual.GetComponent<SpriteRenderer>();
            }
        }

        // If the indicator should NOT be shown, disable it and exit.
        if (!showVisualIndicator)
        {
            if (visualRenderer != null) {
                visualRenderer.gameObject.SetActive(false);
            }
            return;
        }
        
        // If the indicator SHOULD be shown, find or create its components.
        if (visualRenderer == null)
        {
            GameObject visualObj = new GameObject("ZoneVisual");
            visualObj.transform.SetParent(transform);
            
            visualRenderer = visualObj.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = CreateSquareSprite();
            visualRenderer.sortingOrder = -10; // Render behind most things
        }
        
        // Ensure the visual is active before configuring it
        visualRenderer.gameObject.SetActive(true);

        // Update visual properties to match the collider
        visualRenderer.color = zoneColor;
        
        if (zoneCollider != null)
        {
            Bounds bounds = zoneCollider.bounds;
            visualRenderer.transform.position = bounds.center;
            visualRenderer.transform.localScale = bounds.size;
        }
    }

    private Sprite CreateSquareSprite() {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    private void ShrinkCollider() {
        if (boxCollider == null) return;

        Vector2 newSize = new Vector2(
            Mathf.Max(0.1f, originalSize.x - (colliderShrinkAmount * 2f)),
            Mathf.Max(0.1f, originalSize.y - (colliderShrinkAmount * 2f))
        );

        boxCollider.size = newSize;

        if (showDebugMessages) {
            Debug.Log($"SlowdownZone on '{gameObject.name}': Shrunk BoxCollider2D from {originalSize} to {newSize}");
        }
    }
    
    void OnValidate() {
        if (Application.isEditor) {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null) 
                {
                    // This call will now correctly handle both showing and hiding the visual
                    SetupVisualIndicator();
                }
            };
        }
    }

    void OnDrawGizmos() {
        if (zoneCollider != null) {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.8f);
            Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
        }
    }
}
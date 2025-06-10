// Assets\Scripts\Ecosystem\Environment\SlowdownZone.cs

using UnityEngine;
using WegoSystem;
using System.Collections.Generic;

public class SlowdownZone : MonoBehaviour
{
    [SerializeField] int additionalTickCost = 1;
    [SerializeField] bool affectsAnimals = true;
    [SerializeField] bool affectsPlayer = true;

    // These properties expose the settings for other scripts to read
    public bool AffectsPlayer => affectsPlayer;
    public bool AffectsAnimals => affectsAnimals;

    [SerializeField] float colliderShrinkAmount = 0.2f;
    [SerializeField] Color zoneColor = new Color(0.5f, 0.5f, 1f, 0.3f);
    [SerializeField] bool showDebugMessages = false;

    private Collider2D zoneCollider;
    private BoxCollider2D boxCollider; // Kept for shrink logic compatibility
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private SpriteRenderer visualRenderer;
    private const float SHRINK_EPSILON = 0.001f;

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null)
        {
            Debug.LogError($"SlowdownZone on '{gameObject.name}' requires a Collider2D component!", gameObject);
            enabled = false;
            return;
        }

        // The collider for a slowdown zone should always be a trigger
        // so it doesn't physically block movement.
        if (!zoneCollider.isTrigger)
        {
            zoneCollider.isTrigger = true;
        }

        boxCollider = zoneCollider as BoxCollider2D;
        if (boxCollider != null)
        {
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;
            if (colliderShrinkAmount > SHRINK_EPSILON)
            {
                ShrinkCollider();
            }
        }
        else if (colliderShrinkAmount > SHRINK_EPSILON)
        {
            Debug.LogWarning($"SlowdownZone on '{gameObject.name}': Collider shrinking only works with BoxCollider2D. Current collider type: {zoneCollider.GetType().Name}");
        }

        SetupVisualIndicator();
    }

    // --- REMOVED OnTriggerEnter2D and OnTriggerExit2D ---
    // They are no longer needed as we perform a live check instead.

    public bool IsPositionInZone(Vector3 worldPosition)
    {
        if (zoneCollider != null)
        {
            return zoneCollider.OverlapPoint(worldPosition);
        }
        return false;
    }

    public int GetAdditionalTickCost()
    {
        return additionalTickCost;
    }
    
    // --- The rest of the script remains the same ---
    #region Unchanged Methods
    void SetupVisualIndicator()
    {
        visualRenderer = GetComponent<SpriteRenderer>();

        if (visualRenderer == null)
        {
            GameObject visualObj = new GameObject("ZoneVisual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localScale = Vector3.one;

            visualRenderer = visualObj.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = CreateSquareSprite();
            visualRenderer.color = zoneColor;
            visualRenderer.sortingOrder = -10; // Render behind most things
        }
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    void ShrinkCollider()
    {
        if (boxCollider == null) return;

        Vector2 newSize = new Vector2(
            Mathf.Max(0.1f, originalSize.x - (colliderShrinkAmount * 2f)),
            Mathf.Max(0.1f, originalSize.y - (colliderShrinkAmount * 2f))
        );

        boxCollider.size = newSize;

        if (showDebugMessages)
        {
            Debug.Log($"SlowdownZone on '{gameObject.name}': Shrunk BoxCollider2D from {originalSize} to {newSize}");
        }
    }

    void RestoreCollider()
    {
        if (boxCollider == null) return;

        boxCollider.size = originalSize;
        boxCollider.offset = originalOffset;

        if (showDebugMessages)
        {
            Debug.Log($"SlowdownZone on '{gameObject.name}': Restored BoxCollider2D to original size {originalSize} and offset {originalOffset}");
        }
    }
    
    void OnValidate()
    {
        if (boxCollider != null)
        {
            if (colliderShrinkAmount > SHRINK_EPSILON)
            {
                if (originalSize != Vector2.zero || Application.isPlaying)
                {
                    ShrinkCollider();
                }
            }
            else
            {
                if (originalSize != Vector2.zero || Application.isPlaying)
                {
                    RestoreCollider();
                }
            }
        }
    }

    void OnDestroy()
    {
        if (boxCollider != null && colliderShrinkAmount > SHRINK_EPSILON)
        {
            if (originalSize != Vector2.zero)
            {
                RestoreCollider();
            }
        }
    }

    void OnDrawGizmos()
    {
        if (zoneCollider != null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);

            if (zoneCollider is BoxCollider2D box)
            {
                Vector3 center = transform.position + (Vector3)box.offset;
                Vector3 size = new Vector3(box.size.x, box.size.y, 0) * transform.lossyScale.x;
                Gizmos.DrawCube(center, size);

                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
            }
        }
    }
    #endregion
}
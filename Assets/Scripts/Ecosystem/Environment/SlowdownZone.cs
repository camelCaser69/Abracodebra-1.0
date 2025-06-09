// Assets\Scripts\Ecosystem\Environment\SlowdownZone.cs

using UnityEngine;
using System.Collections.Generic;
using WegoSystem;

public class SlowdownZone : MonoBehaviour {
    [Header("Tick Cost Settings")]
    [SerializeField] int additionalTickCost = 1; // Moving through this zone costs extra ticks
    [SerializeField] bool affectsAnimals = true;
    [SerializeField] bool affectsPlayer = true;
    
    [Header("Visual Settings")]
    [SerializeField] float colliderShrinkAmount = 0.2f;
    [SerializeField] Color zoneColor = new Color(0.5f, 0.5f, 1f, 0.3f); // Blue tint for water
    
    [Header("Debug")]
    [SerializeField] bool showDebugMessages = false;

    // Track entities in zone
    Dictionary<int, GridEntity> entitiesInZone = new Dictionary<int, GridEntity>();

    Vector2 originalSize;
    Vector2 originalOffset;
    BoxCollider2D boxCollider;
    SpriteRenderer visualRenderer;

    const float SHRINK_EPSILON = 0.001f;

    void Awake() {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) {
            Debug.LogError($"SlowdownZone on '{gameObject.name}' requires a Collider2D component!", gameObject);
            enabled = false;
            return;
        }

        if (!col.isTrigger) {
            col.isTrigger = true;
        }

        boxCollider = col as BoxCollider2D;

        if (boxCollider != null) {
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;

            if (colliderShrinkAmount > SHRINK_EPSILON) {
                ShrinkCollider();
            }
        }
        else {
            if (colliderShrinkAmount > SHRINK_EPSILON) {
                Debug.LogWarning($"SlowdownZone on '{gameObject.name}': Collider shrinking only works with BoxCollider2D. Current collider type: {col.GetType().Name}");
            }
        }

        // Setup visual indicator
        SetupVisualIndicator();
    }

    void SetupVisualIndicator() {
        // Check if we already have a visual renderer
        visualRenderer = GetComponent<SpriteRenderer>();
        
        if (visualRenderer == null) {
            // Create a child object for the visual
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

    Sprite CreateSquareSprite() {
        // Create a simple white square sprite for the zone visual
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    void ShrinkCollider() {
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

    void RestoreCollider() {
        if (boxCollider == null) return;

        boxCollider.size = originalSize;
        boxCollider.offset = originalOffset;

        if (showDebugMessages) {
            Debug.Log($"SlowdownZone on '{gameObject.name}': Restored BoxCollider2D to original size {originalSize} and offset {originalOffset}");
        }
    }

    void OnValidate() {
        if (boxCollider != null) {
            if (colliderShrinkAmount > SHRINK_EPSILON) {
                if (originalSize != Vector2.zero || Application.isPlaying) {
                    ShrinkCollider();
                }
            }
            else {
                if (originalSize != Vector2.zero || Application.isPlaying) {
                    RestoreCollider();
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        // Check for GridEntity component
        GridEntity gridEntity = other.GetComponent<GridEntity>();
        if (gridEntity == null) return;

        // Check if it's a type we affect
        bool shouldAffect = false;
        
        if (affectsAnimals && other.GetComponent<AnimalController>() != null) {
            shouldAffect = true;
        }
        else if (affectsPlayer && other.GetComponent<GardenerController>() != null) {
            shouldAffect = true;
        }

        if (shouldAffect) {
            int id = gridEntity.GetInstanceID();
            if (!entitiesInZone.ContainsKey(id)) {
                entitiesInZone.Add(id, gridEntity);
                
                // In future, we could notify the entity it's in a slow zone
                // For now, the PlayerActionManager will check zone status when moving
                
                if (showDebugMessages) {
                    Debug.Log($"SlowdownZone: '{other.name}' entered zone (will cost {1 + additionalTickCost} ticks to move)");
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        GridEntity gridEntity = other.GetComponent<GridEntity>();
        if (gridEntity == null) return;

        int id = gridEntity.GetInstanceID();
        if (entitiesInZone.ContainsKey(id)) {
            entitiesInZone.Remove(id);
            
            if (showDebugMessages) {
                Debug.Log($"SlowdownZone: '{other.name}' exited zone (movement cost back to normal)");
            }
        }
    }

    // Public method to check if a position is in this zone
    public bool IsPositionInZone(Vector3 worldPosition) {
        if (boxCollider != null) {
            return boxCollider.bounds.Contains(worldPosition);
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) {
            return col.bounds.Contains(worldPosition);
        }
        
        return false;
    }

    // Public method to get tick cost for being in this zone
    public int GetAdditionalTickCost() {
        return additionalTickCost;
    }

    // Check if a specific entity is in this zone
    public bool IsEntityInZone(GridEntity entity) {
        if (entity == null) return false;
        return entitiesInZone.ContainsKey(entity.GetInstanceID());
    }

    void OnDestroy() {
        entitiesInZone.Clear();

        if (boxCollider != null && colliderShrinkAmount > SHRINK_EPSILON) {
            if (originalSize != Vector2.zero) {
                RestoreCollider();
            }
        }
    }

    void OnDrawGizmos() {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            
            if (col is BoxCollider2D box) {
                Vector3 center = transform.position + (Vector3)box.offset;
                Vector3 size = new Vector3(box.size.x, box.size.y, 0) * transform.lossyScale.x;
                Gizmos.DrawCube(center, size);
                
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireCube(center, size);
            }
            else {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
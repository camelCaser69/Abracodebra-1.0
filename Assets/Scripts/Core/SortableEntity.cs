// FILE: Assets/Scripts/Core/SortableEntity.cs
using UnityEngine;
using WegoSystem;

/// <summary>
/// Handles sprite sorting order based on Y position.
/// Supports MultiTileEntity by optionally using the anchor position for sorting.
/// </summary>
public class SortableEntity : MonoBehaviour {
    [Header("Sorting Configuration")]
    [Tooltip("Offset added to Y position for sorting calculation. Negative values make the entity sort as if it's lower on screen.")]
    [SerializeField] private float sortingLayerYOffset = 0f;

    [Tooltip("Use parent's Y coordinate for sorting instead of this object's Y.")]
    [SerializeField] private bool useParentYCoordinate = false;

    [Tooltip("If this object has a MultiTileEntity, use its anchor (bottom-left) position for sorting. " +
             "This ensures multi-tile objects sort correctly based on their feet, not their center.")]
    [SerializeField] private bool useMultiTileAnchor = true;

    [Header("Sorting Layer")]
    [Tooltip("Optional: Override the sorting layer. Leave empty to keep the current layer.")]
    [SerializeField] private string sortingLayerName = "";

    [Header("Debug")]
    public bool debugSorting = false;

    // Cached references
    private SpriteRenderer spriteRenderer;
    private MultiTileEntity multiTileEntity;
    private bool hasCheckedMultiTile = false;

    void Awake() {
        CacheReferences();
    }

    void Start() {
        UpdateSortingOrder();
    }

    void LateUpdate() {
        UpdateSortingOrder();
    }

    private void CacheReferences() {
        // Find SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null) {
            Debug.LogWarning($"[SortableEntity] No SpriteRenderer found on {gameObject.name}");
        }

        // Check for MultiTileEntity
        CheckForMultiTileEntity();
    }

    private void CheckForMultiTileEntity() {
        if (hasCheckedMultiTile) return;

        multiTileEntity = GetComponent<MultiTileEntity>();
        if (multiTileEntity == null) {
            multiTileEntity = GetComponentInParent<MultiTileEntity>();
        }

        hasCheckedMultiTile = true;

        if (debugSorting && multiTileEntity != null) {
            Debug.Log($"[SortableEntity] Found MultiTileEntity on {gameObject.name}");
        }
    }

    public void UpdateSortingOrder() {
        if (spriteRenderer == null) return;

        float yPositionForSorting = GetYPositionForSorting();
        int sortOrder = CalculateSortOrder(yPositionForSorting);

        spriteRenderer.sortingOrder = sortOrder;

        // Apply sorting layer override if specified
        if (!string.IsNullOrEmpty(sortingLayerName)) {
            spriteRenderer.sortingLayerName = sortingLayerName;
        }

        if (debugSorting) {
            string source = GetSortingSourceDescription();
            Debug.Log($"[SortableEntity] {gameObject.name} - Y: {yPositionForSorting:F2}, Offset: {sortingLayerYOffset}, Sort Order: {sortOrder} ({source})");
        }
    }

    private float GetYPositionForSorting() {
        // Priority 1: MultiTileEntity anchor position
        if (useMultiTileAnchor && multiTileEntity != null && GridPositionManager.Instance != null) {
            Vector3 anchorWorld = GridPositionManager.Instance.GridToWorld(multiTileEntity.AnchorPosition);
            return anchorWorld.y;
        }

        // Priority 2: Parent's Y coordinate
        if (useParentYCoordinate && transform.parent != null) {
            return transform.parent.position.y;
        }

        // Default: This transform's Y
        return transform.position.y;
    }

    private string GetSortingSourceDescription() {
        if (useMultiTileAnchor && multiTileEntity != null) {
            return $"MultiTile Anchor at {multiTileEntity.AnchorPosition}";
        }
        if (useParentYCoordinate && transform.parent != null) {
            return "Parent Y";
        }
        return "Self Y";
    }

    private int CalculateSortOrder(float yPosition) {
        // Negative Y values should have higher sort orders (appear in front)
        // Multiplying by 1000 gives us fine-grained control
        return Mathf.RoundToInt(-(yPosition + sortingLayerYOffset) * 1000f);
    }

    /// <summary>
    /// Get the current sort order being used.
    /// </summary>
    public int GetCurrentSortOrder() {
        if (spriteRenderer == null) return 0;
        return spriteRenderer.sortingOrder;
    }

    /// <summary>
    /// Manually set the sorting order (bypasses automatic calculation).
    /// </summary>
    public void SetSortingOrder(int order) {
        if (spriteRenderer != null) {
            spriteRenderer.sortingOrder = order;
        }
    }

    /// <summary>
    /// Set whether to use parent's Y coordinate for sorting.
    /// </summary>
    public void SetUseParentYCoordinate(bool value) {
        useParentYCoordinate = value;
    }

    /// <summary>
    /// Set whether to use MultiTileEntity anchor for sorting.
    /// </summary>
    public void SetUseMultiTileAnchor(bool value) {
        useMultiTileAnchor = value;
    }

    /// <summary>
    /// Set the Y offset for sorting calculations.
    /// </summary>
    public void SetSortingYOffset(float offset) {
        sortingLayerYOffset = offset;
    }

    /// <summary>
    /// Force re-caching of references (useful if components are added at runtime).
    /// </summary>
    public void RefreshReferences() {
        hasCheckedMultiTile = false;
        CacheReferences();
    }

    private void OnValidate() {
        // Re-cache in editor when values change
        if (!Application.isPlaying) {
            hasCheckedMultiTile = false;
        }
    }
}
using UnityEngine;

public class SortableEntity : MonoBehaviour
{
    [Header("Sorting Configuration")]
    [Tooltip("Offset added to Y position for sorting calculation")]
    [SerializeField] private float sortingLayerYOffset = 0f;

    [Tooltip("Use parent's Y coordinate for sorting")]
    [SerializeField] private bool useParentYCoordinate = false;

    [Tooltip("Debug logging for sorting")]
    public bool debugSorting = false;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // If no SpriteRenderer, try to find one in children
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Ensure sprite renderer exists
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[SortableEntity] No SpriteRenderer found on {gameObject.name}");
        }
    }

    private void Start()
    {
        UpdateSortingOrder();
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;

        // Determine Y position for sorting
        float yPositionForSorting = useParentYCoordinate && transform.parent != null 
            ? transform.parent.position.y 
            : transform.position.y;

        int sortOrder = CalculateSortOrder(yPositionForSorting);
        spriteRenderer.sortingOrder = sortOrder;

        if (debugSorting)
        {
            Debug.Log($"[SortableEntity] {gameObject.name} - Y: {yPositionForSorting}, Offset: {sortingLayerYOffset}, Sort Order: {sortOrder}");
        }
    }

    private int CalculateSortOrder(float yPosition)
    {
        // Subtract offset to effectively move the sorting position
        return Mathf.RoundToInt(-(yPosition + sortingLayerYOffset) * 1000f);
    }
}
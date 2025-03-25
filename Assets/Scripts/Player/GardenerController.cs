using UnityEngine;

public class GardenerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Vector2 seedPlantingOffset = new Vector2(0f, -0.5f); // Configurable offset for seed planting

    private Rigidbody2D rb;
    private Vector2 movement;
    private SortableEntity sortableEntity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sortableEntity = GetComponent<SortableEntity>();
        
        // Add SortableEntity if not already present
        if (sortableEntity == null)
            sortableEntity = gameObject.AddComponent<SortableEntity>();
    }

    private void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
    
    // Returns the position used for planting seeds, now with configurable offset
    public Vector2 GetPlantingPosition()
    {
        return (Vector2)transform.position + seedPlantingOffset;
    }
}
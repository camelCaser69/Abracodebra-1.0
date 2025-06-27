using UnityEngine;
using WegoSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(GridEntity))]
public class FoodItem : MonoBehaviour
{
    [Header("Configuration")]
    public FoodType foodType;
    
    [Header("Grid Integration")]
    [SerializeField] private bool snapToGridOnStart = true;
    [SerializeField] private bool registerAsGridEntity = true;
    
    private GridEntity gridEntity;
    
    void Awake()
    {
        // Ensure we have a GridEntity component
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
        
        // Food items should not be tile occupants (animals can walk over them)
        gridEntity.isTileOccupant = false;
    }
    
    void Start()
    {
        // Validate food type
        if (foodType == null)
        {
            Debug.LogWarning($"FoodItem on GameObject '{gameObject.name}' is missing its FoodType reference!", gameObject);
            enabled = false;
            return;
        }
        
        // Snap to grid if requested
        if (snapToGridOnStart && GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[FoodItem] {foodType.foodName} snapped to grid position {gridEntity.Position}");
        }
        
        // Ensure collider is set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    void OnDestroy()
    {
        // GridEntity will automatically unregister itself
    }
    
    // Helper method to check if an animal can eat this food
    public bool CanBeEatenBy(AnimalController animal)
    {
        if (animal == null || animal.Definition == null || animal.Definition.diet == null)
            return false;
            
        return animal.Definition.diet.CanEat(foodType);
    }
    
    // Helper method to get satiation value for a specific animal
    public float GetSatiationValueFor(AnimalController animal)
    {
        if (animal == null || animal.Definition == null || animal.Definition.diet == null)
            return 0f;
            
        return animal.Definition.diet.GetSatiationValue(foodType);
    }
}
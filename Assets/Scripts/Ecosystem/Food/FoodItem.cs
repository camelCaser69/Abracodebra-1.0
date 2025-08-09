using Abracodabra.Genes;
using UnityEngine;
using WegoSystem;

public class FoodItem : MonoBehaviour
{
    public FoodType foodType;

    [SerializeField]
    private bool snapToGridOnStart = true;

    private GridEntity gridEntity;

    void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }

        // It's a part of something else, not a primary occupant of a tile.
        gridEntity.isTileOccupant = false;
    }

    void Start()
    {
        if (foodType == null)
        {
            Debug.LogWarning($"FoodItem on GameObject '{gameObject.name}' is missing its FoodType reference!", gameObject);
            enabled = false;
            return;
        }

        // --- FIX: Check if this food item is part of a plant ---
        // If it is, it should NOT register its own grid position. The animal logic will
        // find the root PlantGrowth entity to determine the correct tile.
        PlantGrowth parentPlant = GetComponentInParent<PlantGrowth>();
        if (parentPlant != null)
        {
            // This is a plant part (leaf, berry, etc.).
            // Disable its personal GridEntity to avoid polluting the GridPositionManager
            // with incorrect, offset positions.
            if (gridEntity != null)
            {
                gridEntity.enabled = false;
            }
            // Ensure the independent snapping logic below is skipped.
            snapToGridOnStart = false;
        }
        // --- END OF FIX ---


        // This logic will now only run for standalone food items, not plant parts.
        if (snapToGridOnStart && GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[FoodItem] Standalone food '{foodType.foodName}' snapped to grid position {gridEntity.Position}");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnDestroy()
    {
        // No specific cleanup needed here anymore.
    }

    public bool CanBeEatenBy(AnimalController animal)
    {
        if (animal == null || animal.Definition == null || animal.Definition.diet == null)
            return false;

        return animal.Definition.diet.CanEat(foodType);
    }

    public float GetSatiationValueFor(AnimalController animal)
    {
        if (animal == null || animal.Definition == null || animal.Definition.diet == null)
            return 0f;

        return animal.Definition.diet.GetSatiationValue(foodType);
    }
}
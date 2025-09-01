using UnityEngine;
using Abracodabra.Genes;
using WegoSystem;

public class FoodItem : MonoBehaviour
{
    public FoodType foodType;

    private GridEntity gridEntity;
    private bool isInitialized = false; // Prevents Start() from running if manually initialized

    private void Awake()
    {
        gridEntity = GetComponent<GridEntity>();
        if (gridEntity == null)
        {
            gridEntity = gameObject.AddComponent<GridEntity>();
        }
    }

    // This will now only run for STANDALONE food items
    private void Start()
    {
        if (isInitialized) return; // If initialized by PlantCellManager, do nothing.

        if (foodType == null)
        {
            Debug.LogWarning($"FoodItem on GameObject '{gameObject.name}' is missing its FoodType reference!", gameObject);
            enabled = false;
            return;
        }

        gridEntity.isTileOccupant = false;
        gridEntity.enabled = true;

        if (GridPositionManager.Instance != null)
        {
            GridPositionManager.Instance.SnapEntityToGrid(gameObject);
            Debug.Log($"[FoodItem] Registered STANDALONE food '{foodType.foodName}' at grid position {gridEntity.Position}");
        }

        isInitialized = true;
    }

    /// <summary>
    /// A special initialization path for food items that are part of a plant.
    /// This bypasses the default Start() logic to prevent the object from moving itself.
    /// </summary>
    public void InitializeAsPlantPart(FoodType type, GridPosition gridPosition)
    {
        if (isInitialized) return;

        this.foodType = type;
        
        gridEntity.isTileOccupant = false;
        gridEntity.enabled = true;

        if (GridPositionManager.Instance != null)
        {
            // We tell the GridEntity its position and register it WITHOUT moving the transform.
            gridEntity.SetPosition(gridPosition, true); // Instantly set logical state
            GridPositionManager.Instance.RegisterEntity(gridEntity); // Manually register
            Debug.Log($"[FoodItem] Registered PLANT food '{foodType.name}' at grid position {gridPosition}");
        }

        isInitialized = true;
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
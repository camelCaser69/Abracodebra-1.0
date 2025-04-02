using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Still need collider for detection
public class FoodItem : MonoBehaviour
{
    [Header("Food Identification")]
    [Tooltip("Reference to the ScriptableObject defining what type of food this is.")]
    public FoodType foodType;

    private void Start()
    {
        // Simple validation
        if (foodType == null)
        {
            Debug.LogWarning($"FoodItem on GameObject '{gameObject.name}' is missing its FoodType reference!", gameObject);
            // Optionally disable the collider so it can't be detected as food
            // Collider2D col = GetComponent<Collider2D>();
            // if (col != null) col.enabled = false;
            enabled = false; // Disable script if not configured
        }
        // No health, no consume logic needed here anymore.
    }
}
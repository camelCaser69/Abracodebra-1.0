using UnityEngine;

[CreateAssetMenu(fileName = "Animal_", menuName = "Ecosystem/Animal Definition (Simplified)")]
public class AnimalDefinition : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("The species name (e.g., 'Bunny', 'Fox').")]
    public string animalName = "DefaultAnimal";

    [Header("Core Stats")]
    public float maxHealth = 10f; // Keep health for potential future damage/predators
    public float movementSpeed = 2f;

    [Header("Diet")]
    [Tooltip("Reference to the AnimalDiet ScriptableObject defining eating habits.")]
    public AnimalDiet diet; // Needs to reference the simplified AnimalDiet SO

    [Header("Visuals")]
    [Tooltip("The prefab to instantiate for this animal.")]
    public GameObject prefab;

    // Removed meatFoodType
}
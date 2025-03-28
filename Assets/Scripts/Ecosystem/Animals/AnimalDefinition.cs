using UnityEngine;

[CreateAssetMenu(fileName = "AnimalDefinition", menuName = "Ecosystem/Animal Definition")]
public class AnimalDefinition : ScriptableObject
{
    [Header("Basic Stats")]
    public string animalName;
    public float maxHealth = 10f;
    public float hungerDecayRate = 0.5f;  // How quickly the animal gets hungry (units/sec)
    public float movementSpeed = 2f;

    [Header("Eating & Satiation")]
    public float hungerThreshold = 5f;    // If hunger > threshold, tries to eat
    public float eatAmount = 5f;         // How much hunger is reduced when it eats a leaf
    public float leafDamage = 1f;        // How much health (or 'life') is removed from a leaf

    [Header("Prefab/Visuals")]
    public GameObject prefab;  // The character prefab to instantiate
}
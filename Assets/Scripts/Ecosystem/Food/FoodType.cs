using UnityEngine;

[CreateAssetMenu(fileName = "FoodType_", menuName = "Ecosystem/Food Type (Simplified)")]
public class FoodType : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Identifying name for this food type (e.g., 'Leaf', 'Fruit').")]
    public string foodName = "Default Food";
    [Tooltip("Icon used in UI or debugging.")]
    public Sprite icon;

    // Keep category for potential filtering later, but not actively used by core logic yet
    public enum FoodCategory { Plant_Leaf, Plant_Fruit, Plant_Stem, Plant_Seed, Other }
    [Header("Categorization")]
    [Tooltip("General category this food falls into.")]
    public FoodCategory category = FoodCategory.Other;
}
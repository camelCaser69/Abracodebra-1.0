using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodType", menuName = "Ecosystem/Food Type")]
public class FoodType : ScriptableObject
{
    public string foodName = "Default Food";
    public Sprite icon;

    public enum FoodCategory { Plant_Leaf, Plant_Fruit, Plant_Stem, Plant_Seed, Other }
    public FoodCategory category = FoodCategory.Other;

    [Tooltip("The base nutritional value this food provides when eaten.")]
    public float baseSatiationValue = 5f;
}
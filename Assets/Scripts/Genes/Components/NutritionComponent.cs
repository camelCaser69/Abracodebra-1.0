// File: Assets/Scripts/Genes/Components/NutritionComponent.cs
using UnityEngine;

namespace Abracodabra.Genes.Components
{
    /// <summary>
    /// A data component attached to consumable items (like Fruit) to hold their nutritional value.
    /// </summary>
    public class NutritionComponent : MonoBehaviour
    {
        public float nutritionValue;
        public float healAmount;
    }
}
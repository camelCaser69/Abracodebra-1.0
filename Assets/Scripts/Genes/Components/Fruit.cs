// File: Assets/Scripts/Genes/Components/Fruit.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// A placeholder component to be attached to fruit GameObjects.
    /// This allows payload genes to find and configure it.
    /// This should be expanded with actual fruit logic later.
    /// </summary>
    public class Fruit : MonoBehaviour
    {
        public PlantGrowth SourcePlant { get; set; }
        public float GrowthTime { get; set; }

        public void LaunchImmediate(Vector2 force)
        {
            // Placeholder for future physics logic
            Debug.Log($"Fruit '{gameObject.name}' launched with force {force}");
        }

        public void StartGrowing()
        {
            // Placeholder for growth logic
            Debug.Log($"Fruit '{gameObject.name}' has started growing for {GrowthTime}s");
        }

        public void AddVisualEffect(Color color)
        {
            // Placeholder for visual feedback
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
    }
}
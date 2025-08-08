// File: Assets/Scripts/Genes/Components/Fruit.cs
using UnityEngine;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Components
{
    /// <summary>
    /// A component attached to fruit GameObjects, making them interactable and configurable by genes.
    /// </summary>
    public class Fruit : MonoBehaviour
    {
        public PlantGrowth SourcePlant { get; set; }
        public float GrowthTime { get; set; }

        public void LaunchImmediate(Vector2 force)
        {
            var rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        public void StartGrowing()
        {
            // Placeholder for a simple growth timer
            Debug.Log($"Fruit '{gameObject.name}' has started growing for {GrowthTime}s.", this);
        }

        public void AddVisualEffect(Color color)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // Tints the fruit to show a payload is active
                renderer.color = Color.Lerp(renderer.color, color, 0.5f);
            }
        }
    }
}
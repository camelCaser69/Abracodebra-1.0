using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

// Note: Add 'using' for your ItemDefinition namespace if it's not global.

namespace Abracodabra.Genes.Components
{
    public class Fruit : MonoBehaviour
    {
        public PlantGrowth SourcePlant { get; set; }
        public float GrowthTime { get; set; }

        // MODIFIED: This now holds the static definition of the item this fruit will become when harvested.
        public ItemDefinition RepresentingItemDefinition { get; set; }

        // NEW: This dictionary will store runtime-calculated values from genes.
        public Dictionary<string, float> DynamicProperties { get; set; } = new Dictionary<string, float>();
        
        // This is now only used for applying immediate visual effects during creation.
        public List<RuntimeGeneInstance> PayloadGeneInstances { get; set; } = new List<RuntimeGeneInstance>();
        
        public void LaunchImmediate(Vector2 force)
        {
            var rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        public void StartGrowing()
        {
            Debug.Log($"Fruit '{gameObject.name}' has started growing for {GrowthTime}s.", this);
        }

        public void AddVisualEffect(Color color)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.Lerp(renderer.color, color, 0.5f);
            }
        }
    }
}
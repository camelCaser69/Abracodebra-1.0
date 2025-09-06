using UnityEngine;
using System.Collections.Generic; // Added for List
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime; // Added for RuntimeGeneInstance

namespace Abracodabra.Genes.Components
{
    public class Fruit : MonoBehaviour
    {
        public PlantGrowth SourcePlant { get; set; }
        public float GrowthTime { get; set; }

        // NEW: This list will store the gene data for this specific fruit.
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
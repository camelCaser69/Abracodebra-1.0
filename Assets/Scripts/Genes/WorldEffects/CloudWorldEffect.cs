// File: Assets/Scripts/Genes/WorldEffects/CloudWorldEffect.cs
using System.Collections.Generic;
using UnityEngine;

namespace Abracodabra.Genes.WorldEffects
{
    /// <summary>
    /// Persistent cloud area effect. Each tick, finds all creatures within radius
    /// and applies all payload effects to them.
    /// </summary>
    public class CloudWorldEffect : WorldEffect
    {
        private float pulseTimer;
        private float baseSpriteAlpha;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Initialize(PlantGrowth source, List<Abracodabra.Genes.Runtime.RuntimeGeneInstance> payloads, float effectRadius, int duration, float multiplier = 1f)
        {
            base.Initialize(source, payloads, effectRadius, duration, multiplier);

            if (spriteRenderer != null)
            {
                baseSpriteAlpha = spriteRenderer.color.a;
            }
        }

        protected override void OnEffectTick(int tick)
        {
            // Find all creatures within cloud radius
            var creatures = TargetFinder.FindCreaturesInRadius(transform.position, radius);

            foreach (var creature in creatures)
            {
                if (creature == null || creature.IsDying) continue;

                ApplyPayloadsToTarget(creature.gameObject);
            }

            if (creatures.Count > 0)
            {
                Debug.Log($"[CloudWorldEffect] Tick {tick}: Applied payloads to {creatures.Count} creature(s) in radius {radius}");
            }
        }

        private void Update()
        {
            // Visual pulsing effect for "alive" feeling
            if (spriteRenderer == null || !isActive) return;

            pulseTimer += Time.deltaTime * 2f;
            float pulseAlpha = baseSpriteAlpha + Mathf.Sin(pulseTimer) * 0.1f;
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, pulseAlpha);
        }
    }
}

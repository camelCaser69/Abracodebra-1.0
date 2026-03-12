// FILE: Assets/Scripts/Genes/WorldEffects/CloudWorldEffect.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.WorldEffects {
    public class CloudWorldEffect : WorldEffect {
        float pulseTimer;
        float baseSpriteAlpha;

        protected override void Awake() {
            base.Awake();
        }

        public override void Initialize(PlantGrowth source, List<RuntimeGeneInstance> payloads, float effectRadius, int duration, float multiplier = 1f) {
            base.Initialize(source, payloads, effectRadius, duration, multiplier);

            if (spriteRenderer != null) {
                baseSpriteAlpha = spriteRenderer.color.a;
            }
        }

        protected override void OnEffectTick(int tick) {
            // Apply payloads to creatures in radius (original behavior)
            var creatures = TargetFinder.FindCreaturesInRadius(transform.position, radius);

            foreach (var creature in creatures) {
                if (creature == null || creature.IsDying) continue;

                ApplyPayloadsToTarget(creature.gameObject);
            }

            if (creatures.Count > 0) {
                Debug.Log($"[CloudWorldEffect] Tick {tick}: Applied payloads to {creatures.Count} creature(s) in radius {radius}");
            }

            // Task 6.4: If any payload is Healing/Nutrition type, also regrow leaves on plants
            if (HasHealingPayload()) {
                var plantsInRange = TargetFinder.FindPlantsInRadius(transform.position, radius);

                int regrowCount = 0;
                foreach (var plant in plantsInRange) {
                    if (plant == null) continue;
                    if (plant.DestroyedLeafCount <= 0) continue;

                    // 50% chance per tick per plant — Healing Cloud regrows ~1 leaf per 2 cloud ticks
                    if (Random.value < 0.5f) {
                        if (plant.RegrowLeaf()) {
                            regrowCount++;
                        }
                    }
                }

                if (regrowCount > 0) {
                    Debug.Log($"[CloudWorldEffect] Tick {tick}: Regrew {regrowCount} leaf/leaves on plants in healing radius");
                }
            }
        }

        /// <summary>
        /// Checks if any payload in this cloud's payload list is a Healing/Nutrition type.
        /// </summary>
        bool HasHealingPayload() {
            if (payloadInstances == null) return false;

            foreach (var instance in payloadInstances) {
                if (instance == null) continue;

                var payloadGene = instance.GetGene<PayloadGene>();
                if (payloadGene == null) continue;

                if (payloadGene.payloadType == PayloadType.Nutrition) {
                    return true;
                }
            }

            return false;
        }

        void Update() {
            if (spriteRenderer == null || !isActive) return;

            pulseTimer += Time.deltaTime * 2f;
            float pulseAlpha = baseSpriteAlpha + Mathf.Sin(pulseTimer) * 0.1f;
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, pulseAlpha);
        }
    }
}
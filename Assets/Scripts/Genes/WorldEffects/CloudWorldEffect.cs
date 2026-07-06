// FILE: Assets/Scripts/Genes/WorldEffects/CloudWorldEffect.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations;

namespace Abracodabra.Genes.WorldEffects
{
    public class CloudWorldEffect : WorldEffect
    {
        float pulseTimer;
        float baseSpriteAlpha;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Initialize(PlantGrowth source, List<RuntimeGeneInstance> payloads, float effectRadius, int duration, float multiplier = 1f)
        {
            base.Initialize(source, payloads, effectRadius, duration, multiplier);

            if (spriteRenderer != null)
            {
                baseSpriteAlpha = spriteRenderer.color.a;
            }
        }

        protected override void OnEffectTick(int tick)
        {
            // Apply payloads to creatures in radius
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

            // Apply healing to plants if we have a healing-type payload
            float regrowChance = GetPlantRegrowChance();
            if (regrowChance > 0f)
            {
                var plantsInRange = TargetFinder.FindPlantsInRadius(transform.position, radius);

                int regrowCount = 0;
                foreach (var plant in plantsInRange)
                {
                    if (plant == null) continue;
                    if (plant.DestroyedLeafCount <= 0) continue;

                    if (Random.value < regrowChance)
                    {
                        if (plant.RegrowLeaf())
                        {
                            regrowCount++;
                        }
                    }
                }

                if (regrowCount > 0)
                {
                    Debug.Log($"[CloudWorldEffect] Tick {tick}: Regrew {regrowCount} leaf/leaves on plants in healing radius");
                }
            }
        }

        /// <summary>
        /// Returns the plant regrow chance from the first healing payload, or 0 if none.
        /// Checks both the new IsPlantHealingPayload property and legacy PayloadType.Nutrition.
        /// </summary>
        float GetPlantRegrowChance()
        {
            if (payloadInstances == null) return 0f;

            foreach (var instance in payloadInstances)
            {
                if (instance == null) continue;

                var payloadGene = instance.GetGene<PayloadGene>();
                if (payloadGene == null) continue;

                // Check for HealingPayload (Task 5) — uses configurable plantRegrowChance
                if (payloadGene is HealingPayload healingPayload)
                {
                    return healingPayload.plantRegrowChance;
                }

                // Legacy check: NutritiousPayload also triggers plant healing at 50%
                if (payloadGene.payloadType == PayloadType.Nutrition)
                {
                    return 0.5f;
                }
            }

            return 0f;
        }

        void Update()
        {
            if (spriteRenderer == null || !isActive) return;

            pulseTimer += Time.deltaTime * 2f;
            float pulseAlpha = baseSpriteAlpha + Mathf.Sin(pulseTimer) * 0.1f;
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, pulseAlpha);
        }
    }
}
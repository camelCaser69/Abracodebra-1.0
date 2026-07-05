// FILE: Assets\Scripts\Genes\WorldEffects\CloudWorldEffect.cs
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Runtime;
using UnityEngine;

namespace Abracodabra.Genes.WorldEffects
{
    public class CloudWorldEffect : WorldEffect
    {
        protected override void Awake()
        {
            base.Awake();
        }

        public override void Initialize(PlantGrowth source, List<RuntimeGeneInstance> payloads, float effectRadius, int duration, float multiplier = 1f)
        {
            base.Initialize(source, payloads, effectRadius, duration, multiplier);

            // Show tile overlay instead of scaling a circle sprite
            UpdateTileOverlay();

            // Hide the SpriteRenderer if present — we use tile overlays now
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        protected override void OnEffectTick(int tick)
        {
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

        float GetPlantRegrowChance()
        {
            if (payloadInstances == null) return 0f;

            foreach (var instance in payloadInstances)
            {
                if (instance == null) continue;
                var payloadGene = instance.GetGene<PayloadGene>();
                if (payloadGene == null) continue;

                if (payloadGene is HealingPayload healingPayload)
                {
                    return healingPayload.plantRegrowChance;
                }

                if (payloadGene.payloadType == PayloadType.Nutrition)
                {
                    return 0.5f;
                }
            }

            return 0f;
        }

        protected override void OnEffectExpire()
        {
            // Clear tile overlay before the base fade-and-destroy
            ClearTileOverlay();
            base.OnEffectExpire();
        }
    }
}
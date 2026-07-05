// FILE: Assets\Scripts\Genes\WorldEffects\AuraWorldEffect.cs
using System.Collections;
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Runtime;
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Genes.WorldEffects
{
    public class AuraWorldEffect : WorldEffect
    {
        public float energyDrainPerTick = 1.5f;

        bool isEnergyStarved;
        Color activeOverlayColor;
        Color starvedOverlayColor;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }

            isActive = true;
            currentTick = 0;
            isEnergyStarved = false;
        }

        public void InitializeAura(
            PlantGrowth source,
            List<RuntimeGeneInstance> payloads,
            float effectRadius,
            float drainPerTick,
            float multiplier = 1f)
        {
            sourcePlant = source;
            payloadInstances = payloads != null ? new List<RuntimeGeneInstance>(payloads) : new List<RuntimeGeneInstance>();
            radius = effectRadius;
            energyDrainPerTick = drainPerTick;
            effectMultiplier = multiplier;
            durationTicks = int.MaxValue;

            // Compute overlay colors
            activeOverlayColor = GetOverlayColor(0.25f);
            starvedOverlayColor = activeOverlayColor;
            starvedOverlayColor.a *= 0.3f; // Much dimmer when starved

            // Show tile overlay
            UpdateTileOverlay(activeOverlayColor);

            // Hide the SpriteRenderer if present — we use tile overlays now
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            Debug.Log($"[AuraWorldEffect] Initialized on '{source.name}' | Radius: {radius:F1} | Drain: {energyDrainPerTick:F1}/tick | Payloads: {payloadInstances.Count}");
        }

        public void Refresh(
            List<RuntimeGeneInstance> newPayloads,
            float newRadius,
            float newDrainPerTick,
            float newMultiplier)
        {
            payloadInstances = newPayloads ?? new List<RuntimeGeneInstance>();
            radius = newRadius;
            energyDrainPerTick = newDrainPerTick;
            effectMultiplier = newMultiplier;

            // Recompute overlay colors from potentially new payloads
            activeOverlayColor = GetOverlayColor(0.25f);
            starvedOverlayColor = activeOverlayColor;
            starvedOverlayColor.a *= 0.3f;

            // Update tile overlay with new radius/color
            UpdateTileOverlay(isEnergyStarved ? starvedOverlayColor : activeOverlayColor);
        }

        protected override void OnEffectTick(int tick)
        {
            if (sourcePlant == null || sourcePlant.CurrentState == PlantState.Dead)
            {
                Debug.Log("[AuraWorldEffect] Source plant dead or null — destroying aura.");
                DestroyAura();
                return;
            }

            var energySystem = sourcePlant.EnergySystem;
            if (energySystem == null)
            {
                DestroyAura();
                return;
            }

            if (energySystem.CurrentEnergy >= energyDrainPerTick)
            {
                energySystem.CurrentEnergy -= energyDrainPerTick;

                bool wasStarved = isEnergyStarved;
                isEnergyStarved = false;

                // Refresh overlay color if recovering from starvation
                if (wasStarved)
                {
                    UpdateTileOverlay(activeOverlayColor);
                }

                var creatures = TargetFinder.FindCreaturesInRadius(transform.position, radius);
                foreach (var creature in creatures)
                {
                    if (creature == null || creature.IsDying) continue;
                    ApplyPayloadsToTarget(creature.gameObject);
                }

                if (creatures.Count > 0)
                {
                    Debug.Log($"[AuraWorldEffect] Tick {tick}: Applied payloads to {creatures.Count} creature(s) in radius {radius:F1}");
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
                        Debug.Log($"[AuraWorldEffect] Tick {tick}: Regrew {regrowCount} leaf/leaves on plants in healing radius");
                    }
                }
            }
            else
            {
                if (!isEnergyStarved)
                {
                    isEnergyStarved = true;
                    // Dim the overlay when starved
                    UpdateTileOverlay(starvedOverlayColor);
                }

                Debug.Log($"[AuraWorldEffect] Energy starved on '{sourcePlant.name}' — aura dimmed ({energySystem.CurrentEnergy:F1} < {energyDrainPerTick:F1})");
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
            // Auras don't normally expire (durationTicks = MaxValue),
            // but if they do, clean up tile overlay
        }

        void DestroyAura()
        {
            isActive = false;
            ClearTileOverlay();
            Destroy(gameObject, 0.1f);
        }
    }
}
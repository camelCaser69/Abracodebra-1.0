// FILE: Assets/Scripts/Genes/WorldEffects/AuraWorldEffect.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Implementations;
using WegoSystem;

namespace Abracodabra.Genes.WorldEffects
{
    /// <summary>
    /// Persistent area effect that drains energy every tick from its source plant.
    /// Unlike CloudWorldEffect (periodic pulse that fades), Aura is always-on
    /// until the plant dies or runs out of energy.
    /// </summary>
    public class AuraWorldEffect : WorldEffect
    {
        [Header("Aura Settings")]
        [Tooltip("Energy drained from the source plant each tick.")]
        public float energyDrainPerTick = 1.5f;

        float baseSpriteAlpha;
        float pulseTimer;
        bool isEnergyStarved;

        // Visual state
        float targetVisualScale;
        float currentVisualScale;
        const float VISUAL_LERP_SPEED = 4f;
        const float STARVED_SCALE_FACTOR = 0.4f;
        const float STARVED_ALPHA_FACTOR = 0.3f;

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

        /// <summary>
        /// Initialize the aura with its source plant, payloads, radius, and drain rate.
        /// </summary>
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

            targetVisualScale = radius * 2f;
            currentVisualScale = targetVisualScale;
            transform.localScale = Vector3.one * targetVisualScale;

            if (spriteRenderer != null)
            {
                if (payloadInstances.Count > 0)
                {
                    var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                    if (primaryPayload != null)
                    {
                        Color tint = primaryPayload.geneColor;
                        tint.a = 0.35f;
                        spriteRenderer.color = tint;
                    }
                    else
                    {
                        spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
                    }
                }
                else
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
                }

                baseSpriteAlpha = spriteRenderer.color.a;
            }

            Debug.Log($"[AuraWorldEffect] Initialized on '{source.name}' | Radius: {radius:F1} | Drain: {energyDrainPerTick:F1}/tick | Payloads: {payloadInstances.Count}");
        }

        /// <summary>
        /// Refresh an existing aura with potentially updated parameters.
        /// </summary>
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
            targetVisualScale = radius * 2f;

            if (spriteRenderer != null && payloadInstances.Count > 0)
            {
                var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                if (primaryPayload != null)
                {
                    Color tint = primaryPayload.geneColor;
                    tint.a = baseSpriteAlpha;
                    spriteRenderer.color = tint;
                }
            }
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
                isEnergyStarved = false;

                // Apply payloads to creatures
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

                // Apply healing to plants
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
                isEnergyStarved = true;
                Debug.Log($"[AuraWorldEffect] Energy starved on '{sourcePlant.name}' — aura dimmed ({energySystem.CurrentEnergy:F1} < {energyDrainPerTick:F1})");
            }
        }

        /// <summary>
        /// Returns the plant regrow chance from the first healing payload, or 0 if none.
        /// </summary>
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
            // Aura never expires via duration
        }

        void Update()
        {
            if (spriteRenderer == null) return;

            pulseTimer += Time.deltaTime * 2f;

            float targetAlpha;
            float targetScale;

            if (isEnergyStarved)
            {
                targetAlpha = baseSpriteAlpha * STARVED_ALPHA_FACTOR;
                targetScale = targetVisualScale * STARVED_SCALE_FACTOR;
            }
            else
            {
                float pulse = Mathf.Sin(pulseTimer) * 0.08f;
                targetAlpha = baseSpriteAlpha + pulse;
                targetScale = targetVisualScale;
            }

            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * VISUAL_LERP_SPEED);
            spriteRenderer.color = c;

            currentVisualScale = Mathf.Lerp(currentVisualScale, targetScale, Time.deltaTime * VISUAL_LERP_SPEED);
            transform.localScale = Vector3.one * currentVisualScale;
        }

        void DestroyAura()
        {
            isActive = false;
            StartCoroutine(FadeAndDestroyAura());
        }

        IEnumerator FadeAndDestroyAura()
        {
            if (spriteRenderer != null)
            {
                float fadeDuration = 0.5f;
                float elapsed = 0f;
                Color startColor = spriteRenderer.color;
                Vector3 startScale = transform.localScale;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;
                    float alpha = Mathf.Lerp(startColor.a, 0f, t);
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    transform.localScale = Vector3.Lerp(startScale, startScale * 0.3f, t);
                    yield return null;
                }
            }

            Destroy(gameObject);
        }
    }
}
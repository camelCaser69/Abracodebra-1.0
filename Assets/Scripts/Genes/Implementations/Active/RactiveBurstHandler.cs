// FILE: Assets/Scripts/Genes/Implementations/Active/ReactiveBurstHandler.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;
using WegoSystem;

namespace Abracodabra.Genes.Implementations
{
    /// <summary>
    /// MonoBehaviour attached to a plant that handles Reactive Burst event subscriptions.
    /// Subscribes to PlantGrowth.OnLeafConsumed and fires AoE + payloads when a leaf is consumed.
    /// Added automatically by PlantSequenceExecutor when a ReactiveBurstGene is in the sequence.
    /// </summary>
    public class ReactiveBurstHandler : MonoBehaviour, ITickUpdateable
    {
        PlantGrowth plant;
        ReactiveBurstGene burstGene;
        List<RuntimeGeneInstance> payloadInstances;
        List<RuntimeGeneInstance> modifierInstances;
        RuntimeGeneInstance activeInstance;

        int cooldownRemaining = 0;
        bool isInitialized = false;

        public void Initialize(
            PlantGrowth sourcePlant,
            ReactiveBurstGene gene,
            List<RuntimeGeneInstance> payloads,
            List<RuntimeGeneInstance> modifiers,
            RuntimeGeneInstance active)
        {
            plant = sourcePlant;
            burstGene = gene;
            payloadInstances = payloads != null ? new List<RuntimeGeneInstance>(payloads) : new List<RuntimeGeneInstance>();
            modifierInstances = modifiers != null ? new List<RuntimeGeneInstance>(modifiers) : new List<RuntimeGeneInstance>();
            activeInstance = active;
            cooldownRemaining = 0;
            isInitialized = true;

            // Subscribe to leaf consumed event
            if (plant != null)
            {
                plant.OnLeafConsumed += OnLeafConsumed;
            }

            // Register for tick updates (cooldown decrement)
            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
        }

        void OnDestroy()
        {
            // Unsubscribe
            if (plant != null)
            {
                plant.OnLeafConsumed -= OnLeafConsumed;
            }

            if (TickManager.Instance != null)
            {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }
        }

        /// <summary>
        /// Tick update: decrement cooldown.
        /// </summary>
        public void OnTickUpdate(int currentTick)
        {
            if (cooldownRemaining > 0)
            {
                cooldownRemaining--;
            }
        }

        /// <summary>
        /// Event handler: called when any leaf is consumed on this plant.
        /// </summary>
        void OnLeafConsumed(PlantGrowth sourcePlant, Vector2Int leafCoord)
        {
            if (!isInitialized || burstGene == null) return;
            if (plant == null || plant.CurrentState == PlantState.Dead) return;

            // Cooldown check
            if (cooldownRemaining > 0)
            {
                Debug.Log($"[ReactiveBurst] '{burstGene.geneName}' on cooldown ({cooldownRemaining} ticks remaining).");
                return;
            }

            // Energy check — burst costs energy when it fires
            var energySystem = plant.EnergySystem;
            if (energySystem != null && burstGene.burstEnergyCost > 0)
            {
                if (!energySystem.HasEnergy(burstGene.burstEnergyCost))
                {
                    Debug.Log($"[ReactiveBurst] '{burstGene.geneName}' — not enough energy ({energySystem.CurrentEnergy:F1} < {burstGene.burstEnergyCost:F1}).");
                    return;
                }
                energySystem.SpendEnergy(burstGene.burstEnergyCost);
            }

            // Fire the burst at the leaf's world position
            float spacing = plant.GetCellWorldSpacing();
            Vector3 burstPos = plant.transform.position + new Vector3(leafCoord.x * spacing, leafCoord.y * spacing, 0f);

            float multiplier = activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;
            float finalDamage = burstGene.baseAoeDamage * multiplier;

            // AoE damage to all creatures in radius
            var creatures = TargetFinder.FindCreaturesInRadius(burstPos, burstGene.burstRadius);
            int hitCount = 0;

            foreach (var creature in creatures)
            {
                if (creature == null || creature.IsDying) continue;

                creature.TakeDamage(finalDamage);
                hitCount++;

                // Apply payloads
                if (payloadInstances != null)
                {
                    foreach (var payloadInstance in payloadInstances)
                    {
                        if (payloadInstance == null) continue;

                        var payloadGene = payloadInstance.GetGene<PayloadGene>();
                        if (payloadGene == null) continue;

                        var context = new PayloadContext
                        {
                            target = creature.gameObject,
                            source = plant,
                            payloadInstance = payloadInstance,
                            effectMultiplier = multiplier,
                            parentGene = burstGene
                        };

                        try
                        {
                            payloadGene.ApplyPayload(context);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[ReactiveBurst] Error applying payload {payloadGene.geneName}: {e.Message}");
                        }
                    }
                }
            }

            // VFX
            if (burstGene.burstVfxPrefab != null)
            {
                var vfx = Instantiate(burstGene.burstVfxPrefab, burstPos, Quaternion.identity);
                vfx.transform.localScale = Vector3.one * burstGene.burstRadius * 2f;
                Destroy(vfx, 1f);
            }

            // Floating combat text
            FloatingCombatText.Spawn(
                burstPos + Vector3.up * 0.3f,
                $"BURST -{finalDamage:F0}",
                new Color(1f, 0.6f, 0.2f)
            );

            // Set cooldown
            cooldownRemaining = burstGene.cooldownTicks;

            Debug.Log($"[ReactiveBurst] '{burstGene.geneName}' fired at {burstPos}! Hit {hitCount} creature(s) for {finalDamage:F1} damage. Cooldown: {burstGene.cooldownTicks} ticks.");
        }
    }
}
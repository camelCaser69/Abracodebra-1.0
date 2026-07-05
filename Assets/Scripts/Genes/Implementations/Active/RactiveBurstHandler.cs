// FILE: Assets/Scripts/Genes/Implementations/Active/ReactiveBurstHandler.cs
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;
using UnityEngine;
using WegoSystem;

namespace Abracodabra.Genes.Implementations
{
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

            if (plant != null)
            {
                plant.OnLeafConsumed += OnLeafConsumed;
            }

            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
        }

        void OnDestroy()
        {
            if (plant != null)
            {
                plant.OnLeafConsumed -= OnLeafConsumed;
            }

            if (TickManager.Instance != null)
            {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }
        }

        public void OnTickUpdate(int currentTick)
        {
            if (cooldownRemaining > 0)
            {
                cooldownRemaining--;
            }
        }

        void OnLeafConsumed(PlantGrowth sourcePlant, Vector2Int leafCoord)
        {
            if (!isInitialized || burstGene == null) return;
            if (plant == null || plant.CurrentState == PlantState.Dead) return;

            if (cooldownRemaining > 0)
            {
                Debug.Log($"[ReactiveBurst] '{burstGene.geneName}' on cooldown ({cooldownRemaining} ticks remaining).");
                return;
            }

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

            float spacing = plant.GetCellWorldSpacing();
            Vector3 burstPos = plant.transform.position + new Vector3(leafCoord.x * spacing, leafCoord.y * spacing, 0f);

            float multiplier = activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;
            float finalDamage = burstGene.baseAoeDamage * multiplier;

            var creatures = TargetFinder.FindCreaturesInRadius(burstPos, burstGene.burstRadius);
            int hitCount = 0;

            foreach (var creature in creatures)
            {
                if (creature == null || creature.IsDying) continue;

                creature.TakeDamage(finalDamage);
                hitCount++;

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

            if (burstGene.burstVfxPrefab != null)
            {
                var vfx = Instantiate(burstGene.burstVfxPrefab, burstPos, Quaternion.identity);
                vfx.transform.localScale = Vector3.one * burstGene.burstRadius * 2f;
                Destroy(vfx, 1f);
            }

            // Flash affected tiles
            if (GridDebugVisualizer.Instance != null && GridPositionManager.Instance != null)
            {
                GridPosition burstCenter = GridPositionManager.Instance.WorldToGrid(burstPos);
                Color burstColor = new Color(1f, 0.6f, 0.2f, 0.3f); // Orange burst
                if (payloadInstances != null && payloadInstances.Count > 0)
                {
                    var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                    if (primaryPayload != null)
                    {
                        burstColor = primaryPayload.geneColor;
                        burstColor.a = 0.35f;
                    }
                }
                GridDebugVisualizer.Instance.ShowColoredRadiusBurst(
                    this, burstCenter, Mathf.RoundToInt(burstGene.burstRadius),
                    burstColor, 0.4f);
            }

            FloatingCombatText.Spawn(
                burstPos + Vector3.up * 0.3f,
                $"BURST -{finalDamage:F0}",
                new Color(1f, 0.6f, 0.2f)
            );

            cooldownRemaining = burstGene.cooldownTicks;

            Debug.Log($"[ReactiveBurst] '{burstGene.geneName}' fired at {burstPos}! Hit {hitCount} creature(s) for {finalDamage:F1} damage. Cooldown: {burstGene.cooldownTicks} ticks.");
        }
    }
}
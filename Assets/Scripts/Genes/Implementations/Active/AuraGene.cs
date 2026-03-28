// FILE: Assets/Scripts/Genes/Implementations/Active/AuraGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "AuraGene", menuName = "Abracodabra/Genes/Active/Aura")]
    public class AuraGene : ActiveGene
    {
        [Header("Aura Configuration")]
        [Tooltip("Base radius in tiles.")]
        public float baseRadius = 2.5f;

        [Tooltip("Energy drained from the plant's pool every tick the aura is active.")]
        public float energyDrainPerTick = 1.5f;

        [Tooltip("Prefab for the aura visual. Needs a SpriteRenderer with a circle sprite.")]
        public GameObject auraPrefab;

        public AuraGene()
        {
            // Aura has no up-front energy cost from the sequence slot.
            // It drains continuously from the pool instead.
            baseEnergyCost = 0f;
            canExecuteEmpty = true;     // Aura can fire without payloads (does nothing useful, but allowed)
            requiresTarget = false;      // Aura always activates regardless of enemies
        }

        public override void Execute(ActiveGeneContext context)
        {
            if (auraPrefab == null)
            {
                Debug.LogError($"[AuraGene] '{geneName}' is missing its auraPrefab!", this);
                return;
            }

            // Check if this plant already has an active aura from this gene — if so, just refresh it
            var existingAura = FindExistingAura(context.plant);
            if (existingAura != null && existingAura.IsActive)
            {
                // Refresh: update payloads and multiplier in case the gene config changed
                float multiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;
                existingAura.Refresh(
                    new List<RuntimeGeneInstance>(context.payloads),
                    baseRadius * Mathf.Sqrt(multiplier),
                    energyDrainPerTick,
                    multiplier
                );
                Debug.Log($"[AuraGene] '{geneName}' refreshed existing aura on '{context.plant.name}'");
                return;
            }

            // Spawn a new aura
            float spawnMultiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;
            float finalRadius = baseRadius * Mathf.Sqrt(spawnMultiplier);

            Vector3 spawnPosition = context.plant.transform.position;

            GameObject auraObj = Object.Instantiate(auraPrefab, spawnPosition, Quaternion.identity);
            auraObj.name = $"Aura_{geneName}_{context.plant.name}";

            // Parent to the plant so it moves with it (unlikely but defensive)
            auraObj.transform.SetParent(context.plant.transform);

            AuraWorldEffect aura = auraObj.GetComponent<AuraWorldEffect>();
            if (aura == null)
            {
                aura = auraObj.AddComponent<AuraWorldEffect>();
            }

            var payloadsCopy = new List<RuntimeGeneInstance>(context.payloads);

            aura.InitializeAura(
                context.plant,
                payloadsCopy,
                finalRadius,
                energyDrainPerTick,
                spawnMultiplier
            );

            Debug.Log($"[AuraGene] '{geneName}' spawned aura on '{context.plant.name}' | Radius: {finalRadius:F1} | Drain: {energyDrainPerTick:F1}/tick | Payloads: {payloadsCopy.Count}");
        }

        /// <summary>
        /// Finds an existing AuraWorldEffect parented to this plant (if any).
        /// </summary>
        AuraWorldEffect FindExistingAura(PlantGrowth plant)
        {
            if (plant == null) return null;
            return plant.GetComponentInChildren<AuraWorldEffect>();
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true; // Aura works with any payload or even empty
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                $"Creates a persistent field with <b>{baseRadius:F1}</b> tile radius.\n" +
                $"Energy Drain: <b>{energyDrainPerTick:F1} E/tick</b> (continuous).\n" +
                $"Slot Cost: <b>0 E</b> (drain is from pool, not slot).\n" +
                $"Deactivates when the plant runs out of energy.\n" +
                $"Applies payloads to all creatures in range each tick.";
        }
    }
}
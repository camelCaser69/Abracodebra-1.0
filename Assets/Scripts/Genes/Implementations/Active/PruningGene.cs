// FILE: Assets/Scripts/Genes/Implementations/Active/PruningGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "PruningGene", menuName = "Abracodabra/Genes/Active/Pruning")]
    public class PruningGene : ActiveGene
    {
        [Header("Pruning Configuration")]
        [Tooltip("Energy added to the plant's pool per leaf sacrificed.")]
        public float energyBurstAmount = 30f;

        [Tooltip("Optional: prefab for the pruning VFX (leaf wither + energy particle). Auto-destroyed after 1s.")]
        public GameObject pruneVfxPrefab;

        public PruningGene()
        {
            baseEnergyCost = 0f;        // The leaf IS the cost
            canExecuteEmpty = true;      // No payloads needed
            requiresTarget = false;      // Always fires
        }

        public override void Execute(ActiveGeneContext context)
        {
            if (context.plant == null) return;

            // Check if there are leaves to prune
            if (context.plant.ActiveLeafCount <= 0)
            {
                Debug.Log($"[PruningGene] '{geneName}' on '{context.plant.name}' — no leaves to prune. Skipping.");
                return;
            }

            // Sacrifice a leaf
            bool destroyed = context.plant.DestroyRandomLeaf("Pruning");
            if (!destroyed)
            {
                Debug.Log($"[PruningGene] '{geneName}' on '{context.plant.name}' — DestroyRandomLeaf failed. Skipping.");
                return;
            }

            // Add energy burst
            var energySystem = context.plant.EnergySystem;
            if (energySystem != null)
            {
                float multiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;
                float finalEnergy = energyBurstAmount * multiplier;

                energySystem.CurrentEnergy = Mathf.Min(
                    energySystem.CurrentEnergy + finalEnergy,
                    energySystem.MaxEnergy
                );

                // Floating combat text — energy gain
                FloatingCombatText.Spawn(
                    context.plant.transform.position + Vector3.up * 0.4f,
                    $"+{finalEnergy:F0} E",
                    new Color(0.2f, 0.8f, 1f) // Cyan — energy
                );

                Debug.Log($"[PruningGene] '{geneName}' on '{context.plant.name}' — pruned 1 leaf, gained {finalEnergy:F0} energy. Pool: {energySystem.CurrentEnergy:F1}/{energySystem.MaxEnergy:F0}");
            }

            // VFX
            if (pruneVfxPrefab != null)
            {
                var vfx = Object.Instantiate(pruneVfxPrefab, context.plant.transform.position, Quaternion.identity);
                Object.Destroy(vfx, 1f);
            }
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true; // Pruning works standalone — no payloads needed
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            float multiplier = 1f;
            if (context?.instance != null)
            {
                multiplier = context.instance.GetValue("effect_multiplier", 1f);
            }

            return $"{description}\n\n" +
                $"Sacrifices <b>1 leaf</b> to gain <b>{energyBurstAmount * multiplier:F0} energy</b>.\n" +
                $"Energy Cost: <b>0 E</b> (the leaf is the cost).\n" +
                $"<color=#FF6644>Self-damage: destroys 1 leaf per use.</color>\n" +
                $"Skips if no leaves available.\n" +
                $"Pairs with Regrowth for self-reloading builds.";
        }
    }
}
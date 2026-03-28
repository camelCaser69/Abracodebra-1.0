// FILE: Assets/Scripts/Genes/Implementations/Active/ReactiveBurstGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "ReactiveBurstGene", menuName = "Abracodabra/Genes/Active/Reactive Burst")]
    public class ReactiveBurstGene : ActiveGene
    {
        [Header("Reactive Burst Configuration")]
        [Tooltip("AoE damage dealt to all creatures in burst radius on trigger.")]
        public float baseAoeDamage = 15f;

        [Tooltip("Burst radius in tiles.")]
        public float burstRadius = 1.5f;

        [Tooltip("Minimum ticks between bursts (prevents spam).")]
        public int cooldownTicks = 2;

        [Tooltip("Energy cost per burst (deducted when the burst fires, not when the cursor passes).")]
        public float burstEnergyCost = 5f;

        [Tooltip("Optional: prefab for the burst VFX. Auto-destroyed after 1s.")]
        public GameObject burstVfxPrefab;

        public ReactiveBurstGene()
        {
            baseEnergyCost = 0f;        // No cost on cursor pass
            canExecuteEmpty = true;      // Works without payloads (base AoE damage)
            requiresTarget = false;
            isTriggerType = true;        // Cursor passes over without spending a tick
        }

        /// <summary>
        /// Execute() does nothing for trigger-type genes.
        /// The actual burst logic is in ReactiveBurstHandler.
        /// </summary>
        public override void Execute(ActiveGeneContext context)
        {
            // No-op — trigger genes don't fire on the normal strand cycle
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true; // Works with or without payloads
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                $"<b>Trigger:</b> fires when a leaf is consumed (by pests, explosions, or pruning).\n" +
                $"AoE Damage: <b>{baseAoeDamage:F0}</b> in <b>{burstRadius:F1}</b> tile radius.\n" +
                $"Energy per burst: <b>{burstEnergyCost:F0} E</b>.\n" +
                $"Cooldown: <b>{cooldownTicks}</b> ticks between bursts.\n" +
                $"The cursor passes over this slot without spending a tick.";
        }
    }
}
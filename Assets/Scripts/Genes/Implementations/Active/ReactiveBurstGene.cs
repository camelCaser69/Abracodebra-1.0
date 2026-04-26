// FILE: Assets/Scripts/Genes/Implementations/Active/ReactiveBurstGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Active/Reactive Burst", fileName = "Gene_Active_ReactiveBurst")]
    public class ReactiveBurstGene : ActiveGene
    {
        [Header("Reactive Burst Configuration")]
        [Tooltip("Base AoE damage dealt per burst.")]
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
            baseEnergyCost = 0f;
            canExecuteEmpty = true;
            requiresTarget = false;
            isTriggerType = true;
        }

        public override void Execute(ActiveGeneContext context)
        {
            // Trigger-type gene — execution handled by ReactiveBurstHandler
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true;
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
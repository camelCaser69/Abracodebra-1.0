// FILE: Assets/Scripts/Genes/Implementations/Active/ProjectileGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Active/Projectile", fileName = "Gene_Active_Projectile")]
    public class ProjectileGene : ActiveGene
    {
        [Header("Projectile Configuration")]
        [Tooltip("Base damage dealt on hit (before multipliers).")]
        public float baseDamage = 5f;

        [Tooltip("Prefab for the projectile visual. Needs a SpriteRenderer.")]
        public GameObject projectilePrefab;

        [Tooltip("Visual travel speed in world units per second.")]
        public float projectileSpeed = 8f;

        public ProjectileGene()
        {
            baseEnergyCost = 6f;
            canExecuteEmpty = true;
            requiresTarget = true;
            targetRange = 3f;
        }

        public override void Execute(ActiveGeneContext context)
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"[ProjectileGene] '{geneName}' is missing its projectilePrefab!", this);
                return;
            }

            AnimalController target = TargetFinder.FindNearestCreature(
                context.plant.transform.position,
                targetRange
            );

            if (target == null)
            {
                Debug.Log($"[ProjectileGene] '{geneName}' found no target in range {targetRange}. Skipping.");
                return;
            }

            float multiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;

            Vector3 spawnPosition = context.plant.transform.position;
            GameObject projObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            projObj.name = $"Projectile_{geneName}_{Time.frameCount}";

            ProjectileWorldEffect projectile = projObj.GetComponent<ProjectileWorldEffect>();
            if (projectile == null)
            {
                projectile = projObj.AddComponent<ProjectileWorldEffect>();
            }

            var payloadsCopy = new List<RuntimeGeneInstance>(context.payloads);

            projectile.Initialize(
                context.plant,
                target,
                baseDamage,
                projectileSpeed,
                payloadsCopy,
                multiplier
            );

            Debug.Log($"[ProjectileGene] '{geneName}' fired at {target.SpeciesName} | Damage: {baseDamage * multiplier:F1} | Payloads: {payloadsCopy.Count}");
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true;
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                $"Fires a projectile at the nearest creature within <b>{targetRange:F0}</b> tiles.\n" +
                $"Base Damage: <b>{baseDamage:F0}</b>\n" +
                $"Energy Cost: <b>{baseEnergyCost} E</b>\n" +
                $"Skips (saves energy) when no targets nearby.";
        }
    }
}
// File: Assets/Scripts/Genes/Implementations/Active/CloudGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    /// <summary>
    /// Active gene that spawns a persistent cloud area at the plant's position.
    /// Cloud applies payloads to all creatures within radius each tick.
    /// </summary>
    [CreateAssetMenu(fileName = "CloudGene", menuName = "Abracodabra/Genes/Active/Cloud")]
    public class CloudGene : ActiveGene
    {
        [Header("Cloud Configuration")]
        [Tooltip("Base radius in tiles.")]
        public float baseRadius = 2f;

        [Tooltip("Base duration in ticks.")]
        public int baseDurationTicks = 3;

        [Tooltip("Prefab for the cloud visual. Needs a SpriteRenderer with a circle sprite.")]
        public GameObject cloudPrefab;

        public CloudGene()
        {
            baseEnergyCost = 8f;
            canExecuteEmpty = true; // Cloud can fire without payloads (does nothing useful, but allowed)
            requiresTarget = false; // Clouds always fire regardless of enemies
        }

        public override void Execute(ActiveGeneContext context)
        {
            if (cloudPrefab == null)
            {
                Debug.LogError($"[CloudGene] '{geneName}' is missing its cloudPrefab!", this);
                return;
            }

            Vector3 spawnPosition = context.plant.transform.position;

            // Calculate effect multiplier from modifiers
            float multiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;

            // Apply radius scaling from overcharge
            float finalRadius = baseRadius * Mathf.Sqrt(multiplier); // sqrt so overcharge doesn't make clouds insanely large
            int finalDuration = baseDurationTicks;

            // Spawn cloud
            GameObject cloudObj = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);
            cloudObj.name = $"Cloud_{geneName}_{Time.frameCount}";

            CloudWorldEffect cloud = cloudObj.GetComponent<CloudWorldEffect>();
            if (cloud == null)
            {
                cloud = cloudObj.AddComponent<CloudWorldEffect>();
            }

            // Copy payload instances for the cloud
            var payloadsCopy = new List<RuntimeGeneInstance>(context.payloads);

            cloud.Initialize(context.plant, payloadsCopy, finalRadius, finalDuration, multiplier);

            Debug.Log($"[CloudGene] '{geneName}' spawned cloud at {spawnPosition} | Radius: {finalRadius:F1} | Duration: {finalDuration} ticks | Payloads: {payloadsCopy.Count}");
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true; // Cloud works with any payload or even empty
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                $"Spawns a cloud with <b>{baseRadius:F1}</b> tile radius.\n" +
                $"Duration: <b>{baseDurationTicks}</b> ticks.\n" +
                $"Energy Cost: <b>{baseEnergyCost} E</b>\n" +
                $"Applies payloads to all creatures in range each tick.";
        }
    }
}

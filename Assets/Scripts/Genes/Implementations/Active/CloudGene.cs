// FILE: Assets/Scripts/Genes/Implementations/Active/CloudGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.WorldEffects;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Active/Cloud", fileName = "Gene_Active_Cloud")]
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
            canExecuteEmpty = true;
            requiresTarget = false;
        }

        public override void Execute(ActiveGeneContext context)
        {
            if (cloudPrefab == null)
            {
                Debug.LogError($"[CloudGene] '{geneName}' is missing its cloudPrefab!", this);
                return;
            }

            Vector3 spawnPosition = context.plant.transform.position;

            float multiplier = context.activeInstance?.GetValue("effect_multiplier", 1f) ?? 1f;

            float finalRadius = baseRadius * Mathf.Sqrt(multiplier);
            int finalDuration = baseDurationTicks;

            GameObject cloudObj = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);
            cloudObj.name = $"Cloud_{geneName}_{Time.frameCount}";

            CloudWorldEffect cloud = cloudObj.GetComponent<CloudWorldEffect>();
            if (cloud == null)
            {
                cloud = cloudObj.AddComponent<CloudWorldEffect>();
            }

            var payloadsCopy = new List<RuntimeGeneInstance>(context.payloads);

            cloud.Initialize(context.plant, payloadsCopy, finalRadius, finalDuration, multiplier);

            Debug.Log($"[CloudGene] '{geneName}' spawned cloud at {spawnPosition} | Radius: {finalRadius:F1} | Duration: {finalDuration} ticks | Payloads: {payloadsCopy.Count}");
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true;
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
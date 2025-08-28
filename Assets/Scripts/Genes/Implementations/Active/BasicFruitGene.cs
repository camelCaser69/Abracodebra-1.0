using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Components;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Abracodabra.Genes.Implementations
{
    public class BasicFruitGene : ActiveGene
    {
        public GameObject fruitPrefab;
        public float growthTime = 2f;
        public int fruitCount = 1;
        public float launchForce = 5f;

        public override void Execute(ActiveGeneContext context)
        {
            if (fruitPrefab == null)
            {
                Debug.LogError($"BasicFruitGene '{geneName}' is missing its fruitPrefab!", this);
                return;
            }

            var effectPool = GeneServices.Get<IGeneEffectPool>();
            Transform[] fruitPoints = context.plant.GetFruitSpawnPoints();

            if (fruitPoints.Length == 0)
            {
                Debug.LogWarning($"Plant '{context.plant.name}' has no fruit spawn points. Cannot spawn fruit.", context.plant);
                return;
            }

            bool isInstant = false;
            foreach (var modInstance in context.modifiers)
            {
                if (modInstance.GetGene<ModifierGene>()?.modifierType == ModifierType.Trigger)
                {
                    isInstant = true;
                    break;
                }
            }

            int count = Mathf.Min(fruitCount, fruitPoints.Length);
            for (int i = 0; i < count; i++)
            {
                GameObject fruitObj = effectPool != null
                    ? effectPool.GetEffect(fruitPrefab, fruitPoints[i].position, Quaternion.identity)
                    : Instantiate(fruitPrefab, fruitPoints[i].position, Quaternion.identity);

                Fruit fruit = fruitObj.GetComponent<Fruit>();
                if (fruit != null)
                {
                    ConfigureFruit(fruit, context);

                    if (isInstant)
                    {
                        float angle = context.random.Range(0f, 360f);
                        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;
                        fruit.LaunchImmediate(direction.normalized * launchForce);
                    }
                    else
                    {
                        fruit.StartGrowing();
                    }
                }
            }
        }

        private void ConfigureFruit(Fruit fruit, ActiveGeneContext context)
        {
            fruit.SourcePlant = context.plant;
            fruit.GrowthTime = growthTime;

            foreach (var payloadInstance in context.payloads)
            {
                payloadInstance.GetGene<PayloadGene>()?.ConfigureFruit(fruit, payloadInstance);
            }
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads)
        {
            return true;
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return $"{description}\n\n" +
                   $"Grows <b>{fruitCount}</b> fruit(s).\n" +
                   $"Growth Time: <b>{growthTime}s</b>\n" +
                   $"Energy Cost: <b>{baseEnergyCost} E</b>\n" +
                   $"Slots: <b>{slotConfig.modifierSlots}</b> Modifiers, <b>{slotConfig.payloadSlots}</b> Payloads";
        }
    }
}
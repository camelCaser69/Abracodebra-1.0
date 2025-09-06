using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Components;
using Abracodabra.Genes.Runtime;
using WegoSystem;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "BasicFruitGene", menuName = "Abracodabra/Genes/Active/Basic Fruit Gene")]
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

            Transform[] fruitPoints = context.plant.GetFruitSpawnPoints();

            if (fruitPoints.Length == 0)
            {
                Debug.LogWarning($"Plant '{context.plant.name}' has no empty spaces for fruit spawning.", context.plant);
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
            List<Transform> shuffledPoints = fruitPoints.OrderBy(x => Random.value).ToList();

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = shuffledPoints[i].position;

                GameObject fruitObj = Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);

                FoodItem foodItem = fruitObj.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(spawnPosition);
                    foodItem.InitializeAsPlantPart(foodItem.foodType, gridPos);
                }

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

        void ConfigureFruit(Fruit fruit, ActiveGeneContext context)
        {
            fruit.SourcePlant = context.plant;
            fruit.GrowthTime = growthTime;

            // NEW: Directly assign the payload gene instances to the fruit.
            // This is the crucial step for data persistence.
            if (context.payloads != null)
            {
                fruit.PayloadGeneInstances = new List<RuntimeGeneInstance>(context.payloads);
            }

            // The original logic still runs to apply immediate effects like color.
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
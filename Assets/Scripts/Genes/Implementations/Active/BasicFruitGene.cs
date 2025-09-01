using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Components;
using WegoSystem; // <-- ADDED this using statement for GridPosition and GridPositionManager

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "NewBasicFruitGene", menuName = "Abracodabra/Genes/Active/Basic Fruit Gene")]
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
                // Get the correct world position from the temporary spawn point
                Vector3 spawnPosition = shuffledPoints[i].position;
                
                GameObject fruitObj = Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);

                // --- THIS IS THE FIX ---
                // We must manually initialize the fruit as a plant part to prevent it from snapping itself to the grid center.
                FoodItem foodItem = fruitObj.GetComponent<FoodItem>();
                if (foodItem != null)
                {
                    // Convert the spawn position to a grid position
                    GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(spawnPosition);
                    // Initialize it, which registers it correctly without moving it.
                    foodItem.InitializeAsPlantPart(foodItem.foodType, gridPos);
                }
                // --- END OF FIX ---

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
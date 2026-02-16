using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Components;
using Abracodabra.Genes.Implementations;
using Abracodabra.Genes.Runtime;
using WegoSystem;

namespace Abracodabra.Genes.Implementations {
    public class BasicFruitGene : ActiveGene {
        [Header("Fruit Production")]
        public GameObject fruitPrefab;
        public float growthTime = 2f;
        public int fruitCount = 1;
        public float launchForce = 5f;

        [Header("Harvest Item")]
        [Tooltip("The Item Definition this gene produces when the fruit is harvested.")]
        public ItemDefinition harvestedItemDefinition;

        public override void Execute(ActiveGeneContext context) {
            if (fruitPrefab == null) {
                Debug.LogError($"BasicFruitGene '{geneName}' is missing its fruitPrefab!", this);
                return;
            }
            if (harvestedItemDefinition == null) {
                Debug.LogError($"BasicFruitGene '{geneName}' is missing its Harvested Item Definition!", this);
                return;
            }

            // Get potential spawn points. 
            // NOTE: PlantGrowth.GetFruitSpawnPoints no longer auto-reserves these spots in the hashset.
            Transform[] fruitPoints = context.plant.GetFruitSpawnPoints();
            
            if (fruitPoints.Length == 0) {
                Debug.LogWarning($"Plant '{context.plant.name}' has no empty spaces for fruit spawning.", context.plant);
                return;
            }

            bool isInstant = false;
            foreach (var modInstance in context.modifiers) {
                if (modInstance.GetGene<ModifierGene>()?.modifierType == ModifierType.Trigger) {
                    isInstant = true;
                    break;
                }
            }

            int count = Mathf.Min(fruitCount, fruitPoints.Length);
            List<Transform> shuffledPoints = fruitPoints.OrderBy(x => Random.value).ToList();

            for (int i = 0; i < count; i++) {
                Vector3 spawnPosition = shuffledPoints[i].position;
                Vector2Int fruitGridCoord = Vector2Int.RoundToInt(context.plant.transform.InverseTransformPoint(spawnPosition) / context.plant.GetCellWorldSpacing());

                // Spawn the visual cell. 
                // PlantCellManager.SpawnCellVisual will call plant.RegisterFruitPosition(fruitGridCoord), locking the spot.
                GameObject fruitObj = context.plant.CellManager.SpawnCellVisual(PlantCellType.Fruit, fruitGridCoord);
                if (fruitObj == null) continue;

                fruitObj.transform.position = spawnPosition;

                FoodItem foodItem = fruitObj.GetComponent<FoodItem>();
                if (foodItem != null) {
                    GridPosition gridPos = GridPositionManager.Instance.WorldToGrid(spawnPosition);
                    foodItem.InitializeAsPlantPart(foodItem.foodType, gridPos);
                }

                Fruit fruit = fruitObj.GetComponent<Fruit>();
                if (fruit != null) {
                    ConfigureFruit(fruit, context);

                    if (isInstant) {
                        float angle = context.random.Range(0f, 360f);
                        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;
                        fruit.LaunchImmediate(direction.normalized * launchForce);
                    }
                    else {
                        fruit.StartGrowing();
                    }
                }
            }
        }

        void ConfigureFruit(Fruit fruit, ActiveGeneContext context) {
            fruit.SourcePlant = context.plant;
            fruit.GrowthTime = growthTime;

            fruit.RepresentingItemDefinition = harvestedItemDefinition;

            var dynamicProps = new Dictionary<string, float>();
            float totalPotencyMultiplier = 1f;

            foreach (var payloadInstance in context.payloads) {
                var payloadGene = payloadInstance.GetGene<PayloadGene>();
                if (payloadGene is NutritiousPayload) {
                    totalPotencyMultiplier *= payloadGene.GetFinalPotency(payloadInstance);
                }
            }

            dynamicProps["nutrition_multiplier"] = totalPotencyMultiplier;
            fruit.DynamicProperties = dynamicProps;

            // Transfer runtime payload instances to the Fruit component
            // They will later be transferred to the ItemInstance upon harvest
            fruit.PayloadGeneInstances = new List<RuntimeGeneInstance>(context.payloads);

            foreach (var payloadInstance in context.payloads) {
                payloadInstance.GetGene<PayloadGene>()?.ConfigureFruit(fruit, payloadInstance);
            }
        }

        public override bool IsValidConfiguration(List<ModifierGene> modifiers, List<PayloadGene> payloads) {
            // Fruit gene is valid even without payloads (produces basic fruit)
            return true;
        }

        public override string GetTooltip(GeneTooltipContext context) {
            return $"{description}\n\n" +
                   $"Grows <b>{fruitCount}</b> fruit(s).\n" +
                   $"Produces Item: <b>{(harvestedItemDefinition != null ? harvestedItemDefinition.itemName : "None")}</b>\n" +
                   $"Energy Cost: <b>{baseEnergyCost} E</b>\n" +
                   $"Slots: <b>{slotConfig.modifierSlots}</b> Modifiers, <b>{slotConfig.payloadSlots}</b> Payloads";
        }
    }
}
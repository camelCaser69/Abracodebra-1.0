using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Services;
using Abracodabra.Genes.Components;
using Abracodabra.Genes.Implementations; // Added to reference NutritiousPayload
using WegoSystem;

namespace Abracodabra.Genes.Implementations
{
    [CreateAssetMenu(fileName = "BasicFruitGene", menuName = "Abracodabra/Genes/Active/Basic Fruit Gene")]
    public class BasicFruitGene : ActiveGene
    {
        [Header("Fruit Production")]
        public GameObject fruitPrefab;
        public float growthTime = 2f;
        public int fruitCount = 1;
        public float launchForce = 5f;

        [Header("Harvest Item")]
        [Tooltip("The Item Definition this gene produces when the fruit is harvested.")]
        public ItemDefinition harvestedItemDefinition; // NEW: Link to the item asset.

        public override void Execute(ActiveGeneContext context)
        {
            if (fruitPrefab == null)
            {
                Debug.LogError($"BasicFruitGene '{geneName}' is missing its fruitPrefab!", this);
                return;
            }
            // NEW: Check if the ItemDefinition is assigned.
            if (harvestedItemDefinition == null)
            {
                Debug.LogError($"BasicFruitGene '{geneName}' is missing its Harvested Item Definition!", this);
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
                Vector2Int fruitGridCoord = Vector2Int.RoundToInt(context.plant.transform.InverseTransformPoint(spawnPosition) / context.plant.GetCellWorldSpacing());

                GameObject fruitObj = context.plant.CellManager.SpawnCellVisual(PlantCellType.Fruit, fruitGridCoord);
                if (fruitObj == null) continue;
                
                fruitObj.transform.position = spawnPosition;
                
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
            
            // NEW: Assign the static item definition.
            fruit.RepresentingItemDefinition = harvestedItemDefinition;
            
            // NEW: Calculate dynamic properties from payloads and store them on the fruit.
            var dynamicProps = new Dictionary<string, float>();
            float totalPotencyMultiplier = 1f;

            foreach (var payloadInstance in context.payloads)
            {
                var payloadGene = payloadInstance.GetGene<PayloadGene>();
                if (payloadGene is NutritiousPayload)
                {
                    // This is just one example. You could add more properties here.
                    totalPotencyMultiplier *= payloadGene.GetFinalPotency(payloadInstance);
                }
                // Future payloads could add other properties, like "poison_damage", etc.
            }

            // Store the calculated multiplier in the dictionary.
            dynamicProps["nutrition_multiplier"] = totalPotencyMultiplier;
            fruit.DynamicProperties = dynamicProps;
            
            // Apply immediate visual/config effects.
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
                   $"Produces Item: <b>{(harvestedItemDefinition != null ? harvestedItemDefinition.itemName : "None")}</b>\n" +
                   $"Energy Cost: <b>{baseEnergyCost} E</b>\n" +
                   $"Slots: <b>{slotConfig.modifierSlots}</b> Modifiers, <b>{slotConfig.payloadSlots}</b> Payloads";
        }
    }
}
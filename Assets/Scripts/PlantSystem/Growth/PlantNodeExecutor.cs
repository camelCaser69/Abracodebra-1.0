using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

public class PlantNodeExecutor
{
    private readonly PlantGrowth plant;

    public PlantNodeExecutor(PlantGrowth plant)
    {
        this.plant = plant;
    }

    public void ExecuteMatureCycleTick()
    {
        if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0)
        {
            Debug.LogError($"[{plant.gameObject.name}] NodeGraph missing or empty!");
            return;
        }

        // --- PHASE 1: CALCULATION & PREPARATION ---
        // First, we gather all active effects and calculate the total energy cost.

        var effectsToExecute = new List<NodeEffectData>();
        var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects)
            {
                if (effect == null || effect.isPassive) continue; // We only care about ACTIVE effects now

                // Add this active effect to our list for later execution
                effectsToExecute.Add(effect);

                // If this effect has a cost, add it to the total
                if (effect.effectType == NodeEffectType.EnergyCost)
                {
                    totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                }
            }
        }

        // --- PHASE 2: ENERGY CHECK ---
        // Now, check if we have enough energy BEFORE executing anything.

        if (plant.EnergySystem.CurrentEnergy < totalEnergyCostForCycle)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{plant.gameObject.name}] Not enough energy ({plant.EnergySystem.CurrentEnergy}/{totalEnergyCostForCycle}) for mature cycle. Abilities will not fire.");
            }
            return; // Not enough energy, so we stop here.
        }

        // --- PHASE 3: EXECUTION ---
        // We have enough energy. Spend it, then execute all queued effects.

        plant.EnergySystem.SpendEnergy(totalEnergyCostForCycle);

        foreach (var effect in effectsToExecute)
        {
            switch (effect.effectType)
            {
                // FIX: Poop Absorption is now a standard active effect.
                case NodeEffectType.PoopAbsorption:
                    CheckForPoopAndAbsorb(effect.primaryValue, effect.secondaryValue);
                    break;

                case NodeEffectType.GrowBerry:
                    SpawnBerry();
                    break;
                
                // We still gather scent data here for a final application after the loop
                case NodeEffectType.ScentModifier:
                    if (effect.scentDefinitionReference != null)
                    {
                        ScentDefinition key = effect.scentDefinitionReference;
                        if (!accumulatedScentRadiusBonus.ContainsKey(key))
                            accumulatedScentRadiusBonus[key] = 0f;
                        accumulatedScentRadiusBonus[key] += effect.primaryValue;

                        if (!accumulatedScentStrengthBonus.ContainsKey(key))
                            accumulatedScentStrengthBonus[key] = 0f;
                        accumulatedScentStrengthBonus[key] += effect.secondaryValue;
                    }
                    break;
                
                // Note: Damage and EnergyCost effects are handled elsewhere or are purely for calculation
                // and don't have an execution step here.
            }
        }

        // Finally, apply any scent modifiers that were gathered.
        if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0)
        {
            ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
    }

    private void CheckForPoopAndAbsorb(float detectionRadius, float energyBonus)
    {
        if (detectionRadius <= 0f) return;

        GridEntity plantGrid = plant.GetComponent<GridEntity>();
        if (plantGrid == null) return;

        int radiusTiles = Mathf.RoundToInt(detectionRadius);
        var tilesInRadius = GridRadiusUtility.GetTilesInCircle(plantGrid.Position, radiusTiles);

        foreach (var tile in tilesInRadius)
        {
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tile);
            foreach (var entity in entitiesAtTile)
            {
                PoopController poop = entity.GetComponent<PoopController>();
                if (poop != null)
                {
                    if (energyBonus > 0f)
                    {
                        plant.EnergySystem.AddEnergy(energyBonus);
                        if (Debug.isDebugBuild)
                        {
                            Debug.Log($"[{plant.gameObject.name}] Absorbed poop and gained {energyBonus} energy!");
                        }
                    }

                    Object.Destroy(poop.gameObject);
                    break;
                }
            }
        }
    }

    private void SpawnBerry()
    {
        var cells = plant.CellManager.GetCells();
        if (cells.Count == 0) return;

        int maxBerriesAllowed = plant.GrowthLogic.MaxBerries;
        bool hasLimit = maxBerriesAllowed > 0;

        int currentBerryCount = plant.CellManager.GetBerryCount();

        if (hasLimit && currentBerryCount >= maxBerriesAllowed)
        {
            Debug.Log($"[{plant.gameObject.name}] Already has {currentBerryCount} berries (max: {maxBerriesAllowed}). Skipping berry spawn.");
            return;
        }

        HashSet<Vector2Int> availablePositions = new HashSet<Vector2Int>();

        foreach (var kvp in cells)
        {
            if (kvp.Value == PlantCellType.Stem || kvp.Value == PlantCellType.Leaf)
            {
                Vector2Int cellPos = kvp.Key;
                Vector2Int[] surroundingPositions = new Vector2Int[]
                {
                    cellPos + Vector2Int.up,
                    cellPos + Vector2Int.down,
                    cellPos + Vector2Int.left,
                    cellPos + Vector2Int.right,
                    cellPos + Vector2Int.up + Vector2Int.left,    // up-left
                    cellPos + Vector2Int.up + Vector2Int.right,   // up-right
                    cellPos + Vector2Int.down + Vector2Int.left,  // down-left
                    cellPos + Vector2Int.down + Vector2Int.right  // down-right
                };

                foreach (Vector2Int pos in surroundingPositions)
                {
                    if (!plant.CellManager.HasCellAt(pos))
                    {
                        availablePositions.Add(pos);
                    }
                }
            }
        }

        availablePositions.RemoveWhere(pos => pos.y <= 0);

        List<Vector2Int> candidatePositions = availablePositions.ToList();

        if (candidatePositions.Count == 0)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] No available space to spawn berry (all valid positions occupied or filtered).");
            return;
        }

        List<Vector2Int> existingBerryPositions = plant.CellManager.GetBerryPositions();
        List<Vector2Int> preferredPositions = new List<Vector2Int>();
        List<Vector2Int> otherPositions = new List<Vector2Int>();

        if (existingBerryPositions.Count > 0)
        {
            foreach (var candidate in candidatePositions)
            {
                if (IsAdjacentToExistingBerries(candidate, existingBerryPositions))
                {
                    otherPositions.Add(candidate);
                }
                else
                {
                    preferredPositions.Add(candidate);
                }
            }
        }
        else
        {
            preferredPositions.AddRange(candidatePositions);
        }

        List<Vector2Int> finalCandidates = preferredPositions.Count > 0 ? preferredPositions : otherPositions;
        
        if (finalCandidates.Count == 0) {
            finalCandidates = otherPositions;
        }
        
        if (finalCandidates.Count == 0) {
            Debug.LogWarning($"[{plant.gameObject.name}] No final candidates to spawn berry.");
            return;
        }


        int randomIndex = Random.Range(0, finalCandidates.Count);
        Vector2Int chosenPosition = finalCandidates[randomIndex];
        GameObject berry = plant.CellManager.SpawnCellVisual(PlantCellType.Fruit, chosenPosition, null, null);

        if (berry != null)
        {
            string limitInfo = hasLimit ? $"#{currentBerryCount + 1}/{maxBerriesAllowed}" : $"#{currentBerryCount + 1} (no limit)";
            Debug.Log($"[{plant.gameObject.name}] Spawned berry {limitInfo} at {chosenPosition}");
            AddFoodComponentToBerry(berry);
        }
        else
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Failed to spawn berry at {chosenPosition}");
        }
    }

    private bool IsAdjacentToExistingBerries(Vector2Int position, List<Vector2Int> existingBerries)
    {
        foreach (var berryPos in existingBerries)
        {
            if (Mathf.Max(Mathf.Abs(position.x - berryPos.x), Mathf.Abs(position.y - berryPos.y)) == 1)
            {
                return true;
            }
        }
        return false;
    }

    private void AddFoodComponentToBerry(GameObject berry)
    {
        if (berry == null) return;

        FoodItem foodItem = berry.GetComponent<FoodItem>();
        if (foodItem != null) return;

        FoodType berryFoodType = GetBerryFoodType();
        if (berryFoodType != null)
        {
            foodItem = berry.AddComponent<FoodItem>();
            foodItem.foodType = berryFoodType;
        }
    }

    private FoodType GetBerryFoodType()
    {
        FoodType berryType = Resources.Load<FoodType>("FoodTypes/Berry");
        if (berryType != null) return berryType;

        FoodType[] allFoodTypes = Resources.LoadAll<FoodType>("");
        foreach (FoodType foodType in allFoodTypes)
        {
            if (foodType.category == FoodType.FoodCategory.Plant_Fruit)
            {
                return foodType;
            }
        }

        Debug.LogWarning($"[{plant.gameObject.name}] No berry FoodType found. Berry won't be edible by animals.");
        return null;
    }

    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
    {
        if (targetObject == null) return;

        foreach (var kvp in scentRadiusBonuses)
        {
            ScentDefinition scentDef = kvp.Key;
            float radiusBonus = kvp.Value;
            float strengthBonus = scentStrengthBonuses.ContainsKey(scentDef) ? scentStrengthBonuses[scentDef] : 0f;

            ScentSource existingSource = targetObject.GetComponentsInChildren<ScentSource>()
                .FirstOrDefault(s => s.Definition == scentDef);

            if (existingSource != null)
            {
                existingSource.ApplyModifiers(radiusBonus, strengthBonus);
            }
            else
            {
                GameObject scentSourceObj = new GameObject($"ScentSource_{scentDef.displayName}");
                scentSourceObj.transform.SetParent(targetObject.transform);
                scentSourceObj.transform.localPosition = Vector3.zero;

                ScentSource newSource = scentSourceObj.AddComponent<ScentSource>();
                SetScentSourceDefinition(newSource, scentDef);
                newSource.SetRadiusModifier(radiusBonus);
                newSource.SetStrengthModifier(strengthBonus);
            }
        }
    }

    private void SetScentSourceDefinition(ScentSource source, ScentDefinition definition)
    {
        source.SetDefinition(definition);
    }
}
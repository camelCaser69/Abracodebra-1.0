using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantNodeExecutor
{
    readonly PlantGrowth plant;

    public float PoopDetectionRadius { get; private set; }
    public float PoopEnergyBonus { get; private set; }

    public PlantNodeExecutor(PlantGrowth plant)
    {
        this.plant = plant;
    }

    public void ProcessPassiveEffects(NodeGraph nodeGraph)
    {
        PoopDetectionRadius = 0f;
        PoopEnergyBonus = 0f;

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects)
            {
                if (effect == null || !effect.isPassive) continue;

                switch (effect.effectType)
                {
                    case NodeEffectType.PoopAbsorption:
                        PoopDetectionRadius = Mathf.Max(0f, effect.primaryValue);
                        PoopEnergyBonus = Mathf.Max(0f, effect.secondaryValue);
                        break;
                }
            }
        }
    }

    public void ExecuteMatureCycleTick()
{
    if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0)
    {
        Debug.LogError($"[{plant.gameObject.name}] NodeGraph missing or empty!");
        return;
    }

    float damageMultiplier = 1.0f;
    var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
    var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
    float totalEnergyCostForCycle = 0f;

    foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
    {
        if (node?.effects == null) continue;

        foreach (var effect in node.effects)
        {
            if (effect == null || effect.isPassive) continue;

            switch (effect.effectType)
            {
                case NodeEffectType.EnergyCost:
                    totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                    break;

                case NodeEffectType.Damage:
                    damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue);
                    break;

                // FIXED: Added missing GrowBerry case
                case NodeEffectType.GrowBerry:
                    SpawnBerry();
                    break;

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
            }
        }
    }

    if (PoopDetectionRadius > 0f)
    {
        CheckForPoopAndAbsorb();
    }

    if (plant.EnergySystem.CurrentEnergy < totalEnergyCostForCycle)
    {
        if (Debug.isDebugBuild)
        {
            Debug.Log($"[{plant.gameObject.name}] Not enough energy ({plant.EnergySystem.CurrentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
        }
        return;
    }

    plant.EnergySystem.SpendEnergy(totalEnergyCostForCycle);

    if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0)
    {
        ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
    }
}
    
    private void SpawnBerry()
    {
        var cells = plant.CellManager.GetCells();
        if (cells.Count == 0) return;

        int maxBerriesAllowed = plant.GrowthLogic.MaxBerries;
        bool hasLimit = maxBerriesAllowed > 0;

        int currentBerryCount = 0;
        foreach (var kvp in cells)
        {
            if (kvp.Value == PlantCellType.Fruit)
            {
                currentBerryCount++;
            }
        }

        if (hasLimit && currentBerryCount >= maxBerriesAllowed)
        {
            Debug.Log($"[{plant.gameObject.name}] Already has {currentBerryCount} berries (max: {maxBerriesAllowed}). Skipping berry spawn.");
            return;
        }

        HashSet<Vector2Int> availablePositions = new HashSet<Vector2Int>();

        // Find all empty positions adjacent to existing stems or leaves
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

        List<Vector2Int> candidatePositions = availablePositions.ToList();

        if (candidatePositions.Count == 0)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] No available space to spawn berry (all positions occupied)");
            return;
        }
        
        // ** THE FIX IS HERE **
        // Instead of sorting by height, we now pick a random position from the list of candidates.
        int randomIndex = Random.Range(0, candidatePositions.Count);
        Vector2Int chosenPosition = candidatePositions[randomIndex];

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
    
    float GetMinDistanceToExistingBerries(Vector2Int position, Dictionary<Vector2Int, PlantCellType> cells)
    {
        float minDistance = float.MaxValue;
        bool foundBerry = false;
    
        foreach (var kvp in cells)
        {
            if (kvp.Value == PlantCellType.Fruit)
            {
                foundBerry = true;
                float distance = Vector2Int.Distance(position, kvp.Key);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }
    
        return foundBerry ? minDistance : float.MaxValue; // If no berries exist, return max value
    }

// Helper method to get berry food type (you may need to adjust this based on your setup)
    FoodType GetBerryFoodType()
    {
        // Try multiple approaches to find a berry food type
    
        // Approach 1: Try to load from Resources
        FoodType berryType = Resources.Load<FoodType>("FoodTypes/Berry");
        if (berryType != null) return berryType;
    
        berryType = Resources.Load<FoodType>("Berry");
        if (berryType != null) return berryType;
    
        // Approach 2: Find any food type with Plant_Fruit category
        FoodType[] allFoodTypes = Resources.LoadAll<FoodType>("");
        foreach (FoodType foodType in allFoodTypes)
        {
            if (foodType.category == FoodType.FoodCategory.Plant_Fruit)
            {
                return foodType;
            }
        }
    
        // Approach 3: If no specific berry type found, create a warning
        Debug.LogWarning($"[{plant.gameObject.name}] No berry FoodType found. Berry won't be edible by animals.");
        return null;
    }
    
    void AddFoodComponentToBerry(GameObject berry)
    {
        if (berry == null) return;

        FoodItem foodItem = berry.GetComponent<FoodItem>();
        if (foodItem != null) return; // Already has FoodItem component

        // Try to find a berry food type
        FoodType berryFoodType = GetBerryFoodType();
        if (berryFoodType != null)
        {
            foodItem = berry.AddComponent<FoodItem>();
            foodItem.foodType = berryFoodType;
            Debug.Log($"[{plant.gameObject.name}] Added FoodItem component to berry");
        }
    }

    void CheckForPoopAndAbsorb()
    {
        if (PoopDetectionRadius <= 0f) return;

        GridEntity plantGrid = plant.GetComponent<GridEntity>();
        if (plantGrid == null) return;

        int radiusTiles = Mathf.RoundToInt(PoopDetectionRadius);
        var tilesInRadius = GridRadiusUtility.GetTilesInCircle(plantGrid.Position, radiusTiles);

        foreach (var tile in tilesInRadius)
        {
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tile);
            foreach (var entity in entitiesAtTile)
            {
                PoopController poop = entity.GetComponent<PoopController>();
                if (poop != null)
                {
                    // Absorb the poop and gain energy
                    if (PoopEnergyBonus > 0f)
                    {
                        plant.EnergySystem.AddEnergy(PoopEnergyBonus);
                        if (Debug.isDebugBuild)
                        {
                            Debug.Log($"[{plant.gameObject.name}] Absorbed poop and gained {PoopEnergyBonus} energy!");
                        }
                    }
                    
                    // Destroy the poop
                    Object.Destroy(poop.gameObject);
                    break; // Only absorb one poop per tile per cycle
                }
            }
        }
    }

    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses)
    {
        if (targetObject == null) return;

        foreach (var kvp in scentRadiusBonuses)
        {
            ScentDefinition scentDef = kvp.Key;
            float radiusBonus = kvp.Value;
            float strengthBonus = scentStrengthBonuses.ContainsKey(scentDef) ? scentStrengthBonuses[scentDef] : 0f;

            // Try to find existing scent source with this definition
            ScentSource existingSource = targetObject.GetComponentsInChildren<ScentSource>()
                .FirstOrDefault(s => s.Definition == scentDef);

            if (existingSource != null)
            {
                // Modify existing scent source
                existingSource.ApplyModifiers(radiusBonus, strengthBonus);
            }
            else
            {
                // Create new scent source
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

    // Helper method to set scent source definition
    void SetScentSourceDefinition(ScentSource source, ScentDefinition definition)
    {
        source.SetDefinition(definition);
    }
}
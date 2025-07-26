// Assets/Scripts/PlantSystem/Growth/PlantNodeExecutor.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

public class PlantNodeExecutor
{
    private readonly PlantGrowth plant;

    private static readonly HashSet<NodeEffectType> EnergyFreeEffectTypes = new HashSet<NodeEffectType>
    {
        // Add any truly energy-free active effects here if they exist
    };

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

        Debug.Log($"[{plant.gameObject.name}] Executing mature cycle tick. Current energy: {plant.EnergySystem.CurrentEnergy:F1}/{plant.EnergySystem.MaxEnergy:F0}");

        var energyFreeEffects = new List<NodeEffectData>();
        var energyRequiringEffects = new List<NodeEffectData>();
        var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;

            Debug.Log($"[{plant.gameObject.name}] Processing node '{node.nodeDisplayName}' with {node.effects.Count} effects");

            foreach (var effect in node.effects)
            {
                if (effect == null) continue;

                if (effect.IsPassive)
                {
                    Debug.Log($"[{plant.gameObject.name}] Skipping passive effect: {effect.effectType}");
                    continue;
                }

                if (IsEnergyFreeEffect(effect.effectType))
                {
                    energyFreeEffects.Add(effect);
                }
                else
                {
                    energyRequiringEffects.Add(effect);

                    if (effect.effectType == NodeEffectType.EnergyCost)
                    {
                        totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                    }
                }

                Debug.Log($"[{plant.gameObject.name}] Found active effect: {effect.effectType} (energy-free: {IsEnergyFreeEffect(effect.effectType)})");
            }
        }

        if (energyFreeEffects.Count > 0)
        {
            Debug.Log($"[{plant.gameObject.name}] Executing {energyFreeEffects.Count} energy-free effects");
            foreach (var effect in energyFreeEffects)
            {
                ExecuteEffect(effect, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
            }
        }

        float totalCostForAbilities = totalEnergyCostForCycle;

        // Tally up costs for other abilities if they have an implicit cost
        foreach (var effect in energyRequiringEffects)
        {
            if (effect.effectType != NodeEffectType.EnergyCost && effect.effectType == NodeEffectType.GrowBerry) // Example: Berries cost 1 energy
            {
                totalCostForAbilities += 1f;
            }
        }

        Debug.Log($"[{plant.gameObject.name}] Total energy cost for abilities: {totalCostForAbilities}");

        if (totalCostForAbilities > 0 && plant.EnergySystem.CurrentEnergy < totalCostForAbilities)
        {
            Debug.Log($"[{plant.gameObject.name}] Not enough energy ({plant.EnergySystem.CurrentEnergy:F1}/{totalCostForAbilities:F1}) for abilities. Skipping.");

            // Still apply any passive scent mods if necessary (though they should be handled differently)
            if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0)
            {
                ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
            }
            return;
        }

        if (totalCostForAbilities > 0)
        {
            plant.EnergySystem.SpendEnergy(totalCostForAbilities);
            Debug.Log($"[{plant.gameObject.name}] Spent {totalCostForAbilities} energy. Remaining: {plant.EnergySystem.CurrentEnergy:F1}");
        }

        if (energyRequiringEffects.Count > 0)
        {
            Debug.Log($"[{plant.gameObject.name}] Executing {energyRequiringEffects.Count} energy-requiring effects");
            foreach (var effect in energyRequiringEffects)
            {
                ExecuteEffect(effect, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
            }
        }

        // Apply accumulated scent data if any
        if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0)
        {
            ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
    }

    bool IsEnergyFreeEffect(NodeEffectType effectType)
    {
        return EnergyFreeEffectTypes.Contains(effectType);
    }

    void ExecuteEffect(NodeEffectData effect, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus)
    {
        switch (effect.effectType)
        {
            case NodeEffectType.PoopAbsorption:
                Debug.LogWarning($"[{plant.gameObject.name}] Tried to execute PoopAbsorption as an ACTIVE effect. It should be PASSIVE. This is automatically handled; no need to set a checkbox.", plant.gameObject);
                break;

            case NodeEffectType.GrowBerry:
                SpawnBerry(effect);
                break;

            case NodeEffectType.ScentModifier:
                Debug.LogWarning($"[{plant.gameObject.name}] ScentModifier effect needs to be reimplemented without scentDefinitionReference");
                break;
        }
    }

    public void ExecutePassiveAbilities(List<NodeEffectData> passiveEffects = null)
    {
        if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0) return;

        if (passiveEffects == null)
        {
            passiveEffects = new List<NodeEffectData>();
            foreach (var node in plant.NodeGraph.nodes)
            {
                if (node?.effects == null) continue;
                foreach (var effect in node.effects)
                {
                    if (effect != null && effect.IsPassive)
                    {
                        passiveEffects.Add(effect);
                    }
                }
            }
        }

        foreach (var effect in passiveEffects)
        {
            // Execute passive abilities here if they need active logic (uncommon)
        }
    }

    public void SpawnBerry(NodeEffectData growEffectData)
    {
        if (plant == null || (plant.CurrentState != PlantState.Mature_Idle && plant.CurrentState != PlantState.Mature_Executing))
        {
            Debug.LogWarning($"[{plant?.gameObject.name ?? "Unknown"}] Berry cannot be spawned.", plant?.gameObject);
            return;
        }

        Debug.Log($"[{plant.gameObject.name}] SpawnBerry called!");

        if (plant.CellManager == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] CellManager is null!");
            return;
        }

        var cells = plant.CellManager.GetCells();
        if (cells.Count == 0)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] No cells in plant!");
            return;
        }

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
                    cellPos + Vector2Int.up + Vector2Int.left,
                    cellPos + Vector2Int.up + Vector2Int.right,
                    cellPos + Vector2Int.down + Vector2Int.left,
                    cellPos + Vector2Int.down + Vector2Int.right
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
            Debug.LogWarning($"[{plant.gameObject.name}] No available space to spawn berry!");
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

        if (finalCandidates.Count == 0)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] No final candidates to spawn berry.");
            return;
        }

        int randomIndex = Random.Range(0, finalCandidates.Count);
        Vector2Int chosenPosition = finalCandidates[randomIndex];

        GameObject berry = plant.CellManager.SpawnCellVisual(PlantCellType.Fruit, chosenPosition, null, null);

        if (berry != null)
        {
            string limitInfo = hasLimit ? $"#{currentBerryCount + 1}/{maxBerriesAllowed}" : $"#{currentBerryCount + 1} (no limit)";
            Debug.Log($"[{plant.gameObject.name}] SUCCESS: Spawned berry {limitInfo} at {chosenPosition}");

            AddFoodComponentToBerry(berry);
        }
        else
        {
            Debug.LogError($"[{plant.gameObject.name}] FAILED to spawn berry at {chosenPosition} - SpawnCellVisual returned null!");
        }
    }

    bool IsAdjacentToExistingBerries(Vector2Int position, List<Vector2Int> existingBerries)
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

    void AddFoodComponentToBerry(GameObject berry)
    {
        if (berry == null) return;

        FoodItem foodItem = berry.GetComponent<FoodItem>();
        if (foodItem == null)
        {
            FoodType berryFoodType = GetBerryFoodType();
            if (berryFoodType != null)
            {
                foodItem = berry.AddComponent<FoodItem>();
                foodItem.foodType = berryFoodType;
            }
        }

        HarvestableTag tag = berry.AddComponent<HarvestableTag>();

        Debug.Log($"[PlantNodeExecutor] Added HarvestableTag to berry", berry);
    }

    FoodType GetBerryFoodType()
    {
        // A more robust way to load specific assets
        FoodType berryType = Resources.Load<FoodType>("FoodTypes/Berry");
        if (berryType != null) return berryType;

        // Fallback to searching all loaded FoodTypes
        FoodType[] allFoodTypes = Resources.LoadAll<FoodType>("");
        foreach (FoodType foodType in allFoodTypes)
        {
            if (foodType.category == FoodType.FoodCategory.Plant_Fruit)
            {
                return foodType; // Return the first one found
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
                // Create a new ScentSource if one for this definition doesn't exist
                GameObject scentSourceObj = new GameObject($"ScentSource_{scentDef.displayName}");
                scentSourceObj.transform.SetParent(targetObject.transform);
                scentSourceObj.transform.localPosition = Vector3.zero;

                ScentSource newSource = scentSourceObj.AddComponent<ScentSource>();
                SetScentSourceDefinition(newSource, scentDef); // Use helper to set definition
                newSource.SetRadiusModifier(radiusBonus);
                newSource.SetStrengthModifier(strengthBonus);
            }
        }
    }

    void SetScentSourceDefinition(ScentSource source, ScentDefinition definition)
    {
        source.SetDefinition(definition);
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantNodeExecutor
{
    private readonly PlantGrowth plant;

    // This set no longer needs to contain PoopAbsorption
    static readonly HashSet<NodeEffectType> EnergyFreeEffectTypes = new HashSet<NodeEffectType>
    {
    };

    public PlantNodeExecutor(PlantGrowth plant)
    {
        this.plant = plant;
    }

    // In PlantNodeExecutor.cs, replace ExecuteMatureCycleTick with this fixed version:

public void ExecuteMatureCycleTick() {
    if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0) {
        Debug.LogError($"[{plant.gameObject.name}] NodeGraph missing or empty!");
        return;
    }
    
    Debug.Log($"[{plant.gameObject.name}] Executing mature cycle tick. Current energy: {plant.EnergySystem.CurrentEnergy:F1}/{plant.EnergySystem.MaxEnergy:F0}");
    
    var energyFreeEffects = new List<NodeEffectData>();
    var energyRequiringEffects = new List<NodeEffectData>();
    var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
    var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
    float totalEnergyCostForCycle = 0f;
    
    // Categorize effects and calculate total energy cost
    foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex)) {
        if (node?.effects == null) continue;
        
        Debug.Log($"[{plant.gameObject.name}] Processing node '{node.nodeDisplayName}' with {node.effects.Count} effects");
        
        foreach (var effect in node.effects) {
            if (effect == null) continue;
            
            // Skip passive effects - they're handled elsewhere
            if (effect.isPassive) {
                Debug.Log($"[{plant.gameObject.name}] Skipping passive effect: {effect.effectType}");
                continue;
            }
            
            if (IsEnergyFreeEffect(effect.effectType)) {
                energyFreeEffects.Add(effect);
            }
            else {
                energyRequiringEffects.Add(effect);
                
                // Add energy cost
                if (effect.effectType == NodeEffectType.EnergyCost) {
                    totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                }
            }
            
            Debug.Log($"[{plant.gameObject.name}] Found active effect: {effect.effectType} (energy-free: {IsEnergyFreeEffect(effect.effectType)})");
        }
    }
    
    // Execute energy-free effects
    if (energyFreeEffects.Count > 0) {
        Debug.Log($"[{plant.gameObject.name}] Executing {energyFreeEffects.Count} energy-free effects");
        foreach (var effect in energyFreeEffects) {
            ExecuteEffect(effect, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
    }
    
    // Check if we have enough energy for energy-requiring effects
    float totalCostForAbilities = totalEnergyCostForCycle;
    
    // Add implicit energy cost for each energy-requiring ability (if no explicit cost)
    foreach (var effect in energyRequiringEffects) {
        if (effect.effectType != NodeEffectType.EnergyCost && effect.effectType == NodeEffectType.GrowBerry) {
            // Berry growing has implicit cost of 1 energy if not specified
            totalCostForAbilities += 1f;
        }
    }
    
    Debug.Log($"[{plant.gameObject.name}] Total energy cost for abilities: {totalCostForAbilities}");
    
    if (totalCostForAbilities > 0 && plant.EnergySystem.CurrentEnergy < totalCostForAbilities) {
        Debug.Log($"[{plant.gameObject.name}] Not enough energy ({plant.EnergySystem.CurrentEnergy:F1}/{totalCostForAbilities:F1}) for abilities. Skipping.");
        
        // Apply any accumulated scent bonuses even if we can't execute abilities
        if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0) {
            ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
        return;
    }
    
    // Spend energy
    if (totalCostForAbilities > 0) {
        plant.EnergySystem.SpendEnergy(totalCostForAbilities);
        Debug.Log($"[{plant.gameObject.name}] Spent {totalCostForAbilities} energy. Remaining: {plant.EnergySystem.CurrentEnergy:F1}");
    }
    
    // Execute energy-requiring effects
    if (energyRequiringEffects.Count > 0) {
        Debug.Log($"[{plant.gameObject.name}] Executing {energyRequiringEffects.Count} energy-requiring effects");
        foreach (var effect in energyRequiringEffects) {
            ExecuteEffect(effect, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }
    }
    
    // Apply accumulated scent bonuses
    if (accumulatedScentRadiusBonus.Count > 0 || accumulatedScentStrengthBonus.Count > 0) {
        ApplyScentDataToObject(plant.gameObject, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
    }
}

    private bool IsEnergyFreeEffect(NodeEffectType effectType)
    {
        return EnergyFreeEffectTypes.Contains(effectType);
    }

    private void ExecuteEffect(NodeEffectData effect, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus)
    {
        switch (effect.effectType)
        {
            case NodeEffectType.PoopAbsorption:
                // NEW: Add a safeguard warning if this effect is ever misconfigured as "active"
                Debug.LogWarning($"[{plant.gameObject.name}] Tried to execute PoopAbsorption as an ACTIVE effect. It should be PASSIVE. Please check the 'Is Passive' box on its NodeDefinition in the Inspector.");
                break;

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
                    if (effect != null && effect.isPassive)
                    {
                        passiveEffects.Add(effect);
                    }
                }
            }
        }

        foreach (var effect in passiveEffects)
        {
            // Future passive abilities could be executed here if needed.
        }
    }

    // In PlantNodeExecutor.cs, replace the SpawnBerry method with this debug version:

void SpawnBerry() {
    Debug.Log($"[{plant.gameObject.name}] SpawnBerry called!");
    
    // Check if we have the berry prefab
    if (plant.CellManager == null) {
        Debug.LogError($"[{plant.gameObject.name}] CellManager is null!");
        return;
    }
    
    var cells = plant.CellManager.GetCells();
    if (cells.Count == 0) {
        Debug.LogWarning($"[{plant.gameObject.name}] No cells in plant!");
        return;
    }
    
    int maxBerriesAllowed = plant.GrowthLogic.MaxBerries;
    bool hasLimit = maxBerriesAllowed > 0;
    
    int currentBerryCount = plant.CellManager.GetBerryCount();
    
    if (hasLimit && currentBerryCount >= maxBerriesAllowed) {
        Debug.Log($"[{plant.gameObject.name}] Already has {currentBerryCount} berries (max: {maxBerriesAllowed}). Skipping berry spawn.");
        return;
    }
    
    // Find available positions
    HashSet<Vector2Int> availablePositions = new HashSet<Vector2Int>();
    
    foreach (var kvp in cells) {
        if (kvp.Value == PlantCellType.Stem || kvp.Value == PlantCellType.Leaf) {
            Vector2Int cellPos = kvp.Key;
            Vector2Int[] surroundingPositions = new Vector2Int[] {
                cellPos + Vector2Int.up,
                cellPos + Vector2Int.down,
                cellPos + Vector2Int.left,
                cellPos + Vector2Int.right,
                cellPos + Vector2Int.up + Vector2Int.left,
                cellPos + Vector2Int.up + Vector2Int.right,
                cellPos + Vector2Int.down + Vector2Int.left,
                cellPos + Vector2Int.down + Vector2Int.right
            };
            
            foreach (Vector2Int pos in surroundingPositions) {
                if (!plant.CellManager.HasCellAt(pos)) {
                    availablePositions.Add(pos);
                }
            }
        }
    }
    
    Debug.Log($"[{plant.gameObject.name}] Found {availablePositions.Count} potential berry positions");
    
    // Remove positions at or below ground level
    availablePositions.RemoveWhere(pos => pos.y <= 0);
    
    Debug.Log($"[{plant.gameObject.name}] After filtering ground level: {availablePositions.Count} positions");
    
    List<Vector2Int> candidatePositions = availablePositions.ToList();
    
    if (candidatePositions.Count == 0) {
        Debug.LogWarning($"[{plant.gameObject.name}] No available space to spawn berry!");
        return;
    }
    
    // Get existing berry positions
    List<Vector2Int> existingBerryPositions = plant.CellManager.GetBerryPositions();
    List<Vector2Int> preferredPositions = new List<Vector2Int>();
    List<Vector2Int> otherPositions = new List<Vector2Int>();
    
    if (existingBerryPositions.Count > 0) {
        foreach (var candidate in candidatePositions) {
            if (IsAdjacentToExistingBerries(candidate, existingBerryPositions)) {
                otherPositions.Add(candidate);
            }
            else {
                preferredPositions.Add(candidate);
            }
        }
    }
    else {
        preferredPositions.AddRange(candidatePositions);
    }
    
    List<Vector2Int> finalCandidates = preferredPositions.Count > 0 ? preferredPositions : otherPositions;
    
    if (finalCandidates.Count == 0) {
        Debug.LogWarning($"[{plant.gameObject.name}] No final candidates to spawn berry.");
        return;
    }
    
    int randomIndex = Random.Range(0, finalCandidates.Count);
    Vector2Int chosenPosition = finalCandidates[randomIndex];
    
    Debug.Log($"[{plant.gameObject.name}] Attempting to spawn berry at position {chosenPosition}");
    
    GameObject berry = plant.CellManager.SpawnCellVisual(PlantCellType.Fruit, chosenPosition, null, null);
    
    if (berry != null) {
        string limitInfo = hasLimit ? $"#{currentBerryCount + 1}/{maxBerriesAllowed}" : $"#{currentBerryCount + 1} (no limit)";
        Debug.Log($"[{plant.gameObject.name}] SUCCESS: Spawned berry {limitInfo} at {chosenPosition}");
        AddFoodComponentToBerry(berry);
    }
    else {
        Debug.LogError($"[{plant.gameObject.name}] FAILED to spawn berry at {chosenPosition} - SpawnCellVisual returned null!");
    }
}

    bool IsAdjacentToExistingBerries(Vector2Int position, List<Vector2Int> existingBerries)
    {
        foreach(var berryPos in existingBerries)
        {
            // Chebyshev distance of 1 means they are touching cardinally or diagonally
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

        // Don't add if one already exists
        FoodItem foodItem = berry.GetComponent<FoodItem>();
        if (foodItem != null) return;

        FoodType berryFoodType = GetBerryFoodType();
        if (berryFoodType != null)
        {
            foodItem = berry.AddComponent<FoodItem>();
            foodItem.foodType = berryFoodType;
        }
    }

    FoodType GetBerryFoodType()
    {
        // Try loading a specific "Berry" food type first for consistency
        FoodType berryType = Resources.Load<FoodType>("FoodTypes/Berry");
        if (berryType != null) return berryType;

        // Fallback: Find any food type with the "Plant_Fruit" category
        FoodType[] allFoodTypes = Resources.LoadAll<FoodType>("");
        foreach(FoodType foodType in allFoodTypes)
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
                // Modify existing source
                existingSource.ApplyModifiers(radiusBonus, strengthBonus);
            }
            else
            {
                // Create a new ScentSource component
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

    // Helper to abstract setting the definition, might be useful if you use reflection or custom editors later
    void SetScentSourceDefinition(ScentSource source, ScentDefinition definition)
    {
        source.SetDefinition(definition);
    }
}
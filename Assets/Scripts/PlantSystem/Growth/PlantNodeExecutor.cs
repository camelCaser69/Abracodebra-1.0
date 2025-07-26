// Assets/Scripts/PlantSystem/Growth/PlantNodeExecutor.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        var library = NodeEditorGridController.Instance?.DefinitionLibrary;
        if (library == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] Cannot execute cycle, NodeDefinitionLibrary not found!");
            return;
        }

        var nodes = plant.NodeGraph.nodes;

        // Iterate through all nodes to find Active genes
        for (int i = 0; i < nodes.Count; i++)
        {
            var nodeData = nodes[i];
            if (nodeData == null) continue;

            var nodeDef = library.definitions.FirstOrDefault(d => d.name == nodeData.definitionName);

            // Skip if not an Active gene
            if (nodeDef == null || nodeDef.ActivationType != GeneActivationType.Active)
            {
                continue;
            }

            // This is an Active gene. Check its cost and execute it.
            float cost = CalculateGeneEnergyCost(nodeData);
            if (plant.EnergySystem.HasEnergy(cost))
            {
                plant.EnergySystem.SpendEnergy(cost);

                bool isTrigger = nodeData.effects.Any(e => e != null && NodeEffectTypeHelper.IsTriggerEffect(e.effectType));

                if (isTrigger)
                {
                    // This Active gene is a Trigger. Find and execute the next Payload gene.
                    ExecuteTrigger(i);
                }
                else
                {
                    // This is a simple Active gene. Execute its own effects.
                    ExecuteAllEffectsOfGene(nodeData);
                }
            }
            else
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"[{plant.gameObject.name}] Not enough energy for gene '{nodeDef.displayName}'. Have {plant.EnergySystem.CurrentEnergy:F1}, need {cost:F1}.");
                }
            }
        }
    }

    private void ExecuteTrigger(int triggerGeneIndex)
    {
        var nodes = plant.NodeGraph.nodes;
        var library = NodeEditorGridController.Instance.DefinitionLibrary;

        // Start searching for a payload from the node *after* the trigger
        for (int i = triggerGeneIndex + 1; i < nodes.Count; i++)
        {
            var payloadNodeData = nodes[i];
            if (payloadNodeData == null) continue;

            var payloadNodeDef = library.definitions.FirstOrDefault(d => d.name == payloadNodeData.definitionName);
            
            if (payloadNodeDef != null && payloadNodeDef.ActivationType == GeneActivationType.Payload)
            {
                // Found the payload. Execute its effects and stop searching.
                if (Debug.isDebugBuild)
                {
                    var triggerDef = library.definitions.FirstOrDefault(d => d.name == nodes[triggerGeneIndex].definitionName);
                    Debug.Log($"[{plant.gameObject.name}] Trigger '{triggerDef?.displayName}' is activating Payload '{payloadNodeDef.displayName}'.");
                }
                ExecuteAllEffectsOfGene(payloadNodeData);
                return; // Only execute the first payload found
            }
        }
    }

    private float CalculateGeneEnergyCost(NodeData nodeData)
    {
        if (nodeData?.effects == null) return 0f;

        float totalCost = 0f;
        foreach (var effect in nodeData.effects)
        {
            if (effect.effectType == NodeEffectType.EnergyCost)
            {
                totalCost += effect.primaryValue;
            }
        }
        return totalCost;
    }

    private void ExecuteAllEffectsOfGene(NodeData nodeData)
    {
        if (nodeData?.effects == null) return;
        
        foreach (var effect in nodeData.effects)
        {
            ExecuteEffect(effect);
        }
    }

    private void ExecuteEffect(NodeEffectData effect)
    {
        // This is where the logic for what each effect *does* goes.
        switch (effect.effectType)
        {
            case NodeEffectType.GrowBerry:
                SpawnBerry(effect);
                break;
            
            // Add cases for other Active/Payload effects like Damage, ScentModifier etc.
            case NodeEffectType.Damage:
                // Example: Fire a projectile
                Debug.Log("Executing Damage Effect - NOT IMPLEMENTED");
                break;
                
            case NodeEffectType.ScentModifier:
                Debug.LogWarning($"[{plant.gameObject.name}] ScentModifier effect needs to be reimplemented without scentDefinitionReference");
                break;
        }
    }

    // This method is now public to be called by the executor
    public void SpawnBerry(NodeEffectData growEffectData)
    {
        if (plant == null || (plant.CurrentState != PlantState.Mature_Idle && plant.CurrentState != PlantState.Mature_Executing))
        {
            Debug.LogWarning($"[{plant?.gameObject.name ?? "Unknown"}] Berry cannot be spawned.", plant?.gameObject);
            return;
        }

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
            if(Debug.isDebugBuild) Debug.Log($"[{plant.gameObject.name}] Already has {currentBerryCount} berries (max: {maxBerriesAllowed}). Skipping berry spawn.");
            return;
        }

        // Find available positions around the plant to spawn a berry
        HashSet<Vector2Int> availablePositions = new HashSet<Vector2Int>();
        foreach (var kvp in cells)
        {
            if (kvp.Value == PlantCellType.Stem || kvp.Value == PlantCellType.Leaf)
            {
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
            if(Debug.isDebugBuild) Debug.LogWarning($"[{plant.gameObject.name}] No available space to spawn berry!");
            return;
        }
        
        // --- Find best position ---
        List<Vector2Int> existingBerryPositions = plant.CellManager.GetBerryPositions();
        List<Vector2Int> preferredPositions = new List<Vector2Int>();
        List<Vector2Int> otherPositions = new List<Vector2Int>();
        
        // Prefer spots not adjacent to existing berries to spread them out
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
            if(Debug.isDebugBuild) Debug.LogWarning($"[{plant.gameObject.name}] No final candidates to spawn berry.");
            return;
        }

        int randomIndex = Random.Range(0, finalCandidates.Count);
        Vector2Int chosenPosition = finalCandidates[randomIndex];
        
        // --- Spawn the berry ---
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
        if (foodItem == null)
        {
            FoodType berryFoodType = GetBerryFoodType();
            if (berryFoodType != null)
            {
                foodItem = berry.AddComponent<FoodItem>();
                foodItem.foodType = berryFoodType;
            }
        }
        
        // Add harvestable tag if any node in the plant provides it
        bool isHarvestable = plant.NodeGraph.nodes.Any(node => node.effects.Any(e => e.effectType == NodeEffectType.Harvestable));
        if (isHarvestable)
        {
            if (berry.GetComponent<HarvestableTag>() == null)
            {
                berry.AddComponent<HarvestableTag>();
                Debug.Log($"[PlantNodeExecutor] Added HarvestableTag to berry", berry);
            }
        }
    }

    private FoodType GetBerryFoodType()
    {
        // A more robust way would be a central asset manager, but this works.
        FoodType berryType = Resources.Load<FoodType>("FoodTypes/Berry");
        if (berryType != null) return berryType;

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
                GameObject scentSourceObj = new GameObject($"ScentSource_{scentDef.displayName}");
                scentSourceObj.transform.SetParent(targetObject.transform);
                scentSourceObj.transform.localPosition = Vector3.zero;

                ScentSource newSource = scentSourceObj.AddComponent<ScentSource>();
                newSource.SetDefinition(scentDef);
                newSource.SetRadiusModifier(radiusBonus);
                newSource.SetStrengthModifier(strengthBonus);
            }
        }
    }
}
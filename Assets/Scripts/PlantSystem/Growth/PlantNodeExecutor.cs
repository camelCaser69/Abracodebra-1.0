// Assets/Scripts/PlantSystem/Growth/PlantNodeExecutor.cs
using WegoSystem;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class PlantNodeExecutor
{
    private readonly PlantGrowth plant;

    public PlantNodeExecutor(PlantGrowth plant)
    {
        this.plant = plant;
    }

    /// <summary>
    /// Executes the plant's mature cycle, typically for time-based triggers and simple active effects.
    /// This process does not have an inherent target.
    /// </summary>
    public void ExecuteMatureCycleTick()
    {
        if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0) return;

        var library = NodeEditorGridController.Instance?.DefinitionLibrary;
        if (library == null) return;

        var nodes = plant.NodeGraph.nodes;

        // --- FIX: This loop now correctly finds and executes ALL active genes, not just TimerCasts ---
        for (int i = 0; i < nodes.Count; i++)
        {
            var nodeData = nodes[i];
            if (nodeData == null) continue;

            var nodeDef = library.definitions.FirstOrDefault(d => d.name == nodeData.definitionName);
            if (nodeDef == null || nodeDef.ActivationType != GeneActivationType.Active) continue;

            // This is an Active gene, so execute it. The target is null because it's a time-based cycle.
            ExecuteActiveGene(i, nodeData, nodeDef, null);
        }
    }

    /// <summary>
    /// The main entry point for event-based triggers (e.g., On Eat, On Proximity).
    /// </summary>
    /// <param name="triggerType">The type of trigger to look for (e.g., EatCast).</param>
    /// <param name="target">The entity that caused the trigger (e.g., the animal eating).</param>
    public void TriggerEffectsByType(NodeEffectType triggerType, ITriggerTarget target)
    {
        if (plant.NodeGraph?.nodes == null) return;
        var library = NodeEditorGridController.Instance?.DefinitionLibrary;
        if (library == null) return;

        var nodes = plant.NodeGraph.nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var nodeData = nodes[i];
            if (nodeData == null) continue;

            var nodeDef = library.definitions.FirstOrDefault(d => d.name == nodeData.definitionName);
            if (nodeDef == null || nodeDef.ActivationType != GeneActivationType.Active) continue;

            // If this Active gene has the specified trigger effect, execute it with the provided target.
            if (nodeData.effects.Any(e => e.effectType == triggerType))
            {
                ExecuteActiveGene(i, nodeData, nodeDef, target);
            }
        }
    }

    /// <summary>
    /// Central logic for executing any Active gene. Handles energy cost and determines if it's
    /// a simple action, a trigger for a payload, or both.
    /// </summary>
    private void ExecuteActiveGene(int index, NodeData nodeData, NodeDefinition nodeDef, ITriggerTarget target)
    {
        float cost = CalculateGeneEnergyCost(nodeData);
        if (plant.EnergySystem.HasEnergy(cost))
        {
            plant.EnergySystem.SpendEnergy(cost);

            // An Active gene can be a simple action, a trigger, or both.
            bool isSimpleAction = nodeData.effects.Any(e => e != null && !NodeEffectTypeHelper.IsTriggerEffect(e.effectType));
            bool isTrigger = nodeData.effects.Any(e => e != null && NodeEffectTypeHelper.IsTriggerEffect(e.effectType));

            if (isSimpleAction)
            {
                ExecuteAllEffectsOfGene(nodeData, target);
            }
            if (isTrigger)
            {
                ExecuteTrigger(index, target);
            }
        }
        else if (Debug.isDebugBuild)
        {
            Debug.Log($"[{plant.gameObject.name}] Not enough energy for gene '{nodeDef.displayName}'. Have {plant.EnergySystem.CurrentEnergy:F1}, need {cost:F1}.");
        }
    }

    private void ExecuteTrigger(int triggerGeneIndex, ITriggerTarget target)
    {
        var nodes = plant.NodeGraph.nodes;
        var library = NodeEditorGridController.Instance.DefinitionLibrary;

        for (int i = triggerGeneIndex + 1; i < nodes.Count; i++)
        {
            var payloadNodeData = nodes[i];
            if (payloadNodeData == null) continue;

            var payloadNodeDef = library.definitions.FirstOrDefault(d => d.name == payloadNodeData.definitionName);

            if (payloadNodeDef != null && payloadNodeDef.ActivationType == GeneActivationType.Payload)
            {
                if (Debug.isDebugBuild)
                {
                    var triggerDef = library.definitions.FirstOrDefault(d => d.name == nodes[triggerGeneIndex].definitionName);
                    string targetName = (target is IStatusEffectable statusEffectable) ? statusEffectable.GetDisplayName() : "null";
                    Debug.Log($"[{plant.gameObject.name}] Trigger '{triggerDef?.displayName}' is activating Payload '{payloadNodeDef.displayName}' on target '{targetName}'.");
                }
                ExecuteAllEffectsOfGene(payloadNodeData, target);
                return; // Only execute the first payload found
            }
        }
    }

    private float CalculateGeneEnergyCost(NodeData nodeData)
    {
        return nodeData?.effects?.Where(e => e.effectType == NodeEffectType.EnergyCost).Sum(e => e.primaryValue) ?? 0f;
    }

    private void ExecuteAllEffectsOfGene(NodeData nodeData, ITriggerTarget target)
    {
        if (nodeData?.effects == null) return;
        foreach (var effect in nodeData.effects)
        {
            ExecuteEffect(effect, target);
        }
    }

    private void ExecuteEffect(NodeEffectData effect, ITriggerTarget target)
    {
        switch (effect.effectType)
        {
            case NodeEffectType.Damage:
                // --- PROACTIVE FIX & IMPLEMENTATION ---
                if (target is IStatusEffectable damageableTarget)
                {
                    damageableTarget.TakeDamage(effect.primaryValue);
                    if (Debug.isDebugBuild) Debug.Log($"[{plant.gameObject.name}] Dealt {effect.primaryValue} damage to {damageableTarget.GetDisplayName()}.");
                }
                else if (Debug.isDebugBuild)
                {
                    Debug.LogWarning($"[{plant.gameObject.name}] Damage effect triggered, but no valid damageable target was provided (target was null or not IStatusEffectable).");
                }
                break;

            case NodeEffectType.GrowBerry:
                SpawnBerry(effect);
                break;

            case NodeEffectType.ScentModifier:
                Debug.LogWarning($"[{plant.gameObject.name}] ScentModifier effect needs to be reimplemented.");
                break;
        }
    }

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
            if (Debug.isDebugBuild) Debug.Log($"[{plant.gameObject.name}] Already has {currentBerryCount} berries (max: {maxBerriesAllowed}). Skipping berry spawn.");
            return;
        }

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
            if (Debug.isDebugBuild) Debug.LogWarning($"[{plant.gameObject.name}] No available space to spawn berry!");
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
            if (Debug.isDebugBuild) Debug.LogWarning($"[{plant.gameObject.name}] No final candidates to spawn berry.");
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
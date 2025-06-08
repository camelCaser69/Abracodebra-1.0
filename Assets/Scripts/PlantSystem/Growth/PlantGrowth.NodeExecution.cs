using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class PlantGrowth : MonoBehaviour {

    void ExecuteMatureCycleTick() {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) {
            Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!");
            return;
        }

        float damageMultiplier = 1.0f;
        Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        // First pass: Calculate costs and modifiers
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null || node.effects.Count == 0) continue;

            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive) continue;

                switch (effect.effectType) {
                    case NodeEffectType.EnergyCost:
                        totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                        break;
                    case NodeEffectType.Damage:
                        damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue);
                        break;
                    case NodeEffectType.ScentModifier:
                        if (effect.scentDefinitionReference != null) {
                            ScentDefinition key = effect.scentDefinitionReference;
                            accumulatedScentRadiusBonus.TryGetValue(key, out float currentRad);
                            accumulatedScentRadiusBonus[key] = currentRad + effect.primaryValue;

                            accumulatedScentStrengthBonus.TryGetValue(key, out float currentStr);
                            accumulatedScentStrengthBonus[key] = currentStr + effect.secondaryValue;
                        }
                        break;
                }
            }
        }

        // Check for poop if detection radius is set
        if (poopDetectionRadius > 0f) {
            CheckForPoopAndAbsorb();
        }

        // Check energy requirements
        if (currentEnergy < totalEnergyCostForCycle) {
            if (Debug.isDebugBuild) 
                Debug.Log($"[{gameObject.name}] Not enough energy ({currentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
            return;
        }

        // Consume energy
        currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle);

        // Second pass: Execute actions
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null || node.effects.Count == 0) continue;

            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive ||
                    effect.effectType == NodeEffectType.EnergyCost ||
                    effect.effectType == NodeEffectType.Damage ||
                    effect.effectType == NodeEffectType.ScentModifier) continue;

                switch (effect.effectType) {
                    case NodeEffectType.Output:
                        OutputNodeEffect outputComp = GetComponentInChildren<OutputNodeEffect>();
                        if (outputComp != null) {
                            outputComp.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        }
                        break;
                    case NodeEffectType.GrowBerry:
                        TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                }
            }
        }
    }

    IEnumerator ExecuteMatureCycle() {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) {
            Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!", gameObject);
            currentState = PlantState.Mature_Idle;
            cycleTimer = cycleCooldown;
            yield break;
        }

        float damageMultiplier = 1.0f;
        Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        // First pass: Calculate costs and modifiers
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null || node.effects.Count == 0) continue;

            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive) continue;

                switch (effect.effectType) {
                    case NodeEffectType.EnergyCost:
                        totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue);
                        break;
                    case NodeEffectType.Damage:
                        damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue);
                        break;
                    case NodeEffectType.ScentModifier:
                        if (effect.scentDefinitionReference != null) {
                            ScentDefinition key = effect.scentDefinitionReference;
                            accumulatedScentRadiusBonus.TryGetValue(key, out float currentRad);
                            accumulatedScentRadiusBonus[key] = currentRad + effect.primaryValue;

                            accumulatedScentStrengthBonus.TryGetValue(key, out float currentStr);
                            accumulatedScentStrengthBonus[key] = currentStr + effect.secondaryValue;
                        }
                        else {
                            Debug.LogWarning($"Node '{node.nodeDisplayName ?? "Unnamed"}' has ScentModifier effect but ScentDefinition reference is NULL.");
                        }
                        break;
                }
            }
        }

        // Check for poop if detection radius is set
        if (poopDetectionRadius > 0f) {
            CheckForPoopAndAbsorb();
        }

        // Check energy requirements
        if (currentEnergy < totalEnergyCostForCycle) {
            if(Debug.isDebugBuild) 
                Debug.Log($"[{gameObject.name}] Not enough energy ({currentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
            currentState = PlantState.Mature_Idle;
            cycleTimer = cycleCooldown;
            yield break;
        }

        // Consume energy
        currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle);
        UpdateUI();

        // Second pass: Execute actions with delays
        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null || node.effects.Count == 0) continue;

            bool hasActionEffectInNode = node.effects.Any(eff => eff != null && !eff.isPassive && 
                eff.effectType != NodeEffectType.EnergyCost && 
                eff.effectType != NodeEffectType.Damage && 
                eff.effectType != NodeEffectType.ScentModifier);

            if (hasActionEffectInNode && nodeCastDelay > 0.01f) {
                yield return new WaitForSeconds(nodeCastDelay);
            }

            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive ||
                    effect.effectType == NodeEffectType.EnergyCost ||
                    effect.effectType == NodeEffectType.Damage ||
                    effect.effectType == NodeEffectType.ScentModifier) continue;

                switch (effect.effectType) {
                    case NodeEffectType.Output:
                        OutputNodeEffect outputComp = GetComponentInChildren<OutputNodeEffect>();
                        if (outputComp != null) {
                            outputComp.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        } else {
                            Debug.LogWarning($"[{gameObject.name}] Node requested Output effect, but no OutputNodeEffect component found.", this);
                        }
                        break;
                    case NodeEffectType.GrowBerry:
                        TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                }
            }
        }

        cycleTimer = cycleCooldown;
        currentState = PlantState.Mature_Idle;
    }

    void TrySpawnBerry(Dictionary<ScentDefinition, float> scentRadiiBonus, Dictionary<ScentDefinition, float> scentStrengthsBonus) {
        if (berryCellPrefab == null) {
            Debug.LogWarning($"[{gameObject.name}] Berry Prefab not assigned. Cannot spawn berry.", gameObject);
            return;
        }

        var potentialCoords = cells
            .Where(cellKvp => cellKvp.Value == PlantCellType.Stem || cellKvp.Value == PlantCellType.Seed)
            .SelectMany(cellKvp => {
                Vector2Int coord = cellKvp.Key;
                Vector2Int[] berryOffsets = { Vector2Int.up, Vector2Int.left, Vector2Int.right };
                List<Vector2Int> candidates = new List<Vector2Int>();
                foreach(var offset in berryOffsets) {
                    candidates.Add(coord + offset);
                }
                return candidates;
            })
            .Where(coord => !cells.ContainsKey(coord))
            .Distinct()
            .ToList();

        if (potentialCoords.Count > 0) {
            Vector2Int chosenCoord = potentialCoords[Random.Range(0, potentialCoords.Count)];
            GameObject berryGO = SpawnCellVisual(PlantCellType.Fruit, chosenCoord, scentRadiiBonus, scentStrengthsBonus);
            if (berryGO == null) {
                Debug.LogError($"[{gameObject.name}] Failed to spawn berry visual at {chosenCoord}, SpawnCellVisual returned null.");
            }
        } else {
            if(Debug.isDebugBuild) 
                Debug.Log($"[{gameObject.name}] No valid empty adjacent locations found to spawn a berry.");
        }
    }

    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses) {
        if (targetObject == null) {
            Debug.LogError("ApplyScentDataToObject: targetObject is null.");
            return;
        }

        if (EcosystemManager.Instance == null) {
            Debug.LogError("ApplyScentDataToObject: EcosystemManager instance not found.");
            return;
        }

        if (EcosystemManager.Instance.scentLibrary == null) {
            Debug.LogWarning("ApplyScentDataToObject: Scent Library not assigned in EcosystemManager.");
            return;
        }

        ScentDefinition strongestScentDef = null;
        float maxStrengthBonus = -1f;

        if (scentStrengthBonuses != null && scentStrengthBonuses.Count > 0) {
            foreach (var kvp in scentStrengthBonuses) {
                if (kvp.Key != null && kvp.Value > maxStrengthBonus) {
                    maxStrengthBonus = kvp.Value;
                    strongestScentDef = kvp.Key;
                }
            }
        }

        if (strongestScentDef != null) {
            ScentSource scentSource = targetObject.GetComponent<ScentSource>();
            if (scentSource == null) {
                scentSource = targetObject.AddComponent<ScentSource>();
            }

            scentSource.definition = strongestScentDef;

            scentRadiusBonuses.TryGetValue(strongestScentDef, out float radiusBonus);
            scentSource.radiusModifier = radiusBonus;
            scentSource.strengthModifier = maxStrengthBonus;

            if (strongestScentDef.particleEffectPrefab != null) {
                bool particleExists = false;
                foreach(Transform child in targetObject.transform){
                    if(child.TryGetComponent<ParticleSystem>(out _)){
                        particleExists = true;
                        break;
                    }
                }
                if (!particleExists) {
                    Instantiate(strongestScentDef.particleEffectPrefab, targetObject.transform.position, Quaternion.identity, targetObject.transform);
                }
            }
        }
    }

    void CheckForPoopAndAbsorb() {
        bool hasMissingLeaves = leafDataList.Any(leaf => !leaf.IsActive);
        bool canAddEnergy = poopEnergyBonus > 0f;

        if (Debug.isDebugBuild && poopDetectionRadius > 0f) {
            string leafStatus = hasMissingLeaves ?
                $"Has {leafDataList.Count(l => !l.IsActive)} missing leaves" :
                "No missing leaves";
            Debug.Log($"[{gameObject.name}] PoopFertilizer: {leafStatus}, Radius: {poopDetectionRadius}, Energy bonus: {poopEnergyBonus}");
        }

        if (!hasMissingLeaves && !canAddEnergy) return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, poopDetectionRadius);

        if (Debug.isDebugBuild && poopDetectionRadius > 0f) {
            Debug.Log($"[{gameObject.name}] PoopFertilizer: Found {colliders.Length} colliders in radius {poopDetectionRadius}");
            int poopCount = 0;
            foreach (Collider2D col in colliders) {
                if (col.GetComponent<PoopController>() != null)
                    poopCount++;
            }
            Debug.Log($"[{gameObject.name}] PoopFertilizer: {poopCount} of those colliders have PoopController");
        }

        foreach (Collider2D collider in colliders) {
            PoopController poop = collider.GetComponent<PoopController>();
            if (poop != null) {
                bool absorbed = false;

                if (hasMissingLeaves) {
                    absorbed = TryRegrowLeaf();
                }

                if ((!absorbed || !hasMissingLeaves) && canAddEnergy) {
                    currentEnergy = Mathf.Min(finalMaxEnergy, currentEnergy + poopEnergyBonus);
                    absorbed = true;

                    if (Debug.isDebugBuild)
                        Debug.Log($"[{gameObject.name}] Added {poopEnergyBonus} energy from poop fertilizer. Current energy: {currentEnergy}");
                }

                if (absorbed) {
                    Destroy(poop.gameObject);
                    break;
                }
            }
        }
    }

    bool TryRegrowLeaf() {
        int missingLeafIndex = -1;

        if (Debug.isDebugBuild) {
            int totalLeaves = leafDataList.Count;
            int missingLeaves = leafDataList.Count(leaf => !leaf.IsActive);
            Debug.Log($"[{gameObject.name}] TryRegrowLeaf: Total leaves: {totalLeaves}, Missing leaves: {missingLeaves}");
        }

        for (int i = 0; i < leafDataList.Count; i++) {
            if (!leafDataList[i].IsActive) {
                missingLeafIndex = i;
                break;
            }
        }

        if (missingLeafIndex == -1) {
            if (Debug.isDebugBuild)
                Debug.Log($"[{gameObject.name}] TryRegrowLeaf: No missing leaves found to regrow.");
            return false;
        }

        Vector2Int leafCoord = leafDataList[missingLeafIndex].GridCoord;

        if (cells.ContainsKey(leafCoord)) {
            if (Debug.isDebugBuild)
                Debug.Log($"[{gameObject.name}] TryRegrowLeaf: Cannot regrow leaf at {leafCoord} because cell is already occupied.");
            return false;
        }

        GameObject newLeaf = SpawnCellVisual(PlantCellType.Leaf, leafCoord);

        if (newLeaf != null) {
            leafDataList[missingLeafIndex] = new LeafData(leafCoord, true);

            if (Debug.isDebugBuild)
                Debug.Log($"[{gameObject.name}] TryRegrowLeaf: Successfully regrew leaf at {leafCoord} using poop fertilizer!");
            return true;
        }

        if (Debug.isDebugBuild)
            Debug.Log($"[{gameObject.name}] TryRegrowLeaf: Failed to spawn new leaf at {leafCoord}");

        return false;
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PlantGrowth : MonoBehaviour
{
    private void ExecuteMatureCycleTick() {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) {
            Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!");
            return;
        }

        float damageMultiplier = 1.0f;
        var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive) continue;
                switch (effect.effectType) {
                    case NodeEffectType.EnergyCost: totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue); break;
                    case NodeEffectType.Damage: damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue); break;
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

        if (poopDetectionRadius > 0f) CheckForPoopAndAbsorb();

        if (currentEnergy < totalEnergyCostForCycle) {
            if (Debug.isDebugBuild) Debug.Log($"[{gameObject.name}] Not enough energy ({currentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
            return;
        }

        currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle);

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive || effect.effectType == NodeEffectType.EnergyCost || effect.effectType == NodeEffectType.Damage || effect.effectType == NodeEffectType.ScentModifier) continue;
                switch (effect.effectType) {
                    case NodeEffectType.Output:
                        GetComponentInChildren<OutputNodeEffect>()?.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                    case NodeEffectType.GrowBerry:
                        TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                }
            }
        }
    }
    
    private IEnumerator ExecuteMatureCycle() {
        if (nodeGraph?.nodes == null || nodeGraph.nodes.Count == 0) {
            Debug.LogError($"[{gameObject.name}] NodeGraph missing or empty!", gameObject);
            float rtCooldown = maturityCycleTicks * (WegoSystem.TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);
            cycleTimer = rtCooldown;
            currentState = PlantState.Mature_Idle;
            yield break;
        }

        float damageMultiplier = 1.0f;
        var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive) continue;
                switch (effect.effectType) {
                    case NodeEffectType.EnergyCost: totalEnergyCostForCycle += Mathf.Max(0f, effect.primaryValue); break;
                    case NodeEffectType.Damage: damageMultiplier = Mathf.Max(0.1f, damageMultiplier + effect.primaryValue); break;
                    case NodeEffectType.ScentModifier:
                        if (effect.scentDefinitionReference != null) {
                            ScentDefinition key = effect.scentDefinitionReference;
                            accumulatedScentRadiusBonus.TryGetValue(key, out float currentRad);
                            accumulatedScentRadiusBonus[key] = currentRad + effect.primaryValue;
                            accumulatedScentStrengthBonus.TryGetValue(key, out float currentStr);
                            accumulatedScentStrengthBonus[key] = currentStr + effect.secondaryValue;
                        } else { Debug.LogWarning($"Node '{node.nodeDisplayName ?? "Unnamed"}' has ScentModifier effect but ScentDefinition reference is NULL."); }
                        break;
                }
            }
        }

        if (poopDetectionRadius > 0f) CheckForPoopAndAbsorb();

        if (currentEnergy < totalEnergyCostForCycle) {
            if(Debug.isDebugBuild) Debug.Log($"[{gameObject.name}] Not enough energy ({currentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
            float rtCooldown = maturityCycleTicks * (WegoSystem.TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);
            cycleTimer = rtCooldown;
            currentState = PlantState.Mature_Idle;
            yield break;
        }

        currentEnergy = Mathf.Max(0f, currentEnergy - totalEnergyCostForCycle);
        UpdateUI();

        foreach (var node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            bool hasActionEffectInNode = node.effects.Any(eff => eff != null && !eff.isPassive && eff.effectType != NodeEffectType.EnergyCost && eff.effectType != NodeEffectType.Damage && eff.effectType != NodeEffectType.ScentModifier);
            if (hasActionEffectInNode && nodeCastDelay > 0.01f) {
                yield return new WaitForSeconds(nodeCastDelay);
            }
            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive || effect.effectType == NodeEffectType.EnergyCost || effect.effectType == NodeEffectType.Damage || effect.effectType == NodeEffectType.ScentModifier) continue;
                switch (effect.effectType) {
                    case NodeEffectType.Output:
                        GetComponentInChildren<OutputNodeEffect>()?.Activate(damageMultiplier, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                    case NodeEffectType.GrowBerry:
                        TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                }
            }
        }
        
        float finalRtCooldown = maturityCycleTicks * (WegoSystem.TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);
        cycleTimer = finalRtCooldown;
        currentState = PlantState.Mature_Idle;
    }

    private void TrySpawnBerry(Dictionary<ScentDefinition, float> scentRadiiBonus, Dictionary<ScentDefinition, float> scentStrengthsBonus) {
        if (berryCellPrefab == null) {
            Debug.LogWarning($"[{gameObject.name}] Berry Prefab not assigned. Cannot spawn berry.", gameObject);
            return;
        }

        var potentialCoords = cells
            .Where(cellKvp => cellKvp.Value == PlantCellType.Stem || cellKvp.Value == PlantCellType.Seed)
            .SelectMany(cellKvp => {
                Vector2Int[] berryOffsets = { Vector2Int.up, Vector2Int.left, Vector2Int.right };
                return berryOffsets.Select(offset => cellKvp.Key + offset);
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
            if(Debug.isDebugBuild) Debug.Log($"[{gameObject.name}] No valid empty adjacent locations found to spawn a berry.");
        }
    }

    public void ApplyScentDataToObject(GameObject targetObject, Dictionary<ScentDefinition, float> scentRadiusBonuses, Dictionary<ScentDefinition, float> scentStrengthBonuses) {
        if (targetObject == null || EcosystemManager.Instance?.scentLibrary == null) return;
        
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
            ScentSource scentSource = targetObject.GetComponent<ScentSource>() ?? targetObject.AddComponent<ScentSource>();
            scentSource.definition = strongestScentDef;
            scentRadiusBonuses.TryGetValue(strongestScentDef, out float radiusBonus);
            scentSource.radiusModifier = radiusBonus;
            scentSource.strengthModifier = maxStrengthBonus;

            if (strongestScentDef.particleEffectPrefab != null) {
                if (!targetObject.transform.GetComponentInChildren<ParticleSystem>()) {
                    Instantiate(strongestScentDef.particleEffectPrefab, targetObject.transform);
                }
            }
        }
    }

    private void CheckForPoopAndAbsorb() {
        bool hasMissingLeaves = leafDataList.Any(leaf => !leaf.IsActive);
        bool canAddEnergy = poopEnergyBonus > 0f;
        if (!hasMissingLeaves && !canAddEnergy) return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, poopDetectionRadius);
        foreach (Collider2D collider in colliders) {
            PoopController poop = collider.GetComponent<PoopController>();
            if (poop != null) {
                bool absorbed = false;
                if (hasMissingLeaves) absorbed = TryRegrowLeaf();
                if ((!absorbed || !hasMissingLeaves) && canAddEnergy) {
                    currentEnergy = Mathf.Min(finalMaxEnergy, currentEnergy + poopEnergyBonus);
                    absorbed = true;
                }
                if (absorbed) {
                    Destroy(poop.gameObject);
                    break;
                }
            }
        }
    }

    private bool TryRegrowLeaf() {
        int missingLeafIndex = -1;
        for (int i = 0; i < leafDataList.Count; i++) {
            if (!leafDataList[i].IsActive) {
                missingLeafIndex = i;
                break;
            }
        }
        if (missingLeafIndex == -1) return false;

        Vector2Int leafCoord = leafDataList[missingLeafIndex].GridCoord;
        if (cells.ContainsKey(leafCoord)) return false;

        GameObject newLeaf = SpawnCellVisual(PlantCellType.Leaf, leafCoord);
        if (newLeaf != null) {
            leafDataList[missingLeafIndex] = new LeafData(leafCoord, true);
            return true;
        }
        return false;
    }
}
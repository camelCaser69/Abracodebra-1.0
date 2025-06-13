using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantNodeExecutor {
    readonly PlantGrowth plant;

    public float PoopDetectionRadius { get; private set; }
    public float PoopEnergyBonus { get; private set; }

    public PlantNodeExecutor(PlantGrowth plant) {
        this.plant = plant;
    }

    public void ProcessPassiveEffects(NodeGraph nodeGraph) {
        PoopDetectionRadius = 0f;
        PoopEnergyBonus = 0f;

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects) {
                if (effect == null || !effect.isPassive) continue;

                switch (effect.effectType) {
                    case NodeEffectType.PoopAbsorption:
                        PoopDetectionRadius = Mathf.Max(0f, effect.primaryValue);
                        PoopEnergyBonus = Mathf.Max(0f, effect.secondaryValue);
                        break;
                }
            }
        }
    }

    public void ExecuteMatureCycleTick() {
        if (plant.NodeGraph?.nodes == null || plant.NodeGraph.nodes.Count == 0) {
            Debug.LogError($"[{plant.gameObject.name}] NodeGraph missing or empty!");
            return;
        }

        float damageMultiplier = 1.0f;
        var accumulatedScentRadiusBonus = new Dictionary<ScentDefinition, float>();
        var accumulatedScentStrengthBonus = new Dictionary<ScentDefinition, float>();
        float totalEnergyCostForCycle = 0f;

        foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;

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

        if (PoopDetectionRadius > 0f) {
            CheckForPoopAndAbsorb();
        }

        if (plant.EnergySystem.CurrentEnergy < totalEnergyCostForCycle) {
            if (Debug.isDebugBuild) {
                Debug.Log($"[{plant.gameObject.name}] Not enough energy ({plant.EnergySystem.CurrentEnergy}/{totalEnergyCostForCycle}) for mature cycle.");
            }
            return;
        }

        plant.EnergySystem.CurrentEnergy = Mathf.Max(0f, plant.EnergySystem.CurrentEnergy - totalEnergyCostForCycle);

        foreach (var node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects) {
                if (effect == null || effect.isPassive ||
                    effect.effectType == NodeEffectType.EnergyCost ||
                    effect.effectType == NodeEffectType.Damage ||
                    effect.effectType == NodeEffectType.ScentModifier) continue;

                switch (effect.effectType) {
                    case NodeEffectType.GrowBerry:
                        TrySpawnBerry(accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
                        break;
                }
            }
        }
    }

    void CheckForPoopAndAbsorb() {
        bool hasMissingLeaves = plant.CellManager.LeafDataList.Any(leaf => !leaf.IsActive);
        bool canAddEnergy = PoopEnergyBonus > 0f;
        if (!hasMissingLeaves && !canAddEnergy) return;

        int discreteRadius = Mathf.RoundToInt(PoopDetectionRadius);
        if (discreteRadius <= 0) return;

        GridEntity plantGrid = plant.GetComponent<GridEntity>();
        if (plantGrid == null) return;

        var tilesInRange = GridRadiusUtility.GetTilesInCircle(plantGrid.Position, discreteRadius);

        foreach (var tilePos in tilesInRange) {
            Vector3 worldPos = GridPositionManager.Instance.GridToWorld(tilePos);
            Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos);

            foreach (Collider2D collider in colliders) {
                PoopController poop = collider.GetComponent<PoopController>();
                if (poop != null) {
                    bool absorbed = false;

                    if (hasMissingLeaves) {
                        absorbed = plant.CellManager.TryRegrowLeaf();
                    }

                    if ((!absorbed || !hasMissingLeaves) && canAddEnergy) {
                        plant.EnergySystem.CurrentEnergy = Mathf.Min(
                            plant.EnergySystem.MaxEnergy,
                            plant.EnergySystem.CurrentEnergy + PoopEnergyBonus
                        );
                        absorbed = true;
                    }

                    if (absorbed) {
                        Object.Destroy(poop.gameObject);
                        return;
                    }
                }
            }
        }
    }

    void TrySpawnBerry(Dictionary<ScentDefinition, float> scentRadiiBonus, Dictionary<ScentDefinition, float> scentStrengthsBonus) {
        var berryCellPrefab = plant.CellManager.GetType()
            .GetField("berryCellPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(plant.CellManager) as GameObject;

        if (berryCellPrefab == null) {
            Debug.LogWarning($"[{plant.gameObject.name}] Berry Prefab not assigned. Cannot spawn berry.", plant.gameObject);
            return;
        }

        var cells = plant.CellManager.GetCells();
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
            GameObject berryGO = plant.CellManager.SpawnCellVisual(
                PlantCellType.Fruit,
                chosenCoord,
                scentRadiiBonus,
                scentStrengthsBonus
            );

            if (berryGO == null) {
                Debug.LogError($"[{plant.gameObject.name}] Failed to spawn berry visual at {chosenCoord}");
            }
        } else {
            if (Debug.isDebugBuild) {
                Debug.Log($"[{plant.gameObject.name}] No valid empty adjacent locations found to spawn a berry.");
            }
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
                    Object.Instantiate(strongestScentDef.particleEffectPrefab, targetObject.transform);
                }
            }
        }
    }
}
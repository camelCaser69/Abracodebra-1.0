using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using WegoSystem;

public partial class PlantGrowth : MonoBehaviour {

    public void ReportCellDestroyed(Vector2Int coord) {
        if (cells.ContainsKey(coord)) {
            PlantCellType cellType = cells[coord];

            if (cellType == PlantCellType.Leaf) {
                for (int i = 0; i < leafDataList.Count; i++) {
                    if (leafDataList[i].GridCoord == coord) {
                        LeafData updatedData = leafDataList[i];
                        updatedData.IsActive = false; // Mark as eaten/missing
                        leafDataList[i] = updatedData;

                        if (Debug.isDebugBuild)
                            Debug.Log($"[{gameObject.name}] Leaf at {coord} marked as missing for potential regrowth via ReportCellDestroyed.");

                        break;
                    }
                }
            }

            cells.Remove(coord);
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));

            if (enableOutline && outlineController != null) {
                outlineController.OnPlantCellRemoved(coord);
            }
        }
    }

    public void RemovePlantCell(GameObject cellToRemove) {
        if (cellToRemove == null) return;

        PlantCell cellComp = cellToRemove.GetComponent<PlantCell>();
        if (cellComp == null) { Destroy(cellToRemove); return; }

        Vector2Int coord = cellComp.GridCoord;

        if (cellComp.CellType == PlantCellType.Leaf) {
            for (int i = 0; i < leafDataList.Count; i++) {
                if (leafDataList[i].GridCoord == coord) {
                    LeafData updatedData = leafDataList[i];
                    updatedData.IsActive = false; // Mark as eaten/missing
                    leafDataList[i] = updatedData;

                    if (Debug.isDebugBuild)
                        Debug.Log($"[{gameObject.name}] Leaf at {coord} marked as missing for potential regrowth.");

                    break;
                }
            }
        }

        SpriteRenderer partRenderer = cellToRemove.GetComponentInChildren<SpriteRenderer>();
        if (shadowController != null && partRenderer != null) {
            shadowController.UnregisterPlantPart(partRenderer);
        }

        if (cells.ContainsKey(coord)) {
            cells.Remove(coord);
        }
        activeCellGameObjects.Remove(cellToRemove);

        Destroy(cellToRemove);
    }

    void ClearAllVisuals() {
        List<GameObject> cellsToClear = new List<GameObject>(activeCellGameObjects);
        foreach (GameObject cellGO in cellsToClear) {
            if (cellGO != null) {
                Destroy(cellGO);
            }
        }
        activeCellGameObjects.Clear();
        cells.Clear();

        rootCellInstance = null;
    }

    GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords, Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null, Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null) {
        if (cells.ContainsKey(coords)) {
            Debug.LogWarning($"[{gameObject.name}] Trying to spawn {cellType} at occupied coord {coords}.");
            return null;
        }

        GameObject prefab = null;
        switch (cellType) {
            case PlantCellType.Seed: prefab = seedCellPrefab; break;
            case PlantCellType.Stem: prefab = stemCellPrefab; break;
            case PlantCellType.Leaf: prefab = leafCellPrefab; break;
            case PlantCellType.Fruit: prefab = berryCellPrefab; break;
        }

        if (prefab == null) {
            Debug.LogError($"[{gameObject.name}] Prefab for PlantCellType.{cellType} is null!");
            return null;
        }

        Vector2 worldPos = (Vector2)transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        instance.name = $"{gameObject.name}_{cellType}_{coords.x}_{coords.y}";

        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = this;
        cellComp.GridCoord = coords;
        cellComp.CellType = cellType;

        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);

        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
        if (cellType == PlantCellType.Seed) {
            sorter.SetUseParentYCoordinate(false);
        } else {
            sorter.SetUseParentYCoordinate(true);
        }

        if (cellType == PlantCellType.Fruit) {
            ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }

        RegisterShadowForCell(instance, cellType.ToString());
        if (enableOutline && outlineController != null) {
            outlineController.OnPlantCellAdded(coords, instance);
        }

        return instance;
    }

    void RegisterShadowForCell(GameObject cellInstance, string cellTypeName) {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return;

        SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>();
        if (partRenderer != null) {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        } else {
            Debug.LogWarning($"Plant '{gameObject.name}': {cellTypeName} missing SpriteRenderer. No shadow.", cellInstance);
        }
    }

    void CalculateAndApplyStats() {
        if (nodeGraph == null) {
            Debug.LogError($"[{gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
            return;
        }

        float baseEnergyStorage = 10f;
        float basePhotosynthesisRate = 0.5f;
        int baseStemMin = 3;
        int baseStemMax = 5;
        float baseGrowthSpeedInterval = 0.5f;
        int baseLeafGap = 1;
        int baseLeafPattern = 0;
        float baseGrowthRandomness = 0.1f;
        float baseCooldown = 5f;
        float baseCastDelay = 0.1f;

        float accumulatedEnergyStorage = 0f;
        float accumulatedPhotosynthesis = 0f;
        int stemLengthModifier = 0;
        float growthSpeedTimeModifier = 0f;
        int leafGapModifier = 0;
        int currentLeafPattern = baseLeafPattern;
        float growthRandomnessModifier = 0f;
        float cooldownModifier = 0f;
        float castDelayModifier = 0f;
        bool seedFound = false;

        poopDetectionRadius = 0f;
        poopEnergyBonus = 0f;

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            foreach (var effect in node.effects) {
                if (effect == null || !effect.isPassive) continue;
                switch (effect.effectType) {
                    case NodeEffectType.SeedSpawn:
                        seedFound = true;
                        break;
                    case NodeEffectType.EnergyStorage:
                        accumulatedEnergyStorage += effect.primaryValue;
                        break;
                    case NodeEffectType.EnergyPhotosynthesis:
                        accumulatedPhotosynthesis += effect.primaryValue;
                        break;
                    case NodeEffectType.StemLength:
                        stemLengthModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.GrowthSpeed:
                        growthSpeedTimeModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.LeafGap:
                        leafGapModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.LeafPattern:
                        currentLeafPattern = Mathf.Clamp(Mathf.RoundToInt(effect.primaryValue), 0, 4);
                        break;
                    case NodeEffectType.StemRandomness:
                        growthRandomnessModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.Cooldown:
                        cooldownModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.CastDelay:
                        castDelayModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.PoopFertilizer:
                        poopDetectionRadius = Mathf.Max(0f, effect.primaryValue);
                        poopEnergyBonus = Mathf.Max(0f, effect.secondaryValue);
                        break;
                }
            }
        }

        finalMaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        finalPhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis);
        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier);
        int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier);
        finalGrowthSpeed = Mathf.Max(0.01f, baseGrowthSpeedInterval + growthSpeedTimeModifier);
        finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        finalLeafPattern = currentLeafPattern;
        finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        cycleCooldown = Mathf.Max(0.1f, baseCooldown + cooldownModifier);
        nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier);

        targetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;

        if (useWegoSystem && TickManager.Instance?.Config != null) {
            growthTicksPerStage = TickManager.Instance.Config.plantGrowthTicksPerStage;
            maturityCycleTicks = Mathf.RoundToInt(cycleCooldown * (TickManager.Instance.Config.ticksPerRealSecond));
        }

        if (!seedFound) {
            Debug.LogWarning($"[{gameObject.name}] NodeGraph lacks SeedSpawn effect. Growth aborted.", gameObject);
        }
    }
}
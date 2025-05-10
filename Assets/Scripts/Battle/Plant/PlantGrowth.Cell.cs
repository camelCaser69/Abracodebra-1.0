// FILE: Assets/Scripts/Battle/Plant/PlantGrowth.Cell.cs
using System.Collections.Generic;
using System.Linq; // Added this namespace for OrderBy()
using UnityEngine;

public partial class PlantGrowth : MonoBehaviour
{
    // ------------------------------------------------
    // --- CELL MANAGEMENT METHODS ---
    // ------------------------------------------------

    public void ReportCellDestroyed(Vector2Int coord)
    {
        if (cells.ContainsKey(coord)) {
            cells.Remove(coord);
            // Assume RemovePlantCell(GameObject) will be called externally for proper cleanup
            // We still need to clean the GO list if destruction happened unexpectedly
            activeCellGameObjects.RemoveAll(go => go == null || (go.GetComponent<PlantCell>()?.GridCoord == coord));

             // Trigger outline update after internal state change
             if (enableOutline && outlineController != null) {
                 outlineController.OnPlantCellRemoved(coord);
             }
        }
    }

    // RemovePlantCell (Handles shadow AND outline unregistration)
    public void RemovePlantCell(GameObject cellToRemove)
    {
        if (cellToRemove == null) return;

        PlantCell cellComp = cellToRemove.GetComponent<PlantCell>();
        if (cellComp == null) { Destroy(cellToRemove); return; } // Destroy if no component

        Vector2Int coord = cellComp.GridCoord;

        // --- Unregister Visuals FIRST ---
        // Shadow
        SpriteRenderer partRenderer = cellToRemove.GetComponentInChildren<SpriteRenderer>(); // More robust check
        if (shadowController != null && partRenderer != null) {
            shadowController.UnregisterPlantPart(partRenderer);
        }

        // Outline (this now triggers the update based on internal state)
        // No direct call to outlineController needed here, ReportCellDestroyed handles it.

        // --- Remove from internal tracking ---
        if (cells.ContainsKey(coord)) {
            cells.Remove(coord);
        }
        activeCellGameObjects.Remove(cellToRemove);

        // --- Destroy the GameObject ---
        Destroy(cellToRemove);

        // --- Trigger Outline Update AFTER internal state reflects the removal ---
        // Moved notification to ReportCellDestroyed which should be called by PlantCell OnDestroy
        // If called directly, ensure ReportCellDestroyed is also called or call the outline update here:
        // if (enableOutline && outlineController != null) {
        //     outlineController.OnPlantCellRemoved(coord);
        // }
    }


    // ClearAllVisuals (Combined cleanup helper for Shadows and Outlines)
    private void ClearAllVisuals()
    {
        // Create a copy because RemovePlantCell modifies the list
        List<GameObject> cellsToClear = new List<GameObject>(activeCellGameObjects);
        foreach (GameObject cellGO in cellsToClear) {
            if (cellGO != null) {
                // Directly destroy, OnDestroy in PlantCell calls ReportCellDestroyed -> Outline Update
                Destroy(cellGO);
            }
        }
        // Ensure lists/dicts are clear after iteration
        activeCellGameObjects.Clear();
        cells.Clear();

        // Also clear any outlines that might be orphaned
        if (outlineController != null) {
             // This assumes OutlineController has a method to clear all its parts
             // outlineController.ClearAllOutlineParts();
             // Or simply destroy/recreate the outline controller if simpler
        }

        rootCellInstance = null;
    }


    // SpawnCellVisual - Creates a cell visual GameObject
    private GameObject SpawnCellVisual(PlantCellType cellType, Vector2Int coords,
                                 Dictionary<ScentDefinition, float> accumulatedScentRadiusBonus = null,
                                 Dictionary<ScentDefinition, float> accumulatedScentStrengthBonus = null)
    {
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

        // Create visual cell
        Vector2 worldPos = (Vector2)transform.position + ((Vector2)coords * cellSpacing);
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        instance.name = $"{gameObject.name}_{cellType}_{coords.x}_{coords.y}";

        // Set up PlantCell component
        PlantCell cellComp = instance.GetComponent<PlantCell>() ?? instance.AddComponent<PlantCell>();
        cellComp.ParentPlantGrowth = this;
        cellComp.GridCoord = coords;
        cellComp.CellType = cellType;

        // Add to tracking
        cells[coords] = cellType;
        activeCellGameObjects.Add(instance);

        // Set up SortableEntity
        SortableEntity sorter = instance.GetComponent<SortableEntity>() ?? instance.AddComponent<SortableEntity>();
        if (cellType == PlantCellType.Seed) {
            sorter.SetUseParentYCoordinate(false);
        } else {
            sorter.SetUseParentYCoordinate(true);
        }

        // Apply scent data if it's a fruit
        if (cellType == PlantCellType.Fruit) {
            ApplyScentDataToObject(instance, accumulatedScentRadiusBonus, accumulatedScentStrengthBonus);
        }

        // Register visual effects
        RegisterShadowForCell(instance, cellType.ToString());
        // Outline registration now happens via OnPlantCellAdded call
        if (enableOutline && outlineController != null)
        {
             outlineController.OnPlantCellAdded(coords, instance);
        }

        return instance;
    }

    // --- Helper Methods for Shadow & Outline Integration ---
    private void RegisterShadowForCell(GameObject cellInstance, string cellTypeName)
    {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return;

        SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>(); // More robust
        if (partRenderer != null) {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        } else {
            Debug.LogWarning($"Plant '{gameObject.name}': {cellTypeName} missing SpriteRenderer. No shadow.", cellInstance);
        }
    }

    // REMOVED: RegisterOutlineForCell (Logic moved to SpawnCellVisual calling outlineController.OnPlantCellAdded)

    // --- Stat Calculation ---
    private void CalculateAndApplyStats()
    {
        // (Function body remains the same as before)
        if (nodeGraph == null) {
            Debug.LogError($"[{gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
            return;
        }

        float baseEnergyStorage = 10f;
        float basePhotosynthesisRate = 0.5f;
        int baseStemMin = 3;
        int baseStemMax = 5;
        float baseGrowthSpeedInterval = 0.5f; // Base time per step
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

        foreach (NodeData node in nodeGraph.nodes.OrderBy(n => n.orderIndex)) {
            if (node?.effects == null) continue;
            foreach (NodeEffectData effect in node.effects) {
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
                        // Modify the *time interval* per step
                        growthSpeedTimeModifier += effect.primaryValue;
                        break;
                    case NodeEffectType.LeafGap:
                        leafGapModifier += Mathf.RoundToInt(effect.primaryValue);
                        break;
                    case NodeEffectType.LeafPattern:
                        currentLeafPattern = Mathf.Clamp(Mathf.RoundToInt(effect.primaryValue), 0, 4); // Assuming max 4 patterns defined
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
                        // Add other passive effects here if needed
                }
            }
        }

        finalMaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        finalPhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis);
        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier);
        int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier);
        finalGrowthSpeed = Mathf.Max(0.01f, baseGrowthSpeedInterval + growthSpeedTimeModifier); // This is now TIME PER STEP
        finalLeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        finalLeafPattern = currentLeafPattern;
        finalGrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        cycleCooldown = Mathf.Max(0.1f, baseCooldown + cooldownModifier);
        nodeCastDelay = Mathf.Max(0.01f, baseCastDelay + castDelayModifier);

        targetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;
        // REMOVED: totalGrowthDuration calculation (less relevant)

        if (!seedFound) {
            Debug.LogWarning($"[{gameObject.name}] NodeGraph lacks SeedSpawn effect. Growth aborted.", gameObject);
        }
    }
}
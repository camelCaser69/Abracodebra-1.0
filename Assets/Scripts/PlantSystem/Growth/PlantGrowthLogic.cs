using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

#region Using Statements
// This region is for AI formatting. It will be removed in the final output.
#endregion

public class PlantGrowthLogic
{
    private readonly PlantGrowth plant;

    public int TargetStemLength { get; set; }
    public int GrowthTicksPerStage { get; set; }
    public int LeafGap { get; set; }
    public int LeafPattern { get; set; }
    public float GrowthRandomness { get; set; }
    public int MaturityCycleTicks { get; set; }
    public int NodeCastDelayTicks { get; set; }
    public float PhotosynthesisEfficiencyPerLeaf { get; set; }
    public float EnergyCostPerCycle { get; set; }
    public int MaxBerries { get; set; }

    public float PoopDetectionRadius { get; set; }
    public float EnergyPerPoop { get; set; }

    private float growthProgressTicks = 0f;
    private int maturityCycleTick = 0;
    private int currentStemStage = 0;
    private List<PoopController> _poopsToAbsorbNextTick = new List<PoopController>();
    private int _poopScanCooldownTicks = 0;
    private const int POOP_ABSORB_COOLDOWN = 5;

    public PlantGrowthLogic(PlantGrowth plant)
    {
        this.plant = plant;
    }

    // In file: Assets/Scripts/PlantSystem/Growth/PlantGrowthLogic.cs
public void CalculateAndApplyStats()
{
    if (plant.NodeGraph == null)
    {
        Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
        return;
    }

    float basePhotosynthesisEfficiency = FloraManager.Instance != null
        ? FloraManager.Instance.basePhotosynthesisRatePerLeaf
        : 0.1f;

    // Default values if no seed is found or seed is basic
    float baseEnergyStorage = 10f;
    int baseStemMin = 3;
    int baseStemMax = 5;
    int baseGrowthTicksPerStage = 5;
    int baseLeafGap = 1;
    int baseLeafPattern = 0;
    float baseGrowthRandomness = 0.1f;
    int baseCooldownTicks = 20;
    int baseCastDelayTicks = 0;
    bool seedFound = false;
    int baseMaxBerries = 3;

    NodeData firstNode = plant.NodeGraph.nodes.FirstOrDefault();
    if (firstNode != null)
    {
        var seedEffect = firstNode.effects?.FirstOrDefault(e => e != null && e.effectType == NodeEffectType.SeedSpawn);
        if (seedEffect != null && seedEffect.seedData != null)
        {
            seedFound = true;
            baseEnergyStorage = seedEffect.seedData.energyStorage;
            baseGrowthTicksPerStage = seedEffect.seedData.growthSpeed;
            baseStemMin = seedEffect.seedData.stemLengthMin;
            baseStemMax = seedEffect.seedData.stemLengthMax;
            baseLeafGap = seedEffect.seedData.leafGap;
            baseLeafPattern = seedEffect.seedData.leafPattern;
            baseGrowthRandomness = seedEffect.seedData.stemRandomness;
            baseCooldownTicks = seedEffect.seedData.cooldown;
            baseCastDelayTicks = seedEffect.seedData.castDelay;
            baseMaxBerries = seedEffect.seedData.maxBerries;
        }
    }

    // Accumulators for stat modifications
    float accumulatedEnergyStorage = 0f;
    float accumulatedPhotosynthesisEfficiency = 0f;
    int stemLengthMinModifier = 0;
    int stemLengthMaxModifier = 0;
    int growthTicksModifier = 0;
    int leafGapModifier = 0;
    int currentLeafPattern = baseLeafPattern;
    float growthRandomnessModifier = 0f;
    int cooldownTicksModifier = 0;
    int castDelayTicksModifier = 0;
    PoopDetectionRadius = 0f;
    EnergyPerPoop = 0f;

    foreach (NodeData node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
    {
        if (node?.effects == null) continue;

        foreach (var effect in node.effects)
        {
            // --- FIX: Replaced !effect.isPassive with !effect.IsPassive ---
            if (effect == null || !effect.IsPassive) continue;
            if (effect.effectType == NodeEffectType.SeedSpawn)
            {
                seedFound = true; // Re-confirm if another seed node exists (shouldn't happen)
                continue;
            }
            effect.ValidateForTicks(); // Adjust time-based values

            switch (effect.effectType)
            {
                case NodeEffectType.EnergyStorage: accumulatedEnergyStorage += effect.primaryValue; break;
                case NodeEffectType.EnergyPerTick: accumulatedPhotosynthesisEfficiency += effect.primaryValue; break;
                case NodeEffectType.StemLength:
                    stemLengthMinModifier += effect.GetPrimaryValueAsInt();
                    stemLengthMaxModifier += effect.GetSecondaryValueAsInt(); break;
                case NodeEffectType.GrowthSpeed: growthTicksModifier += effect.GetPrimaryValueAsInt(); break;
                case NodeEffectType.LeafGap: leafGapModifier += effect.GetPrimaryValueAsInt(); break;
                case NodeEffectType.LeafPattern: currentLeafPattern = Mathf.Clamp(effect.GetPrimaryValueAsInt(), 0, 4); break;
                case NodeEffectType.StemRandomness: growthRandomnessModifier += effect.primaryValue; break;
                case NodeEffectType.Cooldown: cooldownTicksModifier += effect.GetPrimaryValueAsInt(); break;
                case NodeEffectType.CastDelay: castDelayTicksModifier += effect.GetPrimaryValueAsInt(); break;
                case NodeEffectType.PoopAbsorption:
                    PoopDetectionRadius = Mathf.Max(PoopDetectionRadius, effect.primaryValue);
                    EnergyPerPoop = Mathf.Max(EnergyPerPoop, effect.secondaryValue);
                    break;
            }
        }
    }

    // Apply final calculated stats
    plant.EnergySystem.MaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
    PhotosynthesisEfficiencyPerLeaf = Mathf.Max(0f, basePhotosynthesisEfficiency + accumulatedPhotosynthesisEfficiency);

    int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthMinModifier);
    int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthMaxModifier);
    TargetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;

    GrowthTicksPerStage = Mathf.Max(1, baseGrowthTicksPerStage + growthTicksModifier);
    LeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
    LeafPattern = currentLeafPattern;
    GrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
    MaturityCycleTicks = Mathf.Max(1, baseCooldownTicks + cooldownTicksModifier);
    NodeCastDelayTicks = Mathf.Max(0, baseCastDelayTicks + castDelayTicksModifier);
    MaxBerries = baseMaxBerries;

    if (!seedFound)
    {
        Debug.LogWarning($"[{plant.gameObject.name}] NodeGraph lacks a SeedSpawn effect. Growth aborted.", plant.gameObject);
    }
}

    public void OnTickUpdate(int currentTick)
    {
        AbsorbPendingPoops();
        if (_poopScanCooldownTicks > 0)
        {
            _poopScanCooldownTicks--;
        }
        ScanForPoop();

        switch (plant.CurrentState)
        {
            case PlantState.Growing:
                float growthMultiplier = 1.0f;
                if (PlantGrowthModifierManager.Instance != null)
                {
                    growthMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(plant);
                }
                growthProgressTicks += growthMultiplier;

                if (growthProgressTicks >= GrowthTicksPerStage)
                {
                    growthProgressTicks = 0f;
                    GrowNextStemStage();

                    if (currentStemStage >= TargetStemLength)
                    {
                        CompleteGrowth();
                    }
                }
                break;
            case PlantState.Mature_Idle:
                maturityCycleTick++;
                if (maturityCycleTick >= MaturityCycleTicks && plant.EnergySystem.CurrentEnergy >= 1f)
                {
                    plant.CurrentState = PlantState.Mature_Executing;
                    plant.NodeExecutor.ExecuteMatureCycleTick();
                    maturityCycleTick = 0;
                }
                break;
            case PlantState.Mature_Executing:
                plant.CurrentState = PlantState.Mature_Idle;
                break;
        }
    }

    private void AbsorbPendingPoops()
    {
        if (_poopsToAbsorbNextTick.Count == 0) return;

        foreach (var poop in _poopsToAbsorbNextTick)
        {
            if (poop != null && poop.gameObject != null)
            {
                if (EnergyPerPoop > 0)
                {
                    plant.EnergySystem.AddEnergy(EnergyPerPoop);
                }
                Object.Destroy(poop.gameObject);
                if (Debug.isDebugBuild)
                    Debug.Log($"[{plant.gameObject.name}] Absorbed poop, gained {EnergyPerPoop} energy.");
                _poopScanCooldownTicks = POOP_ABSORB_COOLDOWN;
            }
        }
        _poopsToAbsorbNextTick.Clear();
    }

    private void ScanForPoop()
    {
        if (PoopDetectionRadius <= 0 || _poopsToAbsorbNextTick.Count > 0 || _poopScanCooldownTicks > 0) return;

        GridEntity plantGrid = plant.GetComponent<GridEntity>();
        if (plantGrid == null) return;

        int radiusTiles = Mathf.RoundToInt(PoopDetectionRadius);

        if (Debug.isDebugBuild && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[{plant.gameObject.name}] Scanning for poop with radius {radiusTiles} at position {plantGrid.Position}");
        }

        var tilesInRadius = GridRadiusUtility.GetTilesInCircle(plantGrid.Position, radiusTiles);

        foreach (var tile in tilesInRadius)
        {
            var entitiesAtTile = GridPositionManager.Instance.GetEntitiesAt(tile);
            if (entitiesAtTile.Count > 0)
            {
                foreach (var entity in entitiesAtTile)
                {
                    if (entity == null) continue;
                    PoopController poop = entity.GetComponent<PoopController>();
                    if (poop != null)
                    {
                        _poopsToAbsorbNextTick.Add(poop);
                        if (Debug.isDebugBuild)
                            Debug.Log($"[{plant.gameObject.name}] <color=green>SUCCESS:</color> Detected poop at {poop.GetComponent<GridEntity>().Position}. Will absorb next tick.");
                        return;
                    }
                }
            }
        }
    }

    private void GrowNextStemStage()
    {
        if (currentStemStage >= TargetStemLength) return;
        currentStemStage++;

        Vector2Int stemPos;
        if (currentStemStage == 1)
        {
            stemPos = Vector2Int.up;
        }
        else
        {
            Vector2Int previousStemPos = Vector2Int.zero;
            bool foundPreviousStem = false;

            for (int i = currentStemStage - 1; i >= 1; i--)
            {
                var cells = plant.CellManager.GetCells();
                foreach (var kvp in cells)
                {
                    if (kvp.Value == PlantCellType.Stem && kvp.Key.y == i)
                    {
                        previousStemPos = kvp.Key;
                        foundPreviousStem = true;
                        break;
                    }
                }
                if (foundPreviousStem) break;
            }

            if (!foundPreviousStem)
            {
                Debug.LogError($"[{plant.gameObject.name}] Could not find previous stem at stage {currentStemStage - 1}! Using default position.");
                previousStemPos = Vector2Int.up * (currentStemStage - 1);
            }

            stemPos = previousStemPos + Vector2Int.up;

            if (Random.value < GrowthRandomness)
            {
                int wobbleDirection = (Random.value < 0.5f) ? -1 : 1;
                stemPos.x = previousStemPos.x + wobbleDirection;
            }
            else
            {
                stemPos.x = previousStemPos.x;
            }

            int attempts = 0;
            while (plant.CellManager.HasCellAt(stemPos) && attempts < 5)
            {
                attempts++;
                switch (attempts)
                {
                    case 1: stemPos = previousStemPos + Vector2Int.up; break;
                    case 2: stemPos = previousStemPos + Vector2Int.up + Vector2Int.left; break;
                    case 3: stemPos = previousStemPos + Vector2Int.up + Vector2Int.right; break;
                    case 4: stemPos = previousStemPos + Vector2Int.up + new Vector2Int(0, 1); break;
                    default: stemPos = previousStemPos + Vector2Int.up; break;
                }
            }
            if (plant.CellManager.HasCellAt(stemPos))
            {
                var existingCellType = plant.CellManager.GetCellTypeAt(stemPos);
                Debug.LogError($"[{plant.gameObject.name}] Cannot spawn stem at stage {currentStemStage} - position {stemPos} occupied by {existingCellType}! Previous stem was at {previousStemPos}");
                return;
            }
        }

        GameObject stemCell = plant.CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos, null, null);
        if (stemCell == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] Failed to spawn stem at stage {currentStemStage} at position {stemPos}");
            return;
        }

        if (LeafGap >= 0 && (currentStemStage % (LeafGap + 1)) == 0)
        {
            var leafPositions = plant.CellManager.CalculateLeafPositions(stemPos, LeafPattern, currentStemStage);
            foreach (Vector2Int leafPos in leafPositions)
            {
                if (!plant.CellManager.HasCellAt(leafPos))
                {
                    GameObject leafCell = plant.CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos, null, null);
                    if (leafCell != null)
                    {
                        plant.CellManager.LeafDataList.Add(new LeafData(leafPos, true));
                    }
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Debug.LogWarning($"[{plant.gameObject.name}] Skipping leaf at {leafPos} - position occupied");
                }
            }
        }
    }

    public void CompleteGrowth()
    {
        if (plant.CurrentState == PlantState.GrowthComplete) return;

        plant.CurrentState = PlantState.Mature_Idle;
        maturityCycleTick = 0;

        if (Debug.isDebugBuild)
        {
            Debug.Log($"[{plant.gameObject.name}] Growth completed! Transitioning to mature state.");
        }
    }

    public float GetGrowthProgressNormalized()
    {
        if (GrowthTicksPerStage <= 0) return 1f;
        return growthProgressTicks / GrowthTicksPerStage;
    }

    public int GetCurrentStemStage() => currentStemStage;
    public void HandleGrowthComplete() => CompleteGrowth();
    public int GetStepsCompleted() => currentStemStage;
    public float GetGrowthProgress() => TargetStemLength > 0 ? (float)currentStemStage / TargetStemLength : 0f;
    public float GetActualGrowthProgress() => GrowthTicksPerStage > 0 ? growthProgressTicks / GrowthTicksPerStage : 0f;
    public int GetCurrentStemCount() => currentStemStage;
}
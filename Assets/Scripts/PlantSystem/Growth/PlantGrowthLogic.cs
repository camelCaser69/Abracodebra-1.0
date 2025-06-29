using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantGrowthLogic
{
    private readonly PlantGrowth plant;

    #region Properties
    public int TargetStemLength { get; private set; }
    public int GrowthTicksPerStage { get; private set; }
    public int LeafGap { get; private set; }
    public int LeafPattern { get; private set; }
    public float GrowthRandomness { get; private set; }
    public int MaturityCycleTicks { get; private set; }
    public int NodeCastDelayTicks { get; private set; }
    public float EnergyPerTick { get; private set; }
    public float EnergyCostPerCycle { get; private set; }
    public int MaxBerries { get; private set; }

    // Poop Absorption Properties
    public float PoopDetectionRadius { get; private set; }
    public float EnergyPerPoop { get; private set; }
    #endregion

    #region Private State
    private float growthProgressTicks = 0f;
    private int maturityCycleTick = 0;
    private int currentStemStage = 0;
    private List<PoopController> _poopsToAbsorbNextTick = new List<PoopController>();
    #endregion

    public PlantGrowthLogic(PlantGrowth plant)
    {
        this.plant = plant;
    }

    public void CalculateAndApplyStats()
    {
        if (plant.NodeGraph == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
            return;
        }

        // --- Set Base Stats from Seed ---
        float baseEnergyStorage = 10f;
        float baseEnergyPerTick = 0.25f;
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

        // --- Accumulate Modifiers from All Nodes ---
        float accumulatedEnergyStorage = 0f;
        float accumulatedEnergyPerTick = 0f;
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
                if (effect == null || !effect.isPassive) continue;
                if (effect.effectType == NodeEffectType.SeedSpawn)
                {
                    seedFound = true;
                    continue;
                }
                effect.ValidateForTicks();

                switch (effect.effectType)
                {
                    case NodeEffectType.EnergyStorage:   accumulatedEnergyStorage += effect.primaryValue; break;
                    case NodeEffectType.EnergyPerTick:   accumulatedEnergyPerTick += effect.primaryValue; break;
                    case NodeEffectType.StemLength:      stemLengthMinModifier += effect.GetPrimaryValueAsInt();
                                                         stemLengthMaxModifier += effect.GetSecondaryValueAsInt(); break;
                    case NodeEffectType.GrowthSpeed:     growthTicksModifier += effect.GetPrimaryValueAsInt(); break;
                    case NodeEffectType.LeafGap:         leafGapModifier += effect.GetPrimaryValueAsInt(); break;
                    case NodeEffectType.LeafPattern:     currentLeafPattern = Mathf.Clamp(effect.GetPrimaryValueAsInt(), 0, 4); break;
                    case NodeEffectType.StemRandomness:  growthRandomnessModifier += effect.primaryValue; break;
                    case NodeEffectType.Cooldown:        cooldownTicksModifier += effect.GetPrimaryValueAsInt(); break;
                    case NodeEffectType.CastDelay:       castDelayTicksModifier += effect.GetPrimaryValueAsInt(); break;
                    case NodeEffectType.PoopAbsorption:
                        PoopDetectionRadius = Mathf.Max(PoopDetectionRadius, effect.primaryValue);
                        EnergyPerPoop = Mathf.Max(EnergyPerPoop, effect.secondaryValue);
                        break;
                }
            }
        }

        // --- Apply Final Stats ---
        plant.EnergySystem.MaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        EnergyPerTick = Mathf.Max(0f, baseEnergyPerTick + accumulatedEnergyPerTick);
        plant.EnergySystem.PhotosynthesisRate = EnergyPerTick;

        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthMinModifier);
        int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthMaxModifier);
        TargetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;

        GrowthTicksPerStage = Mathf.Max(1, baseGrowthTicksPerStage + growthTicksModifier);
        LeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        LeafPattern = currentLeafPattern;
        GrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        MaturityCycleTicks = Mathf.Max(1, baseCooldownTicks + cooldownTicksModifier);
        NodeCastDelayTicks = Mathf.Max(0, baseCastDelayTicks + castDelayTicksModifier);

        if (!seedFound)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] NodeGraph lacks a SeedSpawn effect. Growth aborted.", plant.gameObject);
        }
        if (Debug.isDebugBuild)
        {
            Debug.Log($"[{plant.gameObject.name}] Growth stats calculated. " +
                      $"TargetStem: {TargetStemLength}, " +
                      $"GrowthTicks: {GrowthTicksPerStage}, " +
                      $"Cooldown: {MaturityCycleTicks}, " +
                      $"PoopRadius: {PoopDetectionRadius} (Energy: {EnergyPerPoop})");
        }

        MaxBerries = baseMaxBerries;
    }

    public void OnTickUpdate(int currentTick)
    {
        AbsorbPendingPoops();
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
                if(Debug.isDebugBuild)
                    Debug.Log($"[{plant.gameObject.name}] Absorbed poop, gained {EnergyPerPoop} energy.");
            }
        }
        _poopsToAbsorbNextTick.Clear();
    }

    private void ScanForPoop()
    {
        if (PoopDetectionRadius <= 0 || _poopsToAbsorbNextTick.Count > 0) return;

        GridEntity plantGrid = plant.GetComponent<GridEntity>();
        if (plantGrid == null) return;

        int radiusTiles = Mathf.RoundToInt(PoopDetectionRadius);
        
        // ADDED DEBUG LOG
        if (Debug.isDebugBuild && Time.frameCount % 60 == 0) // Log only once per second to avoid spam
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
                        // ADDED DEBUG LOG
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
                foreach(var kvp in cells)
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
                previousStemPos = Vector2Int.up * (currentStemStage-1);
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
                     if(Debug.isDebugBuild)
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

    #region Helper Getters
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
    #endregion
}
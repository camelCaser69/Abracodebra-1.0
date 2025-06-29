using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantGrowthLogic {
    readonly PlantGrowth plant;

    public int TargetStemLength { get; private set; }
    public int GrowthTicksPerStage { get; private set; }
    public int LeafGap { get; private set; }
    public int LeafPattern { get; private set; }
    public float GrowthRandomness { get; private set; }
    public int MaturityCycleTicks { get; private set; }
    public int NodeCastDelayTicks { get; private set; }

    public float EnergyPerTick { get; private set; }
    public float EnergyCostPerCycle { get; private set; }

    float growthProgressTicks = 0f;
    int maturityCycleTick = 0;
    int currentStemStage = 0;
    
    public int MaxBerries { get; set; }

    public PlantGrowthLogic(PlantGrowth plant) {
        this.plant = plant;
    }

    // In file: Assets\Scripts\PlantSystem\Growth\PlantGrowthLogic.cs

public void CalculateAndApplyStats()
    {
        if (plant.NodeGraph == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] CalculateAndApplyStats called with null NodeGraph!");
            return;
        }

        // --- Step 1: Set hardcoded default base values ---
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

        // --- Step 2: If a comprehensive seed exists, use its data to OVERWRITE the defaults ---
        NodeData firstNode = plant.NodeGraph.nodes.FirstOrDefault();
        if (firstNode != null)
        {
            var seedEffect = firstNode.effects?.FirstOrDefault(e => e != null && e.effectType == NodeEffectType.SeedSpawn);
            if (seedEffect != null && seedEffect.seedData != null) {
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
                baseMaxBerries = seedEffect.seedData.maxBerries; // ADD THIS LINE
            }
        }

        // --- Step 3: Loop through ALL passive effects to calculate additional modifiers ---
        float accumulatedEnergyStorage = 0f;
        float accumulatedEnergyPerTick = 0f;
        int stemLengthMinModifier = 0;
        int stemLengthMaxModifier = 0;
        int growthTicksModifier = 0;
        int leafGapModifier = 0;
        int currentLeafPattern = baseLeafPattern; // Start with the (potentially seed-overwritten) base
        float growthRandomnessModifier = 0f;
        int cooldownTicksModifier = 0;
        int castDelayTicksModifier = 0;

        foreach (NodeData node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;

            foreach (var effect in node.effects)
            {
                if (effect == null || !effect.isPassive) continue;

                // The SeedSpawn effect itself doesn't grant stats; its *data* was already used.
                // We only process other passive modifiers here.
                if (effect.effectType == NodeEffectType.SeedSpawn)
                {
                    seedFound = true; // Still need to confirm a seed exists
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
                }
            }
        }

        // --- Step 4: Apply final calculated stats ---
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

        plant.NodeExecutor.ProcessPassiveEffects(plant.NodeGraph);

        if (!seedFound)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] NodeGraph lacks a SeedSpawn effect. Growth aborted.", plant.gameObject);
        }
        
        Debug.Log($"[{plant.gameObject.name}] Growth stats: TargetStem={TargetStemLength}, GrowthTicks={GrowthTicksPerStage}, Cooldown={MaturityCycleTicks}");
        
        MaxBerries = baseMaxBerries;
    }

    public void OnTickUpdate(int currentTick)
    {
        switch (plant.CurrentState)
        {
            case PlantState.Growing:
                // REMOVED: Redundant energy accumulation was happening here and in PlantGrowth.
                
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
                        CompleteGrowth(); // This will change the state to Mature_Idle
                    }
                }
                break;

            case PlantState.Mature_Idle:
                // REMOVED: Redundant energy accumulation.
                maturityCycleTick++;

                if (maturityCycleTick >= MaturityCycleTicks && plant.EnergySystem.CurrentEnergy >= 1f)
                {
                    plant.CurrentState = PlantState.Mature_Executing;
                    plant.NodeExecutor.ExecuteMatureCycleTick();
                    maturityCycleTick = 0;
                }
                break;

            case PlantState.Mature_Executing:
                // REMOVED: Redundant energy accumulation.
                plant.CurrentState = PlantState.Mature_Idle;
                break;
        }
    }

    // Replace the GrowNextStemStage method in PlantGrowthLogic.cs:

// PARTIAL FIX: Only the GrowNextStemStage method needs to be updated
// Replace the existing GrowNextStemStage method in PlantGrowthLogic.cs with this:

    void GrowNextStemStage()
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

        // Find the previous stem position
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

        // Calculate new stem position
        stemPos = previousStemPos + Vector2Int.up;

        if (Random.value < GrowthRandomness)
        {
            int wobbleDirection = (Random.value < 0.5f) ? -1 : 1;
            stemPos.x = previousStemPos.x + wobbleDirection;
        }
        else
        {
            stemPos.x = previousStemPos.x; // Grow straight up
        }

        // Check if the position is already occupied and find alternative
        int attempts = 0;
        while (plant.CellManager.HasCellAt(stemPos) && attempts < 5)
        {
            attempts++;
            // Try alternative positions: straight up, then left, then right
            switch (attempts)
            {
                case 1:
                    stemPos = previousStemPos + Vector2Int.up; // Straight up
                    break;
                case 2:
                    stemPos = previousStemPos + Vector2Int.up + Vector2Int.left; // Up-left
                    break;
                case 3:
                    stemPos = previousStemPos + Vector2Int.up + Vector2Int.right; // Up-right
                    break;
                case 4:
                    stemPos = previousStemPos + Vector2Int.up + new Vector2Int(0, 1); // Two up
                    break;
                default:
                    stemPos = previousStemPos + Vector2Int.up; // Default fallback
                    break;
            }
        }

        // If still occupied after all attempts, log detailed error
        if (plant.CellManager.HasCellAt(stemPos))
        {
            var existingCellType = plant.CellManager.GetCellTypeAt(stemPos);
            Debug.LogError($"[{plant.gameObject.name}] Cannot spawn stem at stage {currentStemStage} - position {stemPos} occupied by {existingCellType}! Previous stem was at {previousStemPos}");
            return;
        }
    }

    // Spawn the stem
    GameObject stemCell = plant.CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos, null, null);
    if (stemCell == null)
    {
        Debug.LogError($"[{plant.gameObject.name}] Failed to spawn stem at stage {currentStemStage} at position {stemPos}");
        return;
    }

    // FIXED: Add leaves with correct parameter order and proper LeafGap logic
    if (LeafGap >= 0 && (currentStemStage % (LeafGap + 1)) == 0)
    {
        // FIXED: Use correct parameter order: stemPos, leafPattern, stageCounter
        var leafPositions = plant.CellManager.CalculateLeafPositions(stemPos, LeafPattern, currentStemStage);
        foreach (Vector2Int leafPos in leafPositions)
        {
            // Check if leaf position is available before spawning
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
                Debug.LogWarning($"[{plant.gameObject.name}] Skipping leaf at {leafPos} - position occupied");
            }
        }
    }
}

    public void CompleteGrowth() {
        if (plant.CurrentState == PlantState.GrowthComplete) return;

        plant.CurrentState = PlantState.Mature_Idle;
        maturityCycleTick = 0;

        if (Debug.isDebugBuild) {
            Debug.Log($"[{plant.gameObject.name}] Growth completed! Transitioning to mature state.");
        }
    }

    public float GetGrowthProgressNormalized() {
        if (GrowthTicksPerStage <= 0) return 1f;
        return growthProgressTicks / GrowthTicksPerStage;
    }

    public int GetCurrentStemStage() => currentStemStage;
    public void HandleGrowthComplete() => CompleteGrowth();
    public int GetStepsCompleted() => currentStemStage;
    public float GetGrowthProgress() => TargetStemLength > 0 ? (float)currentStemStage / TargetStemLength : 0f;
    public int GetTotalPlannedSteps() => TargetStemLength;
    public float GetActualGrowthProgress() => GrowthTicksPerStage > 0 ? growthProgressTicks / GrowthTicksPerStage : 0f;
    public int GetCurrentStemCount() => currentStemStage;
}
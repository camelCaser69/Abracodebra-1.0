using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

public class PlantGrowthLogic
{
    private readonly PlantGrowth plant;

    public int TargetStemLength { get; set; }
    public float GrowthSpeed { get; set; }
    public int LeafGap { get; set; }
    public int LeafPattern { get; set; }
    public float GrowthRandomness { get; set; }
    public int MaturityCycleTicks { get; set; }
    public int NodeCastDelayTicks { get; set; } // Renamed from NodeCastDelay (float to int)

    private float growthProgress = 0f;
    private int maturityCycleTick = 0;
    private int currentStemStage = 0;
    private int currentStemCount = 0;
    private float cycleTimer = 0f;
    private bool isGrowthCompletionHandled = false;

    private Coroutine growthCoroutine;
    private float estimatedTotalGrowthTime = 1f;
    private float actualGrowthProgress = 0f;
    private int stepsCompleted = 0;
    private int totalPlannedSteps = 0;

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

        // Base values
        float baseEnergyStorage = 10f;
        float basePhotosynthesisRate = 0.5f;
        int baseStemMin = 3;
        int baseStemMax = 5;
        float baseGrowthSpeedRate = 0.2f;
        int baseLeafGap = 1;
        int baseLeafPattern = 0;
        float baseGrowthRandomness = 0.1f;
        int baseCooldownTicks = 20;
        int baseCastDelayTicks = 0; // Changed from float to int

        // Accumulators
        float accumulatedEnergyStorage = 0f;
        float accumulatedPhotosynthesis = 0f;
        int stemLengthModifier = 0;
        float growthSpeedRateModifier = 0f;
        int leafGapModifier = 0;
        int currentLeafPattern = baseLeafPattern;
        float growthRandomnessModifier = 0f;
        int cooldownTicksModifier = 0;
        int castDelayTicksModifier = 0; // Changed from float to int
        bool seedFound = false;

        foreach (NodeData node in plant.NodeGraph.nodes.OrderBy(n => n.orderIndex))
        {
            if (node?.effects == null) continue;
            foreach (var effect in node.effects)
            {
                if (effect == null) continue;

                // --- BUG FIX STARTS HERE ---
                // The SeedSpawn effect is a special marker and should be detected regardless of its passive state.
                if (effect.effectType == NodeEffectType.SeedSpawn)
                {
                    seedFound = true;
                    continue; // Skip to the next effect, as SeedSpawn has no other passive value.
                }

                // For all other effects, we only consider the passive ones for stat calculation.
                if (!effect.isPassive) continue;
                // --- BUG FIX ENDS HERE ---

                switch (effect.effectType)
                {
                    // Note: SeedSpawn is now handled above
                    case NodeEffectType.EnergyStorage: accumulatedEnergyStorage += effect.primaryValue; break;
                    case NodeEffectType.EnergyPhotosynthesis: accumulatedPhotosynthesis += effect.primaryValue; break;
                    case NodeEffectType.StemLength: stemLengthModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.GrowthSpeed: growthSpeedRateModifier += effect.primaryValue; break;
                    case NodeEffectType.LeafGap: leafGapModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.LeafPattern: currentLeafPattern = Mathf.Clamp(Mathf.RoundToInt(effect.primaryValue), 0, 4); break;
                    case NodeEffectType.StemRandomness: growthRandomnessModifier += effect.primaryValue; break;
                    case NodeEffectType.Cooldown: cooldownTicksModifier += Mathf.RoundToInt(effect.primaryValue); break;
                    case NodeEffectType.CastDelay: castDelayTicksModifier += Mathf.RoundToInt(effect.primaryValue); break; // Round to int
                }
            }
        }

        // Apply final stats
        plant.EnergySystem.MaxEnergy = Mathf.Max(1f, baseEnergyStorage + accumulatedEnergyStorage);
        plant.EnergySystem.PhotosynthesisRate = Mathf.Max(0f, basePhotosynthesisRate + accumulatedPhotosynthesis);

        int finalStemMin = Mathf.Max(1, baseStemMin + stemLengthModifier);
        int finalStemMax = Mathf.Max(finalStemMin, baseStemMax + stemLengthModifier);
        GrowthSpeed = Mathf.Max(0.01f, baseGrowthSpeedRate + growthSpeedRateModifier);
        LeafGap = Mathf.Max(0, baseLeafGap + leafGapModifier);
        LeafPattern = currentLeafPattern;
        GrowthRandomness = Mathf.Clamp01(baseGrowthRandomness + growthRandomnessModifier);
        MaturityCycleTicks = Mathf.Max(1, baseCooldownTicks + cooldownTicksModifier);
        NodeCastDelayTicks = Mathf.Max(0, baseCastDelayTicks + castDelayTicksModifier); // Use int ticks

        // The final, crucial calculation that was failing
        TargetStemLength = seedFound ? Random.Range(finalStemMin, finalStemMax + 1) : 0;

        plant.NodeExecutor.ProcessPassiveEffects(plant.NodeGraph);

        if (!seedFound)
        {
            // This warning is now more accurate. It will only fire if the seed node truly lacks the SeedSpawn effect.
            Debug.LogWarning($"[{plant.gameObject.name}] NodeGraph lacks a SeedSpawn effect. Growth aborted.", plant.gameObject);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        switch (plant.CurrentState)
        {
            case PlantState.Growing:
                growthProgress += GrowthSpeed;

                if (growthProgress >= 1.0f)
                {
                    int stepsToGrow = Mathf.FloorToInt(growthProgress);
                    growthProgress -= stepsToGrow;

                    for (int i = 0; i < stepsToGrow; i++)
                    {
                        if (currentStemStage >= TargetStemLength)
                        {
                            CompleteGrowth();
                            break;
                        }
                        GrowNextStemStage();
                    }
                }

                if (currentStemStage >= TargetStemLength && plant.CurrentState == PlantState.Growing)
                {
                    CompleteGrowth();
                }

                break;

            case PlantState.Mature_Idle:
                plant.EnergySystem.AccumulateEnergyTick();
                maturityCycleTick++;
                if (maturityCycleTick >= MaturityCycleTicks && plant.EnergySystem.CurrentEnergy >= 1f)
                {
                    plant.CurrentState = PlantState.Mature_Executing;
                    plant.NodeExecutor.ExecuteMatureCycleTick();
                    maturityCycleTick = 0;
                }
                break;

            case PlantState.Mature_Executing:
                plant.EnergySystem.AccumulateEnergyTick();
                plant.CurrentState = PlantState.Mature_Idle;
                break;
        }
    }

    private void GrowNextStemStage()
    {
        if (currentStemStage >= TargetStemLength) return;

        currentStemStage++;
        Vector2Int stemPos = Vector2Int.up * currentStemStage;

        if (Random.value < GrowthRandomness)
        {
            stemPos += (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
        }

        GameObject stemCell = plant.CellManager.SpawnCellVisual(PlantCellType.Stem, stemPos, null, null);
        if (stemCell == null)
        {
            Debug.LogError($"[{plant.gameObject.name}] Failed to spawn stem at stage {currentStemStage}");
            return;
        }

        if (LeafGap >= 0 && (currentStemStage % (LeafGap + 1)) == 0)
        {
            var leafPositions = plant.CellManager.CalculateLeafPositions(stemPos, currentStemStage, LeafPattern);
            foreach (Vector2Int leafPos in leafPositions)
            {
                GameObject leafCell = plant.CellManager.SpawnCellVisual(PlantCellType.Leaf, leafPos, null, null);
                if (leafCell != null)
                {
                    plant.CellManager.LeafDataList.Add(new LeafData(leafPos, true));
                }
            }
        }
    }

    public void CompleteGrowth()
    {
        if (plant.CurrentState == PlantState.GrowthComplete) return;

        plant.CurrentState = PlantState.GrowthComplete;
        isGrowthCompletionHandled = false;

        if (plant.VisualManager.ShowGrowthPercentage)
        {
            plant.VisualManager.UpdateGrowthPercentageUI(true);
        }

        plant.CurrentState = PlantState.Mature_Idle;
        maturityCycleTick = 0;

        if (Debug.isDebugBuild)
        {
            Debug.Log($"[{plant.gameObject.name}] Growth completed! Transitioning to mature state.");
        }
    }

    public void StartRealtimeGrowth()
    {
        if (growthCoroutine != null) return;
        growthCoroutine = plant.StartCoroutine(GrowthCoroutine_Time_Based());
    }

    public void StopGrowthCoroutine()
    {
        if (growthCoroutine != null)
        {
            plant.StopCoroutine(growthCoroutine);
            growthCoroutine = null;
        }
    }

    public void HandleGrowthComplete()
    {
        if (!isGrowthCompletionHandled)
        {
            isGrowthCompletionHandled = true;
            if (plant.VisualManager.ShowGrowthPercentage && TargetStemLength > 0)
            {
                plant.VisualManager.UpdateGrowthPercentageUI(true);
            }
            float rtCooldown = MaturityCycleTicks * (TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);
            cycleTimer = rtCooldown;
            plant.CurrentState = PlantState.Mature_Idle;
            plant.VisualManager.UpdateUI();
        }
    }

    public void UpdateMaturityCycle()
    {
        cycleTimer -= Time.deltaTime;
        if (cycleTimer <= 0f && plant.EnergySystem.CurrentEnergy >= 1f)
        {
            plant.CurrentState = PlantState.Mature_Executing;
            plant.StartCoroutine(ExecuteMatureCycle());
        }
    }

    private IEnumerator GrowthCoroutine_Time_Based()
    {
        if (TargetStemLength <= 0)
        {
            plant.CurrentState = PlantState.GrowthComplete;
            growthCoroutine = null;
            yield break;
        }

        List<GrowthStep> growthPlan = PreCalculateGrowthPlan();
        totalPlannedSteps = growthPlan.Count;
        stepsCompleted = 0;
        actualGrowthProgress = 0f;

        if (totalPlannedSteps == 0)
        {
            plant.CurrentState = PlantState.GrowthComplete;
            growthCoroutine = null;
            yield break;
        }

        float ticksPerSecond = TickManager.Instance?.Config.ticksPerRealSecond ?? 2.0f;
        float baseTimePerStep = 1f / (Mathf.Max(0.01f, GrowthSpeed) * ticksPerSecond);

        if (baseTimePerStep <= 0.001f) baseTimePerStep = 0.01f;

        float initialTileMultiplier = PlantGrowthModifierManager.Instance?.GetGrowthSpeedMultiplier(plant) ?? 1.0f;
        initialTileMultiplier = Mathf.Clamp(initialTileMultiplier, 0.1f, 10.0f);
        float initialEffectiveTimePerStep = baseTimePerStep / initialTileMultiplier;
        if (initialEffectiveTimePerStep < 0.001f) initialEffectiveTimePerStep = 0.001f;

        estimatedTotalGrowthTime = totalPlannedSteps * initialEffectiveTimePerStep;
        if (estimatedTotalGrowthTime < 0.01f) estimatedTotalGrowthTime = 0.01f;

        float progressTowardNextStep = 0f;
        
        // Convert NodeCastDelayTicks to seconds for this coroutine
        float castDelayInSeconds = NodeCastDelayTicks * (TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);

        while (stepsCompleted < totalPlannedSteps && plant.CurrentState == PlantState.Growing)
        {
            float currentTileMultiplier = PlantGrowthModifierManager.Instance?.GetGrowthSpeedMultiplier(plant) ?? 1.0f;
            currentTileMultiplier = Mathf.Clamp(currentTileMultiplier, 0.1f, 10.0f);
            float currentEffectiveTimePerStep = baseTimePerStep / currentTileMultiplier;
            if (currentEffectiveTimePerStep < 0.001f) currentEffectiveTimePerStep = 0.001f;

            float progressRate = initialEffectiveTimePerStep / currentEffectiveTimePerStep;
            progressTowardNextStep += Time.deltaTime * progressRate;
            actualGrowthProgress = Mathf.Clamp01(progressTowardNextStep / initialEffectiveTimePerStep);

            int stepsToProcessThisFrame = 0;
            while (progressTowardNextStep >= initialEffectiveTimePerStep && stepsCompleted < totalPlannedSteps)
            {
                stepsToProcessThisFrame++;
                progressTowardNextStep -= initialEffectiveTimePerStep;
                if (stepsToProcessThisFrame >= 3) break;
            }

            if (stepsToProcessThisFrame > 0)
            {
                for (int i = 0; i < stepsToProcessThisFrame; i++)
                {
                    int currentPlanIndex = stepsCompleted;
                    if (currentPlanIndex >= totalPlannedSteps) break;

                    GrowthStep step = growthPlan[currentPlanIndex];
                    GameObject spawnedCell = plant.CellManager.SpawnCellVisual(step.CellType, step.Position, null, null);
                    if (spawnedCell != null && step.CellType == PlantCellType.Leaf)
                    {
                        plant.CellManager.LeafDataList.Add(new LeafData(step.Position, true));
                    }
                    if (step.CellType == PlantCellType.Stem)
                    {
                        currentStemCount = step.StemIndex;
                        if (!plant.VisualManager.ContinuousIncrement) plant.VisualManager.UpdateGrowthPercentageUI();
                    }
                    stepsCompleted++;

                    // Use the calculated delay in seconds
                    if (castDelayInSeconds > 0.01f && i < stepsToProcessThisFrame - 1)
                    {
                        yield return new WaitForSeconds(castDelayInSeconds);
                    }
                }
            }
            yield return null;
        }

        if (stepsCompleted >= totalPlannedSteps)
        {
            plant.CurrentState = PlantState.GrowthComplete;
            if (plant.VisualManager.ShowGrowthPercentage) plant.VisualManager.UpdateGrowthPercentageUI(true);
        }
        growthCoroutine = null;
    }

    private List<GrowthStep> PreCalculateGrowthPlan()
    {
        var plan = new List<GrowthStep>();
        for (int stemIndex = 1; stemIndex <= TargetStemLength; stemIndex++)
        {
            Vector2Int stemPosition = Vector2Int.up * stemIndex;
            if (Random.value < GrowthRandomness)
            {
                stemPosition += (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
            }
            plan.Add(new GrowthStep { CellType = PlantCellType.Stem, Position = stemPosition, StemIndex = stemIndex });

            if (LeafGap >= 0 && stemIndex > 0 && (stemIndex % (LeafGap + 1)) == 0)
            {
                List<Vector2Int> leafPositions = plant.CellManager.CalculateLeafPositions(stemPosition, stemIndex, LeafPattern);
                foreach (Vector2Int leafPos in leafPositions)
                {
                    plan.Add(new GrowthStep { CellType = PlantCellType.Leaf, Position = leafPos, StemIndex = stemIndex });
                }
            }
        }
        return plan;
    }

    private IEnumerator ExecuteMatureCycle()
    {
        plant.NodeExecutor.ExecuteMatureCycleTick();

        float finalRtCooldown = MaturityCycleTicks * (TickManager.Instance?.Config.GetRealSecondsPerTick() ?? 0.5f);
        cycleTimer = finalRtCooldown;
        plant.CurrentState = PlantState.Mature_Idle;
        yield return null;
    }

    private class GrowthStep
    {
        public PlantCellType CellType;
        public Vector2Int Position;
        public int StemIndex;
    }

    public float GetGrowthProgress() => growthProgress;
    public int GetCurrentStemStage() => currentStemStage;
    public int GetCurrentStemCount() => currentStemCount;
    public float GetActualGrowthProgress() => actualGrowthProgress;
    public int GetStepsCompleted() => stepsCompleted;
    public int GetTotalPlannedSteps() => totalPlannedSteps;
}
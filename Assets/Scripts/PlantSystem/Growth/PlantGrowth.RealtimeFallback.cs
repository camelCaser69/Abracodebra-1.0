using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PlantGrowth : MonoBehaviour
{
    private class GrowthStep { public PlantCellType CellType; public Vector2Int Position; public int StemIndex; }

    private void AccumulateEnergy() {
        if (finalPhotosynthesisRate <= 0 || finalMaxEnergy <= 0) return;

        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf);
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(this) : 1.0f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null) {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly, fireflyManagerInstance.maxPhotosynthesisBonus);
        }

        float standardPhotosynthesis = finalPhotosynthesisRate * leafCount * sunlight;
        float totalRate = (standardPhotosynthesis + fireflyBonusRate) * tileMultiplier;
        float delta = totalRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, finalMaxEnergy);
    }

    private IEnumerator GrowthCoroutine_Time_Based() {
        if (targetStemLength <= 0) {
            currentState = PlantState.GrowthComplete;
            growthCoroutine = null;
            yield break;
        }

        List<GrowthStep> growthPlan = PreCalculateGrowthPlan();
        totalPlannedSteps = growthPlan.Count;
        stepsCompleted = 0;
        actualGrowthProgress = 0f;

        if (totalPlannedSteps == 0) {
            currentState = PlantState.GrowthComplete;
            growthCoroutine = null;
            yield break;
        }

        // REWORKED: Convert growth rate (stems/tick) to time per stem for real-time.
        // Assumes a tick rate for conversion.
        float ticksPerSecond = WegoSystem.TickManager.Instance?.Config.ticksPerRealSecond ?? 2.0f;
        float baseTimePerStep = 1f / (Mathf.Max(0.01f, finalGrowthSpeed) * ticksPerSecond);

        if (baseTimePerStep <= 0.001f) baseTimePerStep = 0.01f;

        float initialTileMultiplier = PlantGrowthModifierManager.Instance?.GetGrowthSpeedMultiplier(this) ?? 1.0f;
        initialTileMultiplier = Mathf.Clamp(initialTileMultiplier, 0.1f, 10.0f);
        float initialEffectiveTimePerStep = baseTimePerStep / initialTileMultiplier;
        if (initialEffectiveTimePerStep < 0.001f) initialEffectiveTimePerStep = 0.001f;

        estimatedTotalGrowthTime = totalPlannedSteps * initialEffectiveTimePerStep;
        if (estimatedTotalGrowthTime < 0.01f) estimatedTotalGrowthTime = 0.01f;

        float progressTowardNextStep = 0f;
        
        while (stepsCompleted < totalPlannedSteps && currentState == PlantState.Growing) {
            float currentTileMultiplier = PlantGrowthModifierManager.Instance?.GetGrowthSpeedMultiplier(this) ?? 1.0f;
            currentTileMultiplier = Mathf.Clamp(currentTileMultiplier, 0.1f, 10.0f);
            float currentEffectiveTimePerStep = baseTimePerStep / currentTileMultiplier;
            if (currentEffectiveTimePerStep < 0.001f) currentEffectiveTimePerStep = 0.001f;
            
            float progressRate = initialEffectiveTimePerStep / currentEffectiveTimePerStep;
            progressTowardNextStep += Time.deltaTime * progressRate;
            actualGrowthProgress = Mathf.Clamp01(progressTowardNextStep / initialEffectiveTimePerStep);
            
            int stepsToProcessThisFrame = 0;
            while (progressTowardNextStep >= initialEffectiveTimePerStep && stepsCompleted < totalPlannedSteps) {
                stepsToProcessThisFrame++;
                progressTowardNextStep -= initialEffectiveTimePerStep;
                if (stepsToProcessThisFrame >= 3) break;
            }

            if (stepsToProcessThisFrame > 0) {
                for (int i = 0; i < stepsToProcessThisFrame; i++) {
                    int currentPlanIndex = stepsCompleted;
                    if (currentPlanIndex >= totalPlannedSteps) break;

                    GrowthStep step = growthPlan[currentPlanIndex];
                    GameObject spawnedCell = SpawnCellVisual(step.CellType, step.Position, null, null);
                    if (spawnedCell != null && step.CellType == PlantCellType.Leaf) {
                        leafDataList.Add(new LeafData(step.Position, true));
                    }
                    if (step.CellType == PlantCellType.Stem) {
                        currentStemCount = step.StemIndex;
                        if (!continuousIncrement) UpdateGrowthPercentageUI();
                    }
                    stepsCompleted++;
                    if (nodeCastDelay > 0.01f && i < stepsToProcessThisFrame - 1) {
                        yield return new WaitForSeconds(nodeCastDelay);
                    }
                }
            }
            yield return null;
        }

        if (stepsCompleted >= totalPlannedSteps) {
            currentState = PlantState.GrowthComplete;
            if (showGrowthPercentage) UpdateGrowthPercentageUI(true);
        }
        growthCoroutine = null;
    }
    
    private List<GrowthStep> PreCalculateGrowthPlan() {
        var plan = new List<GrowthStep>();
        for (int stemIndex = 1; stemIndex <= targetStemLength; stemIndex++) {
            Vector2Int stemPosition = Vector2Int.up * stemIndex;
            if (Random.value < finalGrowthRandomness) {
                stemPosition += (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
            }
            plan.Add(new GrowthStep { CellType = PlantCellType.Stem, Position = stemPosition, StemIndex = stemIndex });

            if (finalLeafGap >= 0 && stemIndex > 0 && (stemIndex % (finalLeafGap + 1)) == 0) {
                List<Vector2Int> leafPositions = CalculateLeafPositions(stemPosition, stemIndex);
                foreach (Vector2Int leafPos in leafPositions) {
                    plan.Add(new GrowthStep { CellType = PlantCellType.Leaf, Position = leafPos, StemIndex = stemIndex });
                }
            }
        }
        return plan;
    }
}
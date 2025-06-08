using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using WegoSystem;

public partial class PlantGrowth : MonoBehaviour {

    void HandleWegoGrowth() {
        currentGrowthTick++;

        if (currentGrowthTick >= growthTicksPerStage) {
            GrowNextStemStage();
            currentGrowthTick = 0;
        }

        if (finalLeafGap >= 0 && currentStemStage > 0 && (currentStemStage % (finalLeafGap + 1)) == 0) {
            SpawnLeavesForCurrentStage();
        }

        if (currentStemStage >= targetStemLength) {
            CompleteGrowth();
        }

        if (showGrowthPercentage) UpdateGrowthPercentageUI();
    }

    void GrowNextStemStage() {
        if (currentStemStage >= targetStemLength) return;

        currentStemStage++;
        Vector2Int stemPos = Vector2Int.up * currentStemStage; // Simple upward growth

        if (Random.value < finalGrowthRandomness) {
            stemPos += (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
        }

        GameObject stemCell = SpawnCellVisual(PlantCellType.Stem, stemPos, null, null);
        if (stemCell == null) {
            Debug.LogError($"[{gameObject.name}] Failed to spawn stem at stage {currentStemStage}");
        }

        if (Debug.isDebugBuild) {
            Debug.Log($"[{gameObject.name}] Grew stem stage {currentStemStage}/{targetStemLength}");
        }
    }

    void SpawnLeavesForCurrentStage() {
        Vector2Int stemPos = Vector2Int.up * currentStemStage;
        List<Vector2Int> leafPositions = CalculateLeafPositions(stemPos, currentStemStage);

        foreach (Vector2Int leafPos in leafPositions) {
            GameObject leafCell = SpawnCellVisual(PlantCellType.Leaf, leafPos, null, null);
            if (leafCell != null) {
                leafDataList.Add(new LeafData(leafPos, true));
            }
        }
    }

    void CompleteGrowth() {
        currentState = PlantState.GrowthComplete;
        isGrowthCompletionHandled = false;

        if (showGrowthPercentage) UpdateGrowthPercentageUI(true);

        currentState = PlantState.Mature_Idle;
        maturityCycleTick = 0;

        if (Debug.isDebugBuild) {
            Debug.Log($"[{gameObject.name}] Growth completed! Transitioning to mature state.");
        }
    }

    void AccumulateEnergyTick() {
        if (finalPhotosynthesisRate <= 0 || finalMaxEnergy <= 0) return;

        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf);
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(this) : 1.0f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null) {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(
                transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(
                nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus);
        }

        float ticksPerSecond = TickManager.Instance?.Config?.ticksPerRealSecond ?? 2f;
        float standardPhotosynthesis = (finalPhotosynthesisRate * leafCount * sunlight) / ticksPerSecond;
        float totalRate = (standardPhotosynthesis + (fireflyBonusRate / ticksPerSecond)) * tileMultiplier;

        currentEnergy = Mathf.Clamp(currentEnergy + totalRate, 0f, finalMaxEnergy);
    }

    void AccumulateEnergy() {
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

    void UpdateGrowthPercentageUI(bool forceComplete = false) {
        if (!showGrowthPercentage || energyText == null) return;

        float rawPercentageFloat = 0f;

        if (forceComplete || currentState == PlantState.GrowthComplete) {
            rawPercentageFloat = 100f;
        }
        else if (useWegoSystem) {
            if (targetStemLength <= 0) {
                rawPercentageFloat = 0f;
            } else {
                rawPercentageFloat = Mathf.Clamp(((float)currentStemStage / targetStemLength) * 100f, 0f, 100f);
            }
        }
        else if (continuousIncrement) {
            if (totalPlannedSteps > 0) {
                rawPercentageFloat = ((float)stepsCompleted / totalPlannedSteps) * 100f;

                if (actualGrowthProgress > 0f && stepsCompleted < totalPlannedSteps) {
                    float stepSize = 100f / totalPlannedSteps;
                    float partialStepProgress = actualGrowthProgress * stepSize;
                    rawPercentageFloat = (stepsCompleted * stepSize) + partialStepProgress;
                }
            }
            else {
                rawPercentageFloat = (currentState == PlantState.Growing) ? 0f : 100f;
            }
        }
        else {
            if (targetStemLength <= 0) {
                rawPercentageFloat = 0f;
            }
            else {
                rawPercentageFloat = Mathf.Clamp(((float)currentStemCount / targetStemLength) * 100f, 0f, 100f);
            }
        }

        int targetDisplayValue;
        if (percentageIncrement <= 1) {
            targetDisplayValue = Mathf.FloorToInt(rawPercentageFloat);
        }
        else {
            targetDisplayValue = Mathf.RoundToInt(rawPercentageFloat / percentageIncrement) * percentageIncrement;
        }
        targetDisplayValue = Mathf.Min(targetDisplayValue, 100);

        if (targetDisplayValue == 100 && currentState == PlantState.Growing && !forceComplete) {
            targetDisplayValue = 95;
        }

        if (targetDisplayValue != displayedGrowthPercentage) {
            displayedGrowthPercentage = targetDisplayValue;
            energyText.text = $"{displayedGrowthPercentage}%";
        }
    }
}
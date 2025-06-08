using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class PlantGrowth : MonoBehaviour {

    class GrowthStep {
        public PlantCellType CellType;
        public Vector2Int Position;
        public int StemIndex;
    }

    IEnumerator GrowthCoroutine_TimeBased() {
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

        int stemStepsCount = growthPlan.Count(step => step.CellType == PlantCellType.Stem);
        if (stemStepsCount == 0) stemStepsCount = 1;

        currentGrowthElapsedTime = 0f;
        float baseTimePerStep = finalGrowthSpeed;
        if (baseTimePerStep <= 0.001f) baseTimePerStep = 0.01f;

        float initialTileMultiplier = 1.0f;
        if (PlantGrowthModifierManager.Instance != null) {
            initialTileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this);
            initialTileMultiplier = Mathf.Clamp(initialTileMultiplier, 0.1f, 10.0f);
        }
        float initialEffectiveTimePerStep = baseTimePerStep / initialTileMultiplier;
        if (initialEffectiveTimePerStep < 0.001f) initialEffectiveTimePerStep = 0.001f;

        estimatedTotalGrowthTime = stemStepsCount * initialEffectiveTimePerStep;
        if (estimatedTotalGrowthTime < 0.01f) estimatedTotalGrowthTime = 0.01f;

        float lastUpdateTime = Time.time;
        float updateProgressInterval = 0.1f;

        float progressTowardNextStep = 0f;

        while (stepsCompleted < totalPlannedSteps && currentState == PlantState.Growing) {
            float frameDeltaTime = Time.deltaTime;
            currentGrowthElapsedTime += frameDeltaTime;

            float currentTileMultiplier = 1.0f;
            if (PlantGrowthModifierManager.Instance != null) {
                currentTileMultiplier = PlantGrowthModifierManager.Instance.GetGrowthSpeedMultiplier(this);
                currentTileMultiplier = Mathf.Clamp(currentTileMultiplier, 0.1f, 10.0f);
            }

            float currentEffectiveTimePerStep = baseTimePerStep / currentTileMultiplier;
            if (currentEffectiveTimePerStep < 0.001f) currentEffectiveTimePerStep = 0.001f;

            float progressRate = initialEffectiveTimePerStep / currentEffectiveTimePerStep;

            progressTowardNextStep += frameDeltaTime * progressRate;

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

                    if (spawnedCell != null) {
                        if (step.CellType == PlantCellType.Leaf) {
                            leafDataList.Add(new LeafData(step.Position, true));
                        }
                    }

                    if (step.CellType == PlantCellType.Stem) {
                        currentStemCount = step.StemIndex;
                        if (!continuousIncrement) {
                            UpdateGrowthPercentageUI();
                        }
                    }

                    stepsCompleted++;

                    if (nodeCastDelay > 0.01f && i < stepsToProcessThisFrame - 1) {
                        yield return new WaitForSeconds(nodeCastDelay);
                    }
                }
            }

            if (continuousIncrement && Time.time - lastUpdateTime >= updateProgressInterval) {
                UpdateGrowthPercentageUI();
                lastUpdateTime = Time.time;
            }

            yield return null;
        }

        if (stepsCompleted >= totalPlannedSteps) {
            currentState = PlantState.GrowthComplete;
            if (showGrowthPercentage) UpdateGrowthPercentageUI(true);
        }

        growthCoroutine = null;
    }

    List<GrowthStep> PreCalculateGrowthPlan() {
        var plan = new List<GrowthStep>();

        // Add the stem growth steps
        for (int stemIndex = 1; stemIndex <= targetStemLength; stemIndex++) {
            Vector2Int stemPosition = CalculateStemPosition(stemIndex);
            plan.Add(new GrowthStep {
                CellType = PlantCellType.Stem,
                Position = stemPosition,
                StemIndex = stemIndex
            });

            // Add leaves if this stem segment should have them
            if (finalLeafGap >= 0 && stemIndex > 0 && (stemIndex % (finalLeafGap + 1)) == 0) {
                List<Vector2Int> leafPositions = CalculateLeafPositions(stemPosition, stemIndex);
                foreach (Vector2Int leafPos in leafPositions) {
                    plan.Add(new GrowthStep {
                        CellType = PlantCellType.Leaf,
                        Position = leafPos,
                        StemIndex = stemIndex
                    });
                }
            }
        }

        return plan;
    }

    Vector2Int CalculateStemPosition(int stemIndex) {
        Vector2Int stemPos = Vector2Int.up * stemIndex;

        // Apply randomness if configured
        if (Random.value < finalGrowthRandomness) {
            // Simple left/right deviation
            stemPos += (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
        }

        return stemPos;
    }

    List<Vector2Int> CalculateLeafPositions(Vector2Int stemPos, int stageCounter) {
        List<Vector2Int> leafPositions = new List<Vector2Int>();
        Vector2Int leftBase = stemPos + Vector2Int.left;
        Vector2Int rightBase = stemPos + Vector2Int.right;

        switch (finalLeafPattern) {
            case 0: // Parallel leaves
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;

            case 1: // Alternating Offset leaves
                if (offsetRightForPattern1 == null)
                    offsetRightForPattern1 = Random.value < 0.5f;

                if (offsetRightForPattern1.Value) {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else {
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;

            case 2: // Alternating Parallel/Offset combination
                switch (stageCounter % 4) {
                    case 0:
                    case 2: // Parallel
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase);
                        break;
                    case 1: // Offset left up
                        leafPositions.Add(leftBase + Vector2Int.up);
                        leafPositions.Add(rightBase);
                        break;
                    case 3: // Offset right up
                        leafPositions.Add(leftBase);
                        leafPositions.Add(rightBase + Vector2Int.up);
                        break;
                }
                break;

            case 3: // Spiral leaves
                int spiralDir = (stageCounter % 2 == 0) ? 1 : -1;
                if (spiralDir > 0) {
                    leafPositions.Add(leftBase);
                    leafPositions.Add(rightBase + Vector2Int.up);
                } else {
                    leafPositions.Add(leftBase + Vector2Int.up);
                    leafPositions.Add(rightBase);
                }
                break;

            case 4: // Symmetric double leaves
                leafPositions.Add(leftBase);
                leafPositions.Add(leftBase + Vector2Int.up);
                leafPositions.Add(rightBase);
                leafPositions.Add(rightBase + Vector2Int.up);
                break;

            default: // Fallback to parallel
                leafPositions.Add(leftBase);
                leafPositions.Add(rightBase);
                break;
        }

        return leafPositions;
    }
}
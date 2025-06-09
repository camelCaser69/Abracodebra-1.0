using UnityEngine;
using WegoSystem;
using System.Linq;   

public partial class PlantGrowth : MonoBehaviour
{
    // OnTickUpdate is the main entry point for Wego system logic.
    // It's defined in this partial class file.
    public void OnTickUpdate(int currentTick)
    {
        if (!useWegoSystem) return;

        switch (currentState)
        {
            case PlantState.Growing:
                // Add the growth rate to our progress accumulator.
                growthProgress += finalGrowthSpeed;
                
                // If progress is 1.0 or more, we can grow one or more steps.
                if (growthProgress >= 1.0f)
                {
                    int stepsToGrow = Mathf.FloorToInt(growthProgress);
                    growthProgress -= stepsToGrow; // Consume the whole number, keep the fraction.

                    for (int i = 0; i < stepsToGrow; i++)
                    {
                        if (currentStemStage >= targetStemLength)
                        {
                            CompleteGrowth();
                            break; // Exit loop if growth is complete.
                        }
                        GrowNextStemStage();
                    }
                }

                // A final check in case the last growth step met the target.
                if (currentStemStage >= targetStemLength && currentState == PlantState.Growing)
                {
                    CompleteGrowth();
                }
                
                if (allowPhotosynthesisDuringGrowth)
                {
                    AccumulateEnergyTick();
                }
                break;

            case PlantState.Mature_Idle:
                AccumulateEnergyTick();
                maturityCycleTick++;
                if (maturityCycleTick >= maturityCycleTicks && currentEnergy >= 1f)
                {
                    currentState = PlantState.Mature_Executing;
                    ExecuteMatureCycleTick(); // This is an instant action within the tick.
                    maturityCycleTick = 0; // Reset for the next cycle.
                }
                break;

            case PlantState.Mature_Executing:
                // After executing, immediately become idle for the next tick.
                AccumulateEnergyTick();
                currentState = PlantState.Mature_Idle;
                break;
        }

        UpdateUI();
    }

    private void GrowNextStemStage()
    {
        if (currentStemStage >= targetStemLength) return;
        
        currentStemStage++;
        Vector2Int stemPos = Vector2Int.up * currentStemStage;

        if (Random.value < finalGrowthRandomness) {
            stemPos += (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
        }

        GameObject stemCell = SpawnCellVisual(PlantCellType.Stem, stemPos, null, null);
        if (stemCell == null) {
            Debug.LogError($"[{gameObject.name}] Failed to spawn stem at stage {currentStemStage}");
            return;
        }
        
        // Spawn leaves for this stage if the gap condition is met.
        if (finalLeafGap >= 0 && (currentStemStage % (finalLeafGap + 1)) == 0)
        {
            var leafPositions = CalculateLeafPositions(stemPos, currentStemStage);
            foreach (Vector2Int leafPos in leafPositions)
            {
                GameObject leafCell = SpawnCellVisual(PlantCellType.Leaf, leafPos, null, null);
                if (leafCell != null) {
                    leafDataList.Add(new LeafData(leafPos, true));
                }
            }
        }
    }
    
    private void CompleteGrowth()
    {
        if (currentState == PlantState.GrowthComplete) return;

        currentState = PlantState.GrowthComplete;
        isGrowthCompletionHandled = false; // For RT fallback compatibility

        if (showGrowthPercentage) UpdateGrowthPercentageUI(true);

        currentState = PlantState.Mature_Idle;
        maturityCycleTick = 0;

        if (Debug.isDebugBuild) {
            Debug.Log($"[{gameObject.name}] Growth completed! Transitioning to mature state.");
        }
    }
    
    private void AccumulateEnergyTick()
    {
        if (finalPhotosynthesisRate <= 0 || finalMaxEnergy <= 0) return;

        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = cells.Values.Count(c => c == PlantCellType.Leaf);
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(this) : 1.0f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null) {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount( transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(
                nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus);
        }

        float ticksPerSecond = TickManager.Instance?.Config?.ticksPerRealSecond ?? 2f;
        float standardPhotosynthesis = (finalPhotosynthesisRate * leafCount * sunlight) / ticksPerSecond;
        float totalRate = (standardPhotosynthesis + (fireflyBonusRate / ticksPerSecond)) * tileMultiplier;

        currentEnergy = Mathf.Clamp(currentEnergy + totalRate, 0f, finalMaxEnergy);
    }
}
using UnityEngine;
using System.Linq;
using WegoSystem;

#region Using Statements
// This region is for AI formatting. It will be removed in the final output.
#endregion

public class PlantEnergySystem
{
    private readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; } = 0f;
    public float MaxEnergy { get; set; } = 10f;
    
    // This property is not used in calculations and is effectively obsolete.
    public float PhotosynthesisRate { get; set; }

    private FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant)
    {
        this.plant = plant;
        fireflyManagerInstance = FireflyManager.Instance;
    }
    
    public void AccumulateEnergyTick()
    {
        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;

        // --- THE ONLY SOURCE OF PHOTOSYNTHESIS ---
        // 1. Get the current, accurate number of active leaves.
        int leafCount = plant.CellManager.GetActiveLeafCount();

        // 2. If there are no leaves, we gain no energy from photosynthesis. Period.
        if (leafCount <= 0)
        {
            return;
        }

        // --- ALL SUBSEQUENT CALCULATIONS ONLY RUN IF leafCount > 0 ---
        
        // 3. Get environmental modifiers
        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(plant) : 1.0f;
        float ticksPerSecond = TickManager.Instance?.Config?.ticksPerRealSecond ?? 2f;

        // 4. Get the base efficiency PER LEAF from the plant's calculated stats
        float efficiencyPerLeaf = plant.GrowthLogic.PhotosynthesisEfficiencyPerLeaf;

        // 5. Calculate firefly bonus. This is a flat bonus to the overall process.
        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null)
        {
            GridEntity plantGrid = plant.GetComponent<GridEntity>();
            if (plantGrid != null)
            {
                int radiusTiles = Mathf.CeilToInt(fireflyManagerInstance.photosynthesisRadius);
                int nearbyFlyCount = 0;
                var fireflies = Object.FindObjectsByType<FireflyController>(FindObjectsSortMode.None);
                foreach (var firefly in fireflies)
                {
                    GridEntity fireflyGrid = firefly.GetComponent<GridEntity>();
                    if (fireflyGrid != null &&
                        GridRadiusUtility.IsWithinCircleRadius(fireflyGrid.Position, plantGrid.Position, radiusTiles))
                    {
                        nearbyFlyCount++;
                    }
                }
                fireflyBonusRate = Mathf.Min(
                    nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                    fireflyManagerInstance.maxPhotosynthesisBonus
                );
            }
        }
        
        // 6. Calculate final energy gain for this tick
        float totalPhotosynthesisRatePerLeaf = (efficiencyPerLeaf * sunlight) + fireflyBonusRate;
        float energyThisTick = (totalPhotosynthesisRatePerLeaf * leafCount) / ticksPerSecond;
        float totalGain = energyThisTick * tileMultiplier;

        CurrentEnergy = Mathf.Clamp(CurrentEnergy + totalGain, 0f, MaxEnergy);
    }

    public void SpendEnergy(float amount)
    {
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
    }

    public void AddEnergy(float amount)
    {
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, MaxEnergy);
    }

    public bool HasEnergy(float amount)
    {
        return CurrentEnergy >= amount;
    }
}
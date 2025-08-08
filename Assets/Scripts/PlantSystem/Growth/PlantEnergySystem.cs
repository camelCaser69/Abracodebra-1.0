// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantEnergySystem.cs
using UnityEngine;
using WegoSystem;

public class PlantEnergySystem
{
    private readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; }
    public float MaxEnergy { get; set; }

    private readonly FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant)
    {
        this.plant = plant;
        this.fireflyManagerInstance = FireflyManager.Instance; // Initialize here
    }

    public void OnTickUpdate()
    {
        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;

        int leafCount = plant.CellManager.GetActiveLeafCount();
        if (leafCount <= 0) return;

        // Calculate sunlight contribution
        float sunlight = WeatherManager.Instance != null ? WeatherManager.Instance.sunIntensity : 0f;
        
        // Calculate firefly contribution
        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null)
        {
            // Simplified for now - assumes a global bonus, can be radius-based
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(plant.transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(
                nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus
            );
        }

        // Total photosynthesis rate per leaf
        // Note: The base efficiency is now part of the PlantGrowthLogic
        float totalPhotosynthesisRatePerLeaf = (plant.GrowthLogic.PhotosynthesisEfficiencyPerLeaf * sunlight) + fireflyBonusRate;
        float energyThisTick = totalPhotosynthesisRatePerLeaf * leafCount;

        CurrentEnergy = Mathf.Clamp(CurrentEnergy + energyThisTick, 0f, MaxEnergy);
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
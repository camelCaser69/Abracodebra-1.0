// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantEnergySystem.cs
using UnityEngine;
using Abracodabra.Genes;
using WegoSystem;

public class PlantEnergySystem
{
    private readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; }
    public float MaxEnergy { get; set; }
    public float BaseEnergyPerLeaf { get; set; } // Base rate from template

    // Cache the FireflyManager instance to avoid repeated singleton lookups
    private readonly FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant)
    {
        this.plant = plant;
        // Cache the reference once during construction. It's okay if it's null.
        this.fireflyManagerInstance = FireflyManager.Instance;
    }

    public void OnTickUpdate()
    {
        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;

        int leafCount = plant.CellManager.GetActiveLeafCount();
        if (leafCount <= 0) return;

        // Ensure WeatherManager exists before using it
        float sunlight = (WeatherManager.Instance != null) ? WeatherManager.Instance.sunIntensity : 1f;

        float fireflyBonusRate = 0f;
        // --- NULL-SAFETY CHECK ---
        // Only attempt to calculate the firefly bonus if the manager was found and is active.
        if (fireflyManagerInstance != null && fireflyManagerInstance.isActiveAndEnabled)
        {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(plant.transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(
                nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus
            );
        }

        float effectiveRate = BaseEnergyPerLeaf * plant.energyGenerationMultiplier;
        float totalPhotosynthesisRatePerLeaf = (effectiveRate * sunlight) + fireflyBonusRate;
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
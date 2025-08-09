// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantEnergySystem.cs

using Abracodabra.Genes;
using UnityEngine;
using WegoSystem;

public class PlantEnergySystem
{
    readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; }
    public float MaxEnergy { get; set; }
    public float BaseEnergyPerLeaf { get; set; } = 0.1f; // Base rate from template

    readonly FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant)
    {
        this.plant = plant;
        this.fireflyManagerInstance = FireflyManager.Instance;
    }

    public void OnTickUpdate()
    {
        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;

        int leafCount = plant.CellManager.GetActiveLeafCount();
        if (leafCount <= 0) return;

        float sunlight = WeatherManager.Instance != null ? WeatherManager.Instance.sunIntensity : 1f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null)
        {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(plant.transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(
                nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                fireflyManagerInstance.maxPhotosynthesisBonus
            );
        }

        // Use base rate from template/passives, modified by plant's multiplier
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
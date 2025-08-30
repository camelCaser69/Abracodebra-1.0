using Abracodabra.Genes;
using UnityEngine;
using WegoSystem;

public class PlantEnergySystem
{
    private readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; }
    public float MaxEnergy { get; set; }
    public float BaseEnergyPerLeaf { get; set; } // Base rate from template

    private readonly FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant)
    {
        this.plant = plant;
        this.fireflyManagerInstance = FireflyManager.Instance;
    }

    public void OnTickUpdate()
    {
        // Redundant safety check: Don't do anything if the plant is still growing (unless overridden).
        // The primary logic is in PlantGrowth, but this makes this class more robust.
        if (plant.CurrentState == PlantState.Growing && 
            plant.gameObject.GetComponent<PlantGrowth>()?.rechargeEnergyDuringGrowth == false)
        {
            return;
        }

        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;

        int leafCount = plant.CellManager.GetActiveLeafCount();
        if (leafCount <= 0) return;

        float sunlight = (WeatherManager.Instance != null) ? WeatherManager.Instance.sunIntensity : 1f;

        float fireflyBonusRate = 0f;
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

        CurrentEnergy = UnityEngine.Mathf.Clamp(CurrentEnergy + energyThisTick, 0f, MaxEnergy);
    }

    public void SpendEnergy(float amount)
    {
        CurrentEnergy = UnityEngine.Mathf.Max(0f, CurrentEnergy - amount);
    }

    public void AddEnergy(float amount)
    {
        CurrentEnergy = UnityEngine.Mathf.Clamp(CurrentEnergy + amount, 0f, MaxEnergy);
    }

    public bool HasEnergy(float amount)
    {
        return CurrentEnergy >= amount;
    }
}
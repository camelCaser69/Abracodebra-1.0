// Assets\Scripts\PlantSystem\Growth\PlantEnergySystem.cs
using System.Linq;
using UnityEngine;
using WegoSystem;

public class PlantEnergySystem
{
    private readonly PlantGrowth plant;

    public float CurrentEnergy { get; set; } = 0f;
    public float MaxEnergy { get; set; } = 10f;
    public float PhotosynthesisRate { get; set; } = 0.5f;

    FireflyManager fireflyManagerInstance;

    public PlantEnergySystem(PlantGrowth plant)
    {
        this.plant = plant;
        fireflyManagerInstance = FireflyManager.Instance;
    }

    public void AccumulateEnergy()
    {
        if (PhotosynthesisRate <= 0 || MaxEnergy <= 0) return;

        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = plant.CellManager.GetCells().Values.Count(c => c == PlantCellType.Leaf);
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(plant) : 1.0f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null)
        {
            int nearbyFlyCount = fireflyManagerInstance.GetNearbyFireflyCount(plant.transform.position, fireflyManagerInstance.photosynthesisRadius);
            fireflyBonusRate = Mathf.Min(nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly, fireflyManagerInstance.maxPhotosynthesisBonus);
        }

        float standardPhotosynthesis = PhotosynthesisRate * leafCount * sunlight;
        float totalRate = (standardPhotosynthesis + fireflyBonusRate) * tileMultiplier;
        float delta = totalRate * Time.deltaTime;
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + delta, 0f, MaxEnergy);
    }

    public void AccumulateEnergyTick()
    {
        if (PhotosynthesisRate <= 0 || MaxEnergy <= 0) return;

        float sunlight = WeatherManager.Instance ? WeatherManager.Instance.sunIntensity : 1f;
        int leafCount = plant.CellManager.GetCells().Values.Count(c => c == PlantCellType.Leaf);
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(plant) : 1.0f;

        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null)
        {
            GridEntity plantGrid = plant.GetComponent<GridEntity>();
            if (plantGrid != null)
            {
                int radiusTiles = Mathf.CeilToInt(fireflyManagerInstance.photosynthesisRadius);

                int nearbyFlyCount = 0;
                // Corrected: Use Object.FindObjectsOfType since this is not a MonoBehaviour
                var fireflies = Object.FindObjectsOfType<FireflyController>();

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

        float ticksPerSecond = TickManager.Instance?.Config?.ticksPerRealSecond ?? 2f;
        float standardPhotosynthesis = (PhotosynthesisRate * leafCount * sunlight) / ticksPerSecond;
        float totalRate = (standardPhotosynthesis + (fireflyBonusRate / ticksPerSecond)) * tileMultiplier;

        CurrentEnergy = Mathf.Clamp(CurrentEnergy + totalRate, 0f, MaxEnergy);
    }
}
using UnityEngine;
using WegoSystem;

public class PlantEnergySystem {
    readonly PlantGrowth plant;
    
    public float CurrentEnergy { get; set; } = 0f;
    public float MaxEnergy { get; set; } = 10f;
    public float PhotosynthesisRate { get; set; }
    
    FireflyManager fireflyManagerInstance;
    
    public PlantEnergySystem(PlantGrowth plant) {
        this.plant = plant;
        fireflyManagerInstance = FireflyManager.Instance;
    }
    
    public void AccumulateEnergyTick() {
        if (plant.GrowthLogic == null || MaxEnergy <= 0) return;
        
        int leafCount = plant.CellManager.GetActiveLeafCount();
        
        if (leafCount <= 0) {
            return;
        }
        
        // Get sunlight intensity
        float sunlight = 1f; // Default to full sunlight
        
        if (WeatherManager.Instance != null) {
            // Only use weather-based sunlight if the day/night cycle is enabled
            // AND we're not in planning phase (which pauses weather)
            if (WeatherManager.Instance.dayNightCycleEnabled && 
                !WeatherManager.Instance.IsPaused) {
                sunlight = WeatherManager.Instance.sunIntensity;
            }
        }
        
        float tileMultiplier = (PlantGrowthModifierManager.Instance != null) ?
            PlantGrowthModifierManager.Instance.GetEnergyRechargeMultiplier(plant) : 1.0f;
        float ticksPerSecond = TickManager.Instance?.Config?.ticksPerRealSecond ?? 2f;
        
        float efficiencyPerLeaf = plant.GrowthLogic.PhotosynthesisEfficiencyPerLeaf;
        
        // Firefly bonus calculation
        float fireflyBonusRate = 0f;
        if (fireflyManagerInstance != null) {
            GridEntity plantGrid = plant.GetComponent<GridEntity>();
            if (plantGrid != null) {
                int radiusTiles = Mathf.CeilToInt(fireflyManagerInstance.photosynthesisRadius);
                int nearbyFlyCount = 0;
                var fireflies = Object.FindObjectsByType<FireflyController>(FindObjectsSortMode.None);
                foreach (var firefly in fireflies) {
                    GridEntity fireflyGrid = firefly.GetComponent<GridEntity>();
                    if (fireflyGrid != null &&
                        GridRadiusUtility.IsWithinCircleRadius(fireflyGrid.Position, plantGrid.Position, radiusTiles)) {
                        nearbyFlyCount++;
                    }
                }
                fireflyBonusRate = Mathf.Min(
                    nearbyFlyCount * fireflyManagerInstance.photosynthesisIntensityPerFly,
                    fireflyManagerInstance.maxPhotosynthesisBonus
                );
            }
        }
        
        // Calculate total energy gain
        float totalPhotosynthesisRatePerLeaf = (efficiencyPerLeaf * sunlight) + fireflyBonusRate;
        float energyThisTick = (totalPhotosynthesisRatePerLeaf * leafCount) / ticksPerSecond;
        float totalGain = energyThisTick * tileMultiplier;
        
        // Debug logging to help track photosynthesis
        if (Debug.isDebugBuild && Time.frameCount % 60 == 0) { // Log every 60 frames
            Debug.Log($"[{plant.gameObject.name}] Photosynthesis: " +
                     $"Sunlight={sunlight:F2}, " +
                     $"Leaves={leafCount}, " +
                     $"FireflyBonus={fireflyBonusRate:F2}, " +
                     $"EnergyGain={totalGain:F2}/tick, " +
                     $"Current={CurrentEnergy:F1}/{MaxEnergy:F0}");
        }
        
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + totalGain, 0f, MaxEnergy);
    }
    
    public void SpendEnergy(float amount) {
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
    }
    
    public void AddEnergy(float amount) {
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, MaxEnergy);
    }
    
    public bool HasEnergy(float amount) {
        return CurrentEnergy >= amount;
    }
}
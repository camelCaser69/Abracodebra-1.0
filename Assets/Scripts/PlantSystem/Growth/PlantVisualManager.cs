// REWORKED FILE: Assets/Scripts/PlantSystem/Growth/PlantVisualManager.cs

using Abracodabra.Genes;
using UnityEngine;
using TMPro;

public class PlantVisualManager
{
    private readonly PlantGrowth plant;
    private readonly PlantShadowController shadowController;
    private readonly GameObject shadowPartPrefab;
    private readonly bool enableOutline;
    public PlantOutlineController OutlineController { get; private set; }
    private readonly GameObject outlinePartPrefab;
    private readonly TMP_Text energyText;
    
    // FIX: Removed the unused 'displayedGrowthPercentage' field.
    // private int displayedGrowthPercentage = -1; 

    public PlantVisualManager(PlantGrowth plant, PlantShadowController shadowController, GameObject shadowPartPrefab, PlantOutlineController outlineController, GameObject outlinePartPrefab, bool enableOutline)
    {
        this.plant = plant;
        this.shadowController = shadowController;
        this.shadowPartPrefab = shadowPartPrefab;
        this.OutlineController = outlineController;
        this.outlinePartPrefab = outlinePartPrefab;
        this.enableOutline = enableOutline;
        this.energyText = plant.GetComponentInChildren<TMP_Text>(true);
    }

    public void RegisterShadowForCell(GameObject cellInstance, string cellTypeName)
    {
        if (shadowController != null && shadowPartPrefab != null && cellInstance != null && cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        }
    }

    public void RegisterOutlineForCell(GameObject cellInstance, string cellTypeName)
    {
        if (enableOutline && OutlineController != null && cellInstance != null && cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            // The prefab is now passed from PlantGrowth to avoid null issues
            OutlineController.RegisterPlantPart(partRenderer, outlinePartPrefab);
        }
    }

    public void UpdateUI()
    {
        if (energyText == null) return;
        
        if (plant.CurrentState == PlantState.Growing || plant.CurrentState == PlantState.Initializing)
        {
            UpdateGrowthPercentageUI();
        }
        else
        {
            UpdateEnergyUI();
        }
    }

    private void UpdateGrowthPercentageUI()
    {
        energyText.text = "Growing...";
    }

    private void UpdateEnergyUI()
    {
        if (energyText == null || plant.EnergySystem == null) return;
        float currentEnergy = plant.EnergySystem.CurrentEnergy;
        float maxEnergy = plant.EnergySystem.MaxEnergy;
        energyText.text = $"{currentEnergy:F1}/{maxEnergy:F0}";
    }
    
    // This method is no longer needed as the variable it reset is gone.
    // public void ResetDisplayState() { ... }
}
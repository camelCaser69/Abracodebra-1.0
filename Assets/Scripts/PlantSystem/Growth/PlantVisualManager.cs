// REWORKED FILE: Assets/Scripts/PlantSystem/Growth/PlantVisualManager.cs
using UnityEngine;
using Abracodabra.Genes;
using TMPro;

public class PlantVisualManager
{
    private readonly PlantGrowth plant;
    private readonly PlantShadowController shadowController;
    private readonly GameObject shadowPartPrefab;
    private readonly bool enableOutline;
    public PlantOutlineController OutlineController { get; set; }
    private readonly GameObject outlinePartPrefab;
    private readonly TMP_Text energyText;

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
    
    /// <summary>
    /// Notifies the PlantShadowController to remove the shadow associated with a given plant cell.
    /// </summary>
    /// <param name="cellInstance">The plant cell's GameObject that is being destroyed.</param>
    public void UnregisterShadowForCell(GameObject cellInstance)
    {
        if (shadowController != null && cellInstance != null && cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            shadowController.UnregisterPlantPart(partRenderer);
        }
    }

    public void RegisterOutlineForCell(GameObject cellInstance, string cellTypeName)
    {
        if (enableOutline && OutlineController != null && cellInstance != null && cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
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
        if (energyText != null)
        {
            energyText.text = "Growing...";
        }
    }

    private void UpdateEnergyUI()
    {
        if (energyText == null || plant.EnergySystem == null) return;
        float currentEnergy = plant.EnergySystem.CurrentEnergy;
        float maxEnergy = plant.EnergySystem.MaxEnergy;
        energyText.text = $"{currentEnergy:F1}/{maxEnergy:F0}";
    }
}
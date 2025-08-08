// REWORKED FILE: Assets/Scripts/PlantSystem/Growth/PlantVisualManager.cs
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
    private readonly bool showGrowthPercentage; // This setting is now read once
    private int displayedGrowthPercentage = -1;

    public PlantVisualManager(PlantGrowth plant, PlantShadowController shadowController, GameObject shadowPartPrefab, PlantOutlineController outlineController, GameObject outlinePartPrefab, bool enableOutline)
    {
        this.plant = plant;
        this.shadowController = shadowController;
        this.shadowPartPrefab = shadowPartPrefab;
        this.OutlineController = outlineController;
        this.outlinePartPrefab = outlinePartPrefab;
        this.enableOutline = enableOutline;

        this.energyText = plant.GetComponentInChildren<TMP_Text>(true);
        this.showGrowthPercentage = true; // Hardcoded for now, can be exposed on PlantGrowth prefab
    }

    public void RegisterShadowForCell(GameObject cellInstance, string cellTypeName)
    {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return;

        if (cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        }
    }

    public void RegisterOutlineForCell(GameObject cellInstance, string cellTypeName)
    {
        if (!enableOutline || OutlineController == null || cellInstance == null) return;

        if (cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            OutlineController.RegisterPlantPart(partRenderer, outlinePartPrefab);
        }
    }

    public void UpdateUI()
    {
        if (energyText == null) return;
        
        // FIX: The property is on PlantGrowth, not this class
        if (showGrowthPercentage && (plant.CurrentState == PlantState.Growing || plant.CurrentState == PlantState.Initializing))
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
        if (energyText == null || !showGrowthPercentage) return;

        // FIX: Growth logic is now on PlantGrowth's GrowthLogic property
        // For now, we'll display a static "Growing..." text as the old logic was removed.
        // A new growth visualization system would need to be implemented.
        energyText.text = "Growing...";
    }

    private void UpdateEnergyUI()
    {
        if (energyText == null || plant.geneRuntimeState == null) return;

        // FIX: Energy values are now on the geneRuntimeState
        float currentEnergy = plant.geneRuntimeState.currentEnergy;
        float maxEnergy = plant.geneRuntimeState.maxEnergy;
        energyText.text = $"{currentEnergy:F1}/{maxEnergy:F0}";
    }

    public void ResetDisplayState()
    {
        displayedGrowthPercentage = -1;
    }
}
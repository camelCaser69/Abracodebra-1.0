using UnityEngine;
using TMPro;

public class PlantVisualManager
{
    // References to core components
    private readonly PlantGrowth plant;
    private readonly PlantShadowController shadowController;
    private readonly GameObject shadowPartPrefab;
    private readonly bool enableOutline;

    public PlantOutlineController OutlineController { get; set; }
    private readonly GameObject outlinePartPrefab;

    // UI and Display State
    private TMP_Text energyText;
    private bool showGrowthPercentage;
    private int displayedGrowthPercentage = -1;

    public PlantVisualManager(PlantGrowth plant, PlantShadowController shadowController, GameObject shadowPartPrefab, PlantOutlineController outlineController, GameObject outlinePartPrefab, bool enableOutline)
    {
        this.plant = plant;
        this.shadowController = shadowController;
        this.shadowPartPrefab = shadowPartPrefab;
        this.OutlineController = outlineController;
        this.outlinePartPrefab = outlinePartPrefab;
        this.enableOutline = enableOutline;

        EnsureUIReferences();
    }

    private void EnsureUIReferences()
    {
        if (energyText) return;

        energyText = plant.GetComponentInChildren<TMP_Text>(true);
        if (!energyText)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Energy Text (TMP_Text) UI reference not assigned and not found in children.", plant.gameObject);
        }

        // Read the setting from the PlantGrowth script
        showGrowthPercentage = plant.showGrowthPercentage;
    }

    public void RegisterShadowForCell(GameObject cellInstance, string cellTypeName)
    {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return;
        SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>();
        if (partRenderer != null)
        {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        }
        else
        {
            Debug.LogWarning($"Plant '{plant.gameObject.name}': {cellTypeName} missing SpriteRenderer. No shadow.", cellInstance);
        }
    }

    public void UpdateUI()
    {
        if (energyText == null) return;
        
        // This is the SINGLE point of control for what the UI shows.
        // If the plant is growing, show percentage. If not, show energy.
        if (showGrowthPercentage && (plant.CurrentState == PlantState.Growing || plant.CurrentState == PlantState.Initializing))
        {
             UpdateGrowthPercentageUI();
        }
        else // State is Mature_Idle, Mature_Executing, GrowthComplete, etc.
        {
            energyText.text = $"{Mathf.FloorToInt(plant.EnergySystem.CurrentEnergy)}/{Mathf.FloorToInt(plant.EnergySystem.MaxEnergy)}";
        }
    }
    
    // These methods are now just wrappers for the main UpdateUI call.
    public void UpdateWegoUI() => UpdateUI();
    public void UpdateRealtimeGrowthUI() => UpdateUI();

    public void UpdateGrowthPercentageUI()
    {
        if (!showGrowthPercentage || energyText == null) return;

        var logic = plant.GrowthLogic;
        float totalSteps = logic.GetTotalPlannedSteps();

        // If there are no steps, avoid division by zero. The state will soon change anyway.
        if (totalSteps <= 0)
        {
            return;
        }
        
        float completedSteps = logic.GetCurrentStemStage();
        
        // Calculate the percentage based purely on completed stages.
        float rawPercentageFloat = (completedSteps / totalSteps) * 100f;
        int targetDisplayValue = Mathf.FloorToInt(rawPercentageFloat);
        
        // Only update the UI text if the percentage value has actually changed.
        if (targetDisplayValue != displayedGrowthPercentage)
        {
            displayedGrowthPercentage = targetDisplayValue;
            energyText.text = $"{displayedGrowthPercentage}%";
        }
    }

    public void ResetDisplayState()
    {
        displayedGrowthPercentage = -1;
    }
}
using UnityEngine;
using TMPro;
using WegoSystem;

public class PlantVisualManager
{
    readonly PlantGrowth plant;
    readonly PlantShadowController shadowController;
    readonly GameObject shadowPartPrefab;
    readonly bool enableOutline;

    public PlantOutlineController OutlineController { get; set; }
    readonly GameObject outlinePartPrefab;

    TMP_Text energyText;
    bool showGrowthPercentage;
    int displayedGrowthPercentage = -1;

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

    void EnsureUIReferences()
    {
        if (energyText) return;

        energyText = plant.GetComponentInChildren<TMP_Text>(true);
        if (!energyText)
        {
            Debug.LogWarning($"[{plant.gameObject.name}] Energy Text (TMP_Text) UI reference not assigned and not found in children.", plant.gameObject);
        }

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

    public void RegisterOutlineForCell(GameObject cellInstance, string cellTypeName)
    {
        if (!enableOutline || OutlineController == null || cellInstance == null) return;

        SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>();
        if (partRenderer != null)
        {
            // Use the outline controller's own prefab reference
            OutlineController.RegisterPlantPart(partRenderer, OutlineController.outlinePartPrefab);
        }
        else
        {
            Debug.LogWarning($"Plant '{plant.gameObject.name}': {cellTypeName} missing SpriteRenderer. No outline.", cellInstance);
        }
    }

    public void UpdateUI()
    {
        if (energyText == null) return;

        if (showGrowthPercentage && (plant.CurrentState == PlantState.Growing || plant.CurrentState == PlantState.Initializing))
        {
            UpdateGrowthPercentageUI();
        }
        else // State is Mature_Idle, Mature_Executing, GrowthComplete, etc.
        {
            UpdateEnergyUI();
        }
    }

    public void UpdateGrowthPercentageUI()
    {
        if (energyText == null || !showGrowthPercentage) return;

        int currentPercentage = 0;
        if (plant.GrowthLogic != null)
        {
            currentPercentage = Mathf.RoundToInt(plant.GrowthLogic.GetGrowthProgress() * 100f);
        }

        if (currentPercentage != displayedGrowthPercentage)
        {
            displayedGrowthPercentage = currentPercentage;
            energyText.text = $"{displayedGrowthPercentage}%";
        }
    }

    public void UpdateEnergyUI()
    {
        if (energyText == null) return;

        float currentEnergy = plant.EnergySystem.CurrentEnergy;
        float maxEnergy = plant.EnergySystem.MaxEnergy;
        energyText.text = $"{currentEnergy:F1}/{maxEnergy:F0}";
    }

    public void UpdateWegoUI()
    {
        UpdateUI();
    }

    public void ResetDisplayState()
    {
        displayedGrowthPercentage = -1;
    }

    public void UpdateShadow()
    {
        if (shadowController == null) return;
        // Shadow parts are registered automatically when cells are created
    }

    public void UpdateOutline()
    {
        if (!enableOutline || OutlineController == null) return;
        // Outline parts are registered automatically when cells are created
    }

    public void SetEnergyTextVisibility(bool visible)
    {
        if (energyText != null)
        {
            energyText.gameObject.SetActive(visible);
        }
    }

    public void SetGrowthPercentageDisplay(bool enabled)
    {
        showGrowthPercentage = enabled;
        if (!enabled)
        {
            displayedGrowthPercentage = -1;
        }
    }

    public bool IsEnergyTextVisible()
    {
        return energyText != null && energyText.gameObject.activeInHierarchy;
    }

    public void ForceUIUpdate()
    {
        displayedGrowthPercentage = -1; // Force refresh
        UpdateUI();
    }
}
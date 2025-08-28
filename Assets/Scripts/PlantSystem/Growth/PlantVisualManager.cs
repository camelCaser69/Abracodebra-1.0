using UnityEngine;
using Abracodabra.Genes;
using TMPro;
using System.Linq;

public class PlantVisualManager
{
    private readonly PlantGrowth plant;
    private readonly PlantShadowController shadowController;
    private readonly GameObject shadowPartPrefab;
    private readonly bool enableOutline;
    public PlantOutlineController OutlineController { get; private set; }
    private readonly GameObject outlinePartPrefab;
    private TMP_Text energyText;

    public PlantVisualManager(PlantGrowth plant, PlantShadowController shadowController, 
        GameObject shadowPartPrefab, PlantOutlineController outlineController, 
        GameObject outlinePartPrefab, bool enableOutline)
    {
        this.plant = plant;
        this.shadowController = shadowController;
        this.shadowPartPrefab = shadowPartPrefab;
        this.OutlineController = outlineController;
        this.outlinePartPrefab = outlinePartPrefab;
        this.enableOutline = enableOutline;
        
        FindEnergyTextComponent();
    }
    
    private void FindEnergyTextComponent()
    {
        // Look for TMP_Text component in children
        energyText = plant.GetComponentInChildren<TMP_Text>(true);
        
        // If not found, create one
        if (energyText == null)
        {
            GameObject textObj = new GameObject("EnergyText");
            textObj.transform.SetParent(plant.transform);
            textObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            
            energyText = textObj.AddComponent<TextMeshPro>();
            energyText.text = "0/0";
            energyText.fontSize = 2;
            energyText.alignment = TextAlignmentOptions.Center;
            
            // Set sorting order through the renderer
            MeshRenderer textRenderer = energyText.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 100; // Ensure it renders on top
            }
        }
    }

    public void RegisterShadowForCell(GameObject cellInstance, string cellTypeName)
    {
        if (shadowController != null && shadowPartPrefab != null && cellInstance != null && 
            cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        }
    }

    public void UnregisterShadowForCell(GameObject cellInstance)
    {
        if (shadowController != null && cellInstance != null && 
            cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            shadowController.UnregisterPlantPart(partRenderer);
        }
    }

    public void RegisterOutlineForCell(GameObject cellInstance, string cellTypeName)
    {
        if (enableOutline && OutlineController != null && cellInstance != null && 
            cellInstance.TryGetComponent<SpriteRenderer>(out var partRenderer))
        {
            OutlineController.RegisterPlantPart(partRenderer, outlinePartPrefab);
        }
    }

    public void UpdateUI()
    {
        if (energyText == null)
        {
            FindEnergyTextComponent();
            if (energyText == null) return;
        }

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
            int currentHeight = plant.CellManager.cells.Count(c => c.Value == PlantCellType.Stem);
            int maxHeight = plant.maxHeight;
            float percentage = (maxHeight > 0) ? (currentHeight / (float)maxHeight * 100f) : 0f;
            energyText.text = $"Growing {percentage:F0}%";
        }
    }

    private void UpdateEnergyUI()
    {
        if (energyText == null || plant.EnergySystem == null) return;
        
        float currentEnergy = plant.EnergySystem.CurrentEnergy;
        float maxEnergy = plant.EnergySystem.MaxEnergy;
        energyText.text = $"{currentEnergy:F0}/{maxEnergy:F0}";
    }
}
using UnityEngine;
using TMPro;

public class PlantVisualManager {
    readonly PlantGrowth plant;
    readonly PlantShadowController shadowController;
    readonly GameObject shadowPartPrefab;
    readonly bool enableOutline;
    
    public PlantOutlineController OutlineController { get; private set; }
    readonly GameObject outlinePartPrefab;
    
    // UI References
    TMP_Text energyText;
    public bool ShowGrowthPercentage { get; set; } = true;
    public bool ContinuousIncrement { get; set; } = false;
    
    [SerializeField] int percentageIncrement = 5;
    int displayedGrowthPercentage = -1;
    
    public PlantVisualManager(PlantGrowth plant, PlantShadowController shadowController, GameObject shadowPartPrefab, 
        PlantOutlineController outlineController, GameObject outlinePartPrefab, bool enableOutline) {
        this.plant = plant;
        this.shadowController = shadowController;
        this.shadowPartPrefab = shadowPartPrefab;
        this.OutlineController = outlineController;
        this.outlinePartPrefab = outlinePartPrefab;
        this.enableOutline = enableOutline;
        
        EnsureUIReferences();
    }
    
    void EnsureUIReferences() {
        if (energyText) return;
        
        // Try to find from serialized field first (will be null for now since we can't serialize in non-MonoBehaviour)
        // Then search in children
        energyText = plant.GetComponentInChildren<TMP_Text>(true);
        if (!energyText) {
            Debug.LogWarning($"[{plant.gameObject.name}] Energy Text (TMP_Text) UI reference not assigned and not found in children.", plant.gameObject);
        }
        
        // Try to read serialized values from the original PlantGrowth component
        var plantType = plant.GetType();
        var showGrowthField = plantType.GetField("showGrowthPercentage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (showGrowthField != null) ShowGrowthPercentage = (bool)showGrowthField.GetValue(plant);
        
        var percentIncrementField = plantType.GetField("percentageIncrement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (percentIncrementField != null) percentageIncrement = (int)percentIncrementField.GetValue(plant);
        
        var continuousField = plantType.GetField("continuousIncrement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (continuousField != null) ContinuousIncrement = (bool)continuousField.GetValue(plant);
        
        var energyTextField = plantType.GetField("energyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (energyTextField != null) energyText = (TMP_Text)energyTextField.GetValue(plant);
    }
    
    public void RegisterShadowForCell(GameObject cellInstance, string cellTypeName) {
        if (shadowController == null || shadowPartPrefab == null || cellInstance == null) return;
        SpriteRenderer partRenderer = cellInstance.GetComponentInChildren<SpriteRenderer>();
        if (partRenderer != null) {
            shadowController.RegisterPlantPart(partRenderer, shadowPartPrefab);
        } else {
            Debug.LogWarning($"Plant '{plant.gameObject.name}': {cellTypeName} missing SpriteRenderer. No shadow.", cellInstance);
        }
    }
    
    public void UpdateUI() {
        if (energyText == null) return;
        if (ShowGrowthPercentage && (plant.CurrentState == PlantState.Growing || 
            (plant.CurrentState == PlantState.GrowthComplete && plant.GrowthLogic.GetStepsCompleted() > 0))) {
            // Growth percentage is being shown
        } else {
            energyText.text = $"{Mathf.FloorToInt(plant.EnergySystem.CurrentEnergy)}/{Mathf.FloorToInt(plant.EnergySystem.MaxEnergy)}";
        }
    }
    
    public void UpdateWegoUI() {
        if (ShowGrowthPercentage && plant.CurrentState == PlantState.Growing) {
            UpdateGrowthPercentageUI();
        }
    }
    
    public void UpdateRealtimeGrowthUI() {
        if (ShowGrowthPercentage && ContinuousIncrement) {
            UpdateGrowthPercentageUI();
        }
    }
    
    public void UpdateGrowthPercentageUI(bool forceComplete = false) {
        if (!ShowGrowthPercentage || energyText == null) return;
        
        float rawPercentageFloat;
        var logic = plant.GrowthLogic;
        
        if (forceComplete || plant.CurrentState == PlantState.GrowthComplete) {
            rawPercentageFloat = 100f;
        } else if (plant.GetType().GetField("useWegoSystem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(plant) is bool useWego && useWego) {
            if (logic.TargetStemLength <= 0) {
                rawPercentageFloat = 100f;
            } else {
                float partialStepProgress = logic.GetGrowthProgress() * (100f / logic.TargetStemLength);
                rawPercentageFloat = ((float)logic.GetCurrentStemStage() / logic.TargetStemLength) * 100f + partialStepProgress;
            }
        } else if (ContinuousIncrement) {
            if (logic.GetTotalPlannedSteps() > 0) {
                rawPercentageFloat = ((float)logic.GetStepsCompleted() / logic.GetTotalPlannedSteps()) * 100f;
                if (logic.GetActualGrowthProgress() > 0f && logic.GetStepsCompleted() < logic.GetTotalPlannedSteps()) {
                    float stepSize = 100f / logic.GetTotalPlannedSteps();
                    float partialStepProgress = logic.GetActualGrowthProgress() * stepSize;
                    rawPercentageFloat = (logic.GetStepsCompleted() * stepSize) + partialStepProgress;
                }
            } else {
                rawPercentageFloat = (plant.CurrentState == PlantState.Growing) ? 0f : 100f;
            }
        } else {
            if (logic.TargetStemLength <= 0) {
                rawPercentageFloat = 100f;
            } else {
                rawPercentageFloat = Mathf.Clamp(((float)logic.GetCurrentStemCount() / logic.TargetStemLength) * 100f, 0f, 100f);
            }
        }
        
        rawPercentageFloat = Mathf.Clamp(rawPercentageFloat, 0f, 100f);
        int targetDisplayValue;
        if (percentageIncrement <= 1) {
            targetDisplayValue = Mathf.FloorToInt(rawPercentageFloat);
        } else {
            targetDisplayValue = Mathf.RoundToInt(rawPercentageFloat / percentageIncrement) * percentageIncrement;
        }
        targetDisplayValue = Mathf.Min(targetDisplayValue, 100);
        
        if (targetDisplayValue == 100 && plant.CurrentState == PlantState.Growing && !forceComplete) {
            targetDisplayValue = 99;
        }
        
        if (targetDisplayValue != displayedGrowthPercentage) {
            displayedGrowthPercentage = targetDisplayValue;
            energyText.text = $"{displayedGrowthPercentage}%";
        }
    }
    
    public void ResetDisplayState() {
        displayedGrowthPercentage = -1;
    }
}
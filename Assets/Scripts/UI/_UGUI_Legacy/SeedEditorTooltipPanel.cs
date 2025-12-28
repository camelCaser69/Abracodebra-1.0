using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.UI.Tooltips
{
    public class SeedEditorTooltipPanel : MonoBehaviour
    {
        public static SeedEditorTooltipPanel Instance { get; private set; }

        #region UI References
        [Header("Main Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Header")]
        [SerializeField] private Image seedIcon;
        [SerializeField] private TextMeshProUGUI seedNameText;
        [SerializeField] private TextMeshProUGUI qualityText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Performance Overview")]
        [SerializeField] private TextMeshProUGUI maturityTimeText;
        [SerializeField] private TextMeshProUGUI energyBalanceText;
        [SerializeField] private TextMeshProUGUI yieldText;
        
        [Header("Attribute Breakdown")]
        [SerializeField] private StatBarUI growthSpeedBar;
        [SerializeField] private StatBarUI energyStorageBar;
        [SerializeField] private StatBarUI energyGenerationBar;
        [SerializeField] private StatBarUI fruitYieldBar;
        [SerializeField] private StatBarUI defenseBar;

        [Header("Sequence Analysis")]
        [SerializeField] private TextMeshProUGUI cycleTimeText;
        [SerializeField] private Transform sequenceBreakdownContainer;
        [SerializeField] private GameObject sequenceBreakdownEntryPrefab;
        
        [Header("Synergies & Warnings")]
        [SerializeField] private Transform synergiesContainer;
        [SerializeField] private Transform warningsContainer;
        [SerializeField] private GameObject synergyWarningEntryPrefab;

        private List<GameObject> pooledSequenceEntries = new List<GameObject>();
        private List<GameObject> pooledSynergyEntries = new List<GameObject>();
        private List<GameObject> pooledWarningEntries = new List<GameObject>();
        #endregion

        [System.Serializable]
        public class StatBarUI
        {
            public TextMeshProUGUI labelText;
            public Slider slider;
            public TextMeshProUGUI valueText;
            public Image fillImage;
            public Gradient colorGradient;

            public void UpdateBar(string label, float value, float baseline, float maxValue = 2f, bool higherIsBetter = true, string format = "P0")
            {
                if (labelText != null) labelText.text = label;
                if (slider != null) slider.value = Mathf.Clamp01((value - 0.5f) / (maxValue - 0.5f)); // Normalize from 0.5 to max
                if (fillImage != null && colorGradient != null) fillImage.color = colorGradient.Evaluate(slider.value);
                
                if (valueText != null)
                {
                    string formattedValue = (format == "P0") ? $"{value:P0}" : $"{value:F0}";
                    string baseFormatted = (format == "P0") ? $"{baseline:P0}" : $"{baseline:F0}";
                    valueText.text = $"{TooltipFormatting.ColorizeValue(value, baseline, formattedValue, higherIsBetter)} <size=70%>(Base: {baseFormatted})</size>";
                }
            }
        }
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if(panelRoot != null) panelRoot.SetActive(false);
        }

        public void LoadSeedForAnalysis(PlantGeneRuntimeState runtimeState)
        {
            if (runtimeState == null || runtimeState.template == null)
            {
                if(panelRoot != null) panelRoot.SetActive(false);
                return;
            }

            var data = SeedTooltipData.CreateFromSeed(runtimeState.template, runtimeState);
            if (data == null)
            {
                if(panelRoot != null) panelRoot.SetActive(false);
                return;
            }
            
            DisplayAnalysis(data, runtimeState.template);
            if(panelRoot != null) panelRoot.SetActive(true);
        }

        private void DisplayAnalysis(SeedTooltipData data, SeedTemplate template)
        {
            // Header
            seedIcon.sprite = template.icon;
            seedNameText.text = data.seedName;
            qualityText.text = SeedQualityCalculator.GetQualityDescription(data.qualityTier);
            qualityText.color = SeedQualityCalculator.GetQualityColor(data.qualityTier);
            descriptionText.text = $"<i>{template.description}</i>";

            // Performance
            maturityTimeText.text = TooltipFormatting.ColorizeValue(data.estimatedMaturityTicks, 40, $"{data.estimatedMaturityTicks:F0} ticks", false);
            energyBalanceText.text = TooltipFormatting.ColorizeValue(data.energySurplusPerCycle, 0, $"{data.energySurplusPerCycle:F1} E/cycle");
            yieldText.text = data.primaryYieldSummary;

            // Attributes
            growthSpeedBar.UpdateBar("Growth", data.growthSpeedMultiplier, 1f, 2.5f, true, "P0");
            energyStorageBar.UpdateBar("Storage", data.energyStorageMultiplier, 1f, 2.5f, true, "P0");
            energyGenerationBar.UpdateBar("Generation", data.energyGenerationMultiplier, 1f, 2.5f, true, "P0");
            fruitYieldBar.UpdateBar("Yield", data.fruitYieldMultiplier, 1f, 2.5f, true, "P0");
            defenseBar.UpdateBar("Defense", data.defenseMultiplier, 1f, 2.5f, true, "P0");
            
            // Sequence
            cycleTimeText.text = $"<b>{data.totalCycleTime} ticks</b> (Cycle Time)";
            UpdateList(sequenceBreakdownContainer, pooledSequenceEntries, data.sequenceSlots.Count, (go, i) => {
                var entryText = go.GetComponent<TextMeshProUGUI>();
                var slotData = data.sequenceSlots[i];
                entryText.text = $"{slotData.actionName}: {slotData.baseCost:F0}E → {TooltipFormatting.ColorizeValue(slotData.modifiedCost, slotData.baseCost, $"{slotData.modifiedCost:F0}E", false)}";
            }, sequenceBreakdownEntryPrefab);

            // Synergies & Warnings
            UpdateList(synergiesContainer, pooledSynergyEntries, data.synergies.Count, (go, i) => {
                go.GetComponent<TextMeshProUGUI>().text = $"<color=#88FF88>✓</color> {data.synergies[i]}";
            }, synergyWarningEntryPrefab);
            
            UpdateList(warningsContainer, pooledWarningEntries, data.warnings.Count, (go, i) => {
                go.GetComponent<TextMeshProUGUI>().text = $"<color=#FF8888>⚠</color> {data.warnings[i]}";
            }, synergyWarningEntryPrefab);
        }
        
        private void UpdateList(Transform container, List<GameObject> pool, int count, System.Action<GameObject, int> updateAction, GameObject prefab)
        {
            // Ensure pool is large enough
            while (pool.Count < count)
            {
                var newEntry = Instantiate(prefab, container);
                pool.Add(newEntry);
            }

            // Activate and update required entries
            for (int i = 0; i < count; i++)
            {
                pool[i].SetActive(true);
                updateAction(pool[i], i);
            }
            
            // Deactivate unused entries
            for (int i = count; i < pool.Count; i++)
            {
                pool[i].SetActive(false);
            }
        }
    }
}
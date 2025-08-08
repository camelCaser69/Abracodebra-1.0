// REWORKED FILE: Assets/Scripts/UI/Genes/GeneSequenceUI.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes; // FIX: Added missing using statement for PlantSequenceExecutor

namespace Abracodabra.UI.Genes
{
    // ... (rest of the file is identical to the one I sent previously)
    public class GeneSequenceUI : MonoBehaviour
    {
        [Header("UI Structure")]
        public Transform passiveGenesContainer;
        public Transform activeSequenceContainer;
        public GameObject sequenceRowPrefab;
        public GameObject passiveSlotPrefab;

        [Header("Display Elements")]
        public TMPro.TextMeshProUGUI energyCostText;
        public TMPro.TextMeshProUGUI currentEnergyText;
        public TMPro.TextMeshProUGUI rechargeTimeText;
        public Slider rechargeProgress;
        public TMPro.TextMeshProUGUI validationMessage;

        [Header("Configuration")]
        public int maxPassiveSlots = 6;
        public int maxSequenceLength = 5;

        // Runtime
        private PlantGeneRuntimeState runtimeState;
        private List<GeneSlotUI> passiveSlots = new List<GeneSlotUI>();
        private List<SequenceRowUI> sequenceRows = new List<SequenceRowUI>();
        private PlantSequenceExecutor executor;

        void Start()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // Create passive slots
            for (int i = 0; i < maxPassiveSlots; i++)
            {
                GameObject slotObj = Instantiate(passiveSlotPrefab, passiveGenesContainer);
                GeneSlotUI slot = slotObj.GetComponent<GeneSlotUI>();
                slot.acceptedCategory = GeneCategory.Passive;
                slot.slotIndex = i;
                passiveSlots.Add(slot);
            }

            // Create sequence rows
            for (int i = 0; i < maxSequenceLength; i++)
            {
                GameObject rowObj = Instantiate(sequenceRowPrefab, activeSequenceContainer);
                SequenceRowUI row = rowObj.GetComponent<SequenceRowUI>();
                row.Initialize(i, this);
                sequenceRows.Add(row);
            }
        }

        public void LoadRuntimeState(PlantGeneRuntimeState state)
        {
            runtimeState = state;
            if (state == null) return;

            // Load passive genes
            for (int i = 0; i < passiveSlots.Count; i++)
            {
                if (i < state.passiveInstances.Count)
                    passiveSlots[i].SetGeneInstance(state.passiveInstances[i]);
                else
                    passiveSlots[i].ClearSlot();
            }

            // Load active sequence
            for (int i = 0; i < sequenceRows.Count; i++)
            {
                if (i < state.activeSequence.Count)
                    sequenceRows[i].LoadSlot(state.activeSequence[i]);
                else
                    sequenceRows[i].ClearRow();
            }

            UpdateDisplay();
        }

        public void OnActiveGeneChanged(int rowIndex, ActiveGene gene)
        {
            if (rowIndex >= 0 && rowIndex < sequenceRows.Count)
            {
                sequenceRows[rowIndex].UpdateAttachmentSlots(gene);
            }
            UpdateDisplay();
        }

        public ActiveGene GetActiveGeneForRow(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < sequenceRows.Count)
            {
                return sequenceRows[rowIndex].GetActiveGene();
            }
            return null;
        }

        void UpdateDisplay()
        {
            if (runtimeState == null) return;

            float totalCost = runtimeState.CalculateTotalEnergyCost();
            if(energyCostText != null) energyCostText.text = $"Cost: {totalCost:F0}⚡/cycle";
            if(currentEnergyText != null) currentEnergyText.text = $"Energy: {runtimeState.currentEnergy:F0}/{runtimeState.maxEnergy:F0}";
            if(rechargeTimeText != null) rechargeTimeText.text = $"Recharge: {runtimeState.template.baseRechargeTime} ticks";

            bool isValid = ValidateConfiguration();
            if(validationMessage != null) validationMessage.gameObject.SetActive(!isValid);
        }

        bool ValidateConfiguration()
        {
            if (runtimeState == null) return false;
            
            bool hasActiveGene = false;
            foreach (var slot in runtimeState.activeSequence)
            {
                if (slot.HasContent)
                {
                    hasActiveGene = true;
                    break;
                }
            }

            if (!hasActiveGene)
            {
                if (validationMessage != null) validationMessage.text = "Sequence requires at least one Active Gene.";
                return false;
            }

            return true;
        }

        public void ConnectToExecutor(PlantSequenceExecutor exec)
        {
            executor = exec;
        }

        void Update()
        {
            if (executor != null && runtimeState != null && runtimeState.template != null)
            {
                // Update recharge progress
                if (rechargeProgress != null && runtimeState.template.baseRechargeTime > 0)
                {
                    float progress = 1f - (runtimeState.rechargeTicksRemaining / (float)runtimeState.template.baseRechargeTime);
                    rechargeProgress.value = progress;
                }

                if (currentEnergyText != null)
                {
                    currentEnergyText.text = $"Energy: {runtimeState.currentEnergy:F0}/{runtimeState.maxEnergy:F0}";
                }
            }
        }
    }
}
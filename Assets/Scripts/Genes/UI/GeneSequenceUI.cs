using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes;

namespace Abracodabra.UI.Genes
{
    public class GeneSequenceUI : MonoBehaviour
    {
        [Header("Containers")]
        public Transform passiveGenesContainer;
        public Transform activeSequenceContainer;

        [Header("Prefabs")]
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

        // --- State ---
        private PlantGeneRuntimeState runtimeState;
        private List<GeneSlotUI> passiveSlots = new List<GeneSlotUI>();
        private List<SequenceRowUI> sequenceRows = new List<SequenceRowUI>();
        private PlantSequenceExecutor executor;

        void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Clear existing
            foreach (Transform child in passiveGenesContainer) Destroy(child.gameObject);
            foreach (Transform child in activeSequenceContainer) Destroy(child.gameObject);
            passiveSlots.Clear();
            sequenceRows.Clear();

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
            this.runtimeState = state;
            if (state == null)
            {
                Debug.LogError("Cannot load null runtime state into GeneSequenceUI");
                return;
            }

            RefreshAllVisuals();
        }
        
        public void UpdateGeneInSequence(int rowIndex, GeneCategory slotCategory, InventoryBarItem newItem)
        {
            if (runtimeState == null || rowIndex < 0 || rowIndex >= runtimeState.activeSequence.Count) return;

            RuntimeSequenceSlot sequenceSlot = runtimeState.activeSequence[rowIndex];
            RuntimeGeneInstance newInstance = newItem?.GeneInstance;

            switch (slotCategory)
            {
                case GeneCategory.Active:
                    sequenceSlot.activeInstance = newInstance;
                    break;
                case GeneCategory.Modifier:
                    if (sequenceSlot.modifierInstances.Count > 0)
                        sequenceSlot.modifierInstances[0] = newInstance;
                    else if (newInstance != null)
                        sequenceSlot.modifierInstances.Add(newInstance);
                    break;
                case GeneCategory.Payload:
                    if (sequenceSlot.payloadInstances.Count > 0)
                        sequenceSlot.payloadInstances[0] = newInstance;
                    else if (newInstance != null)
                        sequenceSlot.payloadInstances.Add(newInstance);
                    break;
            }
            
            RefreshAllVisuals();
        }

        public ActiveGene GetActiveGeneForRow(int rowIndex)
        {
            if (runtimeState != null && rowIndex >= 0 && rowIndex < runtimeState.activeSequence.Count)
            {
                return runtimeState.activeSequence[rowIndex].activeInstance?.GetGene<ActiveGene>();
            }
            return null;
        }

        private void RefreshAllVisuals()
        {
            if (runtimeState == null) return;

            for (int i = 0; i < passiveSlots.Count; i++)
            {
                if (i < runtimeState.passiveInstances.Count)
                {
                    var instance = runtimeState.passiveInstances[i];
                    if (instance != null && instance.GetGene() != null)
                    {
                        passiveSlots[i].SetItem(InventoryBarItem.FromGene(instance));
                    }
                    else
                    {
                        passiveSlots[i].ClearSlot();
                    }
                }
                else
                {
                    passiveSlots[i].ClearSlot();
                }
            }

            for (int i = 0; i < sequenceRows.Count; i++)
            {
                if (i < runtimeState.activeSequence.Count)
                {
                    sequenceRows[i].LoadSlot(runtimeState.activeSequence[i]);
                }
                else
                {
                    sequenceRows[i].ClearRow();
                }
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (runtimeState == null) return;

            float totalCost = runtimeState.CalculateTotalEnergyCost();
            if(energyCostText != null) energyCostText.text = $"Cost: {totalCost:F0}⚡/cycle";
            if(rechargeTimeText != null) rechargeTimeText.text = $"Recharge: {runtimeState.template.baseRechargeTime} ticks";
            
            // FIX: Read energy values from the authoritative source, the EnergySystem, via the executor.
            if (executor != null && executor.plantGrowth != null && executor.plantGrowth.EnergySystem != null)
            {
                var energySystem = executor.plantGrowth.EnergySystem;
                if(currentEnergyText != null) currentEnergyText.text = $"Energy: {energySystem.CurrentEnergy:F0}/{energySystem.MaxEnergy:F0}";
            }
            else if (runtimeState.template != null)
            {
                // Fallback for when the executor isn't connected yet (e.g., in planning phase)
                if(currentEnergyText != null) currentEnergyText.text = $"Energy: --/{runtimeState.template.maxEnergy:F0}";
            }

            bool isValid = ValidateConfiguration();
            if(validationMessage != null) validationMessage.gameObject.SetActive(!isValid);
        }

        private bool ValidateConfiguration()
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
            if (executor != null && executor.plantGrowth != null && executor.plantGrowth.EnergySystem != null && runtimeState != null)
            {
                var energySystem = executor.plantGrowth.EnergySystem;

                if (rechargeProgress != null && runtimeState.template.baseRechargeTime > 0)
                {
                    float progress = 1f - (runtimeState.rechargeTicksRemaining / (float)runtimeState.template.baseRechargeTime);
                    rechargeProgress.value = progress;
                }

                if (currentEnergyText != null)
                {
                    // FIX: Read from the authoritative source during Update as well.
                    currentEnergyText.text = $"Energy: {energySystem.CurrentEnergy:F0}/{energySystem.MaxEnergy:F0}";
                }
            }
        }
    }
}
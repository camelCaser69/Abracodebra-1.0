﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using Abracodabra.Genes;
using Abracodabra.UI.Tooltips; // NEW

namespace Abracodabra.UI.Genes
{
    public class GeneSequenceUI : MonoBehaviour
    {
        #region Fields
        public Transform passiveGenesContainer;
        public Transform activeSequenceContainer;
        [SerializeField] private GeneSlotUI seedEditSlot;

        public GameObject sequenceRowPrefab;
        public GameObject passiveSlotPrefab;

        public TMPro.TextMeshProUGUI energyCostText;
        public TMPro.TextMeshProUGUI currentEnergyText;
        public TMPro.TextMeshProUGUI rechargeTimeText;
        public TMPro.TextMeshProUGUI validationMessage;
        public Slider rechargeProgress;

        public int maxPassiveSlots = 6;
        public int maxSequenceLength = 5;

        private PlantGeneRuntimeState runtimeState;
        private List<GeneSlotUI> passiveSlots = new List<GeneSlotUI>();
        private List<SequenceRowUI> sequenceRows = new List<SequenceRowUI>();
        private PlantSequenceExecutor executor;
        #endregion

        void Start()
        {
            ClearEditor();
        }

        public void LoadSeedForEditing(InventoryBarItem seedItem)
        {
            if (seedItem == null || seedItem.Type != InventoryBarItem.ItemType.Seed)
            {
                ClearEditor();
                return;
            }

            this.runtimeState = seedItem.SeedRuntimeState;
            if (seedEditSlot != null)
            {
                seedEditSlot.SetItem(seedItem);
            }

            GenerateSlotsFromState();
            SetEditorLocked(false);
            RefreshAllVisuals();
        }

        private void ClearEditor()
        {
            this.runtimeState = null;
            if (seedEditSlot != null)
            {
                seedEditSlot.ClearSlot();
            }

            foreach (var slot in passiveSlots) if (slot != null) Destroy(slot.gameObject);
            foreach (var row in sequenceRows) if (row != null) Destroy(row.gameObject);
            passiveSlots.Clear();
            sequenceRows.Clear();

            SetEditorLocked(true);
            RefreshAllVisuals();
        }

        public void CleanupOnPhaseEnd()
        {
            if (seedEditSlot != null && seedEditSlot.CurrentItem != null)
            {
                var itemToReturn = seedEditSlot.CurrentItem;
                if (InventoryGridController.Instance != null)
                {
                    bool success = InventoryGridController.Instance.AddItemToInventory(itemToReturn);
                    if (!success)
                    {
                        Debug.LogWarning($"Could not return seed '{itemToReturn.GetDisplayName()}' to inventory on phase end, inventory may be full.");
                    }
                }
            }
            ClearEditor();
        }

        private void GenerateSlotsFromState()
        {
            foreach (var slot in passiveSlots) if (slot != null) Destroy(slot.gameObject);
            foreach (var row in sequenceRows) if (row != null) Destroy(row.gameObject);
            passiveSlots.Clear();
            sequenceRows.Clear();

            if (runtimeState == null) return;

            for (int i = 0; i < runtimeState.template.passiveSlotCount; i++)
            {
                GameObject slotObj = Instantiate(passiveSlotPrefab, passiveGenesContainer);
                slotObj.SetActive(true);
                GeneSlotUI slot = slotObj.GetComponent<GeneSlotUI>();
                slot.acceptedCategory = GeneCategory.Passive;
                slot.slotIndex = i;
                passiveSlots.Add(slot);
            }

            for (int i = 0; i < runtimeState.template.activeSequenceLength; i++)
            {
                GameObject rowObj = Instantiate(sequenceRowPrefab, activeSequenceContainer);
                rowObj.SetActive(true);
                SequenceRowUI row = rowObj.GetComponent<SequenceRowUI>();
                row.Initialize(i, this);
                sequenceRows.Add(row);
            }
        }

        private void SetEditorLocked(bool isLocked)
        {
            foreach (var slot in passiveSlots) slot.isLocked = isLocked;
            foreach (var row in sequenceRows)
            {
                if (row.activeSlot != null) row.activeSlot.isLocked = isLocked;
            }
        }

        public void UpdateDataForSlot(int slotIndex, GeneCategory slotCategory, InventoryBarItem newItem)
        {
            if (runtimeState == null) return;
            var newInstance = newItem?.GeneInstance;

            if (slotCategory == GeneCategory.Passive)
            {
                if (slotIndex < 0) return;
                while (runtimeState.passiveInstances.Count <= slotIndex) runtimeState.passiveInstances.Add(null);
                runtimeState.passiveInstances[slotIndex] = newInstance;
            }
            else
            {
                if (slotIndex < 0 || slotIndex >= runtimeState.activeSequence.Count) return;
                RuntimeSequenceSlot sequenceSlot = runtimeState.activeSequence[slotIndex];

                switch (slotCategory)
                {
                    case GeneCategory.Active:
                        sequenceSlot.activeInstance = newInstance;
                        break;
                    case GeneCategory.Modifier:
                        if (sequenceSlot.modifierInstances.Count == 0) sequenceSlot.modifierInstances.Add(newInstance);
                        else sequenceSlot.modifierInstances[0] = newInstance;
                        break;
                    case GeneCategory.Payload:
                        if (sequenceSlot.payloadInstances.Count == 0) sequenceSlot.payloadInstances.Add(newInstance);
                        else sequenceSlot.payloadInstances[0] = newInstance;
                        break;
                }
            }
            RefreshAllVisuals();
        }

        public ActiveGene GetActiveGeneForRow(int rowIndex)
        {
            if (runtimeState != null && rowIndex >= 0 && rowIndex < runtimeState.activeSequence.Count)
            {
                return runtimeState.activeSequence[rowIndex]?.activeInstance?.GetGene<ActiveGene>();
            }
            return null;
        }

        private void RefreshAllVisuals()
        {
            // Existing logic to update the main editor UI...
            if (runtimeState == null)
            {
                UpdateDisplay();
                // NEW: Update tooltip panel
                if (SeedEditorTooltipPanel.Instance != null)
                {
                    SeedEditorTooltipPanel.Instance.LoadSeedForAnalysis(null);
                }
                return;
            }

            for (int i = 0; i < passiveSlots.Count; i++)
            {
                if (i < runtimeState.passiveInstances.Count)
                {
                    var instance = runtimeState.passiveInstances[i];
                    passiveSlots[i].SetItem(instance != null && instance.GetGene() != null ? InventoryBarItem.FromGene(instance) : null);
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
            
            // NEW: Update tooltip panel
            if (SeedEditorTooltipPanel.Instance != null)
            {
                SeedEditorTooltipPanel.Instance.LoadSeedForAnalysis(runtimeState);
            }
        }

        private void UpdateDisplay()
        {
            if (runtimeState == null)
            {
                if (energyCostText != null) energyCostText.text = "Cost: --";
                if (currentEnergyText != null) currentEnergyText.text = "Energy: --/--";
                if (rechargeTimeText != null) rechargeTimeText.text = "Recharge: --";
                if (validationMessage != null)
                {
                    validationMessage.gameObject.SetActive(true);
                    validationMessage.text = "Drop a seed into the slot above to begin editing.";
                }
                return;
            }

            float totalCost = runtimeState.CalculateTotalEnergyCost();
            if (energyCostText != null) energyCostText.text = $"Cost: {totalCost:F0} E/cycle";
            if (rechargeTimeText != null) rechargeTimeText.text = $"Recharge: {runtimeState.template.baseRechargeTime} ticks";
            if (currentEnergyText != null) currentEnergyText.text = $"Energy: --/{runtimeState.template.maxEnergy:F0}";

            bool isValid = ValidateConfiguration();
            if (validationMessage != null) validationMessage.gameObject.SetActive(!isValid);
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
                    currentEnergyText.text = $"Energy: {energySystem.CurrentEnergy:F0}/{energySystem.MaxEnergy:F0}";
                }
            }
        }
    }
}
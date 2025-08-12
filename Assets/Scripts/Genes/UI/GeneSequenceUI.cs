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
        [Tooltip("The dedicated slot where a seed is placed to be edited.")]
        [SerializeField] private GeneSlotUI seedEditSlot;

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

        private PlantGeneRuntimeState runtimeState;
        private List<GeneSlotUI> passiveSlots = new List<GeneSlotUI>();
        private List<SequenceRowUI> sequenceRows = new List<SequenceRowUI>();
        private PlantSequenceExecutor executor;

        void Start()
        {
            InitializeUI();
            SetEditorLocked(true);
            RefreshAllVisuals();
        }

        private void InitializeUI()
        {
            foreach (Transform child in passiveGenesContainer) Destroy(child.gameObject);
            foreach (Transform child in activeSequenceContainer) Destroy(child.gameObject);
            passiveSlots.Clear();
            sequenceRows.Clear();

            for (int i = 0; i < maxPassiveSlots; i++)
            {
                GameObject slotObj = Instantiate(passiveSlotPrefab, passiveGenesContainer);
                GeneSlotUI slot = slotObj.GetComponent<GeneSlotUI>();
                slot.acceptedCategory = GeneCategory.Passive;
                slot.slotIndex = i;
                passiveSlots.Add(slot);
            }

            for (int i = 0; i < maxSequenceLength; i++)
            {
                GameObject rowObj = Instantiate(sequenceRowPrefab, activeSequenceContainer);
                
                // FIX: Explicitly activate the newly created row GameObject.
                rowObj.SetActive(true);

                SequenceRowUI row = rowObj.GetComponent<SequenceRowUI>();
                row.Initialize(i, this);
                sequenceRows.Add(row);
            }
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
            SetEditorLocked(true);
            RefreshAllVisuals();
        }
        
        private void SetEditorLocked(bool isLocked)
        {
            foreach (var slot in passiveSlots)
            {
                slot.isLocked = isLocked;
            }
            foreach (var row in sequenceRows)
            {
                if (row.activeSlot != null) row.activeSlot.isLocked = isLocked;
            }
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
                    // Assuming one modifier slot for now
                    if (sequenceSlot.modifierInstances.Count == 0) sequenceSlot.modifierInstances.Add(null);
                    sequenceSlot.modifierInstances[0] = newInstance;
                    break;
                case GeneCategory.Payload:
                    // Assuming one payload slot for now
                    if (sequenceSlot.payloadInstances.Count == 0) sequenceSlot.payloadInstances.Add(null);
                    sequenceSlot.payloadInstances[0] = newInstance;
                    break;
            }
            
            RefreshAllVisuals();
        }

        // NEW: Dedicated method for updating passive genes.
        public void UpdatePassiveGene(int slotIndex, InventoryBarItem newItem)
        {
            if (runtimeState == null || slotIndex < 0) return;

            // Ensure the passive instances list is large enough
            while (runtimeState.passiveInstances.Count <= slotIndex)
            {
                runtimeState.passiveInstances.Add(null);
            }

            runtimeState.passiveInstances[slotIndex] = newItem?.GeneInstance;
            
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
            if (runtimeState == null)
            {
                for (int i = 0; i < passiveSlots.Count; i++) passiveSlots[i].ClearSlot();
                for (int i = 0; i < sequenceRows.Count; i++) sequenceRows[i].ClearRow();
                UpdateDisplay();
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

            if (executor != null && executor.plantGrowth != null && executor.plantGrowth.EnergySystem != null)
            {
                var energySystem = executor.plantGrowth.EnergySystem;
                if (currentEnergyText != null) currentEnergyText.text = $"Energy: {energySystem.CurrentEnergy:F0}/{energySystem.MaxEnergy:F0}";
            }
            else
            {
                if (currentEnergyText != null) currentEnergyText.text = $"Energy: --/{runtimeState.template.maxEnergy:F0}";
            }

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
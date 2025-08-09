using System;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Runtime
{
    public class PlantGeneRuntimeState
    {
        public SeedTemplate template;

        public List<RuntimeGeneInstance> passiveInstances = new List<RuntimeGeneInstance>();
        public List<RuntimeSequenceSlot> activeSequence = new List<RuntimeSequenceSlot>();

        [NonSerialized] public int currentPosition = 0;
        [NonSerialized] public int rechargeTicksRemaining = 0;
        [NonSerialized] public bool isExecuting = false;
        [NonSerialized] public float currentEnergy;
        [NonSerialized] public float maxEnergy;

        public void InitializeFromTemplate()
        {
            if (template == null) return;

            // Initialize passive genes
            passiveInstances.Clear();
            foreach (var entry in template.passiveGenes)
            {
                if (entry.gene == null) continue;
                var instance = new RuntimeGeneInstance(entry.gene);
                instance.SetValue("power_multiplier", entry.powerMultiplier);
                passiveInstances.Add(instance);
            }

            // Initialize the active gene sequence
            activeSequence.Clear();
            foreach (var slotTemplate in template.activeSequence)
            {
                var slot = new RuntimeSequenceSlot();
                slot.InitializeFromTemplate(slotTemplate);
                activeSequence.Add(slot);
            }

            maxEnergy = template.maxEnergy;
            currentEnergy = maxEnergy;
        }

        public void Reset()
        {
            currentPosition = 0;
            rechargeTicksRemaining = 0;
            isExecuting = false;
            currentEnergy = maxEnergy;
        }

        public float CalculateTotalEnergyCost()
        {
            float total = 0;
            foreach (var slot in activeSequence)
            {
                if (slot.HasContent)
                {
                    total += slot.GetEnergyCost();
                }
            }
            return total;
        }
    }
}
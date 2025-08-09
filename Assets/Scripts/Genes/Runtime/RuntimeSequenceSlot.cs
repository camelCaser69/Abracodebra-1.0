using System;
using System.Collections.Generic;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

namespace Abracodabra.Genes.Runtime
{
    public class RuntimeSequenceSlot
    {
        public RuntimeGeneInstance activeInstance;
        public List<RuntimeGeneInstance> modifierInstances = new List<RuntimeGeneInstance>();
        public List<RuntimeGeneInstance> payloadInstances = new List<RuntimeGeneInstance>();

        [NonSerialized] public bool isHighlighted;
        [NonSerialized] public bool isExecuting;

        public bool HasContent => activeInstance != null;

        public void InitializeFromTemplate(SequenceSlotTemplate template)
        {
            if (template.activeGene != null)
                activeInstance = new RuntimeGeneInstance(template.activeGene);

            modifierInstances.Clear();
            foreach (var modEntry in template.modifiers)
            {
                if (modEntry?.gene == null) continue;
                // Create instance and set the power multiplier from the template
                var instance = new RuntimeGeneInstance(modEntry.gene);
                instance.SetValue("power_multiplier", modEntry.powerMultiplier);
                modifierInstances.Add(instance);
            }

            payloadInstances.Clear();
            foreach (var payloadEntry in template.payloads)
            {
                if (payloadEntry?.gene == null) continue;
                // Create instance and set the power multiplier from the template
                var instance = new RuntimeGeneInstance(payloadEntry.gene);
                instance.SetValue("power_multiplier", payloadEntry.powerMultiplier);
                payloadInstances.Add(instance);
            }
        }

        public float GetEnergyCost()
        {
            var active = activeInstance?.GetGene<ActiveGene>();
            if (active == null) return 0;

            return active.GetFinalEnergyCost(modifierInstances);
        }

        public void Clear()
        {
            activeInstance = null;
            modifierInstances.Clear();
            payloadInstances.Clear();
        }
    }
}
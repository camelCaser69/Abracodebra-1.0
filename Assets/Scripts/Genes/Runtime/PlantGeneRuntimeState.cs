// REWORKED FILE: Assets/Scripts/Genes/Runtime/PlantGeneRuntimeState.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Core;

namespace Abracodabra.Genes.Runtime
{
    [Serializable]
    public class PlantGeneRuntimeState
    {
        [Header("Source Template")]
        public SeedTemplate template;

        [Header("Runtime Instances")]
        public List<RuntimeGeneInstance> passiveInstances = new List<RuntimeGeneInstance>();
        public List<RuntimeSequenceSlot> activeSequence = new List<RuntimeSequenceSlot>();

        [Header("Runtime State")]
        [NonSerialized] public int currentPosition = 0;
        [NonSerialized] public int rechargeTicksRemaining = 0;
        [NonSerialized] public bool isExecuting = false;
        [NonSerialized] public float currentEnergy;
        [NonSerialized] public float maxEnergy;

        public void InitializeFromTemplate()
        {
            if (template == null) return;
            passiveInstances.Clear();
            foreach (var entry in template.passiveGenes)
            {
                if (entry.gene == null) continue;
                var instance = new RuntimeGeneInstance(entry.gene);
                instance.SetValue("power_multiplier", entry.powerMultiplier);
                passiveInstances.Add(instance);
            }

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
    
    // This class does not need to be changed.
    [Serializable]
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
            foreach (var mod in template.modifiers)
            {
                if (mod == null) continue;
                modifierInstances.Add(new RuntimeGeneInstance(mod));
            }
            
            payloadInstances.Clear();
            foreach (var payload in template.payloads)
            {
                if (payload == null) continue;
                payloadInstances.Add(new RuntimeGeneInstance(payload));
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
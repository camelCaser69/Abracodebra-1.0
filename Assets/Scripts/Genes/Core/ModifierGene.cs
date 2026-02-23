// File: Assets/Scripts/Genes/Core/ModifierGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    public abstract class ModifierGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Modifier;

        [Header("Modifier Settings")]
        public ModifierType modifierType;
        public float power = 1f;

        public virtual float ModifyEnergyCost(float baseCost, RuntimeGeneInstance instance) => baseCost;
        public virtual void PreExecution(ActiveGeneContext context) { }
        public virtual void PostExecution(ActiveGeneContext context) { }
        public virtual bool ModifyTriggerCondition(ActiveGene gene, PlantGrowth plant) => true;

        /// <summary>
        /// For Trigger-type modifiers: return false to skip the slot (no energy spent).
        /// Default returns true (always allow execution).
        /// Override in TriggerProximityGene, TriggerTimerGene, etc.
        /// </summary>
        public virtual bool CheckTriggerCondition(ActiveGeneContext context)
        {
            return true;
        }

        public override bool CanAttachTo(GeneBase other)
        {
            return other.Category == GeneCategory.Active;
        }
    }

    public enum ModifierType
    {
        Cost,       // Affects energy consumption
        Trigger,    // Changes when active executes
        Behavior,   // Multi-cast, spread, etc.
        Condition   // Adds requirements
    }
}

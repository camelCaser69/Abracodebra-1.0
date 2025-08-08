// File: Assets/Scripts/Genes/Core/ModifierGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// Base class for genes that attach to Active genes to modify their behavior,
    /// such as changing energy cost or trigger conditions.
    /// </summary>
    public abstract class ModifierGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Modifier;

        [Header("Modifier Settings")]
        public ModifierType modifierType;
        public float power = 1f;

        // Different modifier types affect different aspects
        public virtual float ModifyEnergyCost(float baseCost, RuntimeGeneInstance instance) => baseCost;
        public virtual void PreExecution(ActiveGeneContext context) { }
        public virtual void PostExecution(ActiveGeneContext context) { }
        public virtual bool ModifyTriggerCondition(ActiveGene gene, PlantGrowth plant) => true;

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
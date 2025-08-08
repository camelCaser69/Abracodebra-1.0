// File: Assets/Scripts/Genes/Core/PassiveGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// Base class for genes that provide passive, persistent stat modifications.
    /// Their effects are typically applied once when the plant is initialized.
    /// </summary>
    public abstract class PassiveGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Passive;

        [Header("Passive Settings")]
        public bool stacksAdditively = true;
        public float baseValue = 1f;
        public int maxStacks = -1; // -1 for unlimited

        /// <summary>
        /// Called once when the plant is created to apply this gene's permanent effects.
        /// </summary>
        public abstract void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance);

        /// <summary>
        /// Gets a string describing the stat modification for UI purposes.
        /// </summary>
        public abstract string GetStatModificationText();

        /// <summary>
        /// Checks if this passive gene's effects can be applied to the plant.
        /// </summary>
        public virtual bool MeetsRequirements(PlantGrowth plant) => true;

        /// <summary>
        /// Checks if this passive is compatible with another passive gene.
        /// </summary>
        public virtual bool IsCompatibleWith(PassiveGene other) => true;
    }
}
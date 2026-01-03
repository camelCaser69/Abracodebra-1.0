using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core {
    public enum PassiveStatType {
        None,           // For passive genes that don't modify stats (e.g., TerrainAffinityGene)
        GrowthSpeed,
        EnergyGeneration,
        EnergyStorage,
        FruitYield,
        Defense
    }

    public abstract class PassiveGene : GeneBase {
        public override GeneCategory Category => GeneCategory.Passive;

        [Header("Stat Modification")]
        [Tooltip("Which stat this gene modifies. Set to 'None' for genes that don't modify stats (like TerrainAffinity).")]
        public PassiveStatType statToModify = PassiveStatType.None;
        
        [Tooltip("Base value of the modification (multiplier). 1.0 = no change.")]
        public float baseValue = 1f;
        
        [Tooltip("If true, multiple copies stack additively. If false, they stack multiplicatively.")]
        public bool stacksAdditively = true;
        
        [Tooltip("Maximum number of this gene that can be stacked. -1 for unlimited.")]
        public int maxStacks = -1;

        public abstract void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance);

        public abstract string GetStatModificationText();

        public virtual bool MeetsRequirements(PlantGrowth plant) => true;

        public virtual bool IsCompatibleWith(PassiveGene other) => true;
    }
}
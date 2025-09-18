using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    public enum PassiveStatType
    {
        GrowthSpeed,
        EnergyGeneration,
        EnergyStorage,
        FruitYield,
        Defense // NEW
    }

    public abstract class PassiveGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Passive;

        public PassiveStatType statToModify;
        public float baseValue = 1f;
        public bool stacksAdditively = true;
        public int maxStacks = -1; // -1 for unlimited

        public abstract void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance);

        public abstract string GetStatModificationText();

        public virtual bool MeetsRequirements(PlantGrowth plant) => true;

        public virtual bool IsCompatibleWith(PassiveGene other) => true;
    }
}
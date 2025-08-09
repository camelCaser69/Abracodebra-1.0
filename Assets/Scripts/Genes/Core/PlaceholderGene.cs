using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// A special gene that is used as a fallback when a gene cannot be loaded from its GUID.
    /// This prevents null reference exceptions throughout the system.
    /// </summary>
    public class PlaceholderGene : PassiveGene
    {
        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
            // A placeholder gene has no effect.
        }

        public override string GetStatModificationText()
        {
            return "Missing Gene";
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            return "This gene could not be loaded. It may have been deleted or renamed.";
        }
    }
}
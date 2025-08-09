// NEW FILE: Assets/Scripts/Genes/Core/PlaceholderGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// A special passive gene used for error recovery when a gene asset is missing.
    /// It does nothing but ensures the system doesn't crash.
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceholderGene", menuName = "Abracodabra/Genes/System/Placeholder")]
    public class PlaceholderGene : PassiveGene
    {
        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
            // Do nothing - this is for error recovery
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
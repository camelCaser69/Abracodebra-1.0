// FILE: Assets/Scripts/Genes/Core/PlaceholderGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/System/Placeholder", fileName = "Gene_System_Placeholder")]
    public class PlaceholderGene : PassiveGene
    {
        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
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
// File: Assets/Scripts/Genes/Core/ActiveGeneContext.cs
using System.Collections.Generic;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Services;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// A data transfer object containing all the contextual information
    /// needed for an ActiveGene to execute its effect.
    /// </summary>
    public class ActiveGeneContext
    {
        public PlantGrowth plant;
        public RuntimeGeneInstance activeInstance;
        public List<RuntimeGeneInstance> modifiers;
        public List<RuntimeGeneInstance> payloads;
        public int sequencePosition;
        public PlantSequenceExecutor executor;
        public IDeterministicRandom random;
    }
}
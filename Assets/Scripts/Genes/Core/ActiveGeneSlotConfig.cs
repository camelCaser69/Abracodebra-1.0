// File: Assets/Scripts/Genes/Core/ActiveGeneSlotConfig.cs
using System;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// Defines the number of Modifier and Payload slots an ActiveGene has.
    /// </summary>
    [Serializable]
    public class ActiveGeneSlotConfig
    {
        public int modifierSlots = 1;
        public int payloadSlots = 1;
    }
}
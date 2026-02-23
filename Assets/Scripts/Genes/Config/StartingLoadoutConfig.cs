// File: Assets/Scripts/Genes/Config/StartingLoadoutConfig.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Templates;

namespace Abracodabra.Genes.Config
{
    /// <summary>
    /// ScriptableObject that defines what genes and seeds the player starts with.
    /// Create via Assets > Create > Abracodabra > Config > Starting Loadout.
    /// </summary>
    [CreateAssetMenu(fileName = "StartingLoadoutConfig", menuName = "Abracodabra/Config/Starting Loadout")]
    public class StartingLoadoutConfig : ScriptableObject
    {
        [Serializable]
        public class GeneEntry
        {
            public GeneBase gene;
            public int count = 1;
        }

        [Serializable]
        public class SeedEntry
        {
            public SeedTemplate seed;
            public int count = 1;
        }

        [Header("Starting Seeds")]
        [Tooltip("Seeds the player starts with.")]
        public List<SeedEntry> startingSeeds = new List<SeedEntry>();

        [Header("Starting Genes")]
        [Tooltip("Genes the player starts with in inventory.")]
        public List<GeneEntry> startingGenes = new List<GeneEntry>();
    }
}

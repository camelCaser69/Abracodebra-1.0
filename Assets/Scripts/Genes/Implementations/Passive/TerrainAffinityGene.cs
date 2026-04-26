// FILE: Assets/Scripts/Genes/Implementations/Passive/TerrainAffinityGene.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    [CreateAssetMenu(menuName = "Abracodabra/Genes/Passive/Terrain Affinity", fileName = "Gene_Passive_TerrainAffinity")]
    public class TerrainAffinityGene : PassiveGene
    {
        [Header("Terrain Configuration")]
        [Tooltip("List of tile definitions where this plant CAN grow. If empty, uses global rules.")]
        [SerializeField] List<TileDefinition> allowedTiles = new List<TileDefinition>();

        [Tooltip("If true, this gene ADDS to the allowed tiles (permissive). If false, this gene RESTRICTS to only these tiles.")]
        [SerializeField] bool additive = false;

        [Header("Growth Bonuses")]
        [Tooltip("Growth speed bonus when planted on preferred terrain (multiplier, 1.0 = no bonus)")]
        [SerializeField] float preferredTerrainGrowthBonus = 1.2f;

        [Tooltip("Optional: Tiles where this plant grows faster than normal")]
        [SerializeField] List<TileDefinition> preferredTiles = new List<TileDefinition>();

        public IReadOnlyList<TileDefinition> AllowedTiles => allowedTiles;
        public IReadOnlyList<TileDefinition> PreferredTiles => preferredTiles;
        public bool IsAdditive => additive;
        public float PreferredTerrainGrowthBonus => preferredTerrainGrowthBonus;

        void OnEnable()
        {
            statToModify = PassiveStatType.None;
            baseValue = 1f;
        }

        void OnValidate()
        {
            if (statToModify != PassiveStatType.None)
            {
                statToModify = PassiveStatType.None;
            }
        }

        public bool IsTileAllowed(TileDefinition tile)
        {
            if (tile == null) return false;
            if (allowedTiles == null || allowedTiles.Count == 0) return true; // No restriction
            return allowedTiles.Contains(tile);
        }

        public bool IsPreferredTile(TileDefinition tile)
        {
            if (tile == null) return false;
            if (preferredTiles == null || preferredTiles.Count == 0) return false;
            return preferredTiles.Contains(tile);
        }

        public float GetGrowthMultiplierForTile(TileDefinition tile)
        {
            if (IsPreferredTile(tile))
            {
                return preferredTerrainGrowthBonus;
            }
            return 1.0f;
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance)
        {
            if (plant == null) return;
            Debug.Log($"[TerrainAffinityGene] Applied to plant {plant.name}");
        }

        public override string GetStatModificationText()
        {
            if (allowedTiles == null || allowedTiles.Count == 0)
            {
                return "Can grow on any terrain";
            }

            var tileNames = new List<string>();
            foreach (var tile in allowedTiles)
            {
                if (tile != null)
                {
                    tileNames.Add(tile.displayName);
                }
            }

            if (tileNames.Count == 0)
            {
                return "Can grow on any terrain";
            }

            return $"Grows on: {string.Join(", ", tileNames)}";
        }

        public override string GetTooltip(GeneTooltipContext context)
        {
            string baseTooltip = $"<b>{geneName}</b>\n";
            baseTooltip += $"<i>Terrain Affinity Gene</i>\n\n";
            baseTooltip += description + "\n\n";
            baseTooltip += GetStatModificationText();

            if (preferredTiles != null && preferredTiles.Count > 0)
            {
                var prefNames = new List<string>();
                foreach (var tile in preferredTiles)
                {
                    if (tile != null) prefNames.Add(tile.displayName);
                }
                if (prefNames.Count > 0)
                {
                    baseTooltip += $"\n\n<color=#90EE90>Preferred: {string.Join(", ", prefNames)}</color>";
                    baseTooltip += $"\n+{(preferredTerrainGrowthBonus - 1f) * 100:F0}% growth speed on preferred terrain";
                }
            }

            return baseTooltip;
        }
    }
}
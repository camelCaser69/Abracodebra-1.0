using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core {
    /// <summary>
    /// A passive gene that defines which terrain types a seed can be planted on.
    /// If no TerrainAffinityGene is present, the seed uses the global invalidPlantingTiles check.
    /// If present, only the specified tiles are valid for planting.
    /// </summary>
    [CreateAssetMenu(fileName = "New Terrain Affinity Gene", menuName = "Abracodabra/Genes/Passive/Terrain Affinity")]
    public class TerrainAffinityGene : PassiveGene {
        [Header("Terrain Configuration")]
        [Tooltip("List of tile definitions where this plant CAN grow. If empty, uses global rules.")]
        [SerializeField] private List<TileDefinition> allowedTiles = new List<TileDefinition>();
        
        [Tooltip("If true, this gene ADDS to the allowed tiles (permissive). If false, this gene RESTRICTS to only these tiles.")]
        [SerializeField] private bool additive = false;
        
        [Header("Growth Bonuses")]
        [Tooltip("Growth speed bonus when planted on preferred terrain (multiplier, 1.0 = no bonus)")]
        [SerializeField] private float preferredTerrainGrowthBonus = 1.2f;
        
        [Tooltip("Optional: Tiles where this plant grows faster than normal")]
        [SerializeField] private List<TileDefinition> preferredTiles = new List<TileDefinition>();

        public IReadOnlyList<TileDefinition> AllowedTiles => allowedTiles;
        public IReadOnlyList<TileDefinition> PreferredTiles => preferredTiles;
        public bool IsAdditive => additive;
        public float PreferredTerrainGrowthBonus => preferredTerrainGrowthBonus;

        /// <summary>
        /// Check if a tile is valid for planting with this gene
        /// </summary>
        public bool IsTileAllowed(TileDefinition tile) {
            if (tile == null) return false;
            if (allowedTiles == null || allowedTiles.Count == 0) return true; // No restriction
            return allowedTiles.Contains(tile);
        }
        
        /// <summary>
        /// Check if a tile is a preferred growing tile
        /// </summary>
        public bool IsPreferredTile(TileDefinition tile) {
            if (tile == null) return false;
            if (preferredTiles == null || preferredTiles.Count == 0) return false;
            return preferredTiles.Contains(tile);
        }
        
        /// <summary>
        /// Get the growth multiplier for a specific tile
        /// </summary>
        public float GetGrowthMultiplierForTile(TileDefinition tile) {
            if (IsPreferredTile(tile)) {
                return preferredTerrainGrowthBonus;
            }
            return 1.0f;
        }

        public override void ApplyToPlant(PlantGrowth plant, RuntimeGeneInstance instance) {
            // The terrain affinity is checked at planting time, not during growth
            // However, we can store the growth bonus info for the TileGrowthManager to use
            if (plant == null) return;
            
            // The growth bonus is applied through TileGrowthManager which reads this gene
            Debug.Log($"[TerrainAffinityGene] Applied to plant {plant.name}");
        }

        public override string GetStatModificationText() {
            if (allowedTiles == null || allowedTiles.Count == 0) {
                return "Can grow on any terrain";
            }
            
            var tileNames = new List<string>();
            foreach (var tile in allowedTiles) {
                if (tile != null) {
                    tileNames.Add(tile.displayName);
                }
            }
            
            if (tileNames.Count == 0) {
                return "Can grow on any terrain";
            }
            
            return $"Grows on: {string.Join(", ", tileNames)}";
        }

        public override string GetTooltip(GeneTooltipContext context) {
            string baseTooltip = $"<b>{geneName}</b>\n";
            baseTooltip += $"<i>Terrain Affinity Gene</i>\n\n";
            baseTooltip += description + "\n\n";
            baseTooltip += GetStatModificationText();
            
            if (preferredTiles != null && preferredTiles.Count > 0) {
                var prefNames = new List<string>();
                foreach (var tile in preferredTiles) {
                    if (tile != null) prefNames.Add(tile.displayName);
                }
                if (prefNames.Count > 0) {
                    baseTooltip += $"\n\n<color=#90EE90>Preferred: {string.Join(", ", prefNames)}</color>";
                    baseTooltip += $"\n+{(preferredTerrainGrowthBonus - 1f) * 100:F0}% growth speed on preferred terrain";
                }
            }
            
            return baseTooltip;
        }
    }
}

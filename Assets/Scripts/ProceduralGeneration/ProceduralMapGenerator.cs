// FILE: Assets/Scripts/ProceduralGeneration/ProceduralMapGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WegoSystem;

namespace WegoSystem.ProceduralGeneration
{
    public class ProceduralMapGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private MapGenerationProfile profile;

        [SerializeField]
        [Tooltip("A list of tile mappings. Mappings should be ordered from lowest threshold (e.g., water) to highest (e.g., mountains).")]
        private List<NoiseToTileMapping> tileMappings;

        [Header("System References")]
        [SerializeField]
        private TileInteractionManager tileManager;

        #region Public Methods
        /// <summary>
        /// Generates the map based on the assigned profile and tile mappings.
        /// </summary>
        public void GenerateMap()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            Debug.Log($"Starting map generation with seed: {profile.worldSeed}...");

            ClearMap();
            profile.InitializeSeed();

            // Sort mappings by threshold to ensure correct placement.
            // We iterate from lowest to highest value.
            var sortedMappings = tileMappings.OrderBy(m => m.noiseThreshold).ToList();

            for (int x = 0; x < profile.mapSize.x; x++)
            {
                for (int y = 0; y < profile.mapSize.y; y++)
                {
                    // Simplex noise returns [-1, 1], we remap to [0, 1] for our thresholds.
                    float noiseValue = profile.noiseParameters.Sample(x, y);
                    float normalizedNoise = (noiseValue + 1f) / 2f;

                    TileDefinition tileToPlace = null;
                    foreach (var mapping in sortedMappings)
                    {
                        if (normalizedNoise <= mapping.noiseThreshold)
                        {
                            tileToPlace = mapping.tileToPlace;
                            break; // Found the correct tile for this noise level
                        }
                    }

                    if (tileToPlace != null)
                    {
                        // IMPORTANT: We use the TileInteractionManager to place tiles.
                        // This ensures full compatibility with the Dual Grid system.
                        tileManager.PlaceTile(tileToPlace, new Vector3Int(x, y, 0));
                    }
                }
            }

            Debug.Log("Map generation complete.");
        }

        /// <summary>
        /// Clears all tiles within the map's defined size.
        /// </summary>
        public void ClearMap()
        {
            if (!ValidateConfiguration(checkMappings: false))
            {
                return;
            }

            Debug.Log("Clearing existing map...");

            for (int x = 0; x < profile.mapSize.x; x++)
            {
                for (int y = 0; y < profile.mapSize.y; y++)
                {
                    var cellPos = new Vector3Int(x, y, 0);
                    TileDefinition currentTile = tileManager.FindWhichTileDefinitionAt(cellPos);
                    if (currentTile != null)
                    {
                        tileManager.RemoveTile(currentTile, cellPos);
                    }
                }
            }
        }
        #endregion

        private bool ValidateConfiguration(bool checkMappings = true)
        {
            if (profile == null)
            {
                Debug.LogError("[ProceduralMapGenerator] Map Generation Profile is not assigned!", this);
                return false;
            }
            if (tileManager == null)
            {
                Debug.LogError("[ProceduralMapGenerator] Tile Interaction Manager is not assigned!", this);
                return false;
            }
            if (checkMappings && (tileMappings == null || tileMappings.Count == 0))
            {
                Debug.LogError("[ProceduralMapGenerator] No Noise To Tile Mappings have been assigned!", this);
                return false;
            }
            return true;
        }
    }
}
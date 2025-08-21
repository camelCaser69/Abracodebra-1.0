using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
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

        // Internal cache of what was last generated. Used only for generation, not for clearing.
        private Dictionary<Vector2Int, TileDefinition> tileMap = new Dictionary<Vector2Int, TileDefinition>();

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

            profile.InitializeSeed();
            Debug.Log($"Starting map generation with seed: {profile.worldSeed}...");

            ClearMap();

            var sortedMappings = tileMappings.OrderBy(m => m.noiseThreshold).ToList();

            for (int x = 0; x < profile.mapSize.x; x++)
            {
                for (int y = 0; y < profile.mapSize.y; y++)
                {
                    float noiseValue = profile.noiseParameters.Sample(x, y);
                    float normalizedNoise = SimplexNoise.Remap(noiseValue, -1f, 1f, 0f, 1f);

                    TileDefinition tileToPlace = null;
                    foreach (var mapping in sortedMappings)
                    {
                        if (normalizedNoise <= mapping.noiseThreshold)
                        {
                            tileToPlace = mapping.tileToPlace;
                            break;
                        }
                    }

                    if (tileToPlace != null)
                    {
                        var cellPos = new Vector3Int(x, y, 0);
                        tileManager.PlaceTile(tileToPlace, cellPos);
                        tileMap[new Vector2Int(x,y)] = tileToPlace; // Keep track for this session
                    }
                }
            }

            Debug.Log("Map generation complete.");
        }

        /// <summary>
        /// Clears all tiles within the map's defined size by iterating through every cell.
        /// This is a robust method that doesn't rely on a cached map state.
        /// </summary>
        public void ClearMap()
        {
            if (!ValidateConfiguration(checkMappings: false))
            {
                return;
            }

            Debug.Log("Clearing existing map by checking every cell...");

            for (int x = 0; x < profile.mapSize.x; x++)
            {
                for (int y = 0; y < profile.mapSize.y; y++)
                {
                    var cellPos = new Vector3Int(x, y, 0);
                    
                    // Ask the TileInteractionManager what tile is at this position.
                    // This works because the manager knows about all tilemap layers.
                    TileDefinition currentTile = tileManager.FindWhichTileDefinitionAt(cellPos);

                    // If a tile exists, tell the manager to remove it.
                    if (currentTile != null)
                    {
                        tileManager.RemoveTile(currentTile, cellPos);
                    }
                }
            }
            
            // Clear internal caches as well
            ClearCaches();
            Debug.Log("[ProceduralMapGenerator] Map clear complete.");
        }
        #endregion

        /// <summary>
        /// Clears all internal cached data.
        /// </summary>
        private void ClearCaches()
        {
            tileMap.Clear();
        }

        private bool ValidateConfiguration(bool checkMappings = true)
        {
            if (profile == null)
            {
                Debug.LogError("[ProceduralMapGenerator] Map Generation Profile is not assigned!", this);
                return false;
            }
            if (tileManager == null)
            {
                tileManager = TileInteractionManager.Instance;
                if (tileManager == null)
                {
                    Debug.LogError("[ProceduralMapGenerator] Tile Interaction Manager is not assigned and could not be found!", this);
                    return false;
                }
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
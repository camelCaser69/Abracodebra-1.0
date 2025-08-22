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

        [Header("System References")]
        [SerializeField]
        private TileInteractionManager tileManager;

        private Dictionary<Vector2Int, TileDefinition> tileMap = new Dictionary<Vector2Int, TileDefinition>();

        public void GenerateMap()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            profile.InitializeSeed();
            Debug.Log($"Starting map generation with seed: {profile.worldSeed}...");

            ClearMap();

            var sortedLayers = profile.biomeLayers.OrderBy(layer => layer.noiseThreshold).ToList();

            float halfWidth = profile.mapSize.x / 2f;
            float halfHeight = profile.mapSize.y / 2f;

            for (int x = 0; x < profile.mapSize.x; x++)
            {
                for (int y = 0; y < profile.mapSize.y; y++)
                {
                    float sampleX = x - halfWidth;
                    float sampleY = y - halfHeight;
                    float noiseValue = profile.noiseParameters.Sample(sampleX, sampleY);
                    
                    float normalizedNoise = SimplexNoise.Remap(noiseValue, -1f, 1f, 0f, 1f);

                    BiomeLayer chosenLayer = null;
                    foreach (var layer in sortedLayers)
                    {
                        if (normalizedNoise <= layer.noiseThreshold)
                        {
                            chosenLayer = layer;
                            break; 
                        }
                    }

                    if (chosenLayer != null)
                    {
                        var cellPos = new Vector3Int(x, y, 0);
                        
                        // --- NEW LOGIC START ---

                        // Step 1: Place the underlay tile if it's requested and valid.
                        if (chosenLayer.placeUnderlayTile && chosenLayer.underlayTile != null)
                        {
                            tileManager.PlaceTile(chosenLayer.underlayTile, cellPos);
                        }
                        
                        // Step 2: Place the primary tile.
                        if (chosenLayer.tile != null)
                        {
                            tileManager.PlaceTile(chosenLayer.tile, cellPos);
                            tileMap[new Vector2Int(x, y)] = chosenLayer.tile; // Track the top-most tile
                        }

                        // --- NEW LOGIC END ---
                    }
                }
            }

            Debug.Log("Map generation complete.");
        }

        public void ClearMap()
        {
            if (!ValidateConfiguration(checkMappings: false))
            {
                return;
            }

            Debug.Log("Clearing existing map by checking every cell...");

            // This clear method is now even more important because it correctly
            // queries the TileInteractionManager, which knows about all layers.
            for (int x = 0; x < profile.mapSize.x; x++)
            {
                for (int y = 0; y < profile.mapSize.y; y++)
                {
                    var cellPos = new Vector3Int(x, y, 0);
                    
                    // The beauty of this is that FindWhichTileDefinitionAt finds the top-most
                    // non-overlay tile, and the manager handles removing it correctly.
                    // To clear all layers, we might need a loop.
                    
                    // We need to keep clearing until no tile is found at the position.
                    TileDefinition tileOnTop;
                    while ((tileOnTop = tileManager.FindWhichTileDefinitionAt(cellPos)) != null)
                    {
                        tileManager.RemoveTile(tileOnTop, cellPos);
                    }
                }
            }
            
            ClearCaches();
            Debug.Log("[ProceduralMapGenerator] Map clear complete.");
        }

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
            if (checkMappings && (profile.biomeLayers == null || profile.biomeLayers.Count == 0))
            {
                Debug.LogError("[ProceduralMapGenerator] The assigned Profile has no Biome Layers defined!", this);
                return false;
            }
            return true;
        }
    }
}
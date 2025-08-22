using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WegoSystem;

namespace WegoSystem.ProceduralGeneration
{
    public class ProceduralMapGenerator : MonoBehaviour
    {
        [Tooltip("The central configuration for map size. This is the primary source of truth for dimensions.")]
        [SerializeField] MapConfiguration mapConfig;
        
        [Tooltip("Profile containing noise parameters and biome layer definitions.")]
        [SerializeField] MapGenerationProfile profile;

        private TileInteractionManager tileManager;

        private readonly Dictionary<Vector2Int, TileDefinition> tileMap = new Dictionary<Vector2Int, TileDefinition>();

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

            float halfWidth = mapConfig.mapSize.x / 2f;
            float halfHeight = mapConfig.mapSize.y / 2f;

            for (int x = 0; x < mapConfig.mapSize.x; x++)
            {
                for (int y = 0; y < mapConfig.mapSize.y; y++)
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

                        if (chosenLayer.placeUnderlayTile && chosenLayer.underlayTile != null)
                        {
                            tileManager.PlaceTile(chosenLayer.underlayTile, cellPos);
                        }

                        if (chosenLayer.tile != null)
                        {
                            tileManager.PlaceTile(chosenLayer.tile, cellPos);
                            tileMap[new Vector2Int(x, y)] = chosenLayer.tile; // Track the top-most tile
                        }

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

            for (int x = 0; x < mapConfig.mapSize.x; x++)
            {
                for (int y = 0; y < mapConfig.mapSize.y; y++)
                {
                    var cellPos = new Vector3Int(x, y, 0);

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
            if (mapConfig == null)
            {
                Debug.LogError("[ProceduralMapGenerator] Map Configuration is not assigned!", this);
                return false;
            }
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
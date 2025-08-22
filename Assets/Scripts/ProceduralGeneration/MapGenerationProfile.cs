// FILE: Assets/Scripts/ProceduralGeneration/MapGenerationProfile.cs
using UnityEngine;
using System.Collections.Generic;
using WegoSystem.ProceduralGeneration;

[System.Serializable]
public class BiomeLayer
{
    [Tooltip("The primary tile to place for this layer.")]
    public TileDefinition tile;

    [Tooltip("The upper noise threshold for this layer (0-1). It will be placed if noise is <= this value and > the previous layer's value.")]
    [Range(0f, 1f)]
    public float noiseThreshold;
    
    // --- NEW FIELDS START ---
    
    [Space(5)]
    [Tooltip("Check this to place another tile underneath the primary tile.")]
    public bool placeUnderlayTile = false;
    
    [Tooltip("The tile to place underneath the primary one. Only used if 'Place Underlay Tile' is checked.")]
    public TileDefinition underlayTile;
    
    // --- NEW FIELDS END ---
}

namespace WegoSystem.ProceduralGeneration
{
    [CreateAssetMenu(fileName = "MapGenerationProfile", menuName = "Game/Map Generation Profile")]
    public class MapGenerationProfile : ScriptableObject
    {
        // REMOVED: The mapSize is now sourced exclusively from MapConfiguration.
        // public Vector2Int mapSize = new Vector2Int(100, 100); 

        public int worldSeed = 12345;
        public bool useRandomSeed = true;

        public NoiseParameters noiseParameters = new NoiseParameters
        {
            scale = 0.1f,
            octaves = 4,
            persistence = 0.5f,
            lacunarity = 2f
        };

        public List<BiomeLayer> biomeLayers;

        public void InitializeSeed()
        {
            if (useRandomSeed)
            {
                worldSeed = Random.Range(0, int.MaxValue);
            }
            noiseParameters.seed = worldSeed;
        }
    }
}
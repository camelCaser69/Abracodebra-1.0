// FILE: Assets/Scripts/ProceduralGeneration/MapGenerationProfile.cs
using UnityEngine;
using WegoSystem.ProceduralGeneration;

namespace WegoSystem.ProceduralGeneration
{
    [CreateAssetMenu(fileName = "NewMapProfile", menuName = "ProceduralGen/Map Generation Profile")]
    public class MapGenerationProfile : ScriptableObject
    {
        [Header("Map Settings")]
        public Vector2Int mapSize = new Vector2Int(100, 100);
        public int worldSeed = 12345;
        public bool useRandomSeed = true;

        [Header("Noise Settings")]
        public NoiseParameters noiseParameters = new NoiseParameters
        {
            scale = 0.1f,
            octaves = 4,
            persistence = 0.5f,
            lacunarity = 2f
        };

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
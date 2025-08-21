// FILE: Assets/Scripts/ProceduralGeneration/NoiseToTileMapping.cs
using UnityEngine;

namespace WegoSystem.ProceduralGeneration
{
    [CreateAssetMenu(fileName = "NewNoiseToTileMapping", menuName = "ProceduralGen/Noise to Tile Mapping")]
    public class NoiseToTileMapping : ScriptableObject
    {
        [Tooltip("The tile to place if the noise value falls within the threshold.")]
        public TileDefinition tileToPlace;

        [Tooltip("The upper bound for this tile. The tile will be placed if the normalized noise value (0-1) is less than or equal to this threshold.")]
        [Range(0f, 1f)]
        public float noiseThreshold = 0.5f;
    }
}
using System;
using UnityEngine;
using skner.DualGrid;

namespace WegoSystem
{
    /// <summary>
    /// Maps a TileDefinition to its corresponding DualGridTilemapModule.
    /// Extracted to its own file to be shared between TileInteractionManager and TileDefinitionMappingsLibrary.
    /// </summary>
    [Serializable]
    public class TileDefinitionMapping
    {
        [Tooltip("The tile definition that defines behavior and properties.")]
        public TileDefinition tileDef;

        [Tooltip("The dual grid tilemap module that renders this tile type.")]
        public DualGridTilemapModule tilemapModule;
    }
}
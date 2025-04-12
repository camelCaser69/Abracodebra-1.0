using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDefinition", menuName = "Tiles/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    [Header("Basic Identification")]
    [Tooltip("Display name for this tile type (used in UI and debugging)")]
    public string displayName;    // e.g. "Grass", "Dirt", "Wet Dirt"
    
    [Header("Visual Properties")]
    [Tooltip("Optional tint color to apply to the RenderTilemap")]
    public Color tintColor = Color.white;
    
    [Header("Auto-Reversion (optional)")]
    [Tooltip("If > 0, after this many seconds, the tile reverts to 'revertToTile'.")]
    public float revertAfterSeconds = 0f;

    [Tooltip("If revertAfterSeconds > 0, tile reverts to this tile definition.")]
    public TileDefinition revertToTile;

    [Header("Overlay Option")]
    [Tooltip("If true, this tile will be placed on top without removing the tile underneath ")]
    public bool keepBottomTile = false;
}
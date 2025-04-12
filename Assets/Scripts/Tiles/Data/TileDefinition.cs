using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDefinition", menuName = "Tiles/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    [Header("Basic Identification")]
    public string tileId;        // e.g. "Grass", "Dirt", "DirtWet"
    public TileBase tile;        // The tile (RuleTile, etc.)

    [Header("Auto-Reversion (optional)")]
    [Tooltip("If > 0, after this many seconds, the tile reverts to 'revertToTile'.")]
    public float revertAfterSeconds = 0f;

    [Tooltip("If revertAfterSeconds > 0, tile reverts to this tile definition.")]
    public TileDefinition revertToTile;

    [Header("Overlay Option")]
    [Tooltip("If true, placing this tile does NOT remove the old tile underneath.")]
    public bool doNotRemovePrevious = false;
}
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDefinition", menuName = "Tiles/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    [Tooltip("Unique ID, e.g. 'Grass', 'Dirt', 'DirtWet'.")]
    public string tileId;

    [Tooltip("The TileBase to place on the tilemap (e.g., a RuleTile).")]
    public TileBase tile;
}
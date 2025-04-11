using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileInteractionLibrary", menuName = "Tiles/Tile Interaction Library")]
public class TileInteractionLibrary : ScriptableObject
{
    [Tooltip("List of rules: (Tool, fromTile) => toTile.")]
    public List<TileInteractionRule> rules;
}
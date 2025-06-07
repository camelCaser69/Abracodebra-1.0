using System;
using UnityEngine;

[Serializable]
public class TileInteractionRule
{
    [Header("Tool Condition")]
    [Tooltip("Which tool triggers this rule.")]
    public ToolDefinition tool;

    [Header("Tile Transformation")]
    [Tooltip("Which tile must be present to apply the rule.")]
    public TileDefinition fromTile;
    [Tooltip("Which tile to transform into.")]
    public TileDefinition toTile;
}
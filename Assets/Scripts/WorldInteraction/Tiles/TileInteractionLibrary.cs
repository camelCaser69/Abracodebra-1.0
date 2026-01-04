using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class ToolRefillRule
{
    [Tooltip("The tool that can be refilled.")]
    public ToolDefinition toolToRefill;

    [Tooltip("The tile that must be interacted with to trigger the refill.")]
    public TileDefinition refillSourceTile;
}

[CreateAssetMenu(fileName = "TileInteractionLibrary", menuName = "Abracodabra/Tiles/Tile Interaction Library")]
public class TileInteractionLibrary : ScriptableObject
{
    [Header("Standard Tile Transformations")]
    [Tooltip("List of rules: (Tool, fromTile) => toTile.")]
    public List<TileInteractionRule> rules;

    [Header("Tool Refill Rules")]
    [Tooltip("List of rules defining how tools are refilled.")]
    public List<ToolRefillRule> refillRules;

    [Header("Hover Tile Colors")]
    [SerializeField, Tooltip("Color when player is within range to interact")]
    private Color withinRangeColor = new Color(1f, 1f, 1f, 0.8f);

    [SerializeField, Tooltip("Color when player is outside interaction range")]
    private Color outsideRangeColor = new Color(1f, 1f, 1f, 0.3f);

    public Color WithinRangeColor => withinRangeColor;
    public Color OutsideRangeColor => outsideRangeColor;

    public Color GetHoverColorForRange(bool isWithinRange)
    {
        return isWithinRange ? withinRangeColor : outsideRangeColor;
    }

    private void OnValidate()
    {
        if (withinRangeColor.a < outsideRangeColor.a)
        {
            Debug.LogWarning("[TileInteractionLibrary] Within range alpha should typically be higher than outside range alpha for better visibility.");
        }
    }
}
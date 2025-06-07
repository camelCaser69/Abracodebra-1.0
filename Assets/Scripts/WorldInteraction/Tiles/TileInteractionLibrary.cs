using System.Collections.Generic;
using UnityEngine;
using System; // Needed for [Serializable]

// --- NEW: Define the structure for a refill rule ---
[Serializable] // Make it visible in the Inspector
public class ToolRefillRule
{
    [Tooltip("The tool that can be refilled.")]
    public ToolDefinition toolToRefill;

    [Tooltip("The tile that must be interacted with to trigger the refill.")]
    public TileDefinition refillSourceTile;
}
// ----------------------------------------------------

[CreateAssetMenu(fileName = "TileInteractionLibrary", menuName = "Tiles/Tile Interaction Library")]
public class TileInteractionLibrary : ScriptableObject
{
    [Header("Standard Tile Transformations")]
    [Tooltip("List of rules: (Tool, fromTile) => toTile.")]
    public List<TileInteractionRule> rules; // Existing transformation rules

    [Header("Tool Refill Rules")] // <<< NEW HEADER
    [Tooltip("List of rules defining how tools are refilled.")]
    public List<ToolRefillRule> refillRules; // <<< NEW LIST FOR REFILLS
}
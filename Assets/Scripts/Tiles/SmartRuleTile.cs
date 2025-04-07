using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; // Required for List

[CreateAssetMenu(fileName = "SmartRuleTile_", menuName = "2D/Tiles/Smart Rule Tile")]
public class SmartRuleTile : RuleTile
{
    [Header("Smart Tiling Sprites")]
    public Sprite centerSprite;
    [Space]
    public Sprite topEdge;
    public Sprite bottomEdge;
    public Sprite leftEdge;
    public Sprite rightEdge;
    [Space]
    public Sprite topLeftCorner;
    public Sprite topRightCorner;
    public Sprite bottomLeftCorner;
    public Sprite bottomRightCorner;

    [Header("Blending Options")]
    [Tooltip("Optional Tag: If set, it's intended for custom logic (e.g., Rule Transforms or advanced overrides), but base generation still uses This/NotThis.")]
    public string blendingTag = ""; // Store tag, but RuleMatch uses standard logic now

    // Override RuleMatch to provide the basic This/NotThis comparison logic
    // required by the RuleTile system when evaluating generated rules.
    public override bool RuleMatch(int neighbor, TileBase other)
    {
        // The core logic defined by the rules generated in the Editor relies on
        // these standard neighbor definitions.
        switch (neighbor)
        {
            case TilingRule.Neighbor.This: return other == this; // Is the other tile the same as this one?
            case TilingRule.Neighbor.NotThis: return other != this; // Is the other tile different from this one?
        }

        // Fallback for any other neighbor type (shouldn't usually happen with basic rules)
        return base.RuleMatch(neighbor, other);
    }
}
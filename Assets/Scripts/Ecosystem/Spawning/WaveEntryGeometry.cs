// FILE: Assets/Scripts/Ecosystem/Spawning/WaveEntryGeometry.cs
// ============================================================================
//  WAVE ENTRY GEOMETRY (Block 1 B4). Where a FixedEntry group appears, and where
//  the Planning marker for it sits. One definition for both, so the marker and
//  the spawn cannot drift apart. No RNG: the same entry gives the same point.
//
//  The point is derived from the main camera's view (the board has no scene
//  markers): the marker sits just INSIDE the view edge, the animal appears just
//  OUTSIDE it and walks in. (Zooming the camera between Planning and the Day
//  moves both together, but a marker read before a zoom is stale.)
// ============================================================================
using UnityEngine;

public static class WaveEntryGeometry
{
    /// <summary>World point on the chosen side of the view, pushed <paramref name="outward"/> units out (negative = inside).</summary>
    public static Vector2 EdgePoint(Camera cam, WaveEntryEdge edge, float along, float outward)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector2 c = cam.transform.position;
        float t = Mathf.Clamp01(along);

        switch (edge)
        {
            case WaveEntryEdge.West:
                return new Vector2(c.x - halfW - outward, Mathf.Lerp(c.y - halfH, c.y + halfH, t));
            case WaveEntryEdge.East:
                return new Vector2(c.x + halfW + outward, Mathf.Lerp(c.y - halfH, c.y + halfH, t));
            case WaveEntryEdge.North:
                return new Vector2(Mathf.Lerp(c.x - halfW, c.x + halfW, t), c.y + halfH + outward);
            default:
                return new Vector2(Mathf.Lerp(c.x - halfW, c.x + halfW, t), c.y - halfH - outward);
        }
    }

    /// <summary>Where the animals of this entry appear (just off the view).</summary>
    public static Vector2 SpawnPoint(Camera cam, WaveSpawnEntry entry, float offscreenMargin)
    {
        return EdgePoint(cam, entry.entryEdge, entry.entryAlong, offscreenMargin + 0.1f);
    }

    /// <summary>Where the Planning marker for this entry sits (just inside the view).</summary>
    public static Vector2 MarkerPoint(Camera cam, WaveSpawnEntry entry, float inset)
    {
        return EdgePoint(cam, entry.entryEdge, entry.entryAlong, -inset);
    }

    public static string EdgeName(WaveEntryEdge edge)
    {
        switch (edge)
        {
            case WaveEntryEdge.West: return "west";
            case WaveEntryEdge.East: return "east";
            case WaveEntryEdge.North: return "north";
            default: return "south";
        }
    }
}

using UnityEngine;

#region Using Statements
// This region is for AI formatting. It will be removed in the final output.
#endregion

// --- FIX: Changed from class to struct ---
public struct LeafData
{
    public Vector2Int GridCoord;
    public bool IsActive; // True if the leaf exists, false if it was eaten

    public LeafData(Vector2Int coord, bool isActive = true)
    {
        GridCoord = coord;
        IsActive = isActive;
    }
}
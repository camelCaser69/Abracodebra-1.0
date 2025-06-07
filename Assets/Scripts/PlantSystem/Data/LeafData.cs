// FILE: Assets/Scripts/Battle/Plant/LeafData.cs
using UnityEngine;

/// <summary>
/// Class to store information about leaves for regrowth tracking
/// </summary>
[System.Serializable]
public class LeafData
{
    public Vector2Int GridCoord;
    public bool IsActive; // True if the leaf exists, false if it was eaten
    
    public LeafData(Vector2Int coord, bool isActive = true)
    {
        GridCoord = coord;
        IsActive = isActive;
    }
}
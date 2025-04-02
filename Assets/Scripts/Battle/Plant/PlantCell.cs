// FILE: Assets/Scripts/Battle/Plant/PlantCell.cs (or similar path)
using UnityEngine;

public class PlantCell : MonoBehaviour
{
    // Set these references when the cell is spawned by PlantGrowth
    [HideInInspector] public PlantGrowth ParentPlantGrowth;
    [HideInInspector] public Vector2Int GridCoord;
    [HideInInspector] public PlantCellType CellType; // Store type info here

    // Called automatically by Unity when this GameObject is destroyed
    private void OnDestroy()
    {
        // Notify the parent plant that this cell is gone, if the parent still exists
        if (ParentPlantGrowth != null)
        {
            ParentPlantGrowth.ReportCellDestroyed(GridCoord);
        }
    }
}
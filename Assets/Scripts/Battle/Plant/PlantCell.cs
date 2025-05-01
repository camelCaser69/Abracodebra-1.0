// FILE: Assets/Scripts/Battle/Plant/PlantCell.cs
using UnityEngine;

// Define the enum here if it's closely tied to PlantCell,
// or move it to a separate file (e.g., PlantEnums.cs) for better organization.
public enum PlantCellType { Seed, Stem, Leaf, Flower, Fruit } // <<< ADDED ENUM DEFINITION

public class PlantCell : MonoBehaviour
{
    // Set these references when the cell is spawned by PlantGrowth
    [HideInInspector] public PlantGrowth ParentPlantGrowth;
    [HideInInspector] public Vector2Int GridCoord;
    [HideInInspector] public PlantCellType CellType; // Uses the enum defined above

    // Called automatically by Unity when this GameObject is destroyed
    private void OnDestroy()
    {
        // Notify the parent plant that this cell is gone, if the parent still exists
        // Ensure shadow is unregistered *before* destruction, handled in PlantGrowth.RemovePlantCell
        if (ParentPlantGrowth != null)
        {
            // Use the public method on PlantGrowth to handle removal logic cleanly
            // This ensures the dictionary and shadow are handled correctly
            // ParentPlantGrowth.HandleCellDestruction(this); // Or pass GridCoord if preferred
            // Let's stick to the original notification for now, cleanup is in PlantGrowth.RemovePlantCell
            ParentPlantGrowth.ReportCellDestroyed(GridCoord);
        }
    }
}
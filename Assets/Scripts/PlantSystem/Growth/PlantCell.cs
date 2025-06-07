// FILE: Assets/Scripts/Battle/Plant/PlantCell.cs (Fix Leaf Tracking)

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
        if (ParentPlantGrowth != null)
        {
            // If it's a leaf being destroyed, ensure it's marked as inactive for potential regrowth
            if (CellType == PlantCellType.Leaf)
            {
                // Call the plant growth to let it know this is a leaf being destroyed
                // This helps ensure we mark it as inactive for regrowth
                if (Debug.isDebugBuild)
                    Debug.Log($"[PlantCell OnDestroy] Leaf at {GridCoord} is being destroyed - notifying parent plant", gameObject);
            }
            
            // Notify parent to update tracking and handle removal
            ParentPlantGrowth.ReportCellDestroyed(GridCoord);
        }
    }
}
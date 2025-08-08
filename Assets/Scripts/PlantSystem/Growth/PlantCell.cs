// Reworked File: Assets/Scripts/PlantSystem/Growth/PlantCell.cs
using UnityEngine;

public enum PlantCellType { Seed, Stem, Leaf, Flower, Fruit }

public class PlantCell : MonoBehaviour
{
    [HideInInspector] public PlantGrowth ParentPlantGrowth;
    [HideInInspector] public Vector2Int GridCoord;
    [HideInInspector] public PlantCellType CellType;

    void OnDestroy()
    {
        // Notify the parent PlantGrowth component that this cell was destroyed.
        // The PlantGrowth component is responsible for updating its internal state.
        if (ParentPlantGrowth != null)
        {
            ParentPlantGrowth.ReportCellDestroyed(GridCoord);
        }
    }
}
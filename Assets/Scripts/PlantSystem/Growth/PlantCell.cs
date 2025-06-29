using UnityEngine;

public enum PlantCellType { Seed, Stem, Leaf, Flower, Fruit }

public class PlantCell : MonoBehaviour
{
    [HideInInspector] public PlantGrowth ParentPlantGrowth;
    [HideInInspector] public Vector2Int GridCoord;
    [HideInInspector] public PlantCellType CellType;

    void OnDestroy()
    {
        if (ParentPlantGrowth != null)
        {
            if (CellType == PlantCellType.Leaf)
            {
                if (Debug.isDebugBuild)
                    Debug.Log($"[PlantCell OnDestroy] Leaf at {GridCoord} is being destroyed - notifying parent plant", gameObject);
            }

            ParentPlantGrowth.ReportCellDestroyed(GridCoord);
        }
    }
}
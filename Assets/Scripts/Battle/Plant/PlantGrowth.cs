using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlantCellType
{
    Seed,
    Stem,
    Leaf,
    Flower,
    Fruit
}

public class PlantGrowth : MonoBehaviour
{
    [Header("Gene Parameters (set via Seed node effect)")]
    public int stemMinLength = 3;            // Minimum stem length (in cells)
    public int stemMaxLength = 6;            // Maximum stem length (in cells)
    public float growthSpeed = 1f;           // Time (seconds) per growth step
    [Tooltip("Leaf Gap: 0 = leaves on every stem cell; 1 = leaves on every 2nd stem cell; 2 = every 3rd, etc.")]
    public int leafGap = 1;                  // 0: leaves on every cell; 1: every 2nd cell, etc.
    public int leafPattern = 0;       // 0=parallel, 1=alternating
    public float growthRandomness = 0f; // 0..2
    
    [Header("Cell Prefabs")]
    public GameObject seedCellPrefab;
    public GameObject stemCellPrefab;
    public GameObject leafCellPrefab;

    [Header("Cell Grid Settings")]
    [Tooltip("Distance in game units between adjacent cells.")]
    public float cellSpacing = 8f; 

    // Local dictionary to track cell types at grid coordinates (local to plant)
    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();

    private bool growing = true;
    private int currentStemCount = 0;
    private int targetStemLength = 0;
    
    private bool leftSideNext = true; // for alternating pattern


    private void Start()
    {
        // Decide on target stem length between min and max.
        targetStemLength = Random.Range(stemMinLength, stemMaxLength + 1);
        Debug.Log($"[PlantGrowth] Target stem length: {targetStemLength}");

        // Place the seed cell at local grid coordinate (0,0)
        cells[new Vector2Int(0, 0)] = PlantCellType.Seed;
        SpawnCellVisual(PlantCellType.Seed, new Vector2Int(0, 0));

        StartCoroutine(GrowRoutine());
    }

    private IEnumerator GrowRoutine()
    {
        Vector2Int currentPos = new Vector2Int(0,0); // we start at the seed
        while (growing)
        {
            yield return new WaitForSeconds(growthSpeed);

            if (currentStemCount < targetStemLength)
            {
                currentStemCount++;

                Vector2Int dir = GetStemDirection();
                currentPos += dir; 
                // Mark cell as Stem
                cells[currentPos] = PlantCellType.Stem;
                SpawnCellVisual(PlantCellType.Stem, currentPos);

                // Leaf logic
                if ((currentStemCount % (leafGap + 1)) == 0)
                {
                    if (leafPattern == 0) // parallel
                    {
                        // if we consider left or right based on dir, you'd do something more complex
                        // For simplicity, assume left= Vector2Int(-1,0), right=Vector2Int(+1,0)
                        SpawnLeafIfEmpty(currentPos + new Vector2Int(-1,0));
                        SpawnLeafIfEmpty(currentPos + new Vector2Int(+1,0));
                    }
                    else // alternating
                    {
                        if (leftSideNext)
                        {
                            SpawnLeafIfEmpty(currentPos + new Vector2Int(-1,0));
                        }
                        else
                        {
                            SpawnLeafIfEmpty(currentPos + new Vector2Int(+1,0));
                        }
                        leftSideNext = !leftSideNext;
                    }
                }
            }
            else
            {
                growing = false;
            }
        }
    }
    
    private void SpawnLeafIfEmpty(Vector2Int coords)
    {
        if (!cells.ContainsKey(coords))
        {
            cells[coords] = PlantCellType.Leaf;
            SpawnCellVisual(PlantCellType.Leaf, coords);
        }
    }
    
    private Vector2Int GetStemDirection()
    {
        // (0,1) => up, (-1,1) => diag left, (1,1) => diag right
        Vector2Int[] possibleDirs = {
            new Vector2Int(0,1),
            new Vector2Int(-1,1),
            new Vector2Int(1,1)
        };

        // random approach:
        // If growthRandomness=0 => always up
        // If growthRandomness=1 => pick among all 3 with equal prob
        // If growthRandomness=2 => pick only among diag left & diag right

        float rand = Random.value; // [0..1)
        if (growthRandomness <= 0.01f)
        {
            // always up
            return possibleDirs[0];
        }
        else if (growthRandomness < 2f)
        {
            // pick among up, diag left, diag right with equal prob => 1/3 each
            int idx = Random.Range(0, 3);
            return possibleDirs[idx];
        }
        else
        {
            // pick only diag left or diag right => 2/3
            // or if you prefer 50-50, do:
            return (Random.value < 0.5f) ? possibleDirs[1] : possibleDirs[2];
        }
    }



    private void SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
        // Convert local grid coordinates to world position:
        // (transform.position is the plant's origin; each cell is spaced by cellSpacing)
        Vector2 worldPos = (Vector2)transform.position + (Vector2)coords * cellSpacing;
        GameObject prefabToUse = null;
        switch (cellType)
        {
            case PlantCellType.Seed:
                prefabToUse = seedCellPrefab;
                break;
            case PlantCellType.Stem:
                prefabToUse = stemCellPrefab;
                break;
            case PlantCellType.Leaf:
                prefabToUse = leafCellPrefab;
                break;
            // Add cases for Flower, Fruit as needed.
        }
        if (prefabToUse != null)
        {
            Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
        }
        else
        {
            Debug.LogWarning($"[PlantGrowth] No prefab assigned for cell type {cellType}");
        }
    }
}

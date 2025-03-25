using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

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
    [Header("Seed Gene Parameters (set via Seed node effect)")]
    public int stemMinLength = 3;            // Minimum stem length (cells)
    public int stemMaxLength = 6;            // Maximum stem length (cells)
    public float growthSpeed = 1f;           // Seconds per growth step
    [Tooltip("Leaf Gap: 0 = leaves on every stem cell; 1 = leaves on every 2nd stem cell; etc.")]
    public int leafGap = 1;
    public int leafPattern = 0;              // 0 = Parallel, 1 = Alternating
    public float growthRandomness = 0f;      // [0..1]: 0=always up; 1=always diagonal

    [Header("Energy System")]
    [Tooltip("Max energy available from all Energy Storage nodes in BFS.")]
    public float maxEnergy = 0f;
    [Tooltip("Current energy accumulated.")]
    public float currentEnergy = 0f;
    [Tooltip("Base photosynthesis rate from Energy Photosynthesis nodes in BFS.")]
    public float basePhotosynthesis = 0f;

    [Header("Cell Prefabs")]
    public GameObject seedCellPrefab;
    public GameObject stemCellPrefab;
    public GameObject leafCellPrefab;

    [Header("Cell Grid Settings")]
    [Tooltip("Distance (in game units) between adjacent cells.")]
    public float cellSpacing = 8f; 

    [Header("UI")]
    [Tooltip("TextMeshProUGUI displaying current energy and max energy below the plant.")]
    public TMP_Text energyText;

    private Dictionary<Vector2Int, PlantCellType> cells = new Dictionary<Vector2Int, PlantCellType>();
    private bool growing = true;
    private int currentStemCount = 0;
    private int targetStemLength = 0;
    private bool leftSideNext = true;

    private void Start()
    {
        targetStemLength = Random.Range(stemMinLength, stemMaxLength + 1);
        Debug.Log($"[PlantGrowth] Target stem length: {targetStemLength}");

        cells[new Vector2Int(0, 0)] = PlantCellType.Seed;
        SpawnCellVisual(PlantCellType.Seed, new Vector2Int(0, 0));

        // Set up sorting inheritance
        SortableEntity[] cellEntities = GetComponentsInChildren<SortableEntity>();
        foreach (var cellEntity in cellEntities)
        {
            // Skip the seed cell, which will determine sorting
            if (cellEntity.transform == transform)
                continue;

            // Use the public method to set parent Y coordinate usage
            cellEntity.SetUseParentYCoordinate(true);
        }

        StartCoroutine(GrowRoutine());
    }

    private void Update()
    {
        // Use sunIntensity from WeatherManager.
        float sunlight = (WeatherManager.Instance != null) ? WeatherManager.Instance.sunIntensity : 1f;
        float leafCount = cells.Values.Count(cell => cell == PlantCellType.Leaf);
        float deltaPhotosynthesis = basePhotosynthesis * leafCount * sunlight * Time.deltaTime;
        currentEnergy += deltaPhotosynthesis;
        if (currentEnergy > maxEnergy)
            currentEnergy = maxEnergy;

        if (energyText != null)
        {
            energyText.text = $"Energy: {Mathf.Floor(currentEnergy)}/{Mathf.Floor(maxEnergy)}";
            // Positioning remains as set in the prefab.
        }
    }

    private IEnumerator GrowRoutine()
    {
        Vector2Int currentPos = new Vector2Int(0, 0);
        while (growing)
        {
            yield return new WaitForSeconds(growthSpeed);

            if (currentStemCount < targetStemLength)
            {
                currentStemCount++;
                Vector2Int dir;
                if (currentStemCount == 1)
                {
                    // Always grow directly up for the first stem cell.
                    dir = new Vector2Int(0, 1);
                }
                else
                {
                    dir = GetStemDirection();
                }
                currentPos += dir;
                cells[currentPos] = PlantCellType.Stem;
                SpawnCellVisual(PlantCellType.Stem, currentPos);
                Debug.Log($"[PlantGrowth] Stem grown at {currentPos}");

                if ((currentStemCount % (leafGap + 1)) == 0)
                {
                    Vector2Int baseLeftPos = currentPos + new Vector2Int(-1, 0);
                    Vector2Int baseRightPos = currentPos + new Vector2Int(1, 0);

                    if (leafPattern == 0) // Parallel: spawn both leaves.
                    {
                        SpawnLeafIfEmpty(baseLeftPos);
                        SpawnLeafIfEmpty(baseRightPos);
                        Debug.Log($"[PlantGrowth] Parallel leaves at {baseLeftPos} and {baseRightPos}");
                    }
                    else // Alternating: spawn both but with one side offset.
                    {
                        if (leftSideNext)
                        {
                            Vector2Int leftAlt = baseLeftPos + new Vector2Int(0, 1);
                            SpawnLeafIfEmpty(leftAlt);
                            SpawnLeafIfEmpty(baseRightPos);
                            Debug.Log($"[PlantGrowth] Alternating leaves at {leftAlt} (offset) and {baseRightPos}");
                        }
                        else
                        {
                            Vector2Int rightAlt = baseRightPos + new Vector2Int(0, 1);
                            SpawnLeafIfEmpty(baseLeftPos);
                            SpawnLeafIfEmpty(rightAlt);
                            Debug.Log($"[PlantGrowth] Alternating leaves at {baseLeftPos} and {rightAlt} (offset)");
                        }
                        leftSideNext = !leftSideNext;
                    }
                }
            }
            else
            {
                growing = false;
                Debug.Log("[PlantGrowth] Growth complete.");
            }
        }
    }

    private Vector2Int GetStemDirection()
    {
        Vector2Int up = new Vector2Int(0, 1);
        Vector2Int leftDiag = new Vector2Int(-1, 1);
        Vector2Int rightDiag = new Vector2Int(1, 1);

        float r = Mathf.Clamp01(growthRandomness);
        float roll = Random.value;
        float threshold = 1f - r;  
        if (roll < threshold)
            return up;
        else
            return (Random.value < 0.5f) ? leftDiag : rightDiag;
    }

    private void SpawnLeafIfEmpty(Vector2Int coords)
    {
        if (!cells.ContainsKey(coords))
        {
            cells[coords] = PlantCellType.Leaf;
            SpawnCellVisual(PlantCellType.Leaf, coords);
        }
    }

    private void SpawnCellVisual(PlantCellType cellType, Vector2Int coords)
    {
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
        }
        if (prefabToUse != null)
        {
            GameObject cellInstance = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
        
            // Add SortableEntity if not already present
            SortableEntity sortableEntity = cellInstance.GetComponent<SortableEntity>();
            if (sortableEntity == null)
                sortableEntity = cellInstance.AddComponent<SortableEntity>();
        
            // For non-seed cells, enable "Y from parent"
            if (cellType != PlantCellType.Seed)
            {
                sortableEntity.SetUseParentYCoordinate(true);
            }
        }
        else
        {
            Debug.LogWarning($"[PlantGrowth] No prefab assigned for cell type {cellType}");
        }
    }
}
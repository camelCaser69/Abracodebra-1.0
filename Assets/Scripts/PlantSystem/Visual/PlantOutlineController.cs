using System.Collections.Generic;
using UnityEngine;

public class PlantOutlineController : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private bool excludeOuterCorners = false;
    [SerializeField] private bool excludeBaseCell = true;
    [SerializeField] public GameObject outlinePartPrefab; // Made public to fix access issue

    [SerializeField] private string outlineSortingLayerName = "Default";
    [SerializeField] private int outlineSortingOrder = -1;

    [SerializeField] private bool debugLogging = false;

    public Color OutlineColor => outlineColor;
    public int OutlineSortingLayer => outlineSortingLayerID;
    public int OutlineSortingOrder => outlineSortingOrder;

    private int outlineSortingLayerID;
    private PlantGrowth parentPlantGrowth;
    private Dictionary<Vector2Int, OutlinePartController> outlinePartMap = new Dictionary<Vector2Int, OutlinePartController>();
    private HashSet<Vector2Int> plantCellCoords = new HashSet<Vector2Int>();

    private static readonly Vector2Int[] neighborOffsets = new Vector2Int[]
    {
        new Vector2Int(-1, -1), // Down-Left
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, -1),  // Down-Right
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 1),  // Up-Left
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 1),   // Up-Right
    };

    private static readonly Vector2Int[] cardinalOffsets = new Vector2Int[]
    {
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, 1),   // Up
    };

    void Awake()
    {
        parentPlantGrowth = GetComponentInParent<PlantGrowth>();
        if (parentPlantGrowth == null)
        {
            Debug.LogError($"[{gameObject.name} Awake] Missing PlantGrowth parent!", gameObject);
            enabled = false;
            return;
        }

        if (outlinePartPrefab == null)
        {
            Debug.LogError($"[{gameObject.name} Awake] Outline Part Prefab not assigned!", gameObject);
            enabled = false;
            return;
        }

        outlineSortingLayerID = SortingLayer.NameToID(outlineSortingLayerName);
        if (outlineSortingLayerID == 0 && outlineSortingLayerName != "Default")
        {
            Debug.LogWarning($"[{gameObject.name} Awake] Sorting Layer '{outlineSortingLayerName}' not found, using 'Default'.");
            outlineSortingLayerID = SortingLayer.NameToID("Default");
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (debugLogging)
            Debug.Log($"[{gameObject.name} Awake] Initialized outline controller for {parentPlantGrowth.gameObject.name}");
    }

    // Fixed: Added RegisterPlantPart method that was missing
    public void RegisterPlantPart(SpriteRenderer plantPartRenderer, GameObject outlinePartPrefab)
    {
        if (plantPartRenderer == null || outlinePartPrefab == null)
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] RegisterPlantPart: Null parameters provided");
            return;
        }

        // Get the plant cell component to determine coordinates
        PlantCell plantCell = plantPartRenderer.GetComponentInParent<PlantCell>();
        if (plantCell != null)
        {
            OnPlantCellAdded(plantCell.GridCoord, plantCell.gameObject);
        }
        else if (debugLogging)
        {
            Debug.LogWarning($"[{gameObject.name}] RegisterPlantPart: No PlantCell found for renderer {plantPartRenderer.name}");
        }
    }

    public void OnPlantCellAdded(Vector2Int plantCoord, GameObject plantCellGO)
    {
        if (plantCellGO == null)
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] OnPlantCellAdded: Null GameObject at {plantCoord}");
            return;
        }

        plantCellCoords.Add(plantCoord);

        RemoveOutlinePartIfExists(plantCoord);

        SpriteRenderer plantRenderer = plantCellGO.GetComponentInChildren<SpriteRenderer>();
        if (plantRenderer == null)
        {
            Debug.LogWarning($"Plant cell added at {plantCoord} missing SpriteRenderer.", plantCellGO);
            return;
        }

        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoord = plantCoord + offset;

            if (!plantCellCoords.Contains(neighborCoord) &&
                !outlinePartMap.ContainsKey(neighborCoord))
            {
                if (ShouldExcludeOutlineAt(neighborCoord))
                    continue;

                CreateOutlinePart(neighborCoord, plantRenderer);
            }
        }

        if (debugLogging)
            Debug.Log($"[{gameObject.name}] Added cell at {plantCoord}, now tracking {plantCellCoords.Count} cells and {outlinePartMap.Count} outline parts");
    }

    public void OnPlantCellRemoved(Vector2Int plantCoord)
    {
        if (!plantCellCoords.Contains(plantCoord))
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] OnPlantCellRemoved: Coordinate {plantCoord} not found in plant cells!");
            return;
        }

        plantCellCoords.Remove(plantCoord);

        if (debugLogging)
            Debug.Log($"[{gameObject.name}] Removed cell at {plantCoord}, now have {plantCellCoords.Count} cells");

        if (!outlinePartMap.ContainsKey(plantCoord))
        {
            if (HasPlantNeighbor(plantCoord))
            {
                SpriteRenderer sourceRenderer = FindValidNeighborRenderer(plantCoord);
                if (sourceRenderer != null && !ShouldExcludeOutlineAt(plantCoord))
                {
                    CreateOutlinePart(plantCoord, sourceRenderer);
                    if (debugLogging)
                        Debug.Log($"[{gameObject.name}] Created new outline at removed cell position {plantCoord}");
                }
            }
        }

        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoord = plantCoord + offset;

            if (outlinePartMap.TryGetValue(neighborCoord, out OutlinePartController outlinePart))
            {
                if (outlinePart == null)
                {
                    outlinePartMap.Remove(neighborCoord);
                    if (debugLogging)
                        Debug.Log($"[{gameObject.name}] Removed null outline at {neighborCoord} from dictionary");
                    continue;
                }

                bool neighborStillHasPlantNeighbor = HasPlantNeighbor(neighborCoord);

                if (!neighborStillHasPlantNeighbor)
                {
                    RemoveOutlinePartIfExists(neighborCoord);
                    if (debugLogging)
                        Debug.Log($"[{gameObject.name}] Removed orphaned outline at {neighborCoord}");
                }
                else if (!outlinePart.IsSourceRendererValid())
                {
                    SpriteRenderer newSource = FindValidNeighborRenderer(neighborCoord);
                    if (newSource != null)
                    {
                        outlinePart.UpdateSourceRenderer(newSource);
                        outlinePart.SyncSpriteAndTransform();
                        if (debugLogging)
                            Debug.Log($"[{gameObject.name}] Updated source for outline at {neighborCoord}");
                    }
                    else
                    {
                        if (debugLogging)
                            Debug.LogWarning($"[{gameObject.name}] Outline at {neighborCoord} lost source but HasPlantNeighbor=true. Removing.");
                        RemoveOutlinePartIfExists(neighborCoord);
                    }
                }
            }
            else if (!plantCellCoords.Contains(neighborCoord) && HasPlantNeighbor(neighborCoord))
            {
                if (!ShouldExcludeOutlineAt(neighborCoord))
                {
                    SpriteRenderer sourceRenderer = FindValidNeighborRenderer(neighborCoord);
                    if (sourceRenderer != null)
                    {
                        CreateOutlinePart(neighborCoord, sourceRenderer);
                        if (debugLogging)
                            Debug.Log($"[{gameObject.name}] Created new outline at neighbor {neighborCoord} after cell removal");
                    }
                }
            }
        }
    }

    void CreateOutlinePart(Vector2Int coord, SpriteRenderer sourceRenderer)
    {
        if (outlinePartPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] CreateOutlinePart: outlinePartPrefab is null!");
            return;
        }

        if (sourceRenderer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] CreateOutlinePart: sourceRenderer is null for coord {coord}");
            return;
        }

        if (outlinePartMap.ContainsKey(coord))
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] CreateOutlinePart: Outline already exists at {coord}");
            return;
        }

        GameObject outlineInstance = Instantiate(outlinePartPrefab, transform);
        if (outlineInstance == null)
        {
            Debug.LogError($"[{gameObject.name}] CreateOutlinePart: Failed to instantiate outline prefab!");
            return;
        }

        float spacing = parentPlantGrowth.GetCellSpacing();
        outlineInstance.transform.localPosition = (Vector2)coord * spacing;

        OutlinePartController outlineController = outlineInstance.GetComponent<OutlinePartController>();
        if (outlineController != null)
        {
            outlineController.Initialize(sourceRenderer, coord, this);
            outlineController.SetVisibility(true);
            outlinePartMap.Add(coord, outlineController);

            if (debugLogging)
                Debug.Log($"[{gameObject.name}] Created outline part at {coord} using source {sourceRenderer.gameObject.name}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Outline Part Prefab missing OutlinePartController script!", outlinePartPrefab);
            Destroy(outlineInstance);
        }
    }

    void RemoveOutlinePartIfExists(Vector2Int coord)
    {
        if (outlinePartMap.TryGetValue(coord, out OutlinePartController outlinePart))
        {
            if (outlinePart != null)
            {
                outlinePart.DestroyOutlinePart();
                if (debugLogging)
                    Debug.Log($"[{gameObject.name}] Destroyed outline part at {coord}");
            }
            outlinePartMap.Remove(coord);
        }
    }

    bool HasPlantNeighbor(Vector2Int coord)
    {
        foreach (Vector2Int offset in neighborOffsets)
        {
            if (plantCellCoords.Contains(coord + offset))
            {
                return true;
            }
        }
        return false;
    }

    SpriteRenderer FindValidNeighborRenderer(Vector2Int coord)
    {
        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoord = coord + offset;

            if (plantCellCoords.Contains(neighborCoord))
            {
                GameObject plantGO = parentPlantGrowth.GetCellGameObjectAt(neighborCoord);
                if (plantGO != null)
                {
                    SpriteRenderer renderer = plantGO.GetComponentInChildren<SpriteRenderer>();
                    if (renderer != null)
                    {
                        if (debugLogging)
                            Debug.Log($"[{gameObject.name}] Found valid renderer at {neighborCoord} for outline at {coord}");
                        return renderer;
                    }
                }
            }
        }

        if (debugLogging)
            Debug.LogWarning($"[{gameObject.name}] Could not find any valid neighbor renderer for {coord}");
        return null;
    }

    bool ShouldExcludeOutlineAt(Vector2Int coord)
    {
        if (excludeBaseCell && coord == Vector2Int.down && plantCellCoords.Contains(Vector2Int.zero))
        {
            return true;
        }

        if (excludeOuterCorners && IsOuterCornerCandidate(coord))
        {
            return true;
        }

        return false;
    }

    bool IsOuterCornerCandidate(Vector2Int coord)
    {
        if (plantCellCoords.Contains(coord))
            return false;

        int plantNeighborCount = 0;
        foreach (Vector2Int offset in neighborOffsets)
        {
            if (plantCellCoords.Contains(coord + offset))
            {
                plantNeighborCount++;
            }
        }

        return plantNeighborCount == 3;
    }

    void OnDestroy()
    {
        foreach (var kvp in outlinePartMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.DestroyOutlinePart();
            }
        }
        outlinePartMap.Clear();
        plantCellCoords.Clear();
    }
}
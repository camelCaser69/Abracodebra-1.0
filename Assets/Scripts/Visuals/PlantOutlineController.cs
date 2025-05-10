// FILE: Assets/Scripts/Visuals/PlantOutlineController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class PlantOutlineController : MonoBehaviour
{
    // --- Fields ---
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private bool excludeOuterCorners = false;
    [SerializeField] private bool excludeBaseCell = true;
    [SerializeField] private GameObject outlinePartPrefab;
    
    [Header("Performance Options")]
    [SerializeField] private bool batchOutlineUpdates = true;
    [SerializeField] [Range(0.05f, 0.5f)] private float updateInterval = 0.1f;

    [Header("Sorting")]
    [SerializeField] private string outlineSortingLayerName = "Default";
    [SerializeField] private int outlineSortingOrder = -1;

    [Header("Debugging")]
    [SerializeField] private bool debugLogging = false;

    // Public accessors
    public Color OutlineColor => outlineColor;
    public int OutlineSortingLayer => outlineSortingLayerID;
    public int OutlineSortingOrder => outlineSortingOrder;

    // Internal State
    private int outlineSortingLayerID;
    private PlantGrowth parentPlantGrowth;
    private Dictionary<Vector2Int, OutlinePartController> outlinePartMap = new Dictionary<Vector2Int, OutlinePartController>();
    private HashSet<Vector2Int> plantCellCoords = new HashSet<Vector2Int>();
    
    private float lastUpdateTime = 0f;

    // Neighbor offsets - 8 directions around a cell
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

    // Just the cardinal directions (for certain operations)
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
        
        // Get the proper sorting layer ID from the name
        outlineSortingLayerID = SortingLayer.NameToID(outlineSortingLayerName);
        if (outlineSortingLayerID == 0 && outlineSortingLayerName != "Default")
        {
            Debug.LogWarning($"[{gameObject.name} Awake] Sorting Layer '{outlineSortingLayerName}' not found, using 'Default'.");
            outlineSortingLayerID = SortingLayer.NameToID("Default");
        }
        
        // Zero out local transform values to avoid unexpected visual glitches
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        if (debugLogging)
            Debug.Log($"[{gameObject.name} Awake] Initialized outline controller for {parentPlantGrowth.gameObject.name}");
    }

    // --- Methods Called by PlantGrowth ---

    // OnPlantCellAdded - Called when a new plant cell is added
    public void OnPlantCellAdded(Vector2Int plantCoord, GameObject plantCellGO)
    {
        if (plantCellGO == null)
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] OnPlantCellAdded: Null GameObject at {plantCoord}");
            return;
        }
        
        // Add to our plant cells set
        plantCellCoords.Add(plantCoord);
        
        // Remove any existing outline at the plant location (should be clear already, but safety)
        RemoveOutlinePartIfExists(plantCoord);
        
        // Get the SpriteRenderer from the plant cell
        SpriteRenderer plantRenderer = plantCellGO.GetComponentInChildren<SpriteRenderer>();
        if (plantRenderer == null)
        {
            Debug.LogWarning($"Plant cell added at {plantCoord} missing SpriteRenderer.", plantCellGO);
            return;
        }
        
        // Check all neighboring coordinates around this plant cell
        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoord = plantCoord + offset;
            
            // Only create outline parts where:
            // 1. There's no plant cell
            // 2. There's no outline part already
            // 3. It passes any exclusion rules
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

    // OnPlantCellRemoved - Called when a plant cell is removed
    public void OnPlantCellRemoved(Vector2Int plantCoord)
    {
        if (!plantCellCoords.Contains(plantCoord))
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] OnPlantCellRemoved: Coordinate {plantCoord} not found in plant cells!");
            return;
        }
        
        // Remove the coordinate from our internal tracking
        plantCellCoords.Remove(plantCoord);
        
        if (debugLogging)
            Debug.Log($"[{gameObject.name}] Removed cell at {plantCoord}, now have {plantCellCoords.Count} cells");
        
        // 1. Check if outline should appear *at the removed location*
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
        
        // 2. Re-evaluate all neighboring coordinates
        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoord = plantCoord + offset;
            
            // Check if an outline exists at this neighbor
            if (outlinePartMap.TryGetValue(neighborCoord, out OutlinePartController outlinePart))
            {
                // Safety check for destroyed outline part
                if (outlinePart == null)
                {
                    outlinePartMap.Remove(neighborCoord);
                    if (debugLogging)
                        Debug.Log($"[{gameObject.name}] Removed null outline at {neighborCoord} from dictionary");
                    continue;
                }
                
                // Check if this neighbor still needs an outline
                bool neighborStillHasPlantNeighbor = HasPlantNeighbor(neighborCoord);
                
                if (!neighborStillHasPlantNeighbor)
                {
                    // No longer has any plant neighbors, remove it
                    RemoveOutlinePartIfExists(neighborCoord);
                    if (debugLogging)
                        Debug.Log($"[{gameObject.name}] Removed orphaned outline at {neighborCoord}");
                }
                else if (!outlinePart.IsSourceRendererValid())
                {
                    // Outline's source was likely the removed cell, update it
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
                        // This case is unlikely but could happen in complex removals
                        if (debugLogging)
                            Debug.LogWarning($"[{gameObject.name}] Outline at {neighborCoord} lost source but HasPlantNeighbor=true. Removing.");
                        RemoveOutlinePartIfExists(neighborCoord);
                    }
                }
            }
            // If no outline at this neighbor, but there should be one (e.g., it was excluded before)
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

    // --- Internal Helper Methods ---

    // CreateOutlinePart - Creates an outline part at the specified coordinates
    private void CreateOutlinePart(Vector2Int coord, SpriteRenderer sourceRenderer)
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
        
        // Check if already exists (safety)
        if (outlinePartMap.ContainsKey(coord))
        {
            if (debugLogging)
                Debug.LogWarning($"[{gameObject.name}] CreateOutlinePart: Outline already exists at {coord}");
            return;
        }
        
        // Instantiate the outline part
        GameObject outlineInstance = Instantiate(outlinePartPrefab, transform);
        if (outlineInstance == null)
        {
            Debug.LogError($"[{gameObject.name}] CreateOutlinePart: Failed to instantiate outline prefab!");
            return;
        }
        
        // Set position using PlantGrowth's cell spacing
        float spacing = parentPlantGrowth.GetCellSpacing();
        outlineInstance.transform.localPosition = (Vector2)coord * spacing;
        
        // Get the controller and initialize it
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

    // RemoveOutlinePartIfExists - Removes an outline part if it exists at the specified coordinates
    private void RemoveOutlinePartIfExists(Vector2Int coord)
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

    // HasPlantNeighbor - Checks if the specified coordinates have any plant neighbors
    private bool HasPlantNeighbor(Vector2Int coord)
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

    // FindValidNeighborRenderer - Finds a valid SpriteRenderer from a neighboring plant cell
    private SpriteRenderer FindValidNeighborRenderer(Vector2Int coord)
    {
        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoord = coord + offset;
            
            // Check if there's a plant cell at this coordinate
            if (plantCellCoords.Contains(neighborCoord))
            {
                // Get the GameObject from PlantGrowth
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

    // ShouldExcludeOutlineAt - Checks if an outline should be excluded at the specified coordinates
    private bool ShouldExcludeOutlineAt(Vector2Int coord)
    {
        // Exclude base cell (e.g., under the seed) if enabled
        if (excludeBaseCell && coord == Vector2Int.down && plantCellCoords.Contains(Vector2Int.zero))
        {
            return true;
        }
        
        // Exclude outer corners if enabled
        if (excludeOuterCorners && IsOuterCornerCandidate(coord))
        {
            return true;
        }
        
        return false;
    }

    // IsOuterCornerCandidate - Checks if the coordinate is a potential outer corner
    private bool IsOuterCornerCandidate(Vector2Int coord)
    {
        // If it's a plant cell, it's not an outer corner
        if (plantCellCoords.Contains(coord))
            return false;
            
        // Count the number of plant neighbors
        int plantNeighborCount = 0;
        foreach (Vector2Int offset in neighborOffsets)
        {
            if (plantCellCoords.Contains(coord + offset)) {
                plantNeighborCount++;
            }
        }
        
        // In typical 2D grid outline detection, an outer corner has 3 neighbors
        return plantNeighborCount == 3;
    }

    void OnDestroy()
    {
        // Clean up resources when destroyed
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
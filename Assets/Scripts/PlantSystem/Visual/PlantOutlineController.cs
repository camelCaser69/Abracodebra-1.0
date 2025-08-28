// REWORKED FILE: Assets/Scripts/PlantSystem/Visual/PlantOutlineController.cs
using UnityEngine;
using System.Collections.Generic;
using Abracodabra.Genes;

public class PlantOutlineController : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] public GameObject outlinePartPrefab;
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

    private static readonly Vector2Int[] neighborOffsets = {
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(-1, 0),                         new Vector2Int(1, 0),
        new Vector2Int(-1, 1),  new Vector2Int(0, 1),  new Vector2Int(1, 1)
    };

    void Awake()
    {
        parentPlantGrowth = GetComponentInParent<PlantGrowth>();
        if (parentPlantGrowth == null)
        {
            Debug.LogError($"[{gameObject.name}] Missing PlantGrowth parent!", gameObject);
            enabled = false;
            return;
        }
        outlineSortingLayerID = SortingLayer.NameToID(outlineSortingLayerName);
    }

    public void RegisterPlantPart(SpriteRenderer plantPartRenderer, GameObject prefab)
    {
        if (plantPartRenderer == null) return;
        PlantCell plantCell = plantPartRenderer.GetComponentInParent<PlantCell>();
        if (plantCell != null)
        {
            OnPlantCellAdded(plantCell.GridCoord, plantCell.gameObject);
        }
    }

    public void OnPlantCellAdded(Vector2Int plantCoord, GameObject plantCellGO)
    {
        if (plantCellGO == null) return;

        plantCellCoords.Add(plantCoord);
        RemoveOutlinePartIfExists(plantCoord);

        SpriteRenderer plantRenderer = plantCellGO.GetComponentInChildren<SpriteRenderer>();
        if (plantRenderer == null) return;

        foreach (var offset in neighborOffsets)
        {
            Vector2Int neighborCoord = plantCoord + offset;
            if (!plantCellCoords.Contains(neighborCoord) && !outlinePartMap.ContainsKey(neighborCoord))
            {
                CreateOutlinePart(neighborCoord, plantRenderer);
            }
        }
    }

    public void OnPlantCellRemoved(Vector2Int plantCoord)
    {
        if (!plantCellCoords.Remove(plantCoord)) return;

        if (!outlinePartMap.ContainsKey(plantCoord))
        {
            SpriteRenderer sourceRenderer = FindValidNeighborRenderer(plantCoord);
            if (sourceRenderer != null)
            {
                CreateOutlinePart(plantCoord, sourceRenderer);
            }
        }

        foreach (var offset in neighborOffsets)
        {
            Vector2Int neighborCoord = plantCoord + offset;
            if (outlinePartMap.TryGetValue(neighborCoord, out var outlinePart))
            {
                if (outlinePart == null)
                {
                    outlinePartMap.Remove(neighborCoord);
                    continue;
                }
                if (!HasPlantNeighbor(neighborCoord))
                {
                    RemoveOutlinePartIfExists(neighborCoord);
                }
                else if (!outlinePart.IsSourceRendererValid())
                {
                    SpriteRenderer newSource = FindValidNeighborRenderer(neighborCoord);
                    if (newSource != null)
                    {
                        outlinePart.UpdateSourceRenderer(newSource);
                    }
                    else
                    {
                        RemoveOutlinePartIfExists(neighborCoord);
                    }
                }
            }
        }
    }

    void CreateOutlinePart(Vector2Int coord, SpriteRenderer sourceRenderer) {
        if (outlinePartMap.ContainsKey(coord) || sourceRenderer == null || outlinePartPrefab == null) {
            return;
        }
    
        GameObject outlineInstance = Instantiate(outlinePartPrefab, transform);
    
        // Use the plant's spacing calculation
        float spacing = 1f / 6f; // Default fallback
        if (parentPlantGrowth != null) {
            spacing = parentPlantGrowth.GetCellSpacingInWorldUnits();
        }
    
        outlineInstance.transform.localPosition = (Vector2)coord * spacing;
    
        OutlinePartController outlineController = outlineInstance.GetComponent<OutlinePartController>();
        if (outlineController != null) {
            outlineController.Initialize(sourceRenderer, coord, this);
            outlinePartMap.Add(coord, outlineController);
        
            if (debugLogging) {
                Debug.Log($"[{gameObject.name}] Created outline part at {coord} with spacing {spacing}");
            }
        }
        else {
            Debug.LogError("Outline Part Prefab is missing the OutlinePartController script!", outlinePartPrefab);
            Destroy(outlineInstance);
        }
    }

    private void RemoveOutlinePartIfExists(Vector2Int coord)
    {
        if (outlinePartMap.TryGetValue(coord, out var part))
        {
            if (part != null) part.DestroyOutlinePart();
            outlinePartMap.Remove(coord);
        }
    }

    private bool HasPlantNeighbor(Vector2Int coord)
    {
        foreach (var offset in neighborOffsets)
            if (plantCellCoords.Contains(coord + offset)) return true;
        return false;
    }

    private SpriteRenderer FindValidNeighborRenderer(Vector2Int coord)
    {
        foreach (var offset in neighborOffsets)
        {
            Vector2Int neighborCoord = coord + offset;
            if (plantCellCoords.Contains(neighborCoord))
            {
                GameObject plantGO = parentPlantGrowth.GetCellGameObjectAt(neighborCoord);
                if (plantGO != null && plantGO.TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    return renderer;
                }
            }
        }
        // FIX: Using the debug flag
        if (debugLogging)
            Debug.LogWarning($"[{gameObject.name}] Could not find any valid neighbor renderer for outline at {coord}");
        return null;
    }

}
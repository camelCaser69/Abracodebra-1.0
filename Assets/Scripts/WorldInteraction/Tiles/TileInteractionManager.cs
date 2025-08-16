using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using skner.DualGrid;
using TMPro;
using WegoSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TileInteractionManager : SingletonMonoBehaviour<TileInteractionManager>, ITickUpdateable
{
    [System.Serializable]
    public class TileDefinitionMapping
    {
        public TileDefinition tileDef;
        public DualGridTilemapModule tilemapModule;
    }

    public struct TimedTileState
    {
        public TileDefinition tileDef;
        public int ticksRemaining;
    }

    [Tooltip("Define tilemap layers here. For logic, overlays (keepBottomTile=false) are checked first, then base layers (keepBottomTile=true).")]
    public List<TileDefinitionMapping> tileDefinitionMappings;
    public TileInteractionLibrary interactionLibrary;
    public Grid interactionGrid;
    public Camera mainCamera;
    public Transform player;

    public float hoverRadius = 3f;

    public GameObject hoverHighlightObject;
    public TileHoverColorManager hoverColorManager;
    public int baseSortingOrder = 0;

    public bool debugLogs = false;
    public TextMeshProUGUI hoveredTileText;
    public TextMeshProUGUI currentToolText;

    private Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
    private Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();
    private Vector3Int? currentlyHoveredCell;
    private TileDefinition hoveredTileDef;
    private SpriteRenderer hoverSpriteRenderer;
    private bool isWithinInteractionRange = false;

    protected override void OnAwake()
    {
        SetupTilemaps();
        CacheHoverSpriteRenderer();
    }

    void Start()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        UpdateReversionTicks();
    }

    void Update()
    {
        HandleTileHover();
        UpdateDebugUI();
    }

    public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        // First Pass: Search for an overlay tile (e.g., Wet Dirt, Tilled Dirt).
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && !mapping.tileDef.keepBottomTile && mapping.tilemapModule != null)
            {
                if (TileExistsInModule(mapping.tilemapModule, cellPos))
                {
                    return mapping.tileDef;
                }
            }
        }

        // Second Pass: If no overlay was found, search for a base tile (e.g., Dirt, Grass).
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tileDef.keepBottomTile && mapping.tilemapModule != null)
            {
                if (TileExistsInModule(mapping.tilemapModule, cellPos))
                {
                    return mapping.tileDef;
                }
            }
        }
        
        return null;
    }

    private bool TileExistsInModule(DualGridTilemapModule module, Vector3Int cellPos)
    {
        if (module.DataTilemap != null && module.DataTilemap.HasTile(cellPos)) return true;
        
        Transform renderTransform = module.transform.Find("RenderTilemap");
        if (renderTransform?.GetComponent<Tilemap>() is Tilemap renderTilemap && renderTilemap.HasTile(cellPos))
        {
            return true;
        }
        return false;
    }
    
    public void ApplyToolAction(ToolDefinition toolDef)
    {
        if (toolDef == null || !currentlyHoveredCell.HasValue) return;
        Vector3Int targetCell = currentlyHoveredCell.Value;
        TileDefinition currentTileDef = FindWhichTileDefinitionAt(targetCell);
        if (currentTileDef == null) return;

        TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == currentTileDef);

        if (rule != null)
        {
            if (debugLogs) Debug.Log($"[TileInteractionManager] Rule found! Transforming '{currentTileDef.displayName}' to '{(rule.toTile != null ? rule.toTile.displayName : "NULL")}'.");

            // THIS IS THE DEFINITIVE FIX:
            // An overlay (like Wet Dirt) has keepBottomTile = false.
            // A base layer (like Dirt) has keepBottomTile = true.
            // We ONLY remove the original tile IF the NEW tile is a base layer that should replace it.
            // We DO NOT remove the original tile IF the NEW tile is an overlay.
            if (rule.toTile != null && rule.toTile.keepBottomTile)
            {
                RemoveTile(currentTileDef, targetCell);
            }
            else if (rule.toTile == null) // This is an explicit removal rule (e.g. shovel)
            {
                RemoveTile(currentTileDef, targetCell);
            }
            
            // Place the new tile regardless. If it's an overlay, it goes on top.
            // If it's a base layer, the old one has already been removed.
            if (rule.toTile != null)
            {
                PlaceTile(rule.toTile, targetCell);
            }
        }
        else if (debugLogs)
        {
            Debug.Log($"[TileInteractionManager] No transformation rule found for Tool='{toolDef.displayName}' on Tile='{currentTileDef.displayName}'.");
        }
    }

    private void UpdateReversionTicks()
    {
        if (timedCells.Count == 0) return;
        List<Vector3Int> cellsToProcess = timedCells.Keys.ToList();

        foreach (Vector3Int cellPos in cellsToProcess)
        {
            if (timedCells.TryGetValue(cellPos, out TimedTileState state))
            {
                state.ticksRemaining--;
                if (state.ticksRemaining <= 0)
                {
                    TileDefinition actualTile = FindWhichTileDefinitionAt(cellPos);
                    if (actualTile == state.tileDef)
                    {
                        if (debugLogs) Debug.Log($"[TileInteractionManager] Reverting tile '{state.tileDef.displayName}' at {cellPos}.");
                        RemoveTile(state.tileDef, cellPos);
                        if (state.tileDef.revertToTile != null)
                        {
                            PlaceTile(state.tileDef.revertToTile, cellPos);
                        }
                    }
                    else
                    {
                        if (debugLogs) Debug.LogWarning($"[TileInteractionManager] State desync detected at {cellPos}. Expected '{state.tileDef.displayName}' for reversion, but found '{(actualTile != null ? actualTile.displayName : "NULL")}'. Removing stale timer.");
                        timedCells.Remove(cellPos);
                    }
                }
                else
                {
                    timedCells[cellPos] = state;
                }
            }
        }
    }

    public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null) return;
        if (moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module) && module?.DataTilemap != null)
        {
            module.DataTilemap.SetTile(cellPos, ScriptableObject.CreateInstance<Tile>());
            if (tileDef.revertAfterTicks > 0)
            {
                timedCells[cellPos] = new TimedTileState { tileDef = tileDef, ticksRemaining = tileDef.revertAfterTicks };
            }
        }
    }

    public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null) return;
        if (moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module) && module?.DataTilemap != null)
        {
            module.DataTilemap.SetTile(cellPos, null);
        }
        
        if (timedCells.TryGetValue(cellPos, out TimedTileState timedState) && timedState.tileDef == tileDef)
        {
            timedCells.Remove(cellPos);
        }
    }
    
    private void CacheHoverSpriteRenderer()
    {
        if (hoverHighlightObject != null)
        {
            hoverSpriteRenderer = hoverHighlightObject.GetComponent<SpriteRenderer>();
        }
    }

    private void SetupTilemaps()
    {
        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        var definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();
        if (tileDefinitionMappings == null) return;
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tilemapModule != null)
            {
                if (!moduleByDefinition.ContainsKey(mapping.tileDef))
                {
                    moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                    definitionByModule[mapping.tilemapModule] = mapping.tileDef;
                }
                else
                {
                    Debug.LogWarning($"[TileInteractionManager] Duplicate TileDefinition '{mapping.tileDef.displayName}' found in mappings.", mapping.tileDef);
                }
            }
        }
    }

    public void UpdateSortingOrder()
    {
        if (tileDefinitionMappings == null) return;
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping?.tilemapModule != null)
            {
                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    if (renderTilemapTransform.GetComponent<TilemapRenderer>() is TilemapRenderer renderer)
                    {
                        renderer.sortingOrder = baseSortingOrder - i;
                        #if UNITY_EDITOR
                        if (!Application.isPlaying) EditorUtility.SetDirty(renderer);
                        #endif
                    }
                }
            }
        }
    }

    public void UpdateAllColors()
    {
        if (tileDefinitionMappings == null) return;
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tilemapModule != null)
            {
                Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                if (renderTilemapTransform != null)
                {
                    if (renderTilemapTransform.GetComponent<Tilemap>() is Tilemap renderTilemap)
                    {
                        renderTilemap.color = mapping.tileDef.tintColor;
                        #if UNITY_EDITOR
                        if (!Application.isPlaying) EditorUtility.SetDirty(renderTilemap);
                        #endif
                    }
                }
            }
        }
    }
    
    private void HandleTileHover()
    {
        if (mainCamera == null || player == null) return;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int cellPos = WorldToCell(mouseWorldPos);

        if (player.GetComponent<GridEntity>() is GridEntity playerGrid)
        {
            int gridRadius = Mathf.CeilToInt(hoverRadius);
            GridPosition playerGridPos = playerGrid.Position;
            GridPosition hoveredGridPos = new GridPosition(cellPos);
            isWithinInteractionRange = GridRadiusUtility.IsWithinCircleRadius(hoveredGridPos, playerGridPos, gridRadius);
        }

        hoveredTileDef = FindWhichTileDefinitionAt(cellPos);
        currentlyHoveredCell = cellPos;

        if (hoverHighlightObject != null)
        {
            hoverHighlightObject.SetActive(true);
            hoverHighlightObject.transform.position = CellCenterWorld(cellPos);
            UpdateHoverHighlightColor(isWithinInteractionRange);
        }
    }

    private void UpdateHoverHighlightColor(bool withinRange)
    {
        if (hoverSpriteRenderer != null && hoverColorManager != null)
        {
            hoverSpriteRenderer.color = hoverColorManager.GetColorForRange(withinRange);
        }
    }

    private void UpdateDebugUI()
    {
        if (hoveredTileText != null)
        {
            string tileName = hoveredTileDef != null ? hoveredTileDef.displayName : "None";
            if (currentlyHoveredCell.HasValue && timedCells.TryGetValue(currentlyHoveredCell.Value, out TimedTileState timedState))
            {
                tileName += $" [{timedState.ticksRemaining}t]";
            }
            string rangeIndicator = isWithinInteractionRange ? " [IN RANGE]" : " [OUT OF RANGE]";
            hoveredTileText.text = $"Hover: {tileName}{rangeIndicator}";
        }
        if (currentToolText != null)
        {
            if (InventoryBarController.Instance?.SelectedItem != null)
            {
                currentToolText.text = $"Selected: {InventoryBarController.Instance.SelectedItem.GetDisplayName()}";
            }
            else
            {
                currentToolText.text = "Nothing Selected";
            }
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return interactionGrid != null ? interactionGrid.WorldToCell(worldPos) : Vector3Int.zero;
    }

    private Vector3 CellCenterWorld(Vector3Int cellPos)
    {
        return interactionGrid != null ? interactionGrid.GetCellCenterWorld(cellPos) : Vector3.zero;
    }

    public bool IsWithinInteractionRange => isWithinInteractionRange;
    public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
    public TileDefinition HoveredTileDef => hoveredTileDef;
}
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
    private Dictionary<DualGridTilemapModule, TileDefinition> definitionByModule;
    private Vector3Int? currentlyHoveredCell;
    private TileDefinition hoveredTileDef;
    private Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();
    private SpriteRenderer hoverSpriteRenderer;
    private bool isWithinInteractionRange = false;

    protected override void OnAwake()
    {
        SetupTilemaps();
        CacheHoverSpriteRenderer();
    }

    void Start()
    {
        if (TickManager.Instance != null) TickManager.Instance.RegisterTickUpdateable(this);
    }

    void OnDestroy()
    {
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        UpdateReversionTicks();
    }
    
    // REWRITTEN and CORRECTED method, as per your friend's analysis document.
    public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        // First pass: Check for overlay tiles (e.g., tilled dirt, wet dirt) that DON'T have keepBottomTile = true.
        // This ensures we detect the top-most, most specific tile first.
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tilemapModule != null && !mapping.tileDef.keepBottomTile)
            {
                if (TileExistsInModule(mapping.tilemapModule, cellPos))
                {
                    return mapping.tileDef; // Found the top-most tile, return immediately.
                }
            }
        }
        
        // Second pass: If no overlay tile was found, check for base tiles (e.g., grass, water) that DO have keepBottomTile = true.
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tilemapModule != null && mapping.tileDef.keepBottomTile)
            {
                if (TileExistsInModule(mapping.tilemapModule, cellPos))
                {
                    return mapping.tileDef; // Found the base tile.
                }
            }
        }

        // If no tile was found in any configured map, return null.
        // The detailed log from the analysis document is included here.
        if (debugLogs)
        {
            Debug.LogWarning($"[TileInteractionManager] No TileDefinition found at position {cellPos}. " +
                            "Ensure tiles are painted on a configured Tilemap (Data or Render) and mappings are correct.");
        }
        return null;
    }
    
    // Helper method to check both tilemaps within a module to reduce code duplication.
    private bool TileExistsInModule(DualGridTilemapModule module, Vector3Int cellPos)
    {
        // Check DataTilemap first, as it's the intended logical layer.
        if (module.DataTilemap != null && module.DataTilemap.HasTile(cellPos))
        {
            return true;
        }
        
        // If not in data, check RenderTilemap as a fallback.
        Transform renderTransform = module.transform.Find("RenderTilemap");
        if (renderTransform != null)
        {
            Tilemap renderTilemap = renderTransform.GetComponent<Tilemap>();
            if (renderTilemap != null && renderTilemap.HasTile(cellPos))
            {
                return true;
            }
        }
        
        return false;
    }

    public void ApplyToolAction(ToolDefinition toolDef)
    {
        if (toolDef == null || !currentlyHoveredCell.HasValue) return;
        
        Vector3Int targetCell = currentlyHoveredCell.Value;
        TileDefinition currentTileDef = FindWhichTileDefinitionAt(targetCell);

        if (currentTileDef == null) return;
        
        if (debugLogs) Debug.Log($"[TileInteractionManager] Checking interactions for Tool='{toolDef.displayName}' on Tile='{currentTileDef.displayName}'");
        
        if (interactionLibrary?.refillRules != null)
        {
            foreach (var refillRule in interactionLibrary.refillRules)
            {
                if (refillRule != null && refillRule.toolToRefill == toolDef && refillRule.refillSourceTile == currentTileDef)
                {
                    if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool == toolDef)
                    {
                        ToolSwitcher.Instance.RefillCurrentTool();
                        return;
                    }
                }
            }
        }

        if (interactionLibrary?.rules != null)
        {
            TileInteractionRule rule = interactionLibrary.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == currentTileDef);
            if (rule != null)
            {
                if (rule.toTile != null)
                {
                    if (!rule.toTile.keepBottomTile) RemoveTile(currentTileDef, targetCell);
                    PlaceTile(rule.toTile, targetCell);
                }
                else
                {
                    RemoveTile(currentTileDef, targetCell);
                }
            }
        }
    }
    
    // --- Other methods remain unchanged but are included for completeness ---
    
    private void CacheHoverSpriteRenderer()
    {
        if (hoverHighlightObject != null)
        {
            hoverSpriteRenderer = hoverHighlightObject.GetComponent<SpriteRenderer>();
            if (hoverSpriteRenderer == null) Debug.LogWarning("[TileInteractionManager] hoverHighlightObject has no SpriteRenderer component.", this);
        }
    }

    private void SetupTilemaps()
    {
        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();
        if (tileDefinitionMappings == null) return;

        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef == null || mapping.tilemapModule == null) continue;
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

    public void UpdateSortingOrder()
    {
        if (tileDefinitionMappings == null) return;
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping?.tilemapModule == null) continue;
            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                TilemapRenderer renderer = renderTilemapTransform.GetComponent<TilemapRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = baseSortingOrder - i;
                    #if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(renderer);
                    #endif
                }
            }
        }
    }

    public void UpdateAllColors()
    {
        if (tileDefinitionMappings == null) return;
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef == null || mapping.tilemapModule == null) continue;
            Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
            if (renderTilemapTransform != null)
            {
                Tilemap renderTilemap = renderTilemapTransform.GetComponent<Tilemap>();
                if (renderTilemap != null)
                {
                    renderTilemap.color = mapping.tileDef.tintColor;
                    #if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(renderTilemap);
                    #endif
                }
            }
        }
    }

    void Update()
    {
        HandleTileHover();
        UpdateDebugUI();
    }

    private void UpdateReversionTicks()
    {
        if (timedCells.Count == 0) return;
        List<Vector3Int> cellsToRevert = new List<Vector3Int>();
        foreach (var kvp in timedCells.ToList())
        {
            Vector3Int cellPos = kvp.Key;
            TimedTileState state = kvp.Value;
            if (state.tileDef == null)
            {
                timedCells.Remove(cellPos);
                continue;
            }
            state.ticksRemaining--;
            if (state.ticksRemaining <= 0)
            {
                cellsToRevert.Add(cellPos);
            }
            else
            {
                timedCells[cellPos] = state;
            }
        }
        
        foreach (var cellPos in cellsToRevert)
        {
            if (timedCells.TryGetValue(cellPos, out TimedTileState stateToRevert))
            {
                timedCells.Remove(cellPos);
                RemoveTile(stateToRevert.tileDef, cellPos);
                if (stateToRevert.tileDef.revertToTile != null)
                {
                    PlaceTile(stateToRevert.tileDef.revertToTile, cellPos);
                }
            }
        }
    }

    public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null) return;
        if (!moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module)) return;
        if (module?.DataTilemap != null)
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
        if (!moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module)) return;
        if (module?.DataTilemap != null)
        {
            module.DataTilemap.SetTile(cellPos, null);
        }
        if (timedCells.ContainsKey(cellPos))
        {
            timedCells.Remove(cellPos);
        }
    }
    
    private void HandleTileHover()
    {
        if (mainCamera == null || player == null) return;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int cellPos = WorldToCell(mouseWorldPos);

        GridEntity playerGrid = player.GetComponent<GridEntity>();
        if (playerGrid == null) return;

        int gridRadius = Mathf.CeilToInt(hoverRadius);
        GridPosition playerGridPos = playerGrid.Position;
        GridPosition hoveredGridPos = new GridPosition(cellPos);

        isWithinInteractionRange = GridRadiusUtility.IsWithinCircleRadius(hoveredGridPos, playerGridPos, gridRadius);
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
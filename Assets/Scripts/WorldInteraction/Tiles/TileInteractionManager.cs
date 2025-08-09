// Assets/Scripts/WorldInteraction/Tiles/TileInteractionManager.cs
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
        // Safely get the instance once
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }
		
        // The rest of your OnDestroy logic can go here if any
    }

    void OnDisable()
    {
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius("player_tool_use");
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        UpdateReversionTicks();
    }

    public void ApplyToolAction(ToolDefinition toolDef)
{
    if (toolDef == null || !currentlyHoveredCell.HasValue)
    {
        return;
    }

    Vector3Int targetCell = currentlyHoveredCell.Value;
    TileDefinition currentTileDef = FindWhichTileDefinitionAt(targetCell);

    if (currentTileDef == null)
    {
        return;
    }

    if (debugLogs)
    {
        Debug.Log($"[TileInteractionManager] Checking tile interactions for Tool='{toolDef.displayName}' on Tile='{currentTileDef.displayName}'");
    }

    // --- Refill Logic ---
    if (interactionLibrary?.refillRules != null)
    {
        foreach (var refillRule in interactionLibrary.refillRules)
        {
            // Rule must be valid, for the tool we just used, and on the correct source tile.
            if (refillRule != null &&
                refillRule.toolToRefill == toolDef &&
                refillRule.refillSourceTile == currentTileDef)
            {
                // Also ensure the tool being refilled is the one currently selected in the ToolSwitcher.
                if (ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool == toolDef)
                {
                    ToolSwitcher.Instance.RefillCurrentTool();
                    return; // Refill action was performed, so we are done with this interaction.
                }
            }
        }
    }

    // --- Transformation Logic ---
    // If no refill occurred, check for tile transformation rules.
    if (interactionLibrary?.rules != null)
    {
        TileInteractionRule rule = interactionLibrary.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == currentTileDef);
        if (rule != null)
        {
            if (rule.toTile != null)
            {
                if (!rule.toTile.keepBottomTile)
                {
                    RemoveTile(currentTileDef, targetCell);
                }
                PlaceTile(rule.toTile, targetCell);
            }
            else // If toTile is null, it means the tool just removes the fromTile.
            {
                RemoveTile(currentTileDef, targetCell);
            }
        }
    }
}
    
    private void CacheHoverSpriteRenderer()
    {
        if (hoverHighlightObject != null)
        {
            hoverSpriteRenderer = hoverHighlightObject.GetComponent<SpriteRenderer>();
            if (hoverSpriteRenderer == null)
            {
                Debug.LogWarning("[TileInteractionManager] hoverHighlightObject has no SpriteRenderer component. Color management will not work.", this);
            }
        }
    }
    
    private void SetupTilemaps()
    {
        moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
        definitionByModule = new Dictionary<DualGridTilemapModule, TileDefinition>();
        if (tileDefinitionMappings == null)
        {
            if (debugLogs) Debug.LogWarning("[TileInteractionManager SetupTilemaps] No mappings defined.");
            return;
        }
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping == null || mapping.tileDef == null || mapping.tilemapModule == null)
            {
                Debug.LogWarning("[TileInteractionManager SetupTilemaps] Null or incomplete mapping found. Skipping.");
                continue;
            }
            if (!moduleByDefinition.ContainsKey(mapping.tileDef))
            {
                moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                definitionByModule[mapping.tilemapModule] = mapping.tileDef;
            }
            else
            {
                Debug.LogWarning($"[TileInteractionManager SetupTilemaps] Duplicate TileDefinition '{mapping.tileDef.displayName}' found in mappings.", mapping.tileDef);
            }
        }
    }
    
    public void UpdateSortingOrder()
    {
        if (tileDefinitionMappings == null) return;
        for (int i = 0; i < tileDefinitionMappings.Count; i++)
        {
            var mapping = tileDefinitionMappings[i];
            if (mapping == null || mapping.tileDef == null || mapping.tilemapModule == null) continue;
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
            if (mapping == null || mapping.tileDef == null || mapping.tilemapModule == null) continue;
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
        List<Vector3Int> cellsToRevert = null;
        foreach (var kvp in timedCells.ToList())
        {
            Vector3Int cellPos = kvp.Key;
            TimedTileState state = kvp.Value;
            if (state.tileDef == null) { timedCells.Remove(cellPos); continue; }
            state.ticksRemaining--;
            if (state.ticksRemaining <= 0)
            {
                if (cellsToRevert == null) cellsToRevert = new List<Vector3Int>();
                cellsToRevert.Add(cellPos);
            }
            else
            {
                timedCells[cellPos] = state;
            }
        }
        if (cellsToRevert != null)
        {
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
        if (timedCells.TryGetValue(cellPos, out TimedTileState timedState) && timedState.tileDef == tileDef)
        {
            timedCells.Remove(cellPos);
        }
    }

    public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        TileDefinition foundDef = null;
        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tilemapModule?.DataTilemap != null && mapping.tileDef.keepBottomTile)
            {
                if (mapping.tilemapModule.DataTilemap.HasTile(cellPos)) { foundDef = mapping.tileDef; break; }
            }
        }
        if (foundDef == null)
        {
            foreach (var kvp in definitionByModule)
            {
                if (kvp.Key?.DataTilemap != null && kvp.Value != null && !kvp.Value.keepBottomTile)
                {
                    if (kvp.Key.DataTilemap.HasTile(cellPos)) { foundDef = kvp.Value; break; }
                }
            }
        }
        return foundDef;
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
        GridPosition hoveredGridPos = new GridPosition(cellPos.x, cellPos.y);

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
        if (interactionGrid != null) return interactionGrid.WorldToCell(worldPos);
        Debug.LogWarning("[WorldToCell] No valid interactionGrid found.");
        return Vector3Int.zero;
    }
    
    private Vector3 CellCenterWorld(Vector3Int cellPos)
    {
        if (interactionGrid != null) return interactionGrid.GetCellCenterWorld(cellPos);
        Debug.LogWarning("[CellCenterWorld] No valid interactionGrid found.");
        return Vector3.zero;
    }
    
    public bool IsWithinInteractionRange => isWithinInteractionRange;
    public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
    public TileDefinition HoveredTileDef => hoveredTileDef;
}
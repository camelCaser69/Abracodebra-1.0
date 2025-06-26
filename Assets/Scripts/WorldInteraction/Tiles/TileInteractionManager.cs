using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using TMPro;
using WegoSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TileInteractionManager : MonoBehaviour, ITickUpdateable
{
    public static TileInteractionManager Instance { get; set; }

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

    Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
    Dictionary<DualGridTilemapModule, TileDefinition> definitionByModule;
    Vector3Int? currentlyHoveredCell;
    TileDefinition hoveredTileDef;
    Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();
    SpriteRenderer hoverSpriteRenderer;
    bool isWithinInteractionRange = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
        if (Instance == this) Instance = null;
    }

    void OnDisable()
    {
        // Clean up the continuous visualization when this manager is disabled
        if (GridDebugVisualizer.Instance != null)
        {
            GridDebugVisualizer.Instance.HideContinuousRadius("player_tool_use");
        }
    }


    void CacheHoverSpriteRenderer()
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

    public void OnTickUpdate(int currentTick)
    {
        UpdateReversionTicks();
    }

    void SetupTilemaps()
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

                if (mapping.tilemapModule.DataTilemap == null)
                {
                    Debug.LogWarning($"Module for '{mapping.tileDef.displayName}' has no DataTilemap assigned.", mapping.tilemapModule.gameObject);
                }

                if (debugLogs) Debug.Log($"[Mapping] Added: {mapping.tileDef.displayName} => {mapping.tilemapModule.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[TileInteractionManager SetupTilemaps] Duplicate TileDefinition '{mapping.tileDef.displayName}' found in mappings. Ignoring subsequent entries.", mapping.tileDef);
            }
        }
        if (debugLogs) Debug.Log($"[TileInteractionManager SetupTilemaps] Setup complete. {moduleByDefinition.Count} definitions mapped.");
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
                    if (debugLogs) Debug.Log($"Updated sorting order for {mapping.tileDef.displayName} to {renderer.sortingOrder}");
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
                    if (debugLogs) Debug.Log($"Updated color for {mapping.tileDef.displayName} to {renderTilemap.color}");
                }
            }
        }
    }

    void Update()
    {
        HandleTileHover();
        UpdateDebugUI();
    }

    void UpdateReversionTicks()
    {
        if (timedCells.Count == 0) return;

        List<Vector3Int> cellsToRevert = null;

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
                        if (debugLogs) Debug.Log($"Reverting tile at {cellPos} from {stateToRevert.tileDef.displayName} to {stateToRevert.tileDef.revertToTile.displayName}");
                        PlaceTile(stateToRevert.tileDef.revertToTile, cellPos);
                    }
                    else
                    {
                        if (debugLogs) Debug.Log($"Tile {stateToRevert.tileDef.displayName} at {cellPos} expired and removed (no revert target).");
                    }
                }
            }
        }
    }

    public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null)
        {
            Debug.LogWarning("[PlaceTile] Null tile definition.");
            return;
        }

        if (!moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module))
        {
            Debug.LogError($"[PlaceTile] No module mapping found for TileDefinition '{tileDef.displayName}'");
            return;
        }

        if (module?.DataTilemap != null)
        {
            TileBase dataTile = ScriptableObject.CreateInstance<Tile>();
            module.DataTilemap.SetTile(cellPos, dataTile);

            if (tileDef.revertAfterTicks > 0)
            {
                TimedTileState timedState = new TimedTileState
                {
                    tileDef = tileDef,
                    ticksRemaining = tileDef.revertAfterTicks
                };
                timedCells[cellPos] = timedState;
                if (debugLogs) Debug.Log($"Set up timed reversion for {tileDef.displayName} at {cellPos}, reverting in {tileDef.revertAfterTicks} ticks.");
            }

            if (debugLogs) Debug.Log($"Placed tile '{tileDef.displayName}' at {cellPos}");
        }
        else
        {
            Debug.LogWarning($"Module for '{tileDef.displayName}' has no DataTilemap assigned.", module.gameObject);
        }
    }

    public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos)
    {
        if (tileDef == null)
        {
            Debug.LogWarning("[RemoveTile] Null tile definition.");
            return;
        }

        if (!moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module))
        {
            Debug.LogError($"[RemoveTile] No module mapping found for TileDefinition '{tileDef.displayName}'");
            return;
        }

        if (module?.DataTilemap != null)
        {
            module.DataTilemap.SetTile(cellPos, null);
            if (debugLogs) Debug.Log($"Removed tile '{tileDef.displayName}' from {cellPos}");
        }
        else
        {
            Debug.LogWarning($"Module for '{tileDef.displayName}' has no DataTilemap assigned.", module.gameObject);
        }

        if (timedCells.TryGetValue(cellPos, out TimedTileState timedState) && timedState.tileDef == tileDef)
        {
            timedCells.Remove(cellPos);
            if (debugLogs) Debug.Log($"Cleared timed reversion for {tileDef.displayName} at {cellPos} during removal.");
        }
    }

    public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
    {
        if (definitionByModule == null || tileDefinitionMappings == null)
        {
            Debug.LogError("[FindWhichTileDefinitionAt] Dictionaries not initialized!");
            return null;
        }

        TileDefinition foundDef = null;

        foreach (var mapping in tileDefinitionMappings)
        {
            if (mapping?.tileDef != null && mapping.tilemapModule?.DataTilemap != null && mapping.tileDef.keepBottomTile)
            {
                if (mapping.tilemapModule.DataTilemap.HasTile(cellPos))
                {
                    foundDef = mapping.tileDef;
                    break;
                }
            }
        }

        if (foundDef == null)
        {
            foreach (var kvp in definitionByModule)
            {
                DualGridTilemapModule module = kvp.Key;
                TileDefinition def = kvp.Value;

                if (module?.DataTilemap != null && def != null && !def.keepBottomTile)
                {
                    if (module.DataTilemap.HasTile(cellPos))
                    {
                        foundDef = def;
                        break;
                    }
                }
            }
        }

        return foundDef;
    }

    void HandleTileHover()
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

        bool withinRadius = GridRadiusUtility.IsWithinCircleRadius(hoveredGridPos, playerGridPos, gridRadius);
        TileDefinition foundTile = FindWhichTileDefinitionAt(cellPos);

        currentlyHoveredCell = cellPos;
        hoveredTileDef = foundTile;
        isWithinInteractionRange = withinRadius;

        if (hoverHighlightObject != null)
        {
            hoverHighlightObject.SetActive(true);
            hoverHighlightObject.transform.position = CellCenterWorld(cellPos);
            UpdateHoverHighlightColor(withinRadius);
        }

        // CORRECTED: Use the new stateful visualization system
        if (GridDebugVisualizer.Instance != null && Debug.isDebugBuild)
        {
            // This key should be unique for this specific visualization
            string visKey = "player_tool_use"; 
            GridDebugVisualizer.Instance.ShowContinuousRadius(
                visKey, 
                playerGridPos, 
                gridRadius, 
                GridDebugVisualizer.RadiusType.ToolUse
            );
        }
    }

    void UpdateHoverHighlightColor(bool withinRange)
    {
        if (hoverSpriteRenderer == null || hoverColorManager == null) return;

        Color targetColor = hoverColorManager.GetColorForRange(withinRange);
        hoverSpriteRenderer.color = targetColor;
    }

    void UpdateDebugUI()
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
            if (InventoryBarController.Instance != null && InventoryBarController.Instance.SelectedItem != null)
            {
                var selectedItem = InventoryBarController.Instance.SelectedItem;
                if (selectedItem.Type == InventoryBarItem.ItemType.Tool)
                {
                    currentToolText.text = $"Tool: {selectedItem.GetDisplayName()}";
                }
                else if (selectedItem.Type == InventoryBarItem.ItemType.Node && selectedItem.IsSeed())
                {
                    currentToolText.text = $"Seed: {selectedItem.GetDisplayName()}";
                }
                else
                {
                    currentToolText.text = $"Selected: {selectedItem.GetDisplayName()}";
                }
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

        if (tileDefinitionMappings != null && tileDefinitionMappings.Count > 0)
        {
            foreach(var mapping in tileDefinitionMappings)
            {
                if (mapping?.tilemapModule?.DataTilemap?.layoutGrid != null)
                {
                    return mapping.tilemapModule.DataTilemap.layoutGrid.WorldToCell(worldPos);
                }
            }
        }

        Debug.LogWarning("[WorldToCell] No valid interactionGrid or mapped Tilemap found to determine cell position.");
        return Vector3Int.zero;
    }

    Vector3 CellCenterWorld(Vector3Int cellPos)
    {
        if (interactionGrid != null) return interactionGrid.GetCellCenterWorld(cellPos);

        if (tileDefinitionMappings != null && tileDefinitionMappings.Count > 0)
        {
            foreach(var mapping in tileDefinitionMappings)
            {
                if (mapping?.tilemapModule?.DataTilemap?.layoutGrid != null)
                {
                    return mapping.tilemapModule.DataTilemap.layoutGrid.GetCellCenterWorld(cellPos);
                }
            }
        }

        Debug.LogWarning("[CellCenterWorld] No valid interactionGrid or mapped Tilemap found to determine cell center.");
        return Vector3.zero;
    }

    void HandleSeedPlanting(Vector3Int cellPosition)
    {
        PlantPlacementManager plantManager = PlantPlacementManager.Instance;
        if (plantManager == null) { Debug.LogError("Cannot plant: PlantPlacementManager not found!"); return; }

        TileDefinition tileDef = FindWhichTileDefinitionAt(cellPosition);
        if (!plantManager.IsTileValidForPlanting(tileDef))
        {
            if (debugLogs) Debug.Log($"Cannot plant on {tileDef?.displayName ?? "Unknown"} - invalid tile.");
            return;
        }

        GardenerController gardener = player?.GetComponent<GardenerController>();
        if (gardener == null) { Debug.LogError("Cannot plant: GardenerController not found on player!"); return; }

        gardener.Plant();

        Vector3 worldPosition = CellCenterWorld(cellPosition);

        bool planted = plantManager.TryPlantSeed(cellPosition, worldPosition);
        if (debugLogs) Debug.Log(planted ? $"Planted seed successfully at {cellPosition}" : $"Failed to plant seed at {cellPosition}");
    }

    public void ApplyToolAction(ToolDefinition toolDef)
    {
        if (toolDef == null) { Debug.LogWarning("ApplyToolAction called with a NULL toolDef."); return; }
        if (!currentlyHoveredCell.HasValue) return;
        if (hoveredTileDef == null) { if (debugLogs) Debug.Log("ApplyToolAction: No recognized tile at hovered cell."); return; }

        float distance = Vector2.Distance(player.position, CellCenterWorld(currentlyHoveredCell.Value));
        if (distance > hoverRadius) { if (debugLogs) Debug.Log($"ApplyToolAction: Cell too far ({distance:F2} > {hoverRadius})."); return; }

        if (debugLogs) Debug.Log($"[ApplyToolAction] Using Tool='{toolDef.toolType}', On Tile='{hoveredTileDef.displayName}', At Cell={currentlyHoveredCell.Value}");

        bool wasRefillAction = false;
        if (interactionLibrary != null && interactionLibrary.refillRules != null)
        {
            foreach (var refillRule in interactionLibrary.refillRules)
            {
                if (refillRule != null && refillRule.toolToRefill == toolDef && refillRule.refillSourceTile == hoveredTileDef)
                {
                    if (debugLogs) Debug.Log($"Refill rule matched: Tool '{toolDef.displayName}' on Tile '{hoveredTileDef.displayName}'.");

                    Debug.Log($"[TileInteractionManager] Tool refill not implemented for inventory-based tools yet.");
                    wasRefillAction = true;
                    break;
                }
            }
        }

        if (wasRefillAction) return;

        if (interactionLibrary == null || interactionLibrary.rules == null) { Debug.LogWarning("Interaction Library or its standard rules list is null!"); return; }
        TileInteractionRule rule = interactionLibrary.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == hoveredTileDef);

        if (rule == null)
        {
            if (debugLogs) Debug.Log($"No standard interaction rule found for tool '{toolDef.toolType}' on tile '{hoveredTileDef.displayName}'.");
            return;
        }

        if (rule.toTile != null)
        {
            if (debugLogs) Debug.Log($"Applying standard rule: '{hoveredTileDef.displayName}' -> '{rule.toTile.displayName}'");
            if (!rule.toTile.keepBottomTile)
            {
                RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
            }
            PlaceTile(rule.toTile, currentlyHoveredCell.Value);
        }
        else
        {
            if (debugLogs) Debug.Log($"Applying standard rule: Remove '{hoveredTileDef.displayName}'");
            RemoveTile(hoveredTileDef, currentlyHoveredCell.Value);
        }
    }

    public void TryUseSeed()
    {
        if (!currentlyHoveredCell.HasValue) return;
        if (!isWithinInteractionRange) return;

        HandleSeedPlanting(currentlyHoveredCell.Value);
    }

    public bool IsWithinInteractionRange => isWithinInteractionRange;
    public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
    public TileDefinition HoveredTileDef => hoveredTileDef;
}
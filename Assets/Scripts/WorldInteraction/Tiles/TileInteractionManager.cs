﻿// Assets/Scripts/WorldInteraction/Tiles/TileInteractionManager.cs
using System;
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

    [System.Serializable]
    public struct TimedTileState
    {
        public TileDefinition tileDef;
        public int ticksRemaining;
    }

    [Header("Core References")]
    public List<TileDefinitionMapping> tileDefinitionMappings;
    public TileInteractionLibrary interactionLibrary;
    public Grid interactionGrid;
    public Camera mainCamera;
    public Transform player;

    [Header("Interaction Settings")]
    public float hoverRadius = 3f;

    // Tool-specific effect logic has been correctly moved to EnvironmentalStatusEffectSystem
    
    [Header("UI & Visuals")]
    public GameObject hoverHighlightObject;
    public TileHoverColorManager hoverColorManager;
    public int baseSortingOrder = 0;

    [Header("Debugging")]
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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetupTilemaps();
        CacheHoverSpriteRenderer();
    }

    void Start()
    {
        if (TickManager.Instance != null) TickManager.Instance.RegisterTickUpdateable(this);
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null) TickManager.Instance.UnregisterTickUpdateable(this);
        if (Instance == this) Instance = null;
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
        if (toolDef == null || !currentlyHoveredCell.HasValue) return;

        Vector3Int targetCell = currentlyHoveredCell.Value;
        
        // This method is now only responsible for tile-based interactions.
        // Entity-based effects are handled by the EnvironmentalStatusEffectSystem.
        TileDefinition currentTileDef = FindWhichTileDefinitionAt(targetCell);
        if (currentTileDef == null) return;

        if (debugLogs) Debug.Log($"[TileInteractionManager] Checking tile interactions for Tool='{toolDef.toolType}' on Tile='{currentTileDef.displayName}'");

        // Check for tool refill rules
        if (interactionLibrary?.refillRules != null && ToolSwitcher.Instance != null && ToolSwitcher.Instance.CurrentTool == toolDef)
        {
            foreach (var refillRule in interactionLibrary.refillRules)
            {
                if (refillRule != null && refillRule.toolToRefill == toolDef && refillRule.refillSourceTile == currentTileDef)
                {
                    ToolSwitcher.Instance.RefillCurrentTool();
                    return; // Refill action was performed, we are done.
                }
            }
        }
        
        // Check for tile transformation rules
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

    void UpdateReversionTicks()
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
            else { timedCells[cellPos] = state; }
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

    void UpdateHoverHighlightColor(bool withinRange)
    {
        if (hoverSpriteRenderer != null && hoverColorManager != null)
        {
            hoverSpriteRenderer.color = hoverColorManager.GetColorForRange(withinRange);
        }
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
            if (InventoryBarController.Instance?.SelectedItem != null)
            {
                currentToolText.text = $"Selected: {InventoryBarController.Instance.SelectedItem.GetDisplayName()}";
            }
            else { currentToolText.text = "Nothing Selected"; }
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (interactionGrid != null) return interactionGrid.WorldToCell(worldPos);
        Debug.LogWarning("[WorldToCell] No valid interactionGrid found.");
        return Vector3Int.zero;
    }

    Vector3 CellCenterWorld(Vector3Int cellPos)
    {
        if (interactionGrid != null) return interactionGrid.GetCellCenterWorld(cellPos);
        Debug.LogWarning("[CellCenterWorld] No valid interactionGrid found.");
        return Vector3.zero;
    }
    
    public bool IsWithinInteractionRange => isWithinInteractionRange;
    public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
    public TileDefinition HoveredTileDef => hoveredTileDef;
}
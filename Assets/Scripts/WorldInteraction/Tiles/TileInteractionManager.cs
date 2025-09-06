using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Abracodabra.UI.Genes;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using TMPro;
using WegoSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem
{
    public class TileInteractionManager : SingletonMonoBehaviour<TileInteractionManager>, ITickUpdateable
    {
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
        private readonly Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();
        private Vector3Int? currentlyHoveredCell;
        private TileDefinition hoveredTileDef;
        private SpriteRenderer hoverSpriteRenderer;
        private bool isWithinInteractionRange = false;

        protected override void OnAwake()
        {
            EnsureInitialized();
            CacheHoverSpriteRenderer();
        }

        private void EnsureInitialized()
        {
            if (moduleByDefinition == null)
            {
                if (debugLogs) Debug.Log("[TileInteractionManager] Dictionaries are null. Initializing now.");
                SetupTilemaps();
            }
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
            // NEW: Check for tool refill action every frame
            if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat)
            {
                CheckAndRefillTool();
            }
            UpdateDebugUI();
        }

        public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
        {
            EnsureInitialized();
            if (tileDefinitionMappings == null) return null;

            foreach (var mapping in tileDefinitionMappings)
            {
                if (mapping?.tileDef != null && mapping.tilemapModule != null)
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
            return module.DataTilemap != null && module.DataTilemap.HasTile(cellPos);
        }

        public void ApplyToolAction(ToolDefinition toolDef)
{
    if (toolDef == null || !currentlyHoveredCell.HasValue) return;

    Vector3Int targetCell = currentlyHoveredCell.Value;
    TileDefinition currentTileDef = FindWhichTileDefinitionAt(targetCell);

    // NEW: Enhanced debug logging
    if (debugLogs)
    {
        Debug.Log($"[DEBUG] Applying Tool: '{toolDef?.displayName}' on Tile: '{currentTileDef?.displayName}' at {targetCell}");
        if (interactionLibrary?.rules != null)
        {
            Debug.Log($"[DEBUG] Searching through {interactionLibrary.rules.Count} rules...");
            var toolRules = interactionLibrary.rules.Where(r => r != null && r.tool == toolDef).ToList();
            if (toolRules.Any())
            {
                foreach (var r in toolRules)
                {
                    Debug.Log($"[DEBUG] Found rule for '{toolDef.displayName}': From '{r.fromTile?.displayName}' -> To '{r.toTile?.displayName}'");
                }
            }
            else
            {
                Debug.Log($"[DEBUG] No rules found for tool '{toolDef.displayName}'.");
            }
        }
    }

    if (currentTileDef == null) return;

    TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == currentTileDef);

    if (rule != null)
    {
        TileDefinition fromTile = currentTileDef;
        TileDefinition toTile = rule.toTile;

        if (debugLogs) Debug.Log($"[TileInteractionManager] MATCH FOUND! From: '{fromTile.displayName}', To: '{(toTile != null ? toTile.displayName : "NULL")}'.");

        if (toTile == null)
        {
            RemoveTile(fromTile, targetCell);
            return;
        }

        if (!toTile.keepBottomTile)
        {
            RemoveTile(fromTile, targetCell);
        }

        PlaceTile(toTile, targetCell);
    }
    else if (debugLogs)
    {
        Debug.Log($"[TileInteractionManager] NO MATCH FOUND for Tool='{toolDef.displayName}' on Tile='{currentTileDef.displayName}'.");
    }
}

        // NEW: Method to handle tool refilling
        private void CheckAndRefillTool()
        {
            // FIX: Changed from Right-Click (1) to Left-Click (0)
            if (!Input.GetMouseButtonDown(0)) return;

            if (hoveredTileDef == null || ToolSwitcher.Instance == null) return;

            var currentTool = ToolSwitcher.Instance.CurrentTool;
            if (currentTool == null || !currentTool.limitedUses) return;

            if (interactionLibrary?.refillRules != null)
            {
                var refillRule = interactionLibrary.refillRules.FirstOrDefault(
                    r => r.toolToRefill == currentTool &&
                         r.refillSourceTile == hoveredTileDef
                );

                if (refillRule != null && isWithinInteractionRange)
                {
                    ToolSwitcher.Instance.RefillCurrentTool();
                    // We consume a tick for the refill action.
                    if (PlayerActionManager.Instance != null)
                    {
                        PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Interact, currentlyHoveredCell.Value, "Refill");
                    }
                }
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
                        }
                        timedCells.Remove(cellPos);
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
            EnsureInitialized();
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
            EnsureInitialized();
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
            if (tileDefinitionMappings == null) return;
            foreach (var mapping in tileDefinitionMappings)
            {
                if (mapping?.tileDef != null && mapping.tilemapModule != null)
                {
                    if (!moduleByDefinition.ContainsKey(mapping.tileDef))
                    {
                        moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
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

                hoverHighlightObject.transform.position = PixelGridSnapper.SnapToGrid(hoverHighlightObject.transform.position);

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
}
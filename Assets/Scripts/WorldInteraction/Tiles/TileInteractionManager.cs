using UnityEngine;
using Abracodabra.UI.Genes;
using UnityEngine.Tilemaps;
using skner.DualGrid;
using TMPro;
using WegoSystem;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WegoSystem {
    public class TileInteractionManager : SingletonMonoBehaviour<TileInteractionManager>, ITickUpdateable {
        [System.Serializable]
        public class TileDefinitionMapping {
            public TileDefinition tileDef;
            public DualGridTilemapModule tilemapModule;
        }

        public struct TimedTileState {
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
        readonly Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();
        Vector3Int? currentlyHoveredCell;
        TileDefinition hoveredTileDef;
        SpriteRenderer hoverSpriteRenderer;
        bool isWithinInteractionRange = false;
        
        // Flag to indicate a refill happened this frame - prevents tool use from consuming
        bool refillHappenedThisFrame = false;

        protected override void OnAwake() {
            EnsureInitialized();
            CacheHoverSpriteRenderer();
        }

        void EnsureInitialized() {
            if (moduleByDefinition == null) {
                if (debugLogs) Debug.Log("[TileInteractionManager] Dictionaries are null. Initializing now.");
                SetupTilemaps();
            }
        }

        void Start() {
            if (TickManager.Instance != null) {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
        }

        void OnDestroy() {
            if (TickManager.Instance != null) {
                TickManager.Instance.UnregisterTickUpdateable(this);
            }
        }

        public void OnTickUpdate(int currentTick) {
            UpdateReversionTicks();
        }

        void Update() {
            // Reset the refill flag at the start of each frame
            refillHappenedThisFrame = false;
            
            HandleTileHover();
            if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat) {
                CheckAndRefillTool();
            }
            UpdateDebugUI();
        }

        public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos) {
            EnsureInitialized();
            if (tileDefinitionMappings == null) return null;

            foreach (var mapping in tileDefinitionMappings) {
                if (mapping?.tileDef != null && mapping.tilemapModule != null) {
                    if (TileExistsInModule(mapping.tilemapModule, cellPos)) {
                        return mapping.tileDef;
                    }
                }
            }

            return null;
        }

        bool TileExistsInModule(DualGridTilemapModule module, Vector3Int cellPos) {
            return module.DataTilemap != null && module.DataTilemap.HasTile(cellPos);
        }

        public void ApplyToolAction(ToolDefinition toolDef) {
            if (toolDef == null || !currentlyHoveredCell.HasValue) return;

            Vector3Int targetCell = currentlyHoveredCell.Value;

            if (debugLogs) Debug.Log($"[DEBUG] Applying Tool: '{toolDef.displayName}' at {targetCell}. Checking mappings in order...");

            foreach (var mapping in tileDefinitionMappings) {
                if (mapping?.tileDef == null || mapping.tilemapModule == null) continue;

                if (TileExistsInModule(mapping.tilemapModule, targetCell)) {
                    TileDefinition tileToTest = mapping.tileDef;
                    if (debugLogs) Debug.Log($"[DEBUG] Found tile '{tileToTest.displayName}' on its layer. Searching for a matching rule...");

                    TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(r => r != null && r.tool == toolDef && r.fromTile == tileToTest);

                    if (rule != null) {
                        if (debugLogs) Debug.Log($"[TileInteractionManager] MATCH FOUND! Rule: From '{rule.fromTile.displayName}', To: '{(rule.toTile != null ? rule.toTile.displayName : "NULL")}'. Executing action.");

                        if (rule.toTile == null) {
                            // Rule is to remove the tile
                            RemoveTile(rule.fromTile, targetCell);
                        }
                        else {
                            if (!rule.toTile.keepBottomTile) {
                                RemoveTile(rule.fromTile, targetCell);
                            }
                            PlaceTile(rule.toTile, targetCell);
                        }

                        return;
                    }
                    else {
                        if (debugLogs) Debug.Log($"[DEBUG] No rule found for tool '{toolDef.displayName}' on tile '{tileToTest.displayName}'. Checking next layer down.");
                    }
                }
            }

            if (debugLogs) Debug.LogWarning($"[TileInteractionManager] NO MATCH FOUND for tool '{toolDef.displayName}' on any tile layer at {targetCell}.");
        }

        void CheckAndRefillTool() {
            if (!Input.GetMouseButtonDown(0)) return;

            if (hoveredTileDef == null || ToolSwitcher.Instance == null) return;

            var currentTool = ToolSwitcher.Instance.CurrentTool;
            if (currentTool == null || !currentTool.limitedUses) return;

            if (interactionLibrary?.refillRules != null) {
                var refillRule = interactionLibrary.refillRules.FirstOrDefault(
                    r => r.toolToRefill == currentTool && r.refillSourceTile == hoveredTileDef
                );

                if (refillRule != null && isWithinInteractionRange) {
                    ToolSwitcher.Instance.RefillCurrentTool();
                    refillHappenedThisFrame = true; // Set flag to prevent tool use
                    
                    if (debugLogs) Debug.Log($"[TileInteractionManager] Refilled {currentTool.displayName} on {hoveredTileDef.displayName}");
                    
                    if (PlayerActionManager.Instance != null) {
                        PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Interact, currentlyHoveredCell.Value, "Refill");
                    }
                }
            }
        }
        
        /// <summary>
        /// Returns true if a refill happened this frame. Used to prevent tool use from consuming.
        /// </summary>
        public bool DidRefillThisFrame => refillHappenedThisFrame;

        void UpdateReversionTicks() {
            if (timedCells.Count == 0) return;
            List<Vector3Int> cellsToProcess = timedCells.Keys.ToList();

            foreach (Vector3Int cellPos in cellsToProcess) {
                if (timedCells.TryGetValue(cellPos, out TimedTileState state)) {
                    state.ticksRemaining--;
                    if (state.ticksRemaining <= 0) {
                        TileDefinition actualTile = FindWhichTileDefinitionAt(cellPos);
                        if (actualTile == state.tileDef) {
                            if (debugLogs) Debug.Log($"[TileInteractionManager] Reverting tile '{state.tileDef.displayName}' at {cellPos}.");
                            RemoveTile(state.tileDef, cellPos);
                            if (state.tileDef.revertToTile != null) {
                                PlaceTile(state.tileDef.revertToTile, cellPos);
                            }
                        }
                        else {
                            if (debugLogs) Debug.LogWarning($"[TileInteractionManager] State desync detected at {cellPos}. Expected '{state.tileDef.displayName}' for reversion, but found '{(actualTile != null ? actualTile.displayName : "NULL")}'. Removing stale timer.");
                        }
                        timedCells.Remove(cellPos);
                    }
                    else {
                        timedCells[cellPos] = state;
                    }
                }
            }
        }

        public void PlaceTile(TileDefinition tileDef, Vector3Int cellPos) {
            EnsureInitialized();
            if (tileDef == null) return;
            if (moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module) && module?.DataTilemap != null) {
                module.DataTilemap.SetTile(cellPos, ScriptableObject.CreateInstance<Tile>());
                if (tileDef.revertAfterTicks > 0) {
                    timedCells[cellPos] = new TimedTileState { tileDef = tileDef, ticksRemaining = tileDef.revertAfterTicks };
                }
            }
        }

        public void RemoveTile(TileDefinition tileDef, Vector3Int cellPos) {
            EnsureInitialized();
            if (tileDef == null) return;
            if (moduleByDefinition.TryGetValue(tileDef, out DualGridTilemapModule module) && module?.DataTilemap != null) {
                module.DataTilemap.SetTile(cellPos, null);
            }

            if (timedCells.TryGetValue(cellPos, out TimedTileState timedState) && timedState.tileDef == tileDef) {
                timedCells.Remove(cellPos);
            }
        }

        void CacheHoverSpriteRenderer() {
            if (hoverHighlightObject != null) {
                hoverSpriteRenderer = hoverHighlightObject.GetComponent<SpriteRenderer>();
            }
        }

        void SetupTilemaps() {
            moduleByDefinition = new Dictionary<TileDefinition, DualGridTilemapModule>();
            if (tileDefinitionMappings == null) return;
            foreach (var mapping in tileDefinitionMappings) {
                if (mapping?.tileDef != null && mapping.tilemapModule != null) {
                    if (!moduleByDefinition.ContainsKey(mapping.tileDef)) {
                        moduleByDefinition[mapping.tileDef] = mapping.tilemapModule;
                    }
                    else {
                        Debug.LogWarning($"[TileInteractionManager] Duplicate TileDefinition '{mapping.tileDef.displayName}' found in mappings. Using first occurrence.", mapping.tileDef);
                    }
                }
            }
        }

        public void UpdateSortingOrder() {
            if (tileDefinitionMappings == null) return;
            for (int i = 0; i < tileDefinitionMappings.Count; i++) {
                var mapping = tileDefinitionMappings[i];
                if (mapping?.tilemapModule != null) {
                    Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                    if (renderTilemapTransform != null) {
                        if (renderTilemapTransform.GetComponent<TilemapRenderer>() is TilemapRenderer renderer) {
                            renderer.sortingOrder = baseSortingOrder - i;
#if UNITY_EDITOR
                            if (!Application.isPlaying) EditorUtility.SetDirty(renderer);
#endif
                        }
                    }
                }
            }
        }

        public void UpdateAllColors() {
            if (tileDefinitionMappings == null) return;
            foreach (var mapping in tileDefinitionMappings) {
                if (mapping?.tileDef != null && mapping.tilemapModule != null) {
                    Transform renderTilemapTransform = mapping.tilemapModule.transform.Find("RenderTilemap");
                    if (renderTilemapTransform != null) {
                        if (renderTilemapTransform.GetComponent<Tilemap>() is Tilemap renderTilemap) {
                            renderTilemap.color = mapping.tileDef.tintColor;
#if UNITY_EDITOR
                            if (!Application.isPlaying) EditorUtility.SetDirty(renderTilemap);
#endif
                        }
                    }
                }
            }
        }

        void HandleTileHover() {
            if (mainCamera == null || player == null) return;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            Vector3Int cellPos = WorldToCell(mouseWorldPos);

            if (player.GetComponent<GridEntity>() is GridEntity playerGrid) {
                int gridRadius = Mathf.CeilToInt(hoverRadius);
                GridPosition playerGridPos = playerGrid.Position;
                GridPosition hoveredGridPos = new GridPosition(cellPos);
                isWithinInteractionRange = GridRadiusUtility.IsWithinCircleRadius(hoveredGridPos, playerGridPos, gridRadius);
            }

            hoveredTileDef = FindWhichTileDefinitionAt(cellPos);
            currentlyHoveredCell = cellPos;

            if (hoverHighlightObject != null) {
                hoverHighlightObject.SetActive(true);
                hoverHighlightObject.transform.position = CellCenterWorld(cellPos);
                hoverHighlightObject.transform.position = PixelGridSnapper.SnapToGrid(hoverHighlightObject.transform.position);
                UpdateHoverHighlightColor(isWithinInteractionRange);
            }
        }

        void UpdateHoverHighlightColor(bool withinRange) {
            if (hoverSpriteRenderer != null && hoverColorManager != null) {
                hoverSpriteRenderer.color = hoverColorManager.GetColorForRange(withinRange);
            }
        }

        void UpdateDebugUI() {
            if (hoveredTileText != null) {
                string tileName = hoveredTileDef != null ? hoveredTileDef.displayName : "None";
                if (currentlyHoveredCell.HasValue && timedCells.TryGetValue(currentlyHoveredCell.Value, out TimedTileState timedState)) {
                    tileName += $" [{timedState.ticksRemaining}t]";
                }
                string rangeIndicator = isWithinInteractionRange ? " [IN RANGE]" : " [OUT OF RANGE]";
                hoveredTileText.text = $"Hover: {tileName}{rangeIndicator}";
            }

            if (currentToolText != null) {
                var selectedItem = HotbarSelectionService.SelectedItem;
                if (selectedItem != null && selectedItem.IsValid()) {
                    currentToolText.text = $"Selected: {selectedItem.GetDisplayName()}";
                }
                else {
                    currentToolText.text = "Nothing Selected";
                }
            }
        }

        public Vector3Int WorldToCell(Vector3 worldPos) {
            return interactionGrid != null ? interactionGrid.WorldToCell(worldPos) : Vector3Int.zero;
        }

        Vector3 CellCenterWorld(Vector3Int cellPos) {
            return interactionGrid != null ? interactionGrid.GetCellCenterWorld(cellPos) : Vector3.zero;
        }

        public bool IsWithinInteractionRange => isWithinInteractionRange;
        public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
        public TileDefinition HoveredTileDef => hoveredTileDef;
    }
}
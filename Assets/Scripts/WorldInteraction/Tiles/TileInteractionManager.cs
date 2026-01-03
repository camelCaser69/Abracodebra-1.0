using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Abracodabra.UI.Genes;
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
        [Serializable]
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

        [Header("Tile Mappings")]
        [Tooltip("Map TileDefinitions to their DualGridTilemapModules. Order affects sorting only, not detection priority.")]
        public List<TileDefinitionMapping> tileDefinitionMappings;
        
        [Header("Interaction")]
        public TileInteractionLibrary interactionLibrary;
        public Grid interactionGrid;
        public Camera mainCamera;
        public Transform player;
        public float hoverRadius = 3f;

        [Header("Hover Visuals")]
        public GameObject hoverHighlightObject;
        public TileHoverColorManager hoverColorManager;
        public int baseSortingOrder = 0;

        [Header("Debug")]
        public bool debugLogs = false;
        public TextMeshProUGUI hoveredTileText;
        public TextMeshProUGUI currentToolText;

        // Public properties for external access
        public bool IsWithinInteractionRange => isWithinInteractionRange;
        public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
        public TileDefinition HoveredTileDef => hoveredTileDef;

        // Internal state
        private Dictionary<TileDefinition, DualGridTilemapModule> moduleByDefinition;
        private readonly Dictionary<Vector3Int, TimedTileState> timedCells = new Dictionary<Vector3Int, TimedTileState>();
        private Vector3Int? currentlyHoveredCell;
        private TileDefinition hoveredTileDef;
        private SpriteRenderer hoverSpriteRenderer;
        private bool isWithinInteractionRange = false;
        private bool refillHappenedThisFrame = false;

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

        private void Start()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
        }

        private void OnDestroy()
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

        private void Update()
        {
            refillHappenedThisFrame = false;
            HandleTileHover();
            
            if (RunManager.Instance?.CurrentState == RunState.GrowthAndThreat)
            {
                CheckAndRefillTool();
            }
            
            UpdateDebugUI();
        }

        /// <summary>
        /// Finds the tile with the HIGHEST interaction priority at the given position.
        /// This properly handles overlapping tiles (e.g., grass over dirt).
        /// </summary>
        public TileDefinition FindWhichTileDefinitionAt(Vector3Int cellPos)
        {
            EnsureInitialized();
            if (tileDefinitionMappings == null) return null;

            TileDefinition highestPriorityTile = null;
            int highestPriority = int.MinValue;

            foreach (var mapping in tileDefinitionMappings)
            {
                if (mapping?.tileDef == null || mapping.tilemapModule == null) continue;

                if (TileExistsInModule(mapping.tilemapModule, cellPos))
                {
                    int priority = mapping.tileDef.interactionPriority;
                    
                    if (priority > highestPriority)
                    {
                        highestPriority = priority;
                        highestPriorityTile = mapping.tileDef;
                    }
                }
            }

            return highestPriorityTile;
        }

        /// <summary>
        /// Gets ALL tiles present at the given position, sorted by priority (highest first).
        /// Useful for debugging or special interactions that need to know about all layers.
        /// </summary>
        public List<TileDefinition> GetAllTilesAt(Vector3Int cellPos)
        {
            EnsureInitialized();
            var tiles = new List<TileDefinition>();
            
            if (tileDefinitionMappings == null) return tiles;

            foreach (var mapping in tileDefinitionMappings)
            {
                if (mapping?.tileDef == null || mapping.tilemapModule == null) continue;

                if (TileExistsInModule(mapping.tilemapModule, cellPos))
                {
                    tiles.Add(mapping.tileDef);
                }
            }

            // Sort by priority descending
            tiles.Sort((a, b) => b.interactionPriority.CompareTo(a.interactionPriority));
            return tiles;
        }

        private bool TileExistsInModule(DualGridTilemapModule module, Vector3Int cellPos)
        {
            return module.DataTilemap != null && module.DataTilemap.HasTile(cellPos);
        }

        /// <summary>
        /// Applies a tool action at the currently hovered cell.
        /// Checks tiles in PRIORITY ORDER (highest first) to find a matching rule.
        /// </summary>
        public void ApplyToolAction(ToolDefinition toolDef)
        {
            if (toolDef == null || !currentlyHoveredCell.HasValue) return;

            Vector3Int targetCell = currentlyHoveredCell.Value;

            if (debugLogs) 
                Debug.Log($"[TileInteractionManager] Applying Tool: '{toolDef.displayName}' at {targetCell}");

            // Get all tiles at this position, sorted by priority
            var tilesAtPosition = GetAllTilesAt(targetCell);

            if (debugLogs && tilesAtPosition.Count > 0)
            {
                string tileList = string.Join(", ", tilesAtPosition.Select(t => $"{t.displayName}(P:{t.interactionPriority})"));
                Debug.Log($"[TileInteractionManager] Tiles at position (by priority): [{tileList}]");
            }

            // Check each tile in priority order for a matching rule
            foreach (var tileToTest in tilesAtPosition)
            {
                if (debugLogs) 
                    Debug.Log($"[TileInteractionManager] Checking tile '{tileToTest.displayName}' (priority {tileToTest.interactionPriority}) for tool rule...");

                TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(
                    r => r != null && r.tool == toolDef && r.fromTile == tileToTest
                );

                if (rule != null)
                {
                    if (debugLogs) 
                        Debug.Log($"[TileInteractionManager] MATCH! Rule: '{rule.fromTile.displayName}' -> '{(rule.toTile != null ? rule.toTile.displayName : "REMOVE")}'");

                    ExecuteTileTransformation(rule, targetCell);
                    return;
                }
            }

            if (debugLogs) 
                Debug.LogWarning($"[TileInteractionManager] No matching rule for tool '{toolDef.displayName}' on any tile at {targetCell}");
        }

        private void ExecuteTileTransformation(TileInteractionRule rule, Vector3Int targetCell)
        {
            if (rule.toTile == null)
            {
                // Rule removes the tile
                RemoveTile(rule.fromTile, targetCell);
            }
            else
            {
                // Rule transforms the tile
                if (!rule.toTile.keepBottomTile)
                {
                    RemoveTile(rule.fromTile, targetCell);
                }
                PlaceTile(rule.toTile, targetCell);
            }
        }

        private void CheckAndRefillTool()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (hoveredTileDef == null || ToolSwitcher.Instance == null) return;

            var currentTool = ToolSwitcher.Instance.CurrentTool;
            if (currentTool == null || !currentTool.limitedUses) return;

            if (interactionLibrary?.refillRules != null)
            {
                var refillRule = interactionLibrary.refillRules.FirstOrDefault(
                    r => r.toolToRefill == currentTool && r.refillSourceTile == hoveredTileDef
                );

                if (refillRule != null && isWithinInteractionRange)
                {
                    ToolSwitcher.Instance.RefillCurrentTool();
                    refillHappenedThisFrame = true;

                    if (debugLogs) 
                        Debug.Log($"[TileInteractionManager] Refilled {currentTool.displayName} on {hoveredTileDef.displayName}");

                    if (PlayerActionManager.Instance != null)
                    {
                        PlayerActionManager.Instance.ExecutePlayerAction(PlayerActionType.Interact, currentlyHoveredCell.Value, "Refill");
                    }
                }
            }
        }

        public bool DidRefillThisFrame => refillHappenedThisFrame;

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
                            if (debugLogs) 
                                Debug.Log($"[TileInteractionManager] Reverting tile '{state.tileDef.displayName}' at {cellPos}");
                            
                            RemoveTile(state.tileDef, cellPos);
                            
                            if (state.tileDef.revertToTile != null)
                            {
                                PlaceTile(state.tileDef.revertToTile, cellPos);
                            }
                        }
                        else
                        {
                            if (debugLogs) 
                                Debug.LogWarning($"[TileInteractionManager] State desync at {cellPos}. Expected '{state.tileDef.displayName}', found '{actualTile?.displayName ?? "NULL"}'");
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
                    timedCells[cellPos] = new TimedTileState 
                    { 
                        tileDef = tileDef, 
                        ticksRemaining = tileDef.revertAfterTicks 
                    };
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
                        Debug.LogWarning($"[TileInteractionManager] Duplicate TileDefinition '{mapping.tileDef.displayName}' in mappings.", mapping.tileDef);
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
                
                // Also show all tiles at this position for debugging
                if (debugLogs && currentlyHoveredCell.HasValue)
                {
                    var allTiles = GetAllTilesAt(currentlyHoveredCell.Value);
                    if (allTiles.Count > 1)
                    {
                        string allNames = string.Join(", ", allTiles.Select(t => $"{t.displayName}({t.interactionPriority})"));
                        tileName = $"{tileName} [All: {allNames}]";
                    }
                }
                
                hoveredTileText.text = $"Tile: {tileName}";
            }

            if (currentToolText != null && ToolSwitcher.Instance != null)
            {
                var currentTool = ToolSwitcher.Instance.CurrentTool;
                currentToolText.text = currentTool != null ? $"Tool: {currentTool.displayName}" : "Tool: None";
            }
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            if (interactionGrid == null) return Vector3Int.zero;
            return interactionGrid.WorldToCell(worldPosition);
        }

        public Vector3 CellCenterWorld(Vector3Int cellPos)
        {
            if (interactionGrid == null) return Vector3.zero;
            return interactionGrid.GetCellCenterWorld(cellPos);
        }
    }
}
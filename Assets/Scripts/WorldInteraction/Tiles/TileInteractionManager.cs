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
        public int baseSortingOrder = 0;

        [Header("Debug")]
        public bool debugLogs = false;
        public TextMeshProUGUI hoveredTileText;
        public TextMeshProUGUI currentToolText;

        public bool IsWithinInteractionRange => isWithinInteractionRange;
        public Vector3Int? CurrentlyHoveredCell => currentlyHoveredCell;
        public TileDefinition HoveredTileDef => hoveredTileDef;

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

            tiles.Sort((a, b) => b.interactionPriority.CompareTo(a.interactionPriority));
            return tiles;
        }

        private bool TileExistsInModule(DualGridTilemapModule module, Vector3Int cellPos)
        {
            return module.DataTilemap != null && module.DataTilemap.HasTile(cellPos);
        }

        public void ApplyToolAction(ToolDefinition toolDef)
        {
            if (toolDef == null || !currentlyHoveredCell.HasValue) return;

            Vector3Int targetCell = currentlyHoveredCell.Value;
            
            // NEW: Check if the tile is blocked by an entity (like a MultiTileEntity or Plant)
            if (GridPositionManager.Instance != null && 
                GridPositionManager.Instance.IsPositionOccupied(new GridPosition(targetCell)))
            {
                if (debugLogs) 
                    Debug.Log($"[TileInteractionManager] Action blocked: Position {targetCell} is occupied by an entity.");
                return;
            }

            TileDefinition topTile = FindWhichTileDefinitionAt(targetCell);

            if (topTile == null)
            {
                if (debugLogs)
                    Debug.Log($"[TileInteractionManager] No tile at {targetCell}");
                return;
            }

            if (debugLogs)
            {
                Debug.Log($"[TileInteractionManager] Applying Tool: '{toolDef.displayName}' at {targetCell}");
                Debug.Log($"[TileInteractionManager] Top tile: '{topTile.displayName}' (Priority: {topTile.interactionPriority})");

                var allTiles = GetAllTilesAt(targetCell);
                if (allTiles.Count > 1)
                {
                    string tileList = string.Join(", ", allTiles.Select(t => $"{t.displayName}(P:{t.interactionPriority})"));
                    Debug.Log($"[TileInteractionManager] All tiles at position: [{tileList}]");
                }
            }

            TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(
                r => r != null && r.tool == toolDef && r.fromTile == topTile
            );

            if (rule != null)
            {
                if (debugLogs)
                    Debug.Log($"[TileInteractionManager] ✓ MATCH! Rule: '{rule.fromTile.displayName}' -> '{(rule.toTile != null ? rule.toTile.displayName : "REMOVE")}'");

                ExecuteTileTransformation(rule, targetCell);
            }
            else
            {
                if (debugLogs)
                    Debug.Log($"[TileInteractionManager] ✗ No rule for '{toolDef.displayName}' on '{topTile.displayName}'. Action blocked by surface tile.");
            }
        }

        private void ExecuteTileTransformation(TileInteractionRule rule, Vector3Int targetCell)
        {
            if (rule.toTile == null)
            {
                RemoveTile(rule.fromTile, targetCell);
            }
            else
            {
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
            if (ToolSwitcher.Instance == null) return;

            var currentTool = ToolSwitcher.Instance.CurrentTool;
            if (currentTool == null || !currentTool.limitedUses) return;

            if (hoveredTileDef == null) return;

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
                        Debug.Log($"[TileInteractionManager] Refilled {currentTool.displayName} from '{hoveredTileDef.displayName}'");

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
            if (hoverSpriteRenderer != null && interactionLibrary != null)
            {
                hoverSpriteRenderer.color = interactionLibrary.GetHoverColorForRange(withinRange);
            }
        }

        private void UpdateDebugUI()
        {
            if (hoveredTileText != null)
            {
                string tileName = hoveredTileDef != null ? hoveredTileDef.displayName : "None";
                string rangeStatus = isWithinInteractionRange ? "[IN RANGE]" : "[OUT OF RANGE]";
                hoveredTileText.text = $"Tile: {tileName} {rangeStatus}";
            }

            if (currentToolText != null && ToolSwitcher.Instance != null)
            {
                var tool = ToolSwitcher.Instance.CurrentTool;
                currentToolText.text = tool != null ? $"Tool: {tool.displayName}" : "Tool: None";
            }
        }

        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            if (interactionGrid != null)
            {
                return interactionGrid.WorldToCell(worldPos);
            }
            return Vector3Int.FloorToInt(worldPos);
        }

        public Vector3 CellCenterWorld(Vector3Int cellPos)
        {
            if (interactionGrid != null)
            {
                return interactionGrid.GetCellCenterWorld(cellPos);
            }
            return new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
        }
    }
}
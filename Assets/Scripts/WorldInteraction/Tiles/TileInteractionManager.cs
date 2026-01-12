using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Abracodabra.UI.Genes;
using skner.DualGrid;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
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
        [Tooltip("Map TileDefinitions to their DualGridTilemapModules. Auto-backed up on every change!")]
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

        /// <summary>
/// Applies a tool action to the currently hovered cell.
/// Returns true if the action succeeded (tile was transformed), false otherwise.
/// </summary>
public bool ApplyToolAction(ToolDefinition toolDef) {
    if (toolDef == null || !currentlyHoveredCell.HasValue) return false;

    Vector3Int targetCell = currentlyHoveredCell.Value;
    GridPosition gridPos = new GridPosition(targetCell);

    // Check if blocked by multi-tile entity
    if (GridPositionManager.Instance != null) {
        var multiTileEntity = GridPositionManager.Instance.GetMultiTileEntityAt(gridPos);
        if (multiTileEntity != null && multiTileEntity.BlocksToolUsage) {
            if (debugLogs)
                Debug.Log($"[TileInteractionManager] Tool action blocked: Position {targetCell} has tool usage blocked by '{multiTileEntity.gameObject.name}'.");
            return false;
        }
    }

    TileDefinition topTile = FindWhichTileDefinitionAt(targetCell);

    if (topTile == null) {
        if (debugLogs)
            Debug.Log($"[TileInteractionManager] No tile at {targetCell}");
        return false;
    }

    if (debugLogs) {
        Debug.Log($"[TileInteractionManager] Applying Tool: '{toolDef.displayName}' at {targetCell}");
        Debug.Log($"[TileInteractionManager] Top tile: '{topTile.displayName}' (Priority: {topTile.interactionPriority})");

        var allTiles = GetAllTilesAt(targetCell);
        if (allTiles.Count > 1) {
            string tileList = string.Join(", ", allTiles.Select(t => $"{t.displayName}(P:{t.interactionPriority})"));
            Debug.Log($"[TileInteractionManager] All tiles at position: [{tileList}]");
        }
    }

    TileInteractionRule rule = interactionLibrary?.rules.FirstOrDefault(
        r => r != null && r.tool == toolDef && r.fromTile == topTile
    );

    if (rule != null) {
        if (debugLogs)
            Debug.Log($"[TileInteractionManager] ✓ MATCH! Rule: '{rule.fromTile.displayName}' -> '{(rule.toTile != null ? rule.toTile.displayName : "REMOVE")}'");

        ExecuteTileTransformation(rule, targetCell);
        return true; // Action succeeded
    }
    else {
        if (debugLogs)
            Debug.Log($"[TileInteractionManager] ✗ No rule for '{toolDef.displayName}' on '{topTile.displayName}'. Action blocked by surface tile.");
        return false; // No matching rule - action failed
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

#if UNITY_EDITOR
        // ============================================================
        // BACKUP/RESTORE SYSTEM - Prevents data loss on recompilation
        // ============================================================

        private const string BACKUP_FOLDER = "Assets/Editor/TileMappingsBackup";
        private const string BACKUP_FILENAME = "TileMappingsBackup.json";

        [Serializable]
        private class MappingBackupData
        {
            public string tileDefGuid;
            public string tileDefPath;
            public string tilemapModulePath; // Hierarchy path in scene
        }

        [Serializable]
        private class BackupFile
        {
            public string sceneName;
            public string gameObjectName;
            public List<MappingBackupData> mappings = new List<MappingBackupData>();
            public string backupTime;
        }

        private string GetBackupFilePath()
        {
            return Path.Combine(BACKUP_FOLDER, BACKUP_FILENAME);
        }

        [ContextMenu("💾 Backup Mappings to File")]
        public void BackupMappingsToFile()
        {
            if (tileDefinitionMappings == null || tileDefinitionMappings.Count == 0)
            {
                Debug.LogWarning("[TileInteractionManager] No mappings to backup!");
                return;
            }

            // Ensure backup folder exists
            if (!Directory.Exists(BACKUP_FOLDER))
            {
                Directory.CreateDirectory(BACKUP_FOLDER);
            }

            var backup = new BackupFile
            {
                sceneName = gameObject.scene.name,
                gameObjectName = gameObject.name,
                backupTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            foreach (var mapping in tileDefinitionMappings)
            {
                if (mapping == null) continue;

                var data = new MappingBackupData();

                // Store TileDefinition by GUID (asset reference)
                if (mapping.tileDef != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(mapping.tileDef);
                    data.tileDefGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    data.tileDefPath = assetPath;
                }

                // Store TilemapModule by hierarchy path (scene reference)
                if (mapping.tilemapModule != null)
                {
                    data.tilemapModulePath = GetGameObjectPath(mapping.tilemapModule.gameObject);
                }

                backup.mappings.Add(data);
            }

            string json = JsonUtility.ToJson(backup, true);
            File.WriteAllText(GetBackupFilePath(), json);
            AssetDatabase.Refresh();

            Debug.Log($"[TileInteractionManager] ✅ Backed up {backup.mappings.Count} mappings to {GetBackupFilePath()}");
        }

        [ContextMenu("📂 Restore Mappings from File")]
        public void RestoreMappingsFromFile()
        {
            string filePath = GetBackupFilePath();

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[TileInteractionManager] No backup file found at {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            var backup = JsonUtility.FromJson<BackupFile>(json);

            if (backup == null || backup.mappings == null)
            {
                Debug.LogError("[TileInteractionManager] Failed to parse backup file!");
                return;
            }

            // Clear existing mappings
            if (tileDefinitionMappings == null)
            {
                tileDefinitionMappings = new List<TileDefinitionMapping>();
            }
            tileDefinitionMappings.Clear();

            int restoredCount = 0;
            int failedCount = 0;

            foreach (var data in backup.mappings)
            {
                var mapping = new TileDefinitionMapping();

                // Restore TileDefinition from GUID
                if (!string.IsNullOrEmpty(data.tileDefGuid))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(data.tileDefGuid);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        mapping.tileDef = AssetDatabase.LoadAssetAtPath<TileDefinition>(assetPath);
                    }
                    
                    // Fallback to path if GUID fails
                    if (mapping.tileDef == null && !string.IsNullOrEmpty(data.tileDefPath))
                    {
                        mapping.tileDef = AssetDatabase.LoadAssetAtPath<TileDefinition>(data.tileDefPath);
                    }
                }

                // Restore TilemapModule from hierarchy path
                if (!string.IsNullOrEmpty(data.tilemapModulePath))
                {
                    GameObject go = FindGameObjectByPath(data.tilemapModulePath);
                    if (go != null)
                    {
                        mapping.tilemapModule = go.GetComponent<DualGridTilemapModule>();
                    }
                }

                tileDefinitionMappings.Add(mapping);

                if (mapping.tileDef != null && mapping.tilemapModule != null)
                {
                    restoredCount++;
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"[TileInteractionManager] Partial restore - TileDef: {(mapping.tileDef != null ? "✓" : "✗")}, Module: {(mapping.tilemapModule != null ? "✓" : "✗")} (Path: {data.tilemapModulePath})");
                }
            }

            EditorUtility.SetDirty(this);

            Debug.Log($"[TileInteractionManager] ✅ Restored {restoredCount} mappings successfully, {failedCount} had issues. Backup was from: {backup.backupTime}");
        }

        [ContextMenu("🔍 Check Backup Status")]
        public void CheckBackupStatus()
        {
            string filePath = GetBackupFilePath();

            if (!File.Exists(filePath))
            {
                Debug.Log("[TileInteractionManager] No backup file exists yet. Use 'Backup Mappings to File' to create one.");
                return;
            }

            string json = File.ReadAllText(filePath);
            var backup = JsonUtility.FromJson<BackupFile>(json);

            int currentCount = tileDefinitionMappings?.Count ?? 0;
            int backupCount = backup?.mappings?.Count ?? 0;

            if (currentCount == 0 && backupCount > 0)
            {
                Debug.LogWarning($"[TileInteractionManager] ⚠️ MAPPINGS LOST! Current: {currentCount}, Backup has: {backupCount}. Use 'Restore Mappings from File' to recover.");
            }
            else if (currentCount < backupCount)
            {
                Debug.LogWarning($"[TileInteractionManager] ⚠️ Some mappings may be lost. Current: {currentCount}, Backup has: {backupCount}.");
            }
            else
            {
                Debug.Log($"[TileInteractionManager] ✅ Mappings look fine. Current: {currentCount}, Backup: {backupCount}. Last backup: {backup.backupTime}");
            }
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        private GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string[] parts = path.Split('/');
            
            // Find root objects in scene
            GameObject[] rootObjects = gameObject.scene.GetRootGameObjects();
            GameObject current = null;

            foreach (var root in rootObjects)
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }

            if (current == null) return null;

            // Navigate down the hierarchy
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null) return null;
                current = child.gameObject;
            }

            return current;
        }

        // Auto-backup when mappings change
        private int lastMappingCount = -1;
        private void OnValidate()
        {
            if (tileDefinitionMappings == null) return;
            
            // Check if any mapping has both references set
            int validCount = tileDefinitionMappings.Count(m => m?.tileDef != null && m?.tilemapModule != null);
            
            // Only backup if we have valid mappings and the count changed
            if (validCount > 0 && validCount != lastMappingCount)
            {
                lastMappingCount = validCount;
                
                // Delayed call to avoid issues during serialization
                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        BackupMappingsToFile();
                    }
                };
            }
        }
#endif
    }
}
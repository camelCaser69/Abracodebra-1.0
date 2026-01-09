using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using WegoSystem;

namespace Abracodabra.UI.Genes
{
    public class PlantPlacementManager : MonoBehaviour
    {
        public static PlantPlacementManager Instance { get; private set; }

        [SerializeField] private Transform plantParent;
        [SerializeField] private TileInteractionManager tileInteractionManager;
        [SerializeField] private NodeExecutor nodeExecutor;
        [SerializeField] private float spawnRadius = 0.25f;

        [Header("Global Invalid Tiles")]
        [Tooltip("Tiles that are NEVER valid for planting, regardless of seed terrain affinity. " +
                 "Note: This is a BLACKLIST. For restricting seeds to specific tiles, use TerrainAffinityGene instead.")]
        [SerializeField] private List<TileDefinition> invalidPlantingTiles = new List<TileDefinition>();

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        private readonly HashSet<TileDefinition> invalidTilesSet = new HashSet<TileDefinition>();
        private readonly Dictionary<Vector3Int, GameObject> plantsByGridPosition = new Dictionary<Vector3Int, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RebuildInvalidTilesSet();
        }

        public void Initialize()
        {
            if (plantParent == null && EcosystemManager.Instance != null)
                plantParent = EcosystemManager.Instance.plantParent;
            if (tileInteractionManager == null)
                tileInteractionManager = TileInteractionManager.Instance;
            if (nodeExecutor == null)
                nodeExecutor = FindFirstObjectByType<NodeExecutor>();
        }

        private void RebuildInvalidTilesSet()
        {
            invalidTilesSet.Clear();
            foreach (var tile in invalidPlantingTiles)
            {
                if (tile != null)
                {
                    invalidTilesSet.Add(tile);
                }
            }
            
            if (verboseLogging && invalidTilesSet.Count > 0)
            {
                string invalidNames = string.Join(", ", invalidTilesSet.Select(t => t.displayName));
                Debug.Log($"[PlantPlacementManager] Global invalid tiles: [{invalidNames}]");
            }
        }

        public bool IsPositionOccupied(Vector3Int gridPosition)
        {
            CleanupDestroyedPlants();
    
            // Check if there's already a plant at this position
            if (plantsByGridPosition.ContainsKey(gridPosition)) return true;

            // Use the new granular blocking check for seed planting
            if (GridPositionManager.Instance != null)
            {
                return GridPositionManager.Instance.IsSeedPlantingBlockedAt(new GridPosition(gridPosition));
            }

            return false;
        }

        /// <summary>
        /// Checks if a tile is valid for planting (not in global blacklist).
        /// This does NOT check seed-specific terrain affinity.
        /// </summary>
        public bool IsTileValidForPlanting(TileDefinition tileDef)
        {
            return tileDef != null && !invalidTilesSet.Contains(tileDef);
        }

        /// <summary>
        /// Checks if a specific seed can be planted on a specific tile.
        /// Takes into account both global invalid tiles AND seed's TerrainAffinityGene.
        /// </summary>
        public bool IsTileValidForSeed(TileDefinition tileDef, PlantGeneRuntimeState runtimeState)
        {
            // Step 1: Check global blacklist
            if (!IsTileValidForPlanting(tileDef))
            {
                if (verboseLogging)
                    Debug.Log($"[PlantPlacementManager] ❌ Tile '{tileDef?.displayName ?? "NULL"}' is in the global invalid tiles list.");
                return false;
            }

            // Step 2: Get the seed's terrain affinity gene
            var affinityGene = GetTerrainAffinityGene(runtimeState);
            
            if (affinityGene == null)
            {
                // No TerrainAffinityGene = can plant anywhere (that's not globally invalid)
                if (verboseLogging)
                {
                    Debug.LogWarning($"[PlantPlacementManager] ⚠️ Seed '{runtimeState?.template?.templateName ?? "Unknown"}' has NO TerrainAffinityGene. " +
                                   $"It can be planted on ANY tile. Add a TerrainAffinityGene to restrict planting locations.");
                }
                return true;
            }

            // Step 3: Check if allowedTiles is empty (which means "any tile")
            if (affinityGene.AllowedTiles == null || affinityGene.AllowedTiles.Count == 0)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning($"[PlantPlacementManager] ⚠️ TerrainAffinityGene '{affinityGene.geneName}' has EMPTY allowedTiles. " +
                                   $"Seed can be planted anywhere. Add TileDefinitions to restrict.");
                }
                return true;
            }

            // Step 4: Check if this tile is in the allowed list
            bool allowed = affinityGene.IsTileAllowed(tileDef);
            
            if (verboseLogging)
            {
                string allowedTileNames = string.Join(", ", affinityGene.AllowedTiles
                    .Where(t => t != null)
                    .Select(t => t.displayName));
                
                string icon = allowed ? "✓" : "❌";
                Debug.Log($"[PlantPlacementManager] {icon} Tile '{tileDef?.displayName}' (Priority: {tileDef?.interactionPriority}) " +
                         $"allowed: {allowed}. Seed requires: [{allowedTileNames}]");
            }

            return allowed;
        }

        private TerrainAffinityGene GetTerrainAffinityGene(PlantGeneRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                if (verboseLogging)
                    Debug.LogError("[PlantPlacementManager] GetTerrainAffinityGene: runtimeState is null!");
                return null;
            }

            if (runtimeState.passiveInstances == null)
            {
                if (verboseLogging)
                    Debug.LogError("[PlantPlacementManager] GetTerrainAffinityGene: passiveInstances list is null!");
                return null;
            }

            if (verboseLogging)
            {
                Debug.Log($"[PlantPlacementManager] Searching {runtimeState.passiveInstances.Count} passive gene slots...");
                
                for (int i = 0; i < runtimeState.passiveInstances.Count; i++)
                {
                    var instance = runtimeState.passiveInstances[i];
                    if (instance == null)
                    {
                        Debug.Log($"  Slot [{i}]: Empty");
                    }
                    else
                    {
                        var gene = instance.GetGene();
                        string isAffinity = (gene is TerrainAffinityGene) ? " ← TERRAIN AFFINITY" : "";
                        Debug.Log($"  Slot [{i}]: {gene?.geneName ?? "NULL"} ({gene?.GetType().Name ?? "null"}){isAffinity}");
                    }
                }
            }

            // Search for TerrainAffinityGene
            foreach (var instance in runtimeState.passiveInstances)
            {
                if (instance == null) continue;

                var gene = instance.GetGene();
                if (gene is TerrainAffinityGene affinityGene)
                {
                    if (verboseLogging)
                    {
                        string allowedNames = string.Join(", ", affinityGene.AllowedTiles?
                            .Where(t => t != null)
                            .Select(t => t.displayName) ?? Array.Empty<string>());
                        Debug.Log($"[PlantPlacementManager] Found TerrainAffinityGene: '{affinityGene.geneName}' " +
                                 $"with allowed tiles: [{allowedNames}]");
                    }
                    return affinityGene;
                }
            }

            if (verboseLogging)
            {
                Debug.Log("[PlantPlacementManager] No TerrainAffinityGene found in passive gene slots.");
            }
            return null;
        }

        public string GetInvalidTileReason(TileDefinition tileDef, PlantGeneRuntimeState runtimeState)
        {
            if (tileDef == null)
            {
                return "No tile at this position";
            }

            if (invalidTilesSet.Contains(tileDef))
            {
                return $"'{tileDef.displayName}' is globally blocked for planting";
            }

            var affinityGene = GetTerrainAffinityGene(runtimeState);
            if (affinityGene != null && !affinityGene.IsTileAllowed(tileDef))
            {
                var allowedNames = affinityGene.AllowedTiles
                    .Where(t => t != null)
                    .Select(t => t.displayName)
                    .ToList();
                
                if (allowedNames.Count > 0)
                {
                    return $"This seed requires: {string.Join(", ", allowedNames)}";
                }
                else
                {
                    return "TerrainAffinityGene has no allowed tiles configured";
                }
            }

            return "Unknown reason";
        }

        private void CleanupDestroyedPlants()
        {
            var keysToRemove = plantsByGridPosition
                .Where(kvp => kvp.Value == null)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                plantsByGridPosition.Remove(key);
            }
        }

        public bool TryPlantSeedFromInventory(PlantGeneRuntimeState runtimeState, Vector3Int gridPosition, Vector3 worldPosition)
        {
            if (runtimeState == null || runtimeState.template == null)
            {
                Debug.LogError("[PlantPlacementManager] Failed: RuntimeState or its template was null.");
                return false;
            }

            if (!runtimeState.template.IsValid())
            {
                Debug.LogError($"[PlantPlacementManager] Failed: Seed template '{runtimeState.template.templateName}' configuration is invalid.", runtimeState.template);
                return false;
            }

            if (IsPositionOccupied(gridPosition))
            {
                Debug.LogWarning($"[PlantPlacementManager] Failed: Position {gridPosition} is already occupied.", this);
                return false;
            }

            // Get the tile at this position (uses priority-based detection)
            TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
            
            if (verboseLogging)
            {
                Debug.Log($"[PlantPlacementManager] === PLANTING ATTEMPT ===");
                Debug.Log($"  Seed: '{runtimeState.template.templateName}'");
                Debug.Log($"  Position: {gridPosition}");
                Debug.Log($"  Detected Tile: '{tileDef?.displayName ?? "NULL"}' (Priority: {tileDef?.interactionPriority ?? 0})");
                
                // Show all tiles at this position for debugging
                var allTiles = tileInteractionManager?.GetAllTilesAt(gridPosition);
                if (allTiles != null && allTiles.Count > 1)
                {
                    string allNames = string.Join(", ", allTiles.Select(t => $"{t.displayName}(P:{t.interactionPriority})"));
                    Debug.Log($"  All tiles at position: [{allNames}]");
                }
            }

            // Validate tile for this seed
            if (!IsTileValidForSeed(tileDef, runtimeState))
            {
                string reason = GetInvalidTileReason(tileDef, runtimeState);
                Debug.LogWarning($"[PlantPlacementManager] ❌ PLANTING BLOCKED: {reason}", this);
                return false;
            }

            if (nodeExecutor == null)
            {
                nodeExecutor = FindFirstObjectByType<NodeExecutor>();
                if (nodeExecutor == null)
                {
                    Debug.LogError("[PlantPlacementManager] Failed: NodeExecutor not found!");
                    return false;
                }
            }

            Vector3 finalPlantingPosition = GetRandomizedPlantingPosition(worldPosition);
            GameObject plantGO = nodeExecutor.SpawnPlantFromState(runtimeState, finalPlantingPosition, plantParent);

            if (plantGO == null)
            {
                Debug.LogError("[PlantPlacementManager] Failed: NodeExecutor returned null. Check Plant Prefab assignment.", this);
                return false;
            }

            GridPositionManager.Instance.SnapEntityToGrid(plantGO);
            var finalGridEntity = plantGO.GetComponent<GridEntity>();
            Vector3Int finalGridPosition = finalGridEntity.Position.ToVector3Int();

            plantsByGridPosition[finalGridPosition] = plantGO;
            
            if (verboseLogging)
            {
                Debug.Log($"[PlantPlacementManager] ✓ Successfully planted '{runtimeState.template.templateName}' at {finalGridPosition}");
            }
            
            return true;
        }

        private Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition)
        {
            if (spawnRadius <= 0f) return centerPosition;
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            return centerPosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        }
    }
}
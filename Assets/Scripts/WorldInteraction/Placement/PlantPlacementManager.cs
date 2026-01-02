using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Core;
using WegoSystem;

namespace Abracodabra.UI.Genes {
    public class PlantPlacementManager : MonoBehaviour {
        public static PlantPlacementManager Instance { get; private set; }

        [SerializeField] Transform plantParent;
        [SerializeField] TileInteractionManager tileInteractionManager;
        [SerializeField] NodeExecutor nodeExecutor;
        [SerializeField] float spawnRadius = 0.25f;
        
        [Header("Global Invalid Tiles")]
        [Tooltip("Tiles that are NEVER valid for planting, regardless of seed terrain affinity")]
        [SerializeField] List<TileDefinition> invalidPlantingTiles = new List<TileDefinition>();

        readonly HashSet<TileDefinition> invalidTilesSet = new HashSet<TileDefinition>();
        readonly Dictionary<Vector3Int, GameObject> plantsByGridPosition = new Dictionary<Vector3Int, GameObject>();

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RebuildInvalidTilesSet();
        }

        public void Initialize() {
            if (plantParent == null && EcosystemManager.Instance != null)
                plantParent = EcosystemManager.Instance.plantParent;
            if (tileInteractionManager == null)
                tileInteractionManager = TileInteractionManager.Instance;
            if (nodeExecutor == null)
                nodeExecutor = FindFirstObjectByType<NodeExecutor>();
        }

        void RebuildInvalidTilesSet() {
            invalidTilesSet.Clear();
            foreach (var tile in invalidPlantingTiles) {
                if (tile != null) {
                    invalidTilesSet.Add(tile);
                }
            }
        }

        public bool IsPositionOccupied(Vector3Int gridPosition) {
            CleanupDestroyedPlants();
            return plantsByGridPosition.ContainsKey(gridPosition);
        }

        /// <summary>
        /// Check if a tile is valid for planting using only global rules (no seed-specific checks)
        /// </summary>
        public bool IsTileValidForPlanting(TileDefinition tileDef) {
            return tileDef != null && !invalidTilesSet.Contains(tileDef);
        }
        
        /// <summary>
        /// Check if a tile is valid for planting a specific seed (checks TerrainAffinityGene)
        /// </summary>
        public bool IsTileValidForSeed(TileDefinition tileDef, PlantGeneRuntimeState runtimeState) {
            // First check global rules
            if (!IsTileValidForPlanting(tileDef)) {
                return false;
            }
            
            // Then check seed-specific terrain affinity
            var affinityGene = GetTerrainAffinityGene(runtimeState);
            if (affinityGene == null) {
                // No terrain affinity gene means the seed can grow anywhere valid
                return true;
            }
            
            // If the gene is additive, it ADDS to the allowed tiles
            // If not additive (restrictive), the seed can ONLY grow on those tiles
            if (affinityGene.IsAdditive) {
                // Additive: allowed if globally valid OR in the allowed list
                return affinityGene.IsTileAllowed(tileDef);
            }
            else {
                // Restrictive: must be in the allowed list
                return affinityGene.IsTileAllowed(tileDef);
            }
        }
        
        /// <summary>
        /// Get the TerrainAffinityGene from a seed's runtime state, if any
        /// </summary>
        TerrainAffinityGene GetTerrainAffinityGene(PlantGeneRuntimeState runtimeState) {
            if (runtimeState == null || runtimeState.passiveInstances == null) {
                return null;
            }
            
            foreach (var instance in runtimeState.passiveInstances) {
                var gene = instance?.GetGene();
                if (gene is TerrainAffinityGene affinityGene) {
                    return affinityGene;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get a human-readable message explaining why a tile is invalid for a seed
        /// </summary>
        public string GetInvalidTileReason(TileDefinition tileDef, PlantGeneRuntimeState runtimeState) {
            if (tileDef == null) {
                return "No tile at this position";
            }
            
            if (invalidTilesSet.Contains(tileDef)) {
                return $"Cannot plant on {tileDef.displayName}";
            }
            
            var affinityGene = GetTerrainAffinityGene(runtimeState);
            if (affinityGene != null && !affinityGene.IsTileAllowed(tileDef)) {
                var allowedNames = new List<string>();
                foreach (var allowed in affinityGene.AllowedTiles) {
                    if (allowed != null) allowedNames.Add(allowed.displayName);
                }
                return $"This seed requires: {string.Join(", ", allowedNames)}";
            }
            
            return "Unknown reason";
        }

        void CleanupDestroyedPlants() {
            var keysToRemove = plantsByGridPosition
                .Where(kvp => kvp.Value == null)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in keysToRemove) {
                plantsByGridPosition.Remove(key);
            }
        }

        public bool TryPlantSeedFromInventory(PlantGeneRuntimeState runtimeState, Vector3Int gridPosition, Vector3 worldPosition) {
            if (runtimeState == null || runtimeState.template == null) {
                Debug.LogError("[PlantPlacementManager] Failed: RuntimeState or its template was null.");
                return false;
            }

            if (!runtimeState.template.IsValid()) {
                Debug.LogError($"[PlantPlacementManager] Failed: Seed template '{runtimeState.template.templateName}' configuration is invalid.", runtimeState.template);
                return false;
            }

            if (IsPositionOccupied(gridPosition)) {
                Debug.LogWarning($"[PlantPlacementManager] Failed: Position {gridPosition} is already occupied by another plant in the dictionary.", this);
                return false;
            }

            TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
            
            // Use the new seed-specific check
            if (!IsTileValidForSeed(tileDef, runtimeState)) {
                string reason = GetInvalidTileReason(tileDef, runtimeState);
                Debug.LogWarning($"[PlantPlacementManager] Failed: {reason}", this);
                return false;
            }

            if (nodeExecutor == null) {
                nodeExecutor = FindFirstObjectByType<NodeExecutor>();
                if (nodeExecutor == null) {
                    Debug.LogError("[PlantPlacementManager] Failed: NodeExecutor reference is missing and could not be found in the scene!");
                    return false;
                }
            }

            Vector3 finalPlantingPosition = GetRandomizedPlantingPosition(worldPosition);
            GameObject plantGO = nodeExecutor.SpawnPlantFromState(runtimeState, finalPlantingPosition, plantParent);

            if (plantGO == null) {
                Debug.LogError("[PlantPlacementManager] Failed: NodeExecutor returned a null plant GameObject. Check NodeExecutor's logs (is its Plant Prefab assigned?).", this);
                return false;
            }

            GridPositionManager.Instance.SnapEntityToGrid(plantGO);
            var finalGridEntity = plantGO.GetComponent<GridEntity>();
            Vector3Int finalGridPosition = finalGridEntity.Position.ToVector3Int();

            plantsByGridPosition[finalGridPosition] = plantGO;
            Debug.Log($"[PlantPlacementManager] Successfully planted '{runtimeState.template.templateName}' and registered it at final grid position {finalGridPosition}.");
            return true;
        }

        Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition) {
            if (spawnRadius <= 0f) return centerPosition;
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            return centerPosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        }
    }
}

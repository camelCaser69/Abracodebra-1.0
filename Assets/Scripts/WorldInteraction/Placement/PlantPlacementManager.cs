using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Abracodabra.Genes.Templates;
using Abracodabra.Genes.Runtime;
using WegoSystem; // <-- FIX: Added this line to resolve the namespace issue.

namespace Abracodabra.UI.Genes
{
    public class PlantPlacementManager : MonoBehaviour
    {
        public static PlantPlacementManager Instance { get; set; }

        [SerializeField] private Transform plantParent;
        [SerializeField] private TileInteractionManager tileInteractionManager;
        [SerializeField] private NodeExecutor nodeExecutor;
        [SerializeField] private float spawnRadius = 0.25f;
        [SerializeField] private List<TileDefinition> invalidPlantingTiles = new List<TileDefinition>();

        private readonly HashSet<TileDefinition> invalidTilesSet = new HashSet<TileDefinition>();
        private readonly Dictionary<Vector3Int, GameObject> plantsByGridPosition = new Dictionary<Vector3Int, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            RebuildInvalidTilesSet();
        }

        public void Initialize()
        {
            if (plantParent == null && EcosystemManager.Instance != null) plantParent = EcosystemManager.Instance.plantParent;
            if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance;
            if (nodeExecutor == null) nodeExecutor = FindFirstObjectByType<NodeExecutor>();
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
        }

        public bool IsPositionOccupied(Vector3Int gridPosition)
        {
            CleanupDestroyedPlants();
            return plantsByGridPosition.ContainsKey(gridPosition);
        }

        public bool IsTileValidForPlanting(TileDefinition tileDef)
        {
            return tileDef != null && !invalidTilesSet.Contains(tileDef);
        }

        private void CleanupDestroyedPlants()
        {
            var keysToRemove = plantsByGridPosition.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
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
                Debug.LogWarning($"[PlantPlacementManager] Failed: Position {gridPosition} is already occupied by another plant in the dictionary.", this);
                return false;
            }

            TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
            if (!IsTileValidForPlanting(tileDef))
            {
                string tileName = tileDef != null ? tileDef.displayName : "NULL";
                Debug.LogWarning($"[PlantPlacementManager] Failed: Tile '{tileName}' at {gridPosition} is not a valid planting tile.", this);
                return false;
            }

            if (nodeExecutor == null)
            {
                nodeExecutor = FindFirstObjectByType<NodeExecutor>();
                if (nodeExecutor == null)
                {
                    Debug.LogError("[PlantPlacementManager] Failed: NodeExecutor reference is missing and could not be found in the scene!");
                    return false;
                }
            }

            Vector3 finalPlantingPosition = GetRandomizedPlantingPosition(worldPosition);
            GameObject plantGO = nodeExecutor.SpawnPlantFromState(runtimeState, finalPlantingPosition, plantParent);

            if (plantGO == null)
            {
                Debug.LogError("[PlantPlacementManager] Failed: NodeExecutor returned a null plant GameObject. Check NodeExecutor's logs (is its Plant Prefab assigned?).", this);
                return false;
            }

            WegoSystem.GridPositionManager.Instance.SnapEntityToGrid(plantGO);
            var finalGridEntity = plantGO.GetComponent<WegoSystem.GridEntity>();
            Vector3Int finalGridPosition = finalGridEntity.Position.ToVector3Int();

            plantsByGridPosition[finalGridPosition] = plantGO;
            Debug.Log($"[PlantPlacementManager] Successfully planted '{runtimeState.template.templateName}' and registered it at final grid position {finalGridPosition}.");
            return true;
        }

        private Vector3 GetRandomizedPlantingPosition(Vector3 centerPosition)
        {
            if (spawnRadius <= 0f) return centerPosition;
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            return centerPosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        }
    }
}
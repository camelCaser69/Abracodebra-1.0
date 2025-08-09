using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Templates;

namespace Abracodabra.UI.Genes
{
    public class PlantPlacementManager : MonoBehaviour
    {
        public static PlantPlacementManager Instance { get; private set; }

        [SerializeField] private Transform plantParent;
        [SerializeField] private TileInteractionManager tileInteractionManager;
        [SerializeField] private NodeExecutor nodeExecutor;
        [SerializeField] private float spawnRadius = 0.25f;
        [SerializeField] private List<TileDefinition> invalidPlantingTiles = new List<TileDefinition>();

        private HashSet<TileDefinition> invalidTilesSet = new HashSet<TileDefinition>();
        private Dictionary<Vector3Int, GameObject> plantsByGridPosition = new Dictionary<Vector3Int, GameObject>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            RebuildInvalidTilesSet();
        }

        public void Initialize()
        {
            if (plantParent == null && EcosystemManager.Instance != null) plantParent = EcosystemManager.Instance.plantParent; // Corrected to plantParent
            if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance;
            if (nodeExecutor == null) nodeExecutor = FindFirstObjectByType<NodeExecutor>();
        }

        private void RebuildInvalidTilesSet()
        {
            invalidTilesSet = new HashSet<TileDefinition>(invalidPlantingTiles.Where(t => t != null));
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

        public bool TryPlantSeedFromInventory(InventoryBarItem seedItem, Vector3Int gridPosition, Vector3 worldPosition)
        {
            if (seedItem == null || seedItem.Type != InventoryBarItem.ItemType.Seed) return false;

            if (!seedItem.SeedTemplate.IsValid())
            {
                Debug.LogError($"Cannot plant '{seedItem.SeedTemplate.templateName}': The seed template configuration is invalid.", seedItem.SeedTemplate);
                return false;
            }

            if (IsPositionOccupied(gridPosition)) return false;

            TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
            if (!IsTileValidForPlanting(tileDef)) return false;

            if (nodeExecutor == null)
            {
                Debug.LogError("Cannot plant: NodeExecutor reference is missing in PlantPlacementManager.");
                return false;
            }

            Vector3 finalPlantingPosition = GetRandomizedPlantingPosition(worldPosition);
            SeedTemplate templateToPlant = seedItem.SeedTemplate;
            GameObject plantGO = nodeExecutor.SpawnPlantFromTemplate(templateToPlant, finalPlantingPosition, plantParent);

            if (plantGO == null) return false;

            // FIX: Re-snap the entity to the grid *after* applying the random offset to get its true final grid position.
            WegoSystem.GridPositionManager.Instance.SnapEntityToGrid(plantGO);
            var finalGridEntity = plantGO.GetComponent<WegoSystem.GridEntity>();
            Vector3Int finalGridPosition = finalGridEntity.Position.ToVector3Int();


            plantsByGridPosition[finalGridPosition] = plantGO;
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
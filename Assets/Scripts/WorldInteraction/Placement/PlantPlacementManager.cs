// REWORKED FILE: Assets/Scripts/WorldInteraction/Placement/PlantPlacementManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Abracodabra.Genes.Templates;

public class PlantPlacementManager : MonoBehaviour
{
    public static PlantPlacementManager Instance { get; private set; }

    [SerializeField] private Transform plantParent;
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private NodeExecutor nodeExecutor; // This is our "Plant Spawner" now
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
        if (plantParent == null && EcosystemManager.Instance != null) plantParent = EcosystemManager.Instance.plantParent;
        if (tileInteractionManager == null) tileInteractionManager = TileInteractionManager.Instance;
        if (nodeExecutor == null) nodeExecutor = FindObjectOfType<NodeExecutor>();
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

    // FIX: This method is now the primary way to plant. It takes an InventoryBarItem.
    public bool TryPlantSeedFromInventory(InventoryBarItem seedItem, Vector3Int gridPosition, Vector3 worldPosition)
    {
        if (seedItem == null || seedItem.Type != InventoryBarItem.ItemType.Seed) return false;

        if (IsPositionOccupied(gridPosition)) return false;

        TileDefinition tileDef = tileInteractionManager?.FindWhichTileDefinitionAt(gridPosition);
        if (!IsTileValidForPlanting(tileDef)) return false;
        
        if (nodeExecutor == null)
        {
             Debug.LogError("Cannot plant: NodeExecutor reference is missing in PlantPlacementManager.");
             return false;
        }

        SeedTemplate templateToPlant = seedItem.SeedTemplate;
        GameObject plantGO = nodeExecutor.SpawnPlantFromTemplate(templateToPlant, worldPosition, plantParent);

        if (plantGO == null) return false;

        plantsByGridPosition[gridPosition] = plantGO;
        return true;
    }
}
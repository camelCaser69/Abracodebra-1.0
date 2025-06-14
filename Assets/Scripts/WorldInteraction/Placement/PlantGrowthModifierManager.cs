using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class PlantGrowthModifierManager : MonoBehaviour, ITickUpdateable
{
    public static PlantGrowthModifierManager Instance { get; set; }

    [System.Serializable]
    public class TileGrowthModifier
    {
        public TileDefinition tileDefinition;
        [Tooltip("Multiplier for how fast the plant gains growth progress. >1 is faster, <1 is slower.")]
        public float growthSpeedMultiplier = 1.0f;
        [Tooltip("Multiplier for how much energy the plant recharges via photosynthesis. >1 is more, <1 is less.")]
        public float energyRechargeMultiplier = 1.0f;
    }

    [Header("Default Modifiers")]
    public float defaultGrowthSpeedMultiplier = 1.0f;
    public float defaultEnergyRechargeMultiplier = 1.0f;
    
    [Header("Tile-Specific Modifiers")]
    public List<TileGrowthModifier> tileModifiers = new List<TileGrowthModifier>();

    [Header("Dependencies & Debug")]
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private bool showTileChangeMessages = true;
    
    private Dictionary<TileDefinition, TileGrowthModifier> modifierLookup = new Dictionary<TileDefinition, TileGrowthModifier>();
    private Dictionary<PlantGrowth, TileDefinition> plantTiles = new Dictionary<PlantGrowth, TileDefinition>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildModifierLookup();
    }

    void Start()
    {
        if (tileInteractionManager == null)
        {
            tileInteractionManager = TileInteractionManager.Instance;
            if (tileInteractionManager == null && showDebugMessages)
            {
                Debug.LogWarning("PlantGrowthModifierManager: TileInteractionManager not found!");
            }
        }
        
        // Register this manager with the TickManager to receive tick updates
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else
        {
            Debug.LogError("[PlantGrowthModifierManager] TickManager not found! Modifiers will not update.");
        }
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }

    // This method is now called every game tick by the TickManager
    public void OnTickUpdate(int currentTick)
    {
        UpdateAllPlantTiles();
    }
    
    void UpdateAllPlantTiles()
    {
        if (tileInteractionManager == null) return;

        // Use a temporary list to avoid issues with modifying the dictionary while iterating
        List<PlantGrowth> plantsToCheck = new List<PlantGrowth>(plantTiles.Keys);

        foreach (PlantGrowth plant in plantsToCheck)
        {
            if (plant == null)
            {
                plantTiles.Remove(plant);
                continue;
            }

            Vector3Int gridPosition = tileInteractionManager.WorldToCell(plant.transform.position);
            TileDefinition currentTileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);

            // If the plant's tile has changed, update our record
            if (plantTiles.TryGetValue(plant, out TileDefinition previousTileDef) && currentTileDef != previousTileDef)
            {
                plantTiles[plant] = currentTileDef;

                if (showTileChangeMessages)
                {
                    string previousTileName = previousTileDef != null ? previousTileDef.displayName : "None";
                    string currentTileName = currentTileDef != null ? currentTileDef.displayName : "None";
                    Debug.Log($"Plant '{plant.name}' tile changed: {previousTileName} -> {currentTileName}");
                }
            }
        }
    }

    void BuildModifierLookup()
    {
        modifierLookup.Clear();
        foreach (var modifier in tileModifiers)
        {
            if (modifier.tileDefinition != null && !modifierLookup.ContainsKey(modifier.tileDefinition))
            {
                modifierLookup.Add(modifier.tileDefinition, modifier);
            }
        }
    }

    public void RegisterPlantTile(PlantGrowth plant, TileDefinition tileDef)
    {
        if (plant == null) return;
        plantTiles[plant] = tileDef;

        if (showDebugMessages)
        {
            string tileName = tileDef != null ? tileDef.displayName : "Unknown Tile";
            Debug.Log($"Registered plant {plant.name} on tile {tileName}");
        }
    }

    public void UnregisterPlant(PlantGrowth plant)
    {
        if (plant != null)
        {
            plantTiles.Remove(plant);
        }
    }

    public float GetGrowthSpeedMultiplier(PlantGrowth plant)
    {
        if (plant == null) return defaultGrowthSpeedMultiplier;

        if (!plantTiles.ContainsKey(plant))
        {
            RegisterNewPlant(plant); // Auto-register if not found
        }

        if (plantTiles.TryGetValue(plant, out TileDefinition tileDef) && tileDef != null)
        {
            if (modifierLookup.TryGetValue(tileDef, out TileGrowthModifier modifier))
            {
                return modifier.growthSpeedMultiplier;
            }
        }

        return defaultGrowthSpeedMultiplier;
    }

    public float GetEnergyRechargeMultiplier(PlantGrowth plant)
    {
        if (plant == null) return defaultEnergyRechargeMultiplier;

        if (!plantTiles.ContainsKey(plant))
        {
            RegisterNewPlant(plant); // Auto-register if not found
        }
        
        if (plantTiles.TryGetValue(plant, out TileDefinition tileDef) && tileDef != null)
        {
            if (modifierLookup.TryGetValue(tileDef, out TileGrowthModifier modifier))
            {
                return modifier.energyRechargeMultiplier;
            }
        }
        
        return defaultEnergyRechargeMultiplier;
    }
    
    private void RegisterNewPlant(PlantGrowth plant)
    {
        if (plant == null || tileInteractionManager == null) return;

        Vector3Int gridPosition = tileInteractionManager.WorldToCell(plant.transform.position);
        TileDefinition currentTileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
        plantTiles[plant] = currentTileDef;

        if (showDebugMessages)
        {
            string tileName = currentTileDef != null ? currentTileDef.displayName : "Unknown Tile";
            Debug.Log($"Auto-registered new plant {plant.name} on tile {tileName}");
        }
    }
    
    void OnValidate()
    {
        // Rebuild the lookup in the editor for immediate feedback
        BuildModifierLookup();
    }
}
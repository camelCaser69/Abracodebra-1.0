using System.Collections.Generic;
using UnityEngine;

public class PlantGrowthModifierManager : MonoBehaviour
{
    public static PlantGrowthModifierManager Instance { get; private set; }

    [System.Serializable]
    public class TileGrowthModifier
    {
        [Tooltip("The tile definition this modifier applies to")]
        public TileDefinition tileDefinition;

        [Tooltip("Multiplier for plant growth speed when on this tile (1.0 = normal)")]
        [Range(0.1f, 3.0f)]
        public float growthSpeedMultiplier = 1.0f;

        [Tooltip("Multiplier for energy recharge speed when on this tile (1.0 = normal)")]
        [Range(0.1f, 3.0f)]
        public float energyRechargeMultiplier = 1.0f;
    }

    [Header("Default Settings")]
    [Tooltip("Default multiplier for tiles that aren't specifically configured")]
    [Range(0.1f, 3.0f)]
    public float defaultGrowthSpeedMultiplier = 1.0f;

    [Tooltip("Default multiplier for energy recharge on tiles that aren't specifically configured")]
    [Range(0.1f, 3.0f)]
    public float defaultEnergyRechargeMultiplier = 1.0f;

    [Header("Tile Update Settings")]
    [Tooltip("How often (in seconds) to check if plants are on different tiles")]
    [Range(0.5f, 5.0f)]
    public float tileUpdateInterval = 1.0f;

    [Header("Tile Growth Modifiers")]
    [Tooltip("Define growth and energy recharge multipliers for specific tiles")]
    public List<TileGrowthModifier> tileModifiers = new List<TileGrowthModifier>();

    [Header("References")]
    [SerializeField] private TileInteractionManager tileInteractionManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private bool showTileChangeMessages = true;

    // Dictionary for faster lookup of modifiers by tile definition
    private Dictionary<TileDefinition, TileGrowthModifier> modifierLookup = new Dictionary<TileDefinition, TileGrowthModifier>();

    // Dictionary to track what tile each plant is on
    private Dictionary<PlantGrowth, TileDefinition> plantTiles = new Dictionary<PlantGrowth, TileDefinition>();
    
    // Timer for tile updates
    private float tileUpdateTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Build lookup dictionary for faster access
        BuildModifierLookup();
    }

    private void Start()
    {
        // Find TileInteractionManager if not assigned
        if (tileInteractionManager == null)
        {
            tileInteractionManager = TileInteractionManager.Instance;
            if (tileInteractionManager == null && showDebugMessages)
            {
                Debug.LogWarning("PlantGrowthModifierManager: TileInteractionManager not found!");
            }
        }
        
        // Start with a tile update
        tileUpdateTimer = 0f;
    }

    private void Update()
    {
        // Update timer
        tileUpdateTimer -= Time.deltaTime;
        
        // Check if it's time to update tiles
        if (tileUpdateTimer <= 0f)
        {
            UpdateAllPlantTiles();
            tileUpdateTimer = tileUpdateInterval;
        }
    }

    // Check all plants to see if their tiles have changed
    private void UpdateAllPlantTiles()
    {
        if (tileInteractionManager == null)
        {
            return;
        }
        
        // We need to copy the keys to avoid modifying the dictionary during iteration
        List<PlantGrowth> plantsToCheck = new List<PlantGrowth>(plantTiles.Keys);
        
        foreach (PlantGrowth plant in plantsToCheck)
        {
            if (plant == null)
            {
                // Plant has been destroyed, remove from dictionary
                plantTiles.Remove(plant);
                continue;
            }
            
            // Convert plant position to grid position
            Vector3Int gridPosition = tileInteractionManager.WorldToCell(plant.transform.position);
            
            // Get current tile definition at this position
            TileDefinition currentTileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
            
            // Get previously stored tile definition
            TileDefinition previousTileDef = plantTiles[plant];
            
            // Check if tile has changed
            if (currentTileDef != previousTileDef)
            {
                // Update stored tile
                plantTiles[plant] = currentTileDef;
                
                if (showTileChangeMessages)
                {
                    string previousTileName = previousTileDef != null ? previousTileDef.displayName : "None";
                    string currentTileName = currentTileDef != null ? currentTileDef.displayName : "None";
                    Debug.Log($"Plant tile changed: {previousTileName} -> {currentTileName}");
                }
            }
        }
    }

    private void BuildModifierLookup()
    {
        modifierLookup.Clear();
        foreach (var modifier in tileModifiers)
        {
            if (modifier.tileDefinition != null && !modifierLookup.ContainsKey(modifier.tileDefinition))
            {
                modifierLookup.Add(modifier.tileDefinition, modifier);
            }
        }

        if (showDebugMessages)
        {
            Debug.Log($"PlantGrowthModifierManager: Built lookup with {modifierLookup.Count} tile modifiers");
        }
    }

    // Call this when a plant is created to register its tile
    public void RegisterPlantTile(PlantGrowth plant, TileDefinition tileDef)
    {
        if (plant == null)
            return;

        plantTiles[plant] = tileDef;

        if (showDebugMessages)
        {
            string tileName = tileDef != null ? tileDef.displayName : "Unknown Tile";
            Debug.Log($"Registered plant {plant.name} on tile {tileName}");
        }
    }

    // Call this when a plant is destroyed to clean up
    public void UnregisterPlant(PlantGrowth plant)
    {
        if (plant == null)
            return;

        if (plantTiles.ContainsKey(plant))
        {
            plantTiles.Remove(plant);
        }
    }

    // Get growth speed multiplier for a plant based on its tile
    public float GetGrowthSpeedMultiplier(PlantGrowth plant)
    {
        if (plant == null)
            return defaultGrowthSpeedMultiplier;
        
        // If plant not in dictionary, register it with its current tile
        if (!plantTiles.ContainsKey(plant))
        {
            RegisterNewPlant(plant);
        }
        
        TileDefinition tileDef = plantTiles[plant];
        if (tileDef == null)
        {
            return defaultGrowthSpeedMultiplier;
        }

        if (modifierLookup.TryGetValue(tileDef, out TileGrowthModifier modifier))
        {
            return modifier.growthSpeedMultiplier;
        }

        return defaultGrowthSpeedMultiplier;
    }

    // Get energy recharge multiplier for a plant based on its tile
    public float GetEnergyRechargeMultiplier(PlantGrowth plant)
    {
        if (plant == null)
            return defaultEnergyRechargeMultiplier;
        
        // If plant not in dictionary, register it with its current tile
        if (!plantTiles.ContainsKey(plant))
        {
            RegisterNewPlant(plant);
        }
        
        TileDefinition tileDef = plantTiles[plant];
        if (tileDef == null)
        {
            return defaultEnergyRechargeMultiplier;
        }

        if (modifierLookup.TryGetValue(tileDef, out TileGrowthModifier modifier))
        {
            return modifier.energyRechargeMultiplier;
        }

        return defaultEnergyRechargeMultiplier;
    }
    
    // Helper method to register a new plant with its current tile
    private void RegisterNewPlant(PlantGrowth plant)
    {
        if (plant == null || tileInteractionManager == null)
            return;
            
        Vector3Int gridPosition = tileInteractionManager.WorldToCell(plant.transform.position);
        TileDefinition currentTileDef = tileInteractionManager.FindWhichTileDefinitionAt(gridPosition);
        
        plantTiles[plant] = currentTileDef;
        
        if (showDebugMessages)
        {
            string tileName = currentTileDef != null ? currentTileDef.displayName : "Unknown Tile";
            Debug.Log($"Auto-registered plant {plant.name} on tile {tileName}");
        }
    }

    // For editor support - rebuild lookup when modifiers change
    public void OnValidate()
    {
        BuildModifierLookup();
    }
}
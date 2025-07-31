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
        [Tooltip("Multiplier for growth progress per tick. 2.0 is twice as fast, 0.5 is half speed.")]
        public float growthSpeedMultiplier = 1.0f;
        [Tooltip("Multiplier for energy gained per tick. 2.0 is twice as much, 0.5 is half.")]
        public float energyRechargeMultiplier = 1.0f;
    }

    [Header("Default Modifiers")]
    public float defaultGrowthSpeedMultiplier = 1.0f;
    public float defaultEnergyRechargeMultiplier = 1.0f;

    [Header("Tile-Specific Modifiers")]
    public List<TileGrowthModifier> tileModifiers = new List<TileGrowthModifier>();

    [Header("Setup")]
    [SerializeField] private TileInteractionManager tileInteractionManager;
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private bool showTileChangeMessages = true;

    // --- CHANGE 1: The dictionary now uses a string (the tile's display name) as the key. ---
    private Dictionary<string, TileGrowthModifier> modifierLookup = new Dictionary<string, TileGrowthModifier>();
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

    public void Initialize()
    {
        if (tileInteractionManager == null)
        {
            tileInteractionManager = TileInteractionManager.Instance;
            if (tileInteractionManager == null && showDebugMessages)
            {
                Debug.LogWarning("PlantGrowthModifierManager: TileInteractionManager not found!");
            }
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        else
        {
            Debug.LogError("[PlantGrowthModifierManager] TickManager not found! Modifiers will not update.");
        }
    }

    // Assets/Scripts/WorldInteraction/Placement/PlantGrowthModifierManager.cs

    void OnDestroy()
    {
        // Safely get the instance once
        var tickManager = TickManager.Instance;
        if (tickManager != null)
        {
            tickManager.UnregisterTickUpdateable(this);
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        UpdateAllPlantTiles();
    }

    private void UpdateAllPlantTiles()
    {
        if (tileInteractionManager == null) return;

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

    private void BuildModifierLookup()
    {
        modifierLookup.Clear();
        foreach (var modifier in tileModifiers)
        {
            // --- CHANGE 2: Use the tile's display name for the lookup key. ---
            if (modifier.tileDefinition != null && !string.IsNullOrEmpty(modifier.tileDefinition.displayName) && !modifierLookup.ContainsKey(modifier.tileDefinition.displayName))
            {
                modifierLookup.Add(modifier.tileDefinition.displayName, modifier);
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
            // --- CHANGE 3: Perform the lookup using the tile's display name. ---
            if (modifierLookup.TryGetValue(tileDef.displayName, out TileGrowthModifier modifier))
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
            // --- CHANGE 4: Perform the lookup using the tile's display name. ---
            if (modifierLookup.TryGetValue(tileDef.displayName, out TileGrowthModifier modifier))
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
        // Rebuild the lookup in the editor when values change.
        BuildModifierLookup();
    }
}
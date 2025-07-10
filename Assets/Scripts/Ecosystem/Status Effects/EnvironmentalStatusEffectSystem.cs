// Assets/Scripts/Ecosystem/StatusEffects/EnvironmentalStatusEffectSystem.cs
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

// Make the system tick-based to handle continuous effects
public class EnvironmentalStatusEffectSystem : MonoBehaviour, ITickUpdateable
{
    public static EnvironmentalStatusEffectSystem Instance { get; private set; }

    [System.Serializable]
    public class TileStatusRule
    {
        public TileDefinition tile;
        public StatusEffect statusEffectToApply;
    }

    [Header("Environmental Rules")]
    [Tooltip("Rules for applying status effects when an entity is on a specific tile.")]
    public List<TileStatusRule> tileRules = new List<TileStatusRule>();

    private Dictionary<TileDefinition, StatusEffect> tileRuleLookup = new Dictionary<TileDefinition, StatusEffect>();
    private List<IStatusEffectable> allEffectableEntities = new List<IStatusEffectable>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Build a fast lookup dictionary from the list for performance
        BuildLookup();
    }

    void Start()
    {
        // Register this system with the TickManager
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Unregister from the TickManager
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
    }
    
    private void BuildLookup()
    {
        tileRuleLookup.Clear();
        foreach (var rule in tileRules)
        {
            if (rule.tile != null && rule.statusEffectToApply != null)
            {
                tileRuleLookup[rule.tile] = rule.statusEffectToApply;
            }
        }
    }

    // This method will be called on every game tick
    public void OnTickUpdate(int currentTick)
    {
        // Re-check all entities every tick for environmental effects
        RefreshAllEntityTileEffects();
    }
    
    private void RefreshAllEntityTileEffects()
    {
        // Find all animals and the player
        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        var players = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);

        // Clear the list and repopulate it
        allEffectableEntities.Clear();
        foreach (var animal in animals)
        {
            allEffectableEntities.Add(animal);
        }
        foreach (var player in players)
        {
            allEffectableEntities.Add(player);
        }

        // Now check each one
        foreach(var entity in allEffectableEntities)
        {
            CheckAndApplyEffects(entity);
        }
    }

    // This method is now called both on move AND every tick
    public void CheckAndApplyEffects(IStatusEffectable entity)
    {
        if (entity == null || TileInteractionManager.Instance == null) return;

        Component entityComponent = entity as Component;
        if (entityComponent == null || !entityComponent.gameObject.activeInHierarchy)
        {
            return;
        }
        
        // Get the entity's current position
        GridPosition currentPos = entity.GridEntity.Position;
        
        // Find the tile at that position
        TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(currentPos.ToVector3Int());

        if (currentTile == null) return;

        // Check if there's a rule for this tile in our lookup
        if (tileRuleLookup.TryGetValue(currentTile, out StatusEffect effectToApply))
        {
            // If a rule exists, apply the effect to the entity
            // This will refresh the duration if the effect is already present
            entity.StatusManager.ApplyStatusEffect(effectToApply);
        }
    }
}
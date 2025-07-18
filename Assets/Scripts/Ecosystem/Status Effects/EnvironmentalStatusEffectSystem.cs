// Assets/Scripts/Ecosystem/StatusEffects/EnvironmentalStatusEffectSystem.cs
using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

public class EnvironmentalStatusEffectSystem : MonoBehaviour, ITickUpdateable
{
    public static EnvironmentalStatusEffectSystem Instance { get; private set; }

    [System.Serializable]
    public class TileStatusRule
    {
        public TileDefinition tile;
        public List<StatusEffect> statusEffectsToApply;
    }

    // <<< NEW: A rule structure for tools
    [System.Serializable]
    public class ToolStatusRule
    {
        public ToolDefinition tool;
        public List<StatusEffect> statusEffectsToApply;
    }

    [Header("Environmental Rules")]
    [Tooltip("Rules for applying status effects when an entity is on a specific tile.")]
    public List<TileStatusRule> tileRules = new List<TileStatusRule>();

    [Tooltip("Rules for applying status effects when a tool is used on an entity's tile.")]
    public List<ToolStatusRule> toolRules = new List<ToolStatusRule>(); // <<< NEW

    private Dictionary<TileDefinition, List<StatusEffect>> tileRuleLookup = new Dictionary<TileDefinition, List<StatusEffect>>();
    private Dictionary<ToolDefinition, List<StatusEffect>> toolRuleLookup = new Dictionary<ToolDefinition, List<StatusEffect>>(); // <<< NEW
    private List<IStatusEffectable> allEffectableEntities = new List<IStatusEffectable>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildLookups();
    }

    void Start()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
        // <<< NEW: Subscribe to the player action event
        if (PlayerActionManager.Instance != null)
        {
            PlayerActionManager.Instance.OnActionExecuted += HandlePlayerAction;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterTickUpdateable(this);
        }
        // <<< NEW: Unsubscribe from the player action event
        if (PlayerActionManager.Instance != null)
        {
            PlayerActionManager.Instance.OnActionExecuted -= HandlePlayerAction;
        }
    }
    
    private void BuildLookups()
    {
        // Build tile lookup
        tileRuleLookup.Clear();
        foreach (var rule in tileRules)
        {
            if (rule.tile != null && rule.statusEffectsToApply != null && rule.statusEffectsToApply.Count > 0)
            {
                tileRuleLookup[rule.tile] = rule.statusEffectsToApply;
            }
        }
        
        // <<< NEW: Build tool lookup
        toolRuleLookup.Clear();
        foreach (var rule in toolRules)
        {
            if (rule.tool != null && rule.statusEffectsToApply != null && rule.statusEffectsToApply.Count > 0)
            {
                toolRuleLookup[rule.tool] = rule.statusEffectsToApply;
            }
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        RefreshAllEntityTileEffects();
    }
    
    // <<< NEW: This method listens for when the player uses a tool
    private void HandlePlayerAction(PlayerActionType actionType, object actionData)
    {
        if (actionType != PlayerActionType.UseTool) return;
        
        var toolData = actionData as PlayerActionManager.ToolActionData;
        if (toolData == null) return;
        
        // Check if there's a rule for the tool that was used
        if (toolRuleLookup.TryGetValue(toolData.Tool, out List<StatusEffect> effectsToApply))
        {
            // Find all entities on the tile that was targeted
            if (GridPositionManager.Instance == null) return;
            GridPosition gridPos = new GridPosition(toolData.GridPosition);
            HashSet<GridEntity> entitiesOnTile = GridPositionManager.Instance.GetEntitiesAt(gridPos);

            foreach(var entity in entitiesOnTile)
            {
                IStatusEffectable effectable = entity.GetComponent<IStatusEffectable>();
                if (effectable != null)
                {
                    // Apply all effects associated with this tool's rule
                    foreach(var effect in effectsToApply)
                    {
                        effectable.StatusManager.ApplyStatusEffect(effect);
                    }
                }
            }
        }
    }

    private void RefreshAllEntityTileEffects()
    {
        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        var players = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);

        allEffectableEntities.Clear();
        foreach (var animal in animals) allEffectableEntities.Add(animal);
        foreach (var player in players) allEffectableEntities.Add(player);

        foreach(var entity in allEffectableEntities)
        {
            CheckAndApplyTileEffects(entity);
        }
    }

    // Renamed for clarity
    public void CheckAndApplyTileEffects(IStatusEffectable entity)
    {
        if (entity == null || TileInteractionManager.Instance == null) return;

        Component entityComponent = entity as Component;
        if (entityComponent == null || !entityComponent.gameObject.activeInHierarchy) return;
        
        GridPosition currentPos = entity.GridEntity.Position;
        TileDefinition currentTile = TileInteractionManager.Instance.FindWhichTileDefinitionAt(currentPos.ToVector3Int());

        if (currentTile == null) return;

        if (tileRuleLookup.TryGetValue(currentTile, out List<StatusEffect> effectsToApply))
        {
            foreach (var effect in effectsToApply)
            {
                if (effect != null)
                {
                    entity.StatusManager.ApplyStatusEffect(effect);
                }
            }
        }
    }

    void OnValidate()
    {
        BuildLookups();
    }
}
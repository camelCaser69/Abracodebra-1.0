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
        public List<StatusEffect> statusEffectsToApply; // Changed from single to list
    }

    [Header("Environmental Rules")]
    [Tooltip("Rules for applying status effects when an entity is on a specific tile.")]
    public List<TileStatusRule> tileRules = new List<TileStatusRule>();

    private Dictionary<TileDefinition, List<StatusEffect>> tileRuleLookup = new Dictionary<TileDefinition, List<StatusEffect>>();
    private List<IStatusEffectable> allEffectableEntities = new List<IStatusEffectable>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildLookup();
    }

    void Start()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.RegisterTickUpdateable(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
            if (rule.tile != null && rule.statusEffectsToApply != null && rule.statusEffectsToApply.Count > 0)
            {
                tileRuleLookup[rule.tile] = rule.statusEffectsToApply;
            }
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        RefreshAllEntityTileEffects();
    }

    private void RefreshAllEntityTileEffects()
    {
        var animals = FindObjectsByType<AnimalController>(FindObjectsSortMode.None);
        var players = FindObjectsByType<GardenerController>(FindObjectsSortMode.None);

        allEffectableEntities.Clear();
        foreach (var animal in animals) allEffectableEntities.Add(animal);
        foreach (var player in players) allEffectableEntities.Add(player);

        foreach (var entity in allEffectableEntities)
        {
            CheckAndApplyEffects(entity);
        }
    }

    public void CheckAndApplyEffects(IStatusEffectable entity)
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

    // This method is useful for seeing rule changes in the editor without having to restart Play Mode.
    void OnValidate()
    {
        // This ensures that if you change the rules in the inspector while the editor is running,
        // the fast-lookup dictionary is rebuilt.
        BuildLookup();
    }
}
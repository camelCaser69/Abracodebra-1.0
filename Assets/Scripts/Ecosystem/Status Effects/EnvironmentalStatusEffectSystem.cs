using System.Collections.Generic;
using UnityEngine;
using WegoSystem;

namespace WegoSystem
{
    public class EnvironmentalStatusEffectSystem : MonoBehaviour, ITickUpdateable
    {
        public static EnvironmentalStatusEffectSystem Instance { get; set; }

        [System.Serializable]
        public class TileStatusRule
        {
            public TileDefinition tile;
            public List<StatusEffect> statusEffectsToApply;
        }

        [System.Serializable]
        public class ToolStatusRule
        {
            public ToolDefinition tool;
            public List<StatusEffect> statusEffectsToApply;
        }

        public List<TileStatusRule> tileRules = new List<TileStatusRule>();
        public List<ToolStatusRule> toolRules = new List<ToolStatusRule>();

        private readonly Dictionary<TileDefinition, List<StatusEffect>> tileRuleLookup = new Dictionary<TileDefinition, List<StatusEffect>>();
        private readonly Dictionary<ToolDefinition, List<StatusEffect>> toolRuleLookup = new Dictionary<ToolDefinition, List<StatusEffect>>();
        private readonly List<IStatusEffectable> allEffectableEntities = new List<IStatusEffectable>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildLookups();
        }

        public void Initialize()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.RegisterTickUpdateable(this);
            }
            if (PlayerActionManager.Instance != null)
            {
                PlayerActionManager.Instance.OnActionExecuted += HandlePlayerAction;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            var tickManager = TickManager.Instance;
            if (tickManager != null)
            {
                tickManager.UnregisterTickUpdateable(this);
            }

            if (PlayerActionManager.Instance != null)
            {
                PlayerActionManager.Instance.OnActionExecuted -= HandlePlayerAction;
            }
        }

        private void BuildLookups()
        {
            tileRuleLookup.Clear();
            foreach (var rule in tileRules)
            {
                if (rule.tile != null && rule.statusEffectsToApply != null && rule.statusEffectsToApply.Count > 0)
                {
                    tileRuleLookup[rule.tile] = rule.statusEffectsToApply;
                }
            }

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

        private void HandlePlayerAction(PlayerActionType actionType, object actionData)
        {
            if (actionType != PlayerActionType.UseTool) return;

            var toolData = actionData as PlayerActionManager.ToolActionData;
            if (toolData == null) return;

            if (toolRuleLookup.TryGetValue(toolData.Tool, out List<StatusEffect> effectsToApply))
            {
                if (GridPositionManager.Instance == null) return;
                GridPosition gridPos = new GridPosition(toolData.GridPosition);
                HashSet<GridEntity> entitiesOnTile = GridPositionManager.Instance.GetEntitiesAt(gridPos);

                foreach(var entity in entitiesOnTile)
                {
                    IStatusEffectable effectable = entity.GetComponent<IStatusEffectable>();
                    if (effectable != null)
                    {
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

        public void CheckAndApplyTileEffects(IStatusEffectable entity)
        {
            if (entity == null || entity.GridEntity == null || TileInteractionManager.Instance == null) return;

            Component entityComponent = entity as Component;
            if (entityComponent == null || !entityComponent.gameObject.activeInHierarchy) return;

            // --- THE ELEGANT, FOOLPROOF FIX ---
            // We directly use the entity's logical GridPosition, the same one used for movement.
            // This completely eliminates any ambiguity or floating-point errors from world space conversion.
            GridPosition currentPos = entity.GridEntity.Position;
            // --- END OF FIX ---

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
    }
}
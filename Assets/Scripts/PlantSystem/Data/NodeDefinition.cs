// Assets/Scripts/PlantSystem/Data/NodeDefinition.cs
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WegoSystem;

[CreateAssetMenu(fileName = "Node_", menuName = "Nodes/Node Definition")]
public class NodeDefinition : ScriptableObject, ITooltipDataProvider
{
    [Header("Gene Configuration")]
    [SerializeField]
    private GeneActivationType activationType = GeneActivationType.Passive;
    public GeneActivationType ActivationType => activationType;


    [Header("Display Properties")]
    public string displayName;
    [TextArea(3, 5)]
    public string description;

    public Sprite thumbnail;
    public Color thumbnailTintColor = Color.white;
    public Color backgroundColor = Color.gray;

    [Header("System Properties")]
    public GameObject nodeViewPrefab;
    public List<NodeEffectData> effects;

    #region Methods

    public List<NodeEffectData> CloneEffects()
    {
        var copy = new List<NodeEffectData>();

        if (effects == null)
        {
            Debug.LogWarning($"[NodeDefinition '{this.name}'] No effects to clone");
            return copy;
        }

        foreach (var originalEffect in effects)
        {
            if (originalEffect == null)
            {
                Debug.LogWarning($"[NodeDefinition '{this.name}'] Contains a null effect in its list. Skipping.");
                continue;
            }

            var newEffect = new NodeEffectData()
            {
                effectType = originalEffect.effectType,
                primaryValue = originalEffect.primaryValue,
                secondaryValue = originalEffect.secondaryValue,
                consumedOnTrigger = originalEffect.consumedOnTrigger,
            };

            // Deep copy seed data if it exists
            if (originalEffect.effectType == NodeEffectType.SeedSpawn && originalEffect.seedData != null)
            {
                newEffect.seedData = new SeedSpawnData
                {
                    growthSpeed = originalEffect.seedData.growthSpeed,
                    stemLengthMin = originalEffect.seedData.stemLengthMin,
                    stemLengthMax = originalEffect.seedData.stemLengthMax,
                    leafGap = originalEffect.seedData.leafGap,
                    leafPattern = originalEffect.seedData.leafPattern,
                    stemRandomness = originalEffect.seedData.stemRandomness,
                    energyStorage = originalEffect.seedData.energyStorage,
                    cooldown = originalEffect.seedData.cooldown,
                    castDelay = originalEffect.seedData.castDelay,
                    maxBerries = originalEffect.seedData.maxBerries
                };
            }
            copy.Add(newEffect);
        }
        return copy;
    }

    #endregion

    #region Tooltip Implementation

    public string GetTooltipTitle()
    {
        return displayName ?? "Unknown Node";
    }

    public string GetTooltipDescription()
    {
        return description ?? string.Empty;
    }

    public string GetTooltipDetails(object source = null)
    {
        var nodeData = source as NodeData;
        if (nodeData == null) return string.Empty;

        var sb = new StringBuilder();

        // --- Activation Type ---
        Color activationColor = GetColorForActivationType(ActivationType);
        string hexActivationColor = ColorUtility.ToHtmlStringRGB(activationColor);
        sb.Append($"<color=#{hexActivationColor}><b>Activation: {ActivationType}</b></color>\n");


        if (nodeData.effects != null && nodeData.effects.Any())
        {
            sb.Append("<b>Effects:</b>\n");
            const string effectPrefix = "• ";
            const string seedDetailPrefix = "    └ ";

            foreach (var effect in nodeData.effects)
            {
                if (effect == null) continue;

                sb.Append($"{effectPrefix}{GetEffectDisplayName(effect.effectType)}: ");
                sb.Append(GetEffectDescription(effect));
                sb.Append("\n");

                // Show details for passive seed effects
                if (effect.effectType == NodeEffectType.SeedSpawn && effect.seedData != null && ActivationType == GeneActivationType.Passive)
                {
                    var energyStorageEffect = new NodeEffectData { effectType = NodeEffectType.EnergyStorage, primaryValue = effect.seedData.energyStorage };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(energyStorageEffect.effectType)}: {GetEffectDescription(energyStorageEffect)}\n");

                    var growthSpeedEffect = new NodeEffectData { effectType = NodeEffectType.GrowthSpeed, primaryValue = effect.seedData.growthSpeed };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(growthSpeedEffect.effectType)}: {GetEffectDescription(growthSpeedEffect)}\n");

                    var stemLengthEffect = new NodeEffectData { effectType = NodeEffectType.StemLength, primaryValue = effect.seedData.stemLengthMin, secondaryValue = effect.seedData.stemLengthMax };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(stemLengthEffect.effectType)}: {GetEffectDescription(stemLengthEffect)}\n");

                    var leafGapEffect = new NodeEffectData { effectType = NodeEffectType.LeafGap, primaryValue = effect.seedData.leafGap };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(leafGapEffect.effectType)}: {GetEffectDescription(leafGapEffect)}\n");

                    var leafPatternEffect = new NodeEffectData { effectType = NodeEffectType.LeafPattern, primaryValue = effect.seedData.leafPattern };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(leafPatternEffect.effectType)}: {GetEffectDescription(leafPatternEffect)}\n");

                    var stemRandomnessEffect = new NodeEffectData { effectType = NodeEffectType.StemRandomness, primaryValue = effect.seedData.stemRandomness };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(stemRandomnessEffect.effectType)}: {GetEffectDescription(stemRandomnessEffect)}\n");

                    var cooldownEffect = new NodeEffectData { effectType = NodeEffectType.Cooldown, primaryValue = effect.seedData.cooldown };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(cooldownEffect.effectType)}: {GetEffectDescription(cooldownEffect)}\n");

                    var castDelayEffect = new NodeEffectData { effectType = NodeEffectType.CastDelay, primaryValue = effect.seedData.castDelay };
                    sb.Append($"{seedDetailPrefix}{GetEffectDisplayName(castDelayEffect.effectType)}: {GetEffectDescription(castDelayEffect)}\n");
                }
            }
        }

        if (nodeData.IsSeed())
        {
            if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append("\n");
            sb.Append("<b>Seed Sequence:</b> ");
            if (nodeData.storedSequence?.nodes != null && nodeData.storedSequence.nodes.Any())
            {
                sb.Append($"{nodeData.storedSequence.nodes.Count} nodes");
            }
            else
            {
                sb.Append("Empty");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private Color GetColorForActivationType(GeneActivationType type)
    {
        switch (type)
        {
            case GeneActivationType.Passive: return new Color(0.6f, 0.8f, 1f, 1f); // Light Blue
            case GeneActivationType.Active: return new Color(1f, 0.8f, 0.6f, 1f);  // Light Orange
            case GeneActivationType.Payload: return new Color(1f, 0.6f, 1f, 1f);  // Magenta
            default: return Color.white;
        }
    }

    private string GetEffectDisplayName(NodeEffectType type)
    {
        switch (type)
        {
            case NodeEffectType.EnergyStorage: return "Energy Storage";
            case NodeEffectType.EnergyPerTick: return "Energy/Tick";
            case NodeEffectType.EnergyCost: return "Energy Cost";
            case NodeEffectType.StemLength: return "Stem Length";
            case NodeEffectType.GrowthSpeed: return "Growth Speed";
            case NodeEffectType.LeafGap: return "Leaf Gap";
            case NodeEffectType.LeafPattern: return "Leaf Pattern";
            case NodeEffectType.StemRandomness: return "Stem Wobble";
            case NodeEffectType.Cooldown: return "Cooldown";
            case NodeEffectType.CastDelay: return "Cast Delay";
            case NodeEffectType.PoopAbsorption: return "Poop Absorption";
            case NodeEffectType.Damage: return "Damage";
            case NodeEffectType.GrowBerry: return "Berry Growth";
            case NodeEffectType.SeedSpawn: return "Seed";
            case NodeEffectType.ScentModifier: return "Scent";
            case NodeEffectType.TimerCast: return "Timer Cast";
            case NodeEffectType.ProximityCast: return "Proximity Cast";
            case NodeEffectType.EatCast: return "Eat Cast";
            case NodeEffectType.LeafLossCast: return "Leaf Loss Cast";
            case NodeEffectType.Nutritious: return "Nutritious";
            case NodeEffectType.Harvestable: return "Harvestable";
            default: return type.ToString();
        }
    }

    private string GetEffectDescription(NodeEffectData effect)
    {
        switch (effect.effectType)
        {
            case NodeEffectType.EnergyStorage:
                return $"+{effect.primaryValue:F0} max energy";

            case NodeEffectType.EnergyPerTick:
                return $"+{effect.primaryValue:F2} energy/tick";

            case NodeEffectType.EnergyCost:
                return $"{effect.primaryValue:F0} energy to activate";

            case NodeEffectType.StemLength:
                if (effect.secondaryValue > 0 && effect.secondaryValue != effect.primaryValue)
                {
                    return $"+{effect.primaryValue:F0} to +{effect.secondaryValue:F0} segments";
                }
                else
                {
                    return $"+{effect.primaryValue:F0} segments";
                }

            case NodeEffectType.GrowthSpeed:
                return effect.primaryValue <= 1
                    ? "Instant growth"
                    : $"{effect.primaryValue:F0} ticks/stage";

            case NodeEffectType.LeafGap:
                return effect.primaryValue == 0
                    ? "Leaves every segment"
                    : $"Leaves every {effect.primaryValue + 1:F0} segments";

            case NodeEffectType.LeafPattern:
                string patternName = GetLeafPatternName((int)effect.primaryValue);
                return $"Pattern: {patternName}";

            case NodeEffectType.StemRandomness:
                return $"{effect.primaryValue * 100:F0}% chance to wobble";

            case NodeEffectType.Cooldown:
                return $"{effect.primaryValue:F0} tick cooldown";

            case NodeEffectType.CastDelay:
                return effect.primaryValue > 0
                    ? $"{effect.primaryValue:F0} tick delay"
                    : "No delay";

            case NodeEffectType.PoopAbsorption:
                string result = "";
                if (effect.primaryValue > 0) result += $"{effect.primaryValue:F0} tile radius";
                if (effect.secondaryValue > 0)
                {
                    if (result.Length > 0) result += ", ";
                    result += $"+{effect.secondaryValue:F0} energy";
                }
                return result;

            case NodeEffectType.Damage:
                return $"Deals {effect.primaryValue:F0} damage"; // <<< MODIFIED

            case NodeEffectType.GrowBerry:
                return "Grows berries";

            case NodeEffectType.SeedSpawn:
                return "Defines a new seed type";

            case NodeEffectType.ScentModifier:
                string scentResult = "";
                if (effect.primaryValue != 0) scentResult += $"Radius {(effect.primaryValue >= 0 ? "+" : "")}{effect.primaryValue:F0}";
                if (effect.secondaryValue != 0)
                {
                    if (scentResult.Length > 0) scentResult += ", ";
                    scentResult += $"Strength {(effect.secondaryValue >= 0 ? "+" : "")}{effect.secondaryValue:F0}";
                }
                return scentResult;

            case NodeEffectType.TimerCast:
                return $"Triggers every {effect.primaryValue:F0} ticks";
            case NodeEffectType.ProximityCast:
                return $"Triggers within {effect.primaryValue:F0} tiles";
            case NodeEffectType.EatCast:
                return "Triggers when eaten";
            case NodeEffectType.LeafLossCast:
                return "Triggers when a leaf is lost";
            case NodeEffectType.Nutritious:
                return $"Restores {effect.primaryValue:F0} hunger";
            case NodeEffectType.Harvestable:
                return "Can be harvested";

            default:
                string defaultResult = $"{effect.primaryValue:F1}";
                if (effect.secondaryValue != 0)
                {
                    defaultResult += $" / {effect.secondaryValue:F1}";
                }
                return defaultResult;
        }
    }

    private string GetLeafPatternName(int pattern)
    {
        switch (pattern)
        {
            case 0: return "Symmetrical";
            case 1: return "Offset";
            case 2: return "Alternating";
            case 3: return "Spiral";
            case 4: return "Dense";
            default: return $"Pattern {pattern}";
        }
    }

    public string GetStatsAsString()
    {
        if (effects == null || effects.Count == 0) return "No effects";

        var result = new System.Text.StringBuilder();

        foreach (var effect in effects)
        {
            result.AppendLine($"{GetEffectDisplayName(effect.effectType)}: {GetEffectDescription(effect)}");
        }

        return result.ToString();
    }

    #endregion
}
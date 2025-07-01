using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WegoSystem;

/// <summary>
/// A ScriptableObject that defines the template for a "gene" or "node".
/// It contains all the base properties and effects that a node can have.
/// This acts as the source from which NodeData instances are created.
/// </summary>
[CreateAssetMenu(fileName = "Node_001_", menuName = "Nodes/Node Definition")]
public class NodeDefinition : ScriptableObject, ITooltipDataProvider
{
    #region Core Properties

    [Header("Display Information")]
    public string displayName;

    [TextArea(3, 10)]
    public string description;

    public Sprite thumbnail;
    public Color thumbnailTintColor = Color.white;
    public Color backgroundColor = Color.gray;

    [Header("Prefab & Effects")]
    public GameObject nodeViewPrefab;
    public List<NodeEffectData> effects;

    #endregion

    #region Data Handling

    /// <summary>
    /// Creates a deep copy of the effects list. This is crucial to ensure that
    /// each NodeData instance gets its own modifiable copy of the effects,
    /// especially for complex data types like SeedSpawnData.
    /// </summary>
    /// <returns>A new list containing deep copies of the NodeEffectData.</returns>
    // In NodeDefinition.cs, replace the CloneEffects method with this debug version:

public List<NodeEffectData> CloneEffects() {
    var copy = new List<NodeEffectData>();
    
    if (effects == null) {
        Debug.LogWarning($"[NodeDefinition '{this.name}'] No effects to clone");
        return copy;
    }
    
    Debug.Log($"[NodeDefinition '{this.name}'] Cloning {effects.Count} effects:");
    
    foreach (var originalEffect in effects) {
        if (originalEffect == null) {
            Debug.LogWarning($"[NodeDefinition '{this.name}'] Contains a null effect in its list.");
            continue;
        }
        
        var newEffect = new NodeEffectData() {
            effectType = originalEffect.effectType,
            primaryValue = originalEffect.primaryValue,
            secondaryValue = originalEffect.secondaryValue,
            isPassive = originalEffect.isPassive,
            scentDefinitionReference = originalEffect.scentDefinitionReference
        };
        
        // Clone seed data if present
        if (originalEffect.effectType == NodeEffectType.SeedSpawn && originalEffect.seedData != null) {
            newEffect.seedData = new SeedSpawnData {
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
        
        Debug.Log($"  - Cloned: {newEffect.effectType} (passive: {newEffect.isPassive}, primary: {newEffect.primaryValue}, secondary: {newEffect.secondaryValue})");
        
        copy.Add(newEffect);
    }
    
    Debug.Log($"[NodeDefinition '{this.name}'] Successfully cloned {copy.Count} effects");
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

        // Display all effects associated with this node.
        if (nodeData.effects != null && nodeData.effects.Any())
        {
            sb.Append("<b>Effects:</b>\n");
            var passiveEffectColor = new Color(0.6f, 0.8f, 1f, 1f); // Light Blue
            var activeEffectColor = new Color(1f, 0.8f, 0.6f, 1f);  // Light Orange
            const string effectPrefix = "• ";
            const string seedDetailPrefix = "    └ ";

            foreach (var effect in nodeData.effects)
            {
                if (effect == null) continue;
                Color effectColor = effect.isPassive ? passiveEffectColor : activeEffectColor;
                string hexColor = ColorUtility.ToHtmlStringRGB(effectColor);

                // Print the primary effect line
                sb.Append($"<color=#{hexColor}>{effectPrefix}{GetEffectDisplayName(effect.effectType)}: ");
                sb.Append(GetEffectDescription(effect));
                sb.Append("</color>\n");

                // If this is a passive seed, also display its packaged stats for convenience.
                if (effect.effectType == NodeEffectType.SeedSpawn && effect.seedData != null && effect.isPassive)
                {
                    // Create temporary effect data to reuse the description generation logic
                    var energyStorageEffect = new NodeEffectData { effectType = NodeEffectType.EnergyStorage, primaryValue = effect.seedData.energyStorage };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(energyStorageEffect.effectType)}: {GetEffectDescription(energyStorageEffect)}</color>\n");

                    var growthSpeedEffect = new NodeEffectData { effectType = NodeEffectType.GrowthSpeed, primaryValue = effect.seedData.growthSpeed };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(growthSpeedEffect.effectType)}: {GetEffectDescription(growthSpeedEffect)}</color>\n");

                    var stemLengthEffect = new NodeEffectData { effectType = NodeEffectType.StemLength, primaryValue = effect.seedData.stemLengthMin, secondaryValue = effect.seedData.stemLengthMax };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(stemLengthEffect.effectType)}: {GetEffectDescription(stemLengthEffect)}</color>\n");

                    var leafGapEffect = new NodeEffectData { effectType = NodeEffectType.LeafGap, primaryValue = effect.seedData.leafGap };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(leafGapEffect.effectType)}: {GetEffectDescription(leafGapEffect)}</color>\n");

                    var leafPatternEffect = new NodeEffectData { effectType = NodeEffectType.LeafPattern, primaryValue = effect.seedData.leafPattern };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(leafPatternEffect.effectType)}: {GetEffectDescription(leafPatternEffect)}</color>\n");

                    var stemRandomnessEffect = new NodeEffectData { effectType = NodeEffectType.StemRandomness, primaryValue = effect.seedData.stemRandomness };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(stemRandomnessEffect.effectType)}: {GetEffectDescription(stemRandomnessEffect)}</color>\n");

                    var cooldownEffect = new NodeEffectData { effectType = NodeEffectType.Cooldown, primaryValue = effect.seedData.cooldown };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(cooldownEffect.effectType)}: {GetEffectDescription(cooldownEffect)}</color>\n");

                    var castDelayEffect = new NodeEffectData { effectType = NodeEffectType.CastDelay, primaryValue = effect.seedData.castDelay };
                    sb.Append($"<color=#{hexColor}>{seedDetailPrefix}{GetEffectDisplayName(castDelayEffect.effectType)}: {GetEffectDescription(castDelayEffect)}</color>\n");
                }
            }
        }

        // If the node is a seed, display its sequence information.
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


    /// <summary>
    /// Gets a user-friendly name for a given effect type.
    /// </summary>
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
            default: return type.ToString();
        }
    }

    /// <summary>
    /// Generates a descriptive string for a specific node effect instance.
    /// </summary>
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
                return $"+{effect.primaryValue * 100:F0}% damage";

            case NodeEffectType.GrowBerry:
                return "Grows berries";

            case NodeEffectType.SeedSpawn:
                return effect.isPassive ? "Contains seed" : "Active seed";

            case NodeEffectType.ScentModifier:
                string scentResult = "";
                if (effect.primaryValue != 0) scentResult += $"Radius {(effect.primaryValue >= 0 ? "+" : "")}{effect.primaryValue:F0}";
                if (effect.secondaryValue != 0)
                {
                    if (scentResult.Length > 0) scentResult += ", ";
                    scentResult += $"Strength {(effect.secondaryValue >= 0 ? "+" : "")}{effect.secondaryValue:F0}";
                }
                if (effect.scentDefinitionReference != null)
                {
                    scentResult += $" ({effect.scentDefinitionReference.displayName})";
                }
                return scentResult;

            default:
                string defaultResult = $"{effect.primaryValue:F1}";
                if (effect.secondaryValue != 0)
                {
                    defaultResult += $" / {effect.secondaryValue:F1}";
                }
                return defaultResult;
        }
    }

    /// <summary>
    /// Gets a user-friendly name for a given leaf pattern index.
    /// </summary>
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

    #endregion

    #region Utility

    /// <summary>
    /// Gets a simple string representation of all effects. Useful for debugging.
    /// </summary>
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
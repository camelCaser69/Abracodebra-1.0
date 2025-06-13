using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using WegoSystem;

public class NodeDefinition : ScriptableObject, ITooltipDataProvider {
    public string displayName;
    public string description;
    public Sprite thumbnail;
    public Color thumbnailTintColor = Color.white;
    public Color backgroundColor = Color.gray;
    public GameObject nodeViewPrefab;
    public List<NodeEffectData> effects;

    public List<NodeEffectData> CloneEffects() {
        var copy = new List<NodeEffectData>();
        if (effects == null) {
            return copy;
        }

        foreach (var originalEffect in effects) {
            if (originalEffect == null) {
                Debug.LogWarning($"NodeDefinition '{this.name}' contains a null effect in its list.");
                continue;
            }

            var newEffect = new NodeEffectData() {
                effectType = originalEffect.effectType,
                primaryValue = originalEffect.primaryValue,
                secondaryValue = originalEffect.secondaryValue,
                isPassive = originalEffect.isPassive,
                scentDefinitionReference = originalEffect.scentDefinitionReference
            };
            copy.Add(newEffect);
        }
        return copy;
    }

    public string GetTooltipTitle() {
        return displayName ?? "Unknown Node";
    }

    public string GetTooltipDescription() {
        return description ?? string.Empty;
    }

    public string GetTooltipDetails(object source = null) {
        var nodeData = source as NodeData;
        if (nodeData == null) return string.Empty;

        var sb = new StringBuilder();

        if (nodeData.effects != null && nodeData.effects.Any()) {
            sb.Append("<b>Effects:</b>\n");
            var passiveEffectColor = new Color(0.6f, 0.8f, 1f, 1f); // Blue-ish for passive
            var activeEffectColor = new Color(1f, 0.8f, 0.6f, 1f);  // Orange-ish for active
            const string effectPrefix = "• ";

            foreach (var effect in nodeData.effects) {
                if (effect == null) continue;
                Color effectColor = effect.isPassive ? passiveEffectColor : activeEffectColor;
                string hexColor = ColorUtility.ToHtmlStringRGB(effectColor);
                sb.Append($"<color=#{hexColor}>{effectPrefix}{GetEffectDisplayName(effect.effectType)}: ");
                sb.Append(GetEffectDescription(effect));
                sb.Append("</color>\n");
            }
        }

        if (nodeData.IsSeed()) {
            if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append("\n");
            sb.Append("<b>Seed Sequence:</b> ");
            if (nodeData.storedSequence?.nodes != null && nodeData.storedSequence.nodes.Any()) {
                sb.Append($"{nodeData.storedSequence.nodes.Count} nodes");
            }
            else {
                sb.Append("Empty");
            }
        }

        return sb.ToString().TrimEnd();
    }

    string GetEffectDisplayName(NodeEffectType type) {
        switch (type) {
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

    string GetEffectDescription(NodeEffectData effect) {
        switch (effect.effectType) {
            // Energy & Resources
            case NodeEffectType.EnergyStorage:
                return $"+{effect.primaryValue:F0} max energy";

            case NodeEffectType.EnergyPerTick:
                return $"+{effect.primaryValue:F2} energy/tick";

            case NodeEffectType.EnergyCost:
                return $"{effect.primaryValue:F0} energy to activate";

            // Growth & Structure
            case NodeEffectType.StemLength:
                if (effect.secondaryValue > 0 && effect.secondaryValue != effect.primaryValue) {
                    return $"+{effect.primaryValue:F0} to +{effect.secondaryValue:F0} stem segments";
                } else {
                    return $"+{effect.primaryValue:F0} stem segments";
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

            // Timing
            case NodeEffectType.Cooldown:
                return $"{effect.primaryValue:F0} tick cooldown";

            case NodeEffectType.CastDelay:
                return effect.primaryValue > 0 
                    ? $"{effect.primaryValue:F0} tick delay" 
                    : "No delay";

            // Environmental
            case NodeEffectType.PoopAbsorption:
                string result = "";
                if (effect.primaryValue > 0) result += $"{effect.primaryValue:F0} tile radius";
                if (effect.secondaryValue > 0) {
                    if (result.Length > 0) result += ", ";
                    result += $"+{effect.secondaryValue:F0} energy";
                }
                return result;

            // Combat & Effects
            case NodeEffectType.Damage:
                return $"+{effect.primaryValue * 100:F0}% damage";

            // Spawning
            case NodeEffectType.GrowBerry:
                return "Grows berries";

            case NodeEffectType.SeedSpawn:
                return effect.isPassive ? "Contains seed" : "Active seed";

            // Modifiers
            case NodeEffectType.ScentModifier:
                string result = "";
                if (effect.primaryValue != 0) result += $"Radius {(effect.primaryValue >= 0 ? "+" : "")}{effect.primaryValue:F0}";
                if (effect.secondaryValue != 0) {
                    if (result.Length > 0) result += ", ";
                    result += $"Strength {(effect.secondaryValue >= 0 ? "+" : "")}{effect.secondaryValue:F0}";
                }
                if (effect.scentDefinitionReference != null) {
                    result += $" ({effect.scentDefinitionReference.displayName})";
                }
                return result;

            default:
                string defaultResult = $"{effect.primaryValue:F1}";
                if (effect.secondaryValue != 0) {
                    defaultResult += $" / {effect.secondaryValue:F1}";
                }
                return defaultResult;
        }
    }

    string GetLeafPatternName(int pattern) {
        switch (pattern) {
            case 0: return "Symmetrical";
            case 1: return "Offset";
            case 2: return "Alternating";
            case 3: return "Spiral";
            case 4: return "Dense";
            default: return $"Pattern {pattern}";
        }
    }

    public string GetStatsAsString() {
        if (effects == null || effects.Count == 0) return "No effects";

        var result = new System.Text.StringBuilder();

        foreach (var effect in effects) {
            result.AppendLine($"{GetEffectDisplayName(effect.effectType)}: {GetEffectDescription(effect)}");
        }

        return result.ToString();
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WegoSystem;

public class NodeDefinition : ScriptableObject, ITooltipDataProvider {

    public string displayName;
    [TextArea(3, 5)]
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
                sb.Append($"<color=#{hexColor}>{effectPrefix}{FormatEffectName(effect.effectType)}: ");
                sb.Append(FormatEffectValue(effect));
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

    private string FormatEffectName(NodeEffectType type)
    {
        switch (type)
        {
            case NodeEffectType.GrowthSpeed:
                return "Growth Rate";
            case NodeEffectType.Cooldown:
                return "Cooldown";
            default:
                return type.ToString();
        }
    }

    private string FormatEffectValue(NodeEffectData effect) {
        string result = effect.primaryValue.ToString("G3");
        bool hasSecondaryValue = false;

        switch (effect.effectType) {
            case NodeEffectType.GrowthSpeed:
                return $"{effect.primaryValue:F2} (stems/tick)";
            case NodeEffectType.Cooldown:
                return $"{Mathf.RoundToInt(effect.primaryValue)} Ticks";
            case NodeEffectType.ScentModifier:
            case NodeEffectType.PoopFertilizer:
                hasSecondaryValue = true;
                break;
            default:
                if (effect.secondaryValue != 0) hasSecondaryValue = true;
                break;
        }

        if (hasSecondaryValue) {
            result += $" / {effect.secondaryValue.ToString("G3")}";
        }

        if (effect.effectType == NodeEffectType.ScentModifier && effect.scentDefinitionReference != null) {
            result += $" ({effect.scentDefinitionReference.displayName})";
        }

        return result;
    }
}
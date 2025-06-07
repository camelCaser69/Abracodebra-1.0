using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// NOTE: The ITooltipDataProvider interface is defined in UniversalTooltipManager.cs

[CreateAssetMenu(fileName = "Node_001_", menuName = "Nodes/Node Definition")]
public class NodeDefinition : ScriptableObject, ITooltipDataProvider
{
    #region Fields

    [Header("Display")]
    public string displayName;
    [TextArea(3, 5)]
    public string description;
    public Sprite thumbnail;
    public Color thumbnailTintColor = Color.white;
    public Color backgroundColor = Color.gray;

    [Header("System")]
    public GameObject nodeViewPrefab;
    [Space]
    [Tooltip("The list of effects this node will have.")]
    public List<NodeEffectData> effects;

    #endregion

    public List<NodeEffectData> CloneEffects()
    {
        var copy = new List<NodeEffectData>();
        if (effects == null)
        {
            return copy;
        }

        foreach (var originalEffect in effects)
        {
            if (originalEffect == null)
            {
                Debug.LogWarning($"NodeDefinition '{this.name}' contains a null effect in its list.");
                continue;
            }

            var newEffect = new NodeEffectData()
            {
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

    #region ITooltipDataProvider Implementation

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

        // Build Effects Details
        if (nodeData.effects != null && nodeData.effects.Any())
        {
            sb.Append("<b>Effects:</b>\n");
            var passiveEffectColor = new Color(0.6f, 0.8f, 1f, 1f); // Blue-ish for passive
            var activeEffectColor = new Color(1f, 0.8f, 0.6f, 1f);  // Orange-ish for active
            const string effectPrefix = "• ";

            foreach (var effect in nodeData.effects)
            {
                if (effect == null) continue;
                Color effectColor = effect.isPassive ? passiveEffectColor : activeEffectColor;
                string hexColor = ColorUtility.ToHtmlStringRGB(effectColor);
                sb.Append($"<color=#{hexColor}>{effectPrefix}{effect.effectType}: ");
                sb.Append(FormatEffectValue(effect));
                sb.Append("</color>\n");
            }
        }

        // Build Seed Sequence Details
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
    /// Formats the primary and secondary values of a node effect for display.
    /// </summary>
    private string FormatEffectValue(NodeEffectData effect)
    {
        string result = effect.primaryValue.ToString("G3");
        bool hasSecondaryValue = false;
        
        // Define which effects explicitly use a secondary value.
        switch (effect.effectType)
        {
            case NodeEffectType.ScentModifier:
            case NodeEffectType.PoopFertilizer:
                hasSecondaryValue = true;
                break;
            default:
                // For other types, only show secondary value if it's not zero.
                if (effect.secondaryValue != 0) hasSecondaryValue = true;
                break;
        }

        if (hasSecondaryValue)
        {
            result += $" / {effect.secondaryValue.ToString("G3")}";
        }

        // Add scent name if applicable
        if (effect.effectType == NodeEffectType.ScentModifier && effect.scentDefinitionReference != null)
        {
            result += $" ({effect.scentDefinitionReference.displayName})";
        }

        return result;
    }

    #endregion
}
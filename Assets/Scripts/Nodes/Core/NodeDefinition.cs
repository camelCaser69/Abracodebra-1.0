using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    [TextArea]
    public string description;
    public Sprite thumbnail;
    [Tooltip("Tint color applied to the thumbnail image.")]
    public Color thumbnailTintColor = Color.white; // ADDED: Thumbnail tint
    [Tooltip("Background color for the Node View representation.")]
    public Color backgroundColor = Color.gray;

    [Header("Prefab & Effects")]
    [Tooltip("Optional: Specific NodeView prefab for this node type. If null, the default from NodeEditorGridController is used.")]
    public GameObject nodeViewPrefab;
    [Tooltip("List of effects this node applies.")]
    public List<NodeEffectData> effects;

    // Method to clone the effects list for NodeData.
    public List<NodeEffectData> CloneEffects()
    {
        List<NodeEffectData> copy = new List<NodeEffectData>();
        if (effects == null) return copy; // Handle null list

        foreach (var eff in effects)
        {
            NodeEffectData newEff = new NodeEffectData()
            {
                effectType = eff.effectType,
                primaryValue = eff.primaryValue,
                secondaryValue = eff.secondaryValue,
                isPassive = eff.isPassive  // Add this line to copy the flag
            };
            copy.Add(newEff);
        }
        return copy;
    }
}
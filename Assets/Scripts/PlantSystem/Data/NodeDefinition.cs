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
    public Color thumbnailTintColor = Color.white;
    [Tooltip("Background color for the Node View representation.")]
    public Color backgroundColor = Color.gray;

    [Header("Prefab & Effects")]
    [Tooltip("Optional: Specific NodeView prefab for this node type. If null, the default from NodeEditorGridController is used.")]
    public GameObject nodeViewPrefab;
    [Tooltip("List of effects this node applies. Configure these effects carefully.")]
    public List<NodeEffectData> effects; // This is the list configured in the Inspector

    /// <summary>
    /// Creates a deep copy of the effects list configured in this NodeDefinition asset.
    /// This ensures that runtime NodeData instances get their own copies of effects.
    /// </summary>
    /// <returns>A new list containing copies of the NodeEffectData.</returns>
    public List<NodeEffectData> CloneEffects()
    {
        List<NodeEffectData> copy = new List<NodeEffectData>();
        if (effects == null) {
            // Debug.LogWarning($"NodeDefinition '{this.name}' has a null effects list."); // Optional warning
            return copy; // Handle null list
        }

        foreach (var originalEffect in effects)
        {
            if (originalEffect == null) {
                 Debug.LogWarning($"NodeDefinition '{this.name}' contains a null effect in its list."); // Optional warning
                 continue; // Skip null effects
            }

            // Create a new instance and copy ALL relevant fields
            NodeEffectData newEffect = new NodeEffectData()
            {
                effectType = originalEffect.effectType,
                primaryValue = originalEffect.primaryValue,
                secondaryValue = originalEffect.secondaryValue,
                isPassive = originalEffect.isPassive,
                // --- FIXED: Copy the ScentDefinition reference ---
                scentDefinitionReference = originalEffect.scentDefinitionReference
                // --------------------------------------------------
            };
            copy.Add(newEffect);
        }
        return copy;
    }
}
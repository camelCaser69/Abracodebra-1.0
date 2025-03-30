using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;
    public Color backgroundColor = Color.gray;
    [TextArea]
    public string description;
    public Sprite thumbnail;

    // Add this field for the NodeView prefab.
    public GameObject nodeViewPrefab;

    // List of effects
    public List<NodeEffectData> effects;

    // Method to clone the effects list for NodeData.
    public List<NodeEffectData> CloneEffects()
    {
        List<NodeEffectData> copy = new List<NodeEffectData>();
        foreach (var eff in effects)
        {
            NodeEffectData newEff = new NodeEffectData()
            {
                effectType = eff.effectType,
                primaryValue = eff.primaryValue,
                secondaryValue = eff.secondaryValue
            };
            copy.Add(newEff);
        }
        return copy;
    }

}
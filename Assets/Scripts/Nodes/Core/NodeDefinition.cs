// Assets/Scripts/Nodes/Core/NodeDefinition.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;    
    public Color backgroundColor = Color.gray;
    [TextArea] public string description;

    // Predefined ports for this node
    public List<PortDefinition> ports;

    // The unified effects list
    public List<NodeEffectData> effects = new List<NodeEffectData>();
}
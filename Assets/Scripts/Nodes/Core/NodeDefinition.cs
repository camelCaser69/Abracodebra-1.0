// Assets/Scripts/Nodes/Core/NodeDefinition.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeDefinition", menuName = "Nodes/NodeDefinition")]
public class NodeDefinition : ScriptableObject
{
    public string displayName;            // e.g. "Mana Source"
    public Color backgroundColor = Color.gray;
    
    [TextArea]
    public string description;            // Optional: text for tooltips

    public List<PortDefinition> ports;    // Predefined ports for this node
}
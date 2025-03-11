// Assets/Scripts/Nodes/Core/NodeTypeDefinition.cs
// ScriptableObject for defining a node type's metadata (icon, color, default ports, etc.).

using UnityEngine;

[CreateAssetMenu(fileName = "NodeTypeDefinition", menuName = "Nodes/NodeTypeDefinition")]
public class NodeTypeDefinition : ScriptableObject
{
    public string nodeTypeName;   // e.g. "Damage", "Mana", "Cooldown"
    public Color nodeColor = Color.white;
    
    // Expand as needed, e.g., default input/output port templates.
}
// Assets/Scripts/Nodes/Core/NodeData.cs
// Core data class for a single node in the graph (no MonoBehaviour).

using System;
using System.Collections.Generic;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeType;    // e.g. "Mana", "Damage", "Cooldown"
    
    public List<NodePort> inputs;
    public List<NodePort> outputs;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
    }
}
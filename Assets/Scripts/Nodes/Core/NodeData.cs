// Assets/Scripts/Nodes/Core/NodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDefinitionId;   // e.g. GUID or name referencing the NodeDefinition
    public string nodeDisplayName;    // "Mana Source", "Damage", etc.

    public Vector2 editorPosition;

    // The actual ports used at runtime
    public List<NodePort> inputs;
    public List<NodePort> outputs;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
    }
}
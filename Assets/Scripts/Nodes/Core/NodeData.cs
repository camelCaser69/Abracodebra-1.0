// Assets/Scripts/Nodes/Core/NodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName; // e.g., "Mana Source"
    public Vector2 editorPosition; // Position in the editor

    public List<NodePort> inputs;
    public List<NodePort> outputs;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
    }
}

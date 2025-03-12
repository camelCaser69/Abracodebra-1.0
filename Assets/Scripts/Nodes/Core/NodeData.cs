// Assets/Scripts/Nodes/Core/NodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName;
    public Vector2 editorPosition;

    public List<NodePort> inputs;
    public List<NodePort> outputs;

    public List<NodeEffectData> effects;  // Copied from NodeDefinition

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
        effects = new List<NodeEffectData>();
    }
}
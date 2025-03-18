using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName;
    public Vector2 editorPosition;
    
    public Color backgroundColor = Color.gray;
    public string description = ""; // Ensure this field exists.

    public List<NodePort> inputs;
    public List<NodePort> outputs;
    public List<NodeEffectData> effects;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
        effects = new List<NodeEffectData>();
    }
}
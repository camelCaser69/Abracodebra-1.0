using System;
using System.Collections.Generic;
using UnityEngine;

// NodeData.cs
[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName;
    public Vector2 editorPosition;

    public List<NodePort> inputs;
    public List<NodePort> outputs;
    public List<NodeEffectData> effects;

    // REMOVED: public float manaStorageCapacity;
    // REMOVED: public float currentManaStorage;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
        effects = new List<NodeEffectData>();
    }
}

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
    public List<NodeEffectData> effects;

    // NEW: Unique mana storage fields for nodes that provide mana.
    public float manaStorageCapacity = 0f;   // Capacity value (if this node is a mana storage node)
    public float currentManaStorage = 0f;      // Current mana stored

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        inputs = new List<NodePort>();
        outputs = new List<NodePort>();
        effects = new List<NodeEffectData>();
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();

    // (Optional) To store the order if you need explicit ordering.
    public int orderIndex;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
    }
}
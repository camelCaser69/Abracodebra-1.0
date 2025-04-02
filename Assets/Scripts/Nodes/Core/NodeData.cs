using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public string nodeId;
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
    public int orderIndex;

    // Runtime flags
    [HideInInspector] // Don't show in NodeDefinition inspector, set at runtime
    public bool canBeDeleted = true; // <<< NEW FLAG (Defaults to true)

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        // Ensure default deletability on creation
        canBeDeleted = true;
    }
}
// FILE: Assets/Scripts/Nodes/Core/NodeData.cs
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
    public bool canBeDeleted = true;

    // NEW: For Seed items, this will store their internal node sequence
    [Tooltip("If this NodeData represents a Seed, this holds its internal sequence of nodes.")]
    public NodeGraph storedSequence = new NodeGraph(); // Initialize to avoid nulls

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        canBeDeleted = true;
        // Ensure storedSequence is initialized, which it is by its declaration.
    }

    // Helper to quickly check if this NodeData represents a seed
    public bool IsSeed()
    {
        if (effects == null) return false;
        foreach (var effect in effects)
        {
            if (effect != null && effect.effectType == NodeEffectType.SeedSpawn && effect.isPassive)
            {
                return true;
            }
        }
        return false;
    }
}
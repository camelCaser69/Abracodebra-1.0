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

    [HideInInspector]
    public bool canBeDeleted = true;

    [Tooltip("If this NodeData represents a Seed, this holds its internal sequence of nodes. Should be null for non-seed nodes or nodes within a sequence.")]
    [SerializeField] private NodeGraph _storedSequence;
    
    public NodeGraph storedSequence
    {
        get 
        {
            // Safety check: if this is not a seed, force null
            if (!IsSeed())
            {
                _storedSequence = null;
            }
            return _storedSequence;
        }
        set 
        {
            // Safety check: only allow setting if this is a seed
            if (!IsSeed() && value != null)
            {
                Debug.LogError($"Attempted to set storedSequence on non-seed node '{nodeDisplayName}'. Ignoring.");
                _storedSequence = null;
            }
            else
            {
                _storedSequence = value;
                // If we're setting a sequence, ensure all nodes in it are clean
                if (_storedSequence != null && _storedSequence.nodes != null)
                {
                    foreach (var node in _storedSequence.nodes)
                    {
                        if (node != null)
                        {
                            node._storedSequence = null; // Direct access to backing field
                        }
                    }
                }
            }
        }
    }

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        canBeDeleted = true;
        _storedSequence = null;
    }

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

    public void EnsureSeedSequenceInitialized()
    {
        if (IsSeed() && _storedSequence == null)
        {
            _storedSequence = new NodeGraph();
        }
        else if (!IsSeed())
        {
            _storedSequence = null;
        }
    }

    public void ClearStoredSequence()
    {
        _storedSequence = null;
    }
    
    public void ForceCleanNestedSequences(int depth = 0)
    {
        if (depth > 10)
        {
            Debug.LogError($"[NodeData] ForceCleanNestedSequences depth limit exceeded at node '{nodeDisplayName}'");
            return;
        }
        
        if (!IsSeed())
        {
            _storedSequence = null;
            return;
        }
        
        if (_storedSequence != null && _storedSequence.nodes != null)
        {
            foreach (var innerNode in _storedSequence.nodes)
            {
                if (innerNode == null) continue;
                
                // Direct access to backing field to force null
                innerNode._storedSequence = null;
                
                // Don't recurse - we've forcibly cleaned it
            }
        }
    }
}
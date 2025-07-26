// Assets/Scripts/PlantSystem/Data/NodeData.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class NodeData : ISerializationCallbackReceiver
{
    public string nodeId;
    public string definitionName; // The asset name of the NodeDefinition
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
    public int orderIndex;

    public bool canBeDeleted = true;

    // These fields are for managing sequences within seeds
    private NodeGraph _storedSequence;
    private bool _isPartOfSequence = false;

    public NodeData()
    {
        nodeId = Guid.NewGuid().ToString();
        _storedSequence = null;
        _isPartOfSequence = false;
    }

    /// <summary>
    /// Checks if this node contains a SeedSpawn effect, defining it as a potential seed.
    /// </summary>
    public bool HasSeedEffect()
    {
        // A node is a seed if it contains the SeedSpawn effect. The activation type is irrelevant for this check.
        // This resolves the 'IsPassive' compiler error.
        return effects != null &&
               effects.Any(e => e != null && e.effectType == NodeEffectType.SeedSpawn);
    }

    /// <summary>
    /// A NodeData represents a usable Seed if it has a seed effect AND is not part of another seed's sequence.
    /// </summary>
    public bool IsSeed()
    {
        return HasSeedEffect() && !_isPartOfSequence;
    }

    public NodeGraph storedSequence
    {
        get
        {
            return IsSeed() ? _storedSequence : null;
        }
        set
        {
            if (!HasSeedEffect() || _isPartOfSequence)
            {
                _storedSequence = null;
                return;
            }

            _storedSequence = value;

            if (_storedSequence?.nodes != null)
            {
                foreach (var node in _storedSequence.nodes.Where(n => n != null))
                {
                    node._isPartOfSequence = true;
                    node._storedSequence = null; // Nested sequences are not allowed
                }
            }
        }
    }

    public void SetPartOfSequence(bool isPartOfSequence)
    {
        _isPartOfSequence = isPartOfSequence;

        if (isPartOfSequence)
        {
            _storedSequence = null;
        }
    }

    public void EnsureSeedSequenceInitialized()
    {
        if (IsSeed() && _storedSequence == null)
        {
            _storedSequence = new NodeGraph { nodes = new List<NodeData>() };
        }
    }

    public void ClearStoredSequence()
    {
        _storedSequence = null;
    }

    #region Serialization Callbacks

    public void OnBeforeSerialize()
    {
        // To prevent serialization loops or invalid data, ensure a node within a sequence cannot also store a sequence.
        if (_isPartOfSequence)
        {
            _storedSequence = null;
        }
    }

    public void OnAfterDeserialize()
    {
        // By default, an item is not part of a sequence until it's explicitly added to one.
        _isPartOfSequence = false;

        // If this item isn't a seed, it can't have a stored sequence.
        if (!HasSeedEffect())
        {
            _storedSequence = null;
        }
    }

    #endregion
}
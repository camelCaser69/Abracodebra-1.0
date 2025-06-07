using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NodeData : ISerializationCallbackReceiver
{
    public string nodeId;
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
    public int orderIndex;

    [HideInInspector] public bool canBeDeleted = true;

    [NonSerialized] private bool _isContainedInSequence = false;
    public bool IsContainedInSequence => _isContainedInSequence;

    [NonSerialized] private NodeGraph _storedSequence;

    public NodeGraph storedSequence
    {
        get
        {
            if (_isContainedInSequence || !IsPotentialSeedContainer())
            {
                if (_storedSequence != null) _storedSequence = null;
            }
            return _storedSequence;
        }
        set
        {
            if (_isContainedInSequence || (!IsPotentialSeedContainer() && value != null))
            {
                _storedSequence = null;      // Disallow illegal assignments
            }
            else
            {
                _storedSequence = value;
                if (_storedSequence?.nodes != null)
                {
                    foreach (var node in _storedSequence.nodes.Where(n => n != null))
                    {
                        node.SetContainedInSequence(true);
                        node._storedSequence = null;
                    }
                }
            }
        }
    }

    public NodeData()
    {
        nodeId             = Guid.NewGuid().ToString();
        _storedSequence    = null;
        _isContainedInSequence = false;
    }

    public bool IsPotentialSeedContainer() =>
        effects != null &&
        effects.Any(e => e != null &&
                        e.effectType == NodeEffectType.SeedSpawn &&
                        e.isPassive);

    public bool IsSeed() => IsPotentialSeedContainer() && !_isContainedInSequence;

    public void SetContainedInSequence(bool isContained)
    {
        _isContainedInSequence = isContained;
        if (isContained) _storedSequence = null;
    }

    public void EnsureSeedSequenceInitialized()
    {
        if (IsPotentialSeedContainer() && !_isContainedInSequence && _storedSequence == null)
        {
            _storedSequence = new NodeGraph { nodes = new List<NodeData>() };
        }
        else if (!IsPotentialSeedContainer() || _isContainedInSequence)
        {
            _storedSequence = null;
        }
    }

    public void ClearStoredSequence() => _storedSequence = null;

    public void OnBeforeSerialize()
    {
        if (_isContainedInSequence || !IsPotentialSeedContainer())
        {
            _storedSequence = null;
        }
    }

    public void OnAfterDeserialize()
    {
        _isContainedInSequence = false;
        if (!IsPotentialSeedContainer())
        {
            _storedSequence = null;
        }
    }
}
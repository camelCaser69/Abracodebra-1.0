﻿// FILE: Assets/Scripts/Nodes/Core/NodeData.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class NodeData : ISerializationCallbackReceiver
{
    // ──────────────────────────────────────
    // Public serialised fields
    // ──────────────────────────────────────
    public string nodeId;
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
    public int orderIndex;

    [HideInInspector] public bool canBeDeleted = true;

    // ──────────────────────────────────────
    // Internal state flags
    // ──────────────────────────────────────
    [NonSerialized] private bool _isContainedInSequence = false;
    public bool IsContainedInSequence => _isContainedInSequence;

    // ──────────────────────────────────────
    // RUNTIME-ONLY reference -- **NOT** serialised
    // ──────────────────────────────────────
    [NonSerialized] private NodeGraph _storedSequence;

    /// <summary>Access to the runtime sequence, with safety guards.</summary>
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
                // Ensure all inner nodes know they’re inside a sequence
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

    // ──────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────
    public NodeData()
    {
        nodeId             = Guid.NewGuid().ToString();
        _storedSequence    = null;
        _isContainedInSequence = false;
    }

    // ──────────────────────────────────────
    // Seed helpers
    // ──────────────────────────────────────
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

    // ──────────────────────────────────────
    // Serialization guards
    // ──────────────────────────────────────
    public void CleanForSerialization(int depth = 0, string logPrefix = "CLEAN_SERIALIZE")
    {
        if (depth > 7)
        {
            Debug.LogError($"{logPrefix} (Depth {depth}) Node '{nodeDisplayName ?? nodeId}': " +
                           "Serialization clean depth limit. Forcing _storedSequence to null.");
            _storedSequence = null;
            return;
        }

        if (_isContainedInSequence || !IsPotentialSeedContainer())
        {
            if (_storedSequence != null) _storedSequence = null;
            return;
        }

        if (_storedSequence?.nodes != null)
        {
            foreach (var inner in _storedSequence.nodes.Where(n => n != null))
            {
                inner.SetContainedInSequence(true);
                inner.CleanForSerialization(depth + 1, $"{logPrefix} Inner");
            }
        }
        else if (_storedSequence == null)
        {
            _storedSequence = new NodeGraph { nodes = new List<NodeData>() };
        }
    }

    // Unity callback -- before snapshot
    public void OnBeforeSerialize()
    {
        CleanForSerialization(0, $"OBS ({nodeDisplayName ?? nodeId})");
        // No need to null _storedSequence here; [NonSerialized] already keeps it out of the snapshot.
    }

    // Unity callback -- after load
    public void OnAfterDeserialize()
    {
        _isContainedInSequence = false;  // Always start fresh
        // _storedSequence is already null (not serialised), but we run the cleaner to update flags
        CleanForSerialization(0, $"OADS ({nodeDisplayName ?? nodeId})");
    }
}

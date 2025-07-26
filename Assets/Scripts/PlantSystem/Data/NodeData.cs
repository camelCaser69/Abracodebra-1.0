// Assets/Scripts/PlantSystem/Data/NodeData.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData : ISerializationCallbackReceiver {
    public string nodeId;
    public string definitionName; // The asset name of the NodeDefinition
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
    public int orderIndex;
    
    public bool canBeDeleted = true;
    
    // IMPORTANT: Do NOT mark these with [SerializeField] to avoid circular references
    [NonSerialized] private NodeGraph _storedSequence;
    [NonSerialized] private bool _isPartOfSequence = false;
    
    // For serialization, we'll store the sequence data separately
    [SerializeField] private List<NodeData> _serializedSequenceNodes;
    
    public NodeData() {
        nodeId = Guid.NewGuid().ToString();
        _storedSequence = null;
        _isPartOfSequence = false;
    }
    
    public bool HasSeedEffect() {
        return effects != null && 
               effects.Any(e => e != null && e.effectType == NodeEffectType.SeedSpawn);
    }
    
    public bool IsSeed() {
        return HasSeedEffect() && !_isPartOfSequence;
    }
    
    public NodeGraph storedSequence {
        get {
            return IsSeed() ? _storedSequence : null;
        }
        set {
            if (!HasSeedEffect() || _isPartOfSequence) {
                _storedSequence = null;
                return;
            }
            
            _storedSequence = value;
            
            if (_storedSequence?.nodes != null) {
                foreach (var node in _storedSequence.nodes.Where(n => n != null)) {
                    node._isPartOfSequence = true;
                    node._storedSequence = null; // Nested sequences are not allowed
                }
            }
        }
    }
    
    public void SetPartOfSequence(bool isPartOfSequence) {
        _isPartOfSequence = isPartOfSequence;
        
        if (isPartOfSequence) {
            _storedSequence = null;
        }
    }
    
    public void EnsureSeedSequenceInitialized() {
        if (IsSeed() && _storedSequence == null) {
            _storedSequence = new NodeGraph { nodes = new List<NodeData>() };
        }
    }
    
    public void ClearStoredSequence() {
        _storedSequence = null;
    }
    
    // ISerializationCallbackReceiver implementation to handle serialization properly
    public void OnBeforeSerialize() {
        // If this is a seed with a sequence, serialize just the node data
        if (IsSeed() && _storedSequence?.nodes != null) {
            _serializedSequenceNodes = new List<NodeData>();
            foreach (var node in _storedSequence.nodes) {
                if (node != null) {
                    // Create a shallow copy to avoid circular references
                    var copy = new NodeData {
                        nodeId = node.nodeId,
                        definitionName = node.definitionName,
                        nodeDisplayName = node.nodeDisplayName,
                        effects = node.effects, // This is safe as effects don't reference NodeData
                        orderIndex = node.orderIndex,
                        canBeDeleted = node.canBeDeleted
                    };
                    _serializedSequenceNodes.Add(copy);
                }
            }
        }
        else {
            _serializedSequenceNodes = null;
        }
    }
    
    public void OnAfterDeserialize() {
        _isPartOfSequence = false;
        
        // Reconstruct the sequence from serialized data
        if (_serializedSequenceNodes != null && _serializedSequenceNodes.Count > 0 && HasSeedEffect()) {
            _storedSequence = new NodeGraph { nodes = new List<NodeData>() };
            foreach (var serializedNode in _serializedSequenceNodes) {
                if (serializedNode != null) {
                    serializedNode._isPartOfSequence = true;
                    _storedSequence.nodes.Add(serializedNode);
                }
            }
            _serializedSequenceNodes = null; // Clear after reconstruction
        }
        else {
            _storedSequence = null;
        }
    }
}
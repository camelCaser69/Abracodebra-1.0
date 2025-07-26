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
    
    // Sequence storage - only seeds can store sequences
    [NonSerialized] 
    private NodeGraph _storedSequence;
    
    [NonSerialized] 
    private bool _isPartOfSequence = false;
    
    // Constructor
    public NodeData() {
        nodeId = Guid.NewGuid().ToString();
        _storedSequence = null;
        _isPartOfSequence = false;
    }
    
    // ====== SEED DETECTION ======
    // A seed is a node that:
    // 1. Has a passive SeedSpawn effect
    // 2. Is NOT part of another sequence
    
    /// <summary>
    /// Checks if this node has the potential to be a seed (has passive SeedSpawn effect)
    /// </summary>
    public bool HasSeedEffect() {
        return effects != null && 
               effects.Any(e => e != null && 
                          e.effectType == NodeEffectType.SeedSpawn && 
                          e.isPassive);
    }
    
    /// <summary>
    /// Checks if this is actually a seed (has seed effect AND is not part of a sequence)
    /// </summary>
    public bool IsSeed() {
        return HasSeedEffect() && !_isPartOfSequence;
    }
    
    // ====== SEQUENCE MANAGEMENT ======
    
    /// <summary>
    /// Gets or sets the stored sequence. Only seeds can store sequences.
    /// </summary>
    public NodeGraph storedSequence {
        get {
            // Only return sequence if this is a seed
            return IsSeed() ? _storedSequence : null;
        }
        set {
            // Only seeds can store sequences
            if (!HasSeedEffect() || _isPartOfSequence) {
                _storedSequence = null;
                return;
            }
            
            _storedSequence = value;
            
            // Mark all nodes in the sequence as "part of sequence"
            if (_storedSequence?.nodes != null) {
                foreach (var node in _storedSequence.nodes.Where(n => n != null)) {
                    node._isPartOfSequence = true;
                    node._storedSequence = null; // Nested sequences not allowed
                }
            }
        }
    }
    
    /// <summary>
    /// Marks this node as being part of a sequence (or not)
    /// </summary>
    public void SetPartOfSequence(bool isPartOfSequence) {
        _isPartOfSequence = isPartOfSequence;
        
        // If node becomes part of a sequence, it can't have its own sequence
        if (isPartOfSequence) {
            _storedSequence = null;
        }
    }
    
    /// <summary>
    /// Ensures a seed has an initialized sequence container
    /// </summary>
    public void EnsureSeedSequenceInitialized() {
        if (IsSeed() && _storedSequence == null) {
            _storedSequence = new NodeGraph { nodes = new List<NodeData>() };
        }
    }
    
    /// <summary>
    /// Clears the stored sequence
    /// </summary>
    public void ClearStoredSequence() {
        _storedSequence = null;
    }
    
    // ====== SERIALIZATION ======
    // Clean up invalid states during serialization
    
    public void OnBeforeSerialize() {
        // Nodes that are part of sequences can't have their own sequences
        if (_isPartOfSequence) {
            _storedSequence = null;
        }
    }
    
    public void OnAfterDeserialize() {
        // Reset runtime state
        _isPartOfSequence = false;
        
        // Non-seeds can't have sequences
        if (!HasSeedEffect()) {
            _storedSequence = null;
        }
    }
}
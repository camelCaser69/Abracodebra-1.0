// Assets/Scripts/PlantSystem/Data/NodeData.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Assets/Scripts/PlantSystem/Data/NodeData.cs

public class NodeData : ISerializationCallbackReceiver {
    public string nodeId;
    public string definitionName; // The asset name of the NodeDefinition
    public string nodeDisplayName;
    public List<NodeEffectData> effects = new List<NodeEffectData>();
    public int orderIndex;
    
    public bool canBeDeleted = true;
    
    [NonSerialized] NodeGraph _storedSequence;
    [NonSerialized] bool _isPartOfSequence = false;
    
    [SerializeField] List<NodeData> _serializedSequenceNodes;
    
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
    
    public void OnBeforeSerialize() {
        if (IsSeed() && _storedSequence?.nodes != null) {
            _serializedSequenceNodes = new List<NodeData>();
            foreach (var node in _storedSequence.nodes) {
                if (node != null) {
                    var copy = new NodeData {
                        nodeId = node.nodeId,
                        definitionName = node.definitionName,
                        nodeDisplayName = node.nodeDisplayName,
                        effects = CloneEffectsListForSerialization(node.effects), // Deep clone effects
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
    
    // Helper method to deep clone effects list for serialization
    private static List<NodeEffectData> CloneEffectsListForSerialization(List<NodeEffectData> originalEffects) {
        if (originalEffects == null) return new List<NodeEffectData>();
        
        var clonedList = new List<NodeEffectData>();
        
        foreach (var effect in originalEffects) {
            if (effect == null) continue;
            
            var clonedEffect = new NodeEffectData {
                effectType = effect.effectType,
                primaryValue = effect.primaryValue,
                secondaryValue = effect.secondaryValue,
                consumedOnTrigger = effect.consumedOnTrigger,
            };
            
            // Deep clone seedData if present, but EXCLUDE any nested NodeData references
            if (effect.effectType == NodeEffectType.SeedSpawn && effect.seedData != null) {
                clonedEffect.seedData = new SeedSpawnData {
                    growthSpeed = effect.seedData.growthSpeed,
                    stemLengthMin = effect.seedData.stemLengthMin,
                    stemLengthMax = effect.seedData.stemLengthMax,
                    leafGap = effect.seedData.leafGap,
                    leafPattern = effect.seedData.leafPattern,
                    stemRandomness = effect.seedData.stemRandomness,
                    energyStorage = effect.seedData.energyStorage,
                    cooldown = effect.seedData.cooldown,
                    castDelay = effect.seedData.castDelay,
                    maxBerries = effect.seedData.maxBerries
                };
            }
            
            clonedList.Add(clonedEffect);
        }
        
        return clonedList;
    }
}
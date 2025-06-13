using System;
using UnityEngine;

[Serializable]
public class NodeEffectData {
    public NodeEffectType effectType;
    
    [Tooltip("Primary value - meaning depends on effect type")]
    public float primaryValue;
    
    [Tooltip("Secondary value - meaning depends on effect type")]
    public float secondaryValue;
    
    public bool isPassive = false;
    
    [Tooltip("For ScentModifier effects only")]
    public ScentDefinition scentDefinitionReference;
    
    // Helper methods for clarity
    public int GetPrimaryValueAsInt() => Mathf.RoundToInt(primaryValue);
    public int GetSecondaryValueAsInt() => Mathf.RoundToInt(secondaryValue);
    
    // Validation
    public void ValidateForTicks() {
        switch (effectType) {
            case NodeEffectType.GrowthSpeed:
            case NodeEffectType.Cooldown:
            case NodeEffectType.CastDelay:
                // These should be integers
                primaryValue = Mathf.Max(1, Mathf.RoundToInt(primaryValue));
                break;
                
            case NodeEffectType.PoopAbsorption:
                // Radius in tiles (integer)
                primaryValue = Mathf.Max(0, Mathf.RoundToInt(primaryValue));
                break;
                
            case NodeEffectType.EnergyPerTick:
                // Can be fractional
                primaryValue = Mathf.Max(0f, primaryValue);
                break;
        }
    }
}
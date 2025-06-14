using System;
using UnityEngine;

[Serializable]
public class NodeEffectData {
    public NodeEffectType effectType;
    public float primaryValue;
    public float secondaryValue;
    public bool isPassive = false;
    public ScentDefinition scentDefinitionReference;
    
    // New field for comprehensive seed data
    public SeedSpawnData seedData;

    public int GetPrimaryValueAsInt() => Mathf.RoundToInt(primaryValue);
    public int GetSecondaryValueAsInt() => Mathf.RoundToInt(secondaryValue);

    public void ValidateForTicks() {
        switch (effectType) {
            case NodeEffectType.GrowthSpeed:
            case NodeEffectType.Cooldown:
            case NodeEffectType.CastDelay:
                primaryValue = Mathf.Max(1, Mathf.RoundToInt(primaryValue));
                break;

            case NodeEffectType.PoopAbsorption:
                primaryValue = Mathf.Max(0, Mathf.RoundToInt(primaryValue));
                secondaryValue = Mathf.Max(0f, secondaryValue);
                break;

            case NodeEffectType.EnergyPerTick:
            case NodeEffectType.EnergyStorage:
            case NodeEffectType.EnergyCost:
                primaryValue = Mathf.Max(0f, primaryValue);
                break;
                
            case NodeEffectType.SeedSpawn:
                // Validate seed data if present
                if (seedData != null) {
                    seedData.growthSpeed = Mathf.Max(1, seedData.growthSpeed);
                    seedData.stemLengthMin = Mathf.Max(1, seedData.stemLengthMin);
                    seedData.stemLengthMax = Mathf.Max(seedData.stemLengthMin, seedData.stemLengthMax);
                    seedData.leafGap = Mathf.Max(0, seedData.leafGap);
                    seedData.leafPattern = Mathf.Clamp(seedData.leafPattern, 0, 4);
                    seedData.stemRandomness = Mathf.Clamp01(seedData.stemRandomness);
                    seedData.energyStorage = Mathf.Max(1f, seedData.energyStorage);
                    seedData.cooldown = Mathf.Max(1, seedData.cooldown);
                    seedData.castDelay = Mathf.Max(0, seedData.castDelay);
                }
                break;
        }
    }
}
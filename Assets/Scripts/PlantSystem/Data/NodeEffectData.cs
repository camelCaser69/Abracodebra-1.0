using System;
using UnityEngine;
using WegoSystem;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;
    public float primaryValue;
    public float secondaryValue;
    public bool isPassive = false;
    public bool consumedOnTrigger = false; // <<< NEW: Determines if a trigger is single-use
    public ScentDefinition scentDefinitionReference;
    public SeedSpawnData seedData;

    public int GetPrimaryValueAsInt() => Mathf.RoundToInt(primaryValue);
    public int GetSecondaryValueAsInt() => Mathf.RoundToInt(secondaryValue);

    public void ValidateForTicks()
    {
        if (effectType == NodeEffectType.EnergyPerTick && TickManager.Instance?.Config != null)
        {
            float ticksPerSecond = TickManager.Instance.Config.ticksPerRealSecond;
            primaryValue /= ticksPerSecond;
        }
    }
}
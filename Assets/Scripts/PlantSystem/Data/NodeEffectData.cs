using System;
using UnityEngine;
using WegoSystem;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;
    public bool isPassive = false;
    public bool consumedOnTrigger = false;
    public float primaryValue = 0f;
    public float secondaryValue = 0f;
    public SeedSpawnData seedData;
    
    // REMOVED: nodeDefinitionReference - no longer needed!

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
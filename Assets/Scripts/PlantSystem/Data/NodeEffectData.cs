// Assets/Scripts/PlantSystem/Data/NodeEffectData.cs
using System;
using UnityEngine;
using WegoSystem;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;
    public bool consumedOnTrigger = false;
    public float primaryValue = 0f;
    public float secondaryValue = 0f;
    public SeedSpawnData seedData;

    // NEW: Property to check if effect is passive
    public bool IsPassive => NodeEffectTypeHelper.IsPassiveEffect(effectType);

    // NEW: Property to check if effect is active
    public bool IsActive => NodeEffectTypeHelper.IsActiveEffect(effectType);

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
// Assets/Scripts/PlantSystem/Data/NodeEffectData.cs
using UnityEngine;
using WegoSystem;

[System.Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;
    public bool consumedOnTrigger = false;
    public float primaryValue = 0f;
    public float secondaryValue = 0f;
    public SeedSpawnData seedData;
    
    // IsPassive and IsActive properties are removed.
    // This logic is now handled by NodeDefinition.ActivationType.

    public int GetPrimaryValueAsInt() => Mathf.RoundToInt(primaryValue);
    public int GetSecondaryValueAsInt() => Mathf.RoundToInt(secondaryValue);

    /// <summary>
    /// If the effect is time-based (per second), this converts it to a per-tick value.
    /// </summary>
    public void ValidateForTicks()
    {
        if (effectType == NodeEffectType.EnergyPerTick && TickManager.Instance?.Config != null)
        {
            float ticksPerSecond = TickManager.Instance.Config.ticksPerRealSecond;
            if (ticksPerSecond > 0)
            {
                primaryValue /= ticksPerSecond;
            }
        }
    }
}
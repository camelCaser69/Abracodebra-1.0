using System;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;

    // For Seed:
    // effectValue     = stemMinLength
    // secondaryValue  = stemMaxLength
    // extra1          = growthSpeed
    // extra2          = leafGap
    public float effectValue;
    public float secondaryValue;
    public float extra1;
    public float extra2;

    // Two brand-new fields:
    public int leafPattern = 0;       // 0=Parallel, 1=Alternating
    public float growthRandomness = 0f; // Range [0..2]
}
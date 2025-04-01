using System;
using UnityEngine;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;
    public float primaryValue;
    public float secondaryValue;

    // Renamed from isGrowthPhaseOnly and default changed to false
    [Tooltip("If TRUE, this effect only contributes once to calculate initial plant stats (Passive Growth Effect). If FALSE, this effect executes during the periodic Mature Phase cycles (Active Effect).")]
    public bool isPassive = false; // Defaulting to false, meaning effects are Active unless specified otherwise
}
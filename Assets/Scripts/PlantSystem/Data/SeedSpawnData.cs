using System;
using UnityEngine;

[Serializable]
public class SeedSpawnData {
    [Header("Growth Properties")]
    [Tooltip("Base ticks between growth stages")]
    public int growthSpeed = 5;
    
    [Tooltip("Minimum stem segments")]
    public int stemLengthMin = 3;
    
    [Tooltip("Maximum stem segments")]
    public int stemLengthMax = 5;
    
    [Tooltip("Segments between leaf spawns (0 = every segment)")]
    public int leafGap = 1;
    
    [Tooltip("Leaf pattern (0-4)")]
    [Range(0, 4)]
    public int leafPattern = 0;
    
    [Tooltip("Chance of stem growing sideways (0-1)")]
    [Range(0f, 1f)]
    public float stemRandomness = 0.1f;
    
    [Header("Energy Properties")]
    [Tooltip("Base maximum energy storage")]
    public float energyStorage = 10f;
    
    [Header("Timing Properties")]
    [Tooltip("Ticks between mature cycles")]
    public int cooldown = 20;
    
    [Tooltip("Ticks before growth starts")]
    public int castDelay = 0;
}
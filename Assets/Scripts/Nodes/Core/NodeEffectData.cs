// Assets/Scripts/Nodes/Core/NodeEffectData.cs
using System;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;  
    public float effectValue;          // Primary value (for ManaStorage: capacity)
    public float secondaryValue;       // Secondary value (for ManaStorage: starting mana)
}
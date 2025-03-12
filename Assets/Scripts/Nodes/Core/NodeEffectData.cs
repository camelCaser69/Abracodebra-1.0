// Assets/Scripts/Nodes/Core/NodeEffectData.cs
using System;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;  // e.g. ManaCost, Damage
    public float effectValue;          // e.g. 5 for ManaCost, 10 for Damage
}
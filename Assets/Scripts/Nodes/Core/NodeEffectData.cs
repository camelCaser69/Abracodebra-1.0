// Assets/Scripts/Nodes/Core/NodeEffectData.cs
using System;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;  
    public float effectValue;       // For ManaStorage => capacity
    public float secondaryValue;    // For ManaStorage => current / starting
}

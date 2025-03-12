// Assets/Scripts/Nodes/Core/NodeEffectType.cs
public enum NodeEffectType
{
    ManaCost,
    Damage,
    ManaStorage,      // effectValue = capacity, secondaryValue = starting mana
    ManaRechargeRate,
    Output
}
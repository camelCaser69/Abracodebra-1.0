// Assets/Scripts/PlantSystem/Data/NodeEffectTypeHelper.cs
using System.Collections.Generic;

public static class NodeEffectTypeHelper
{
    // PassiveEffects and ActiveEffects HashSets are removed.
    // IsPassiveEffect and IsActiveEffect methods are removed.

    private static readonly HashSet<NodeEffectType> TriggerEffects = new HashSet<NodeEffectType>
    {
        NodeEffectType.TimerCast,
        NodeEffectType.ProximityCast,
        NodeEffectType.EatCast,
        NodeEffectType.LeafLossCast,
        NodeEffectType.Cooldown, // Cooldown itself is a trigger-related property
        NodeEffectType.CastDelay // CastDelay is also a trigger-related property
    };

    public static bool IsTriggerEffect(NodeEffectType type)
    {
        return TriggerEffects.Contains(type);
    }

    public static bool RequiresPrimaryValue(NodeEffectType type)
    {
        // A gene being harvestable is a flag, it doesn't need a value.
        // Growing a berry is also a flag; the properties of the berry come from other effects.
        return type != NodeEffectType.Harvestable &&
               type != NodeEffectType.GrowBerry;
    }

    public static bool RequiresSecondaryValue(NodeEffectType type)
    {
        return type == NodeEffectType.StemLength ||
               type == NodeEffectType.PoopAbsorption ||
               type == NodeEffectType.ScentModifier;
    }
}
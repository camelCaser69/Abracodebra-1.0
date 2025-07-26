// Assets/Scripts/PlantSystem/Data/NodeEffectTypeHelper.cs
using System.Collections.Generic;

public static class NodeEffectTypeHelper
{
    // Passive effects that modify stats/properties
    private static readonly HashSet<NodeEffectType> PassiveEffects = new HashSet<NodeEffectType>
    {
        NodeEffectType.EnergyStorage,
        NodeEffectType.EnergyPerTick,
        NodeEffectType.StemLength,
        NodeEffectType.GrowthSpeed,
        NodeEffectType.LeafGap,
        NodeEffectType.LeafPattern,
        NodeEffectType.StemRandomness,
        NodeEffectType.PoopAbsorption,
        NodeEffectType.SeedSpawn,
        NodeEffectType.Nutritious,
        NodeEffectType.Harvestable
    };

    // Active effects that consume energy or execute actions
    private static readonly HashSet<NodeEffectType> ActiveEffects = new HashSet<NodeEffectType>
    {
        NodeEffectType.EnergyCost,
        NodeEffectType.GrowBerry,
        NodeEffectType.ScentModifier,
        NodeEffectType.Damage
    };

    // Trigger effects (special active category)
    private static readonly HashSet<NodeEffectType> TriggerEffects = new HashSet<NodeEffectType>
    {
        NodeEffectType.TimerCast,
        NodeEffectType.ProximityCast,
        NodeEffectType.EatCast,
        NodeEffectType.LeafLossCast,
        NodeEffectType.Cooldown,
        NodeEffectType.CastDelay
    };

    public static bool IsPassiveEffect(NodeEffectType type)
    {
        return PassiveEffects.Contains(type);
    }

    public static bool IsActiveEffect(NodeEffectType type)
    {
        return ActiveEffects.Contains(type) || TriggerEffects.Contains(type);
    }

    public static bool IsTriggerEffect(NodeEffectType type)
    {
        return TriggerEffects.Contains(type);
    }

    public static bool RequiresPrimaryValue(NodeEffectType type)
    {
        // Effects that don't need primary value
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
using System;

public enum NodeEffectType
{
    // --- Growth & Stats (Mostly Passive) ---
    EnergyStorage = 0,
    EnergyPerTick = 1,
    EnergyCost = 2,
    StemLength = 3,
    GrowthSpeed = 4,
    LeafGap = 5,
    LeafPattern = 6,
    StemRandomness = 7,
    Cooldown = 8,
    CastDelay = 9,
    PoopAbsorption = 10,
    ScentModifier = 14,
    SeedSpawn = 13,

    // --- Spell Triggers (Casts) ---
    TimerCast = 15,
    ProximityCast = 16,
    EatCast = 17,
    LeafLossCast = 18,

    // --- Spell Spawners & Effects ---
    GrowBerry = 12,
    Damage = 11,
    Nutritious = 19,
    Harvestable = 20 // <<< NEW
}
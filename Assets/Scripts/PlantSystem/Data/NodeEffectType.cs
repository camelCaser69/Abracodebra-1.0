using System;

public enum NodeEffectType {
    // Energy & Resources
    EnergyStorage,           // Max energy capacity
    EnergyPerTick,          // Energy generated per tick
    EnergyCost,             // Energy cost when executing mature cycle

    // Growth & Structure
    StemLength,             // Number of stem segments
    GrowthSpeed,            // Ticks between growth stages
    LeafGap,                // Stem segments between leaves
    LeafPattern,            // Pattern type (0-4)
    StemRandomness,         // Chance of random stem direction (0-1)

    // Timing
    Cooldown,               // Ticks between mature cycles
    CastDelay,              // Ticks delay before growth starts

    // Environmental Interaction
    PoopAbsorption,         // Detection radius (primary) and energy bonus (secondary)

    // Combat & Effects
    Damage,                 // Damage multiplier

    // Spawning
    GrowBerry,              // Spawns berry
    SeedSpawn,              // Makes this a seed

    // Modifiers
    ScentModifier,          // Modifies scent radius/strength
}
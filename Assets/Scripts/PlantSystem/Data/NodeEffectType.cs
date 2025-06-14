using System;

public enum NodeEffectType {
    // Energy & Resources
    EnergyStorage = 0,           // Max energy capacity
    EnergyPerTick = 1,          // Energy generated per tick
    EnergyCost = 2,             // Energy cost when executing mature cycle

    // Growth & Structure
    StemLength = 3,             // Number of stem segments
    GrowthSpeed = 4,            // Ticks between growth stages
    LeafGap = 5,                // Stem segments between leaves
    LeafPattern = 6,            // Pattern type (0-4)
    StemRandomness = 7,         // Chance of random stem direction (0-1)

    // Timing
    Cooldown = 8,               // Ticks between mature cycles
    CastDelay = 9,              // Ticks delay before growth starts

    // Environmental Interaction
    PoopAbsorption = 10,        // Detection radius (primary) and energy bonus (secondary)

    // Combat & Effects
    Damage = 11,                // Damage multiplier

    // Spawning
    GrowBerry = 12,             // Spawns berry
    SeedSpawn = 13,             // Makes this a seed with comprehensive plant data

    // Modifiers
    ScentModifier = 14,         // Modifies scent radius/strength
}
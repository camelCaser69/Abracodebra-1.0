public enum NodeEffectType
{
    // Config / Growth Phase Effects (isGrowthPhaseOnly = true)
    ManaCost,           // Base cost calculation (might apply later too?)
    EnergyStorage,      // Determines max energy
    EnergyPhotosynthesis, // Determines base energy generation rate
    SeedSpawn,          // Marker effect to allow spawning
    StemLength,         // Modifies min/max stem length
    GrowthSpeed,        // Modifies time between growth steps
    LeafGap,            // Modifies spacing between leaves
    LeafPattern,        // Sets leaf pattern type
    StemRandomness,     // Modifies stem growth direction variance
    Cooldown,           // Base time between Mature Phase cycles
    CastDelay,          // Base time between Nodes in a Mature Phase cycle

    // Active / Mature Phase Effects (isGrowthPhaseOnly = false)
    Output,             // Triggers spell/projectile spawning
    Damage,             // Adds damage potential to outputs
    // Add potentially more active effects: Heal, ApplyStatus, AreaEffect, etc.
}
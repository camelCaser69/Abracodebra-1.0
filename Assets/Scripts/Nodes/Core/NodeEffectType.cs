public enum NodeEffectType
{
    ManaCost,
    Damage,
    ManaStorage,
    ManaRechargeRate,
    EnergyStorage,
    EnergyPhotosynthesis,
    Output,
    AimSpread,    // formerly Accuracy
    Burning,
    Piercing,
    FriendlyFire,
    // Replaced Seed with more granular effects
    SeedSpawn,        // Base effect to spawn a plant
    StemLength,       // Min/Max stem length
    GrowthSpeed,      // Controls growth speed
    LeafGap,          // Spacing between leaves
    LeafPattern,      // Parallel or alternating pattern
    StemRandomness    // Randomness of stem growth
}
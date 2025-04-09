// FILE: Assets\Scripts\Nodes\Core\NodeEffectType.cs
using System;
using UnityEngine;

public enum NodeEffectType
{
    // --- Passive / Growth Phase Effects (isPassive = true) ---
    // These typically run once at the start to define the plant's structure and base stats.

    // [Tooltip("Base cost calculation (Not currently implemented in PlantGrowth execution).")]
    // ManaCost, // REMOVED
    [Tooltip("Determines the maximum energy the plant can store.")]
    EnergyStorage,
    [Tooltip("Determines the base rate of energy generation through photosynthesis per leaf.")]
    EnergyPhotosynthesis,
    [Tooltip("A required marker effect for a node chain to be spawnable as a plant.")]
    SeedSpawn,
    [Tooltip("Modifies the minimum and maximum potential length of the main stem.")]
    StemLength,
    [Tooltip("Modifies the time interval between each step of stem/leaf growth.")]
    GrowthSpeed,
    [Tooltip("Modifies the number of stem segments between leaf spawns.")]
    LeafGap,
    [Tooltip("Sets the pattern in which leaves are spawned (e.g., Parallel, Alternating).")]
    LeafPattern,
    [Tooltip("Modifies the chance for the stem to grow diagonally instead of straight up.")]
    StemRandomness,
    [Tooltip("Modifies the base time duration between Mature Phase execution cycles.")]
    Cooldown,
    [Tooltip("Modifies the base time delay between executing the effects of sequential nodes within a Mature Phase cycle.")]
    CastDelay,


    // --- Active / Mature Phase Effects (isPassive = false) ---
    // These execute periodically after the plant has finished growing.

    [Tooltip("Energy cost deducted from the plant when this node's active effects are executed during the mature cycle.")]
    EnergyCost, // <<< RENAMED/ADDED
    [Tooltip("Triggers the spawning of a projectile or other output effect (requires OutputNodeEffect component).")]
    Output,
    [Tooltip("Modifies the damage potential of subsequent 'Output' effects in the same cycle.")]
    Damage,
    [Tooltip("Causes the plant to attempt to spawn a berry in an available adjacent slot during the mature cycle.")]
    GrowBerry,
    // Add potentially more active effects: Heal, ApplyStatus, AreaEffect, etc.
    
    
    [Tooltip("Modifies the scent emitted by the next spawned carrier (Berry, Projectile). PrimaryValue=Radius Add, SecondaryValue=Strength Add.")]
    ScentModifier,
}
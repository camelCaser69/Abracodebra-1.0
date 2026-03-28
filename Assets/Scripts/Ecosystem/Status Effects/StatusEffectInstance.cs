// FILE: Assets/Scripts/Ecosystem/Status Effects/StatusEffectInstance.cs
using UnityEngine;

public class StatusEffectInstance
{
    public StatusEffect effect;
    public int remainingTicks;
    public int stackCount = 1;
    public GameObject visualEffectInstance;

    // ═══════════════════════════════════════════════════════
    //  FREEZE TRACKING
    // ═══════════════════════════════════════════════════════

    /// <summary>When > 0, the creature is fully frozen (immobilized).</summary>
    public int frozenTicksRemaining = 0;

    /// <summary>Counts ticks since last stack refresh. Used for stack decay.</summary>
    public int ticksSinceLastRefresh = 0;

    /// <summary>True if the creature is currently in the fully-frozen state.</summary>
    public bool IsFullyFrozen => frozenTicksRemaining > 0;

    public StatusEffectInstance(StatusEffect effect)
    {
        this.effect = effect;
        this.remainingTicks = effect.durationTicks;
        this.stackCount = 1;
        this.frozenTicksRemaining = 0;
        this.ticksSinceLastRefresh = 0;
    }
}
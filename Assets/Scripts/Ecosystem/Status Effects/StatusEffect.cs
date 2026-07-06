// FILE: Assets/Scripts/Ecosystem/Status Effects/StatusEffect.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Abracodabra/Status Effect")]
public class StatusEffect : ScriptableObject
{
    public string effectID = "status_effect";
    public string displayName = "Status Effect";
    public Sprite icon;
    public string unicodeSymbol = "?";
    public Color effectColor = Color.white;

    public bool isPermanent = false;
    public int durationTicks = 10;

    public bool modifyAnimalColor = false;
    public Color animalTintColor = Color.white;
    public GameObject visualEffectPrefab;

    public bool damagePerTick = false;
    public float damageAmount = 0f;

    public bool healPerTick = false;
    public float healAmount = 0f;

    public bool modifyHunger = false;
    public float hungerModifier = 0f;

    [Tooltip("Multiplier for the VISUAL movement speed between tiles. Does not affect logical tiles-per-tick speed.")]
    public float visualInterpolationSpeedMultiplier = 1f;

    public float damageResistanceMultiplier = 1f;

    public int additionalMoveTicks = 0;

    public bool canStack = false;
    public int maxStacks = 1;

    // ═══════════════════════════════════════════════════════
    //  FREEZE-SPECIFIC FIELDS
    // ═══════════════════════════════════════════════════════

    [Header("Freeze Behavior (Stack-Based CC)")]
    [Tooltip("If true, this effect uses freeze logic: full freeze at max stacks, stack decay, progressive tint.")]
    public bool isFreezeType = false;

    [Tooltip("How many ticks the creature is fully frozen (immobilized) when reaching max stacks.")]
    public int frozenDurationTicks = 3;

    [Tooltip("After frozen duration expires, stacks drop to this value instead of being removed entirely.")]
    public int frozenDropToStacks = 2;

    [Tooltip("Ticks between automatic -1 stack decay when not being refreshed. 0 = no decay.")]
    public int stackDecayIntervalTicks = 5;

    [Tooltip("Progressive tint colors per stack level (index 0 = 1 stack, etc). Falls back to animalTintColor if empty.")]
    public Color[] stackTintColors = new Color[0];
}
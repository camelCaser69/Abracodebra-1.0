// FILE: Assets/Scripts/Ecosystem/Status Effects/StatusEffectManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    IStatusEffectable owner;
    List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    Dictionary<string, StatusEffectInstance> effectLookup = new Dictionary<string, StatusEffectInstance>();

    float cachedVisualInterpolationSpeedMultiplier = 1f;
    float cachedDamageResistanceMultiplier = 1f;
    int cachedAdditionalMoveTicks = 0;
    Color originalColor;
    SpriteRenderer spriteRenderer;

    public float VisualInterpolationSpeedMultiplier => cachedVisualInterpolationSpeedMultiplier;
    public float DamageResistanceMultiplier => cachedDamageResistanceMultiplier;
    public int AdditionalMoveTicks => cachedAdditionalMoveTicks;

    /// <summary>
    /// True if any freeze-type effect has reached max stacks and the creature is fully frozen.
    /// </summary>
    public bool IsFrozen
    {
        get
        {
            foreach (var instance in activeEffects)
            {
                if (instance.effect.isFreezeType && instance.IsFullyFrozen)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Returns the current freeze stack count for the given effectID, or 0 if not present.
    /// </summary>
    public int GetStackCount(string effectID)
    {
        if (effectLookup.TryGetValue(effectID, out var instance))
            return instance.stackCount;
        return 0;
    }

    public void Initialize(IStatusEffectable owner)
    {
        this.owner = owner;

        Component ownerComponent = owner as Component;
        if (ownerComponent != null)
        {
            spriteRenderer = ownerComponent.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (owner == null || (owner as Component) == null)
        {
            Destroy(this);
            return;
        }

        ProcessStatusEffects();
        UpdateCachedModifiers();
        UpdateVisualEffects();
    }

    public void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;

        if (effectLookup.ContainsKey(effect.effectID))
        {
            var existing = effectLookup[effect.effectID];

            if (effect.canStack && existing.stackCount < effect.maxStacks)
            {
                // Don't add stacks while fully frozen — just refresh duration
                if (!existing.IsFullyFrozen)
                {
                    existing.stackCount++;
                }
                existing.remainingTicks = effect.durationTicks;
                existing.ticksSinceLastRefresh = 0;

                // Check if we just hit max stacks → enter full freeze
                if (effect.isFreezeType && existing.stackCount >= effect.maxStacks && !existing.IsFullyFrozen)
                {
                    existing.frozenTicksRemaining = effect.frozenDurationTicks;
                    Debug.Log($"[StatusEffect] {owner.GetDisplayName()} is FULLY FROZEN for {effect.frozenDurationTicks} ticks!");
                }
            }
            else if (effect.canStack && existing.stackCount >= effect.maxStacks)
            {
                // At max stacks — just refresh duration
                existing.remainingTicks = effect.durationTicks;
                existing.ticksSinceLastRefresh = 0;
            }
            else if (!effect.canStack)
            {
                existing.remainingTicks = effect.durationTicks;
                existing.ticksSinceLastRefresh = 0;
            }
        }
        else
        {
            var instance = new StatusEffectInstance(effect);
            activeEffects.Add(instance);
            effectLookup[effect.effectID] = instance;

            if (effect.visualEffectPrefab != null)
            {
                instance.visualEffectInstance = Instantiate(
                    effect.visualEffectPrefab,
                    (owner as Component).transform.position,
                    Quaternion.identity,
                    (owner as Component).transform
                );
            }
            Debug.Log($"[StatusEffect] Applied {effect.displayName} to {owner.GetDisplayName()}");

            // Immediately check for full freeze on first application (e.g., if maxStacks == 1)
            if (effect.isFreezeType && instance.stackCount >= effect.maxStacks)
            {
                instance.frozenTicksRemaining = effect.frozenDurationTicks;
                Debug.Log($"[StatusEffect] {owner.GetDisplayName()} is FULLY FROZEN for {effect.frozenDurationTicks} ticks!");
            }
        }

        UpdateCachedModifiers();
    }

    public void RemoveStatusEffect(string effectID)
    {
        if (!effectLookup.ContainsKey(effectID)) return;
        var instance = effectLookup[effectID];
        if (instance.visualEffectInstance != null)
        {
            Destroy(instance.visualEffectInstance);
        }
        activeEffects.Remove(instance);
        effectLookup.Remove(effectID);
        Debug.Log($"[StatusEffect] Removed {instance.effect.displayName} from {owner.GetDisplayName()}");
        UpdateCachedModifiers();
    }

    void ProcessStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var instance = activeEffects[i];
            var effect = instance.effect;

            // ── Freeze-type special processing ──
            if (effect.isFreezeType)
            {
                ProcessFreezeEffect(instance);
                // Don't apply damage/heal/hunger while fully frozen (creature is inert)
                if (instance.IsFullyFrozen) continue;
            }

            // ── Standard per-tick effects ──
            if (effect.damagePerTick) owner.TakeDamage(effect.damageAmount * instance.stackCount);
            if (effect.healPerTick) owner.Heal(effect.healAmount * instance.stackCount);
            if (effect.modifyHunger) owner.ModifyHunger(effect.hungerModifier * instance.stackCount);

            // ── Duration expiry (non-freeze or non-permanent) ──
            if (!effect.isPermanent && !effect.isFreezeType)
            {
                instance.remainingTicks--;
                if (instance.remainingTicks <= 0)
                {
                    RemoveStatusEffect(effect.effectID);
                }
            }
        }
    }

    /// <summary>
    /// Handles freeze-specific tick logic: full freeze countdown, stack decay.
    /// </summary>
    void ProcessFreezeEffect(StatusEffectInstance instance)
    {
        var effect = instance.effect;

        // ── Full freeze countdown ──
        if (instance.IsFullyFrozen)
        {
            instance.frozenTicksRemaining--;

            if (instance.frozenTicksRemaining <= 0)
            {
                // Thaw: drop stacks instead of removing entirely
                instance.stackCount = Mathf.Min(instance.stackCount, effect.frozenDropToStacks);
                instance.ticksSinceLastRefresh = 0;

                if (instance.stackCount <= 0)
                {
                    RemoveStatusEffect(effect.effectID);
                    return;
                }

                Debug.Log($"[StatusEffect] {owner.GetDisplayName()} thawed! Stacks dropped to {instance.stackCount}.");
            }
            return; // While frozen, don't decay stacks
        }

        // ── Stack decay (only when not being refreshed) ──
        if (effect.stackDecayIntervalTicks > 0 && instance.stackCount > 0)
        {
            instance.ticksSinceLastRefresh++;

            if (instance.ticksSinceLastRefresh >= effect.stackDecayIntervalTicks)
            {
                instance.stackCount--;
                instance.ticksSinceLastRefresh = 0;

                if (instance.stackCount <= 0)
                {
                    RemoveStatusEffect(effect.effectID);
                    return;
                }

                Debug.Log($"[StatusEffect] {owner.GetDisplayName()} freeze stack decayed to {instance.stackCount}.");
            }
        }

        // Freeze effects don't expire by remainingTicks — they decay by stacks
        // (remainingTicks is only used for refresh tracking)
    }

    void UpdateCachedModifiers()
    {
        cachedVisualInterpolationSpeedMultiplier = 1f;
        cachedDamageResistanceMultiplier = 1f;
        cachedAdditionalMoveTicks = 0;

        foreach (var instance in activeEffects)
        {
            var effect = instance.effect;

            // If fully frozen, add maximum slow (effectively infinite)
            if (effect.isFreezeType && instance.IsFullyFrozen)
            {
                cachedAdditionalMoveTicks += 999; // Effectively stops all movement
                continue;
            }

            cachedVisualInterpolationSpeedMultiplier *= effect.visualInterpolationSpeedMultiplier;
            cachedDamageResistanceMultiplier *= effect.damageResistanceMultiplier;
            cachedAdditionalMoveTicks += effect.additionalMoveTicks * instance.stackCount;
        }
    }

    public bool HasStatusEffect(string effectID)
    {
        return effectLookup.ContainsKey(effectID);
    }

    void UpdateVisualEffects()
    {
        if (spriteRenderer == null) return;

        Color targetColor = originalColor;
        bool hasColorEffect = false;

        foreach (var instance in activeEffects)
        {
            if (!instance.effect.modifyAnimalColor) continue;

            // Freeze-type: use progressive tint based on stack count
            if (instance.effect.isFreezeType && instance.effect.stackTintColors != null && instance.effect.stackTintColors.Length > 0)
            {
                int tintIndex = Mathf.Clamp(instance.stackCount - 1, 0, instance.effect.stackTintColors.Length - 1);
                targetColor = instance.effect.stackTintColors[tintIndex];
                hasColorEffect = true;
            }
            else
            {
                targetColor = instance.effect.animalTintColor;
                hasColorEffect = true;
            }
            break; // Only use first color-modifying effect
        }

        spriteRenderer.color = hasColorEffect ? targetColor : originalColor;
    }

    public List<StatusEffectInstance> GetActiveEffects()
    {
        return new List<StatusEffectInstance>(activeEffects);
    }

    public void ClearAllEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RemoveStatusEffect(activeEffects[i].effect.effectID);
        }
    }

    void OnDestroy()
    {
        foreach (var instance in activeEffects)
        {
            if (instance.visualEffectInstance != null)
            {
                Destroy(instance.visualEffectInstance);
            }
        }
    }
}
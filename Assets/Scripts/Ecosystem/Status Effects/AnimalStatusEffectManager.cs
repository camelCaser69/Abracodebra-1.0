// Assets/Scripts/Ecosystem/StatusEffects/AnimalStatusEffectManager.cs
using System.Collections.Generic;
using UnityEngine;

public class AnimalStatusEffectManager : MonoBehaviour
{
    private AnimalController controller;
    private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    private Dictionary<string, StatusEffectInstance> effectLookup = new Dictionary<string, StatusEffectInstance>();

    // Cached values
    private float cachedMovementSpeedMultiplier = 1f;
    private float cachedDamageResistanceMultiplier = 1f;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    public float MovementSpeedMultiplier => cachedMovementSpeedMultiplier;
    public float DamageResistanceMultiplier => cachedDamageResistanceMultiplier;

    public void Initialize(AnimalController controller)
    {
        this.controller = controller;
        spriteRenderer = controller.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void OnTickUpdate(int currentTick)
    {
        if (!enabled || controller.IsDying) return;

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
                existing.stackCount++;
                existing.remainingTicks = effect.durationTicks; // Refresh duration
            }
            else if (!effect.canStack)
            {
                existing.remainingTicks = effect.durationTicks; // Just refresh duration
            }
        }
        else
        {
            var instance = new StatusEffectInstance(effect);
            activeEffects.Add(instance);
            effectLookup[effect.effectID] = instance;

            // Create visual effect
            if (effect.visualEffectPrefab != null)
            {
                instance.visualEffectInstance = Instantiate(
                    effect.visualEffectPrefab,
                    transform.position,
                    Quaternion.identity,
                    transform
                );
            }

            Debug.Log($"[StatusEffect] Applied {effect.displayName} to {controller.SpeciesName}");
        }
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

        Debug.Log($"[StatusEffect] Removed {instance.effect.displayName} from {controller.SpeciesName}");
    }

    public bool HasStatusEffect(string effectID)
    {
        return effectLookup.ContainsKey(effectID);
    }

    private void ProcessStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var instance = activeEffects[i];
            var effect = instance.effect;

            // Apply tick effects
            if (effect.damagePerTick)
            {
                controller.TakeDamage(effect.damageAmount * instance.stackCount);
            }

            if (effect.healPerTick)
            {
                controller.Needs.Heal(effect.healAmount * instance.stackCount);
            }

            if (effect.modifyHunger)
            {
                controller.Needs.ModifyHunger(effect.hungerModifier * instance.stackCount);
            }

            // Update duration
            if (!effect.isPermanent)
            {
                instance.remainingTicks--;
                if (instance.remainingTicks <= 0)
                {
                    RemoveStatusEffect(effect.effectID);
                }
            }
        }
    }

    private void UpdateCachedModifiers()
    {
        cachedMovementSpeedMultiplier = 1f;
        cachedDamageResistanceMultiplier = 1f;

        foreach (var instance in activeEffects)
        {
            var effect = instance.effect;
            cachedMovementSpeedMultiplier *= effect.movementSpeedMultiplier;
            cachedDamageResistanceMultiplier *= effect.damageResistanceMultiplier;
        }
    }

    private void UpdateVisualEffects()
    {
        if (spriteRenderer == null) return;

        // Find the highest priority color effect
        Color targetColor = originalColor;
        bool hasColorEffect = false;

        foreach (var instance in activeEffects)
        {
            if (instance.effect.modifyAnimalColor)
            {
                targetColor = instance.effect.animalTintColor;
                hasColorEffect = true;
                break; // Use first color effect found
            }
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
    
    private void OnDestroy()
    {
        // Clean up visual effects
        foreach (var instance in activeEffects)
        {
            if (instance.visualEffectInstance != null)
            {
                Destroy(instance.visualEffectInstance);
            }
        }
    }
}
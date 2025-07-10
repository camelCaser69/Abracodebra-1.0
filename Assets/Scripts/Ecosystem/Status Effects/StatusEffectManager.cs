// Assets/Scripts/Ecosystem/StatusEffects/StatusEffectManager.cs
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    // The interface allows this to work with Animals, Players, or anything else
    private IStatusEffectable owner; 
    private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    private Dictionary<string, StatusEffectInstance> effectLookup = new Dictionary<string, StatusEffectInstance>();

    // Cached values
    private float cachedMovementSpeedMultiplier = 1f;
    private float cachedDamageResistanceMultiplier = 1f;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    public float MovementSpeedMultiplier => cachedMovementSpeedMultiplier;
    public float DamageResistanceMultiplier => cachedDamageResistanceMultiplier;

    // The Initialize method now takes the generic interface
    public void Initialize(IStatusEffectable owner)
    {
        this.owner = owner;
        
        // We need to get the SpriteRenderer from the owner's GameObject
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
        // Check if the owner is still valid
        if (owner == null || (owner as Component) == null)
        {
            Destroy(this); // Clean up if owner is gone
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
                existing.stackCount++;
                existing.remainingTicks = effect.durationTicks;
            }
            else if (!effect.canStack)
            {
                existing.remainingTicks = effect.durationTicks;
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
        }
    }
    
    // ... (RemoveStatusEffect and HasStatusEffect remain the same) ...
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

            // Use the interface methods to apply effects
            if (effect.damagePerTick)
            {
                owner.TakeDamage(effect.damageAmount * instance.stackCount);
            }

            if (effect.healPerTick)
            {
                owner.Heal(effect.healAmount * instance.stackCount);
            }

            if (effect.modifyHunger)
            {
                owner.ModifyHunger(effect.hungerModifier * instance.stackCount);
            }

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

        Color targetColor = originalColor;
        bool hasColorEffect = false;

        foreach (var instance in activeEffects)
        {
            if (instance.effect.modifyAnimalColor)
            {
                targetColor = instance.effect.animalTintColor;
                hasColorEffect = true;
                break;
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
        foreach (var instance in activeEffects)
        {
            if (instance.visualEffectInstance != null)
            {
                Destroy(instance.visualEffectInstance);
            }
        }
    }
}
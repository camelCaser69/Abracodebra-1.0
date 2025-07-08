// Assets/Scripts/Ecosystem/StatusEffects/StatusEffectInstance.cs
using UnityEngine;

[System.Serializable]
public class StatusEffectInstance
{
    public StatusEffect effect;
    public int remainingTicks;
    public int stackCount = 1;
    public GameObject visualEffectInstance;

    public StatusEffectInstance(StatusEffect effect)
    {
        this.effect = effect;
        this.remainingTicks = effect.durationTicks;
        this.stackCount = 1;
    }
}
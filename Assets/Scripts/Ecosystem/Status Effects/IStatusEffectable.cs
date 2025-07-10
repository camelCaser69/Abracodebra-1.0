// Assets/Scripts/Ecosystem/StatusEffects/IStatusEffectable.cs
using UnityEngine;
using WegoSystem; // For GridEntity

public interface IStatusEffectable
{
    // A reference to the object's GridEntity for movement modification
    GridEntity GridEntity { get; }

    // A reference to the object's StatusEffectManager
    StatusEffectManager StatusManager { get; }
    
    // A unique name for logging/debugging
    string GetDisplayName();
    
    // Methods to handle effects
    void TakeDamage(float amount);
    void Heal(float amount);
    void ModifyHunger(float amount); // Can be left empty for player
}
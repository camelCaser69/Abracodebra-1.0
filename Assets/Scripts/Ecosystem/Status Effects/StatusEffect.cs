// Assets/Scripts/Ecosystem/StatusEffects/StatusEffect.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Status Effect", menuName = "Ecosystem/Status Effect")]
public class StatusEffect : ScriptableObject
{
    #region Basic Info
    [Header("Basic Info")]
    public string effectID = "status_effect";
    public string displayName = "Status Effect";
    public Sprite icon;
    public string unicodeSymbol = "?";
    public Color effectColor = Color.white;
    #endregion

    #region Duration
    [Header("Duration")]
    public bool isPermanent = false;
    public int durationTicks = 10;
    #endregion

    #region Visual Effects
    [Header("Visual Effects")]
    public bool modifyAnimalColor = false;
    public Color animalTintColor = Color.white;
    public GameObject visualEffectPrefab;
    #endregion

    #region Tick Effects
    [Header("Tick Effects")]
    public bool damagePerTick = false;
    public float damageAmount = 0f;

    public bool healPerTick = false;
    public float healAmount = 0f;

    public bool modifyHunger = false;
    public float hungerModifier = 0f;
    #endregion

    #region Modifiers
    [Header("Modifiers")]
    [Tooltip("Modifies the visual speed of the movement animation. Does NOT affect the tick cost. 1 = normal, 0.5 = 50% slower, 2 = 200% faster.")]
    public float visualSpeedMultiplier = 1f; // <<< RENAMED
    
    [Tooltip("Modifies incoming damage. 1 = normal, 0.5 = 50% resistance, 2 = 200% vulnerability.")]
    public float damageResistanceMultiplier = 1f;
    
    [Tooltip("How many extra ticks a 1-tile move costs. 1 means a move takes 2 ticks total.")]
    public int additionalMoveTicks = 0;
    #endregion

    #region Stacking
    [Header("Stacking")]
    public bool canStack = false;
    public int maxStacks = 1;
    #endregion
}
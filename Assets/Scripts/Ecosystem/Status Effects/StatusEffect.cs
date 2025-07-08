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
    public string unicodeSymbol = "?"; // Fallback if no icon sprite
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
    public float movementSpeedMultiplier = 1f;
    public float damageResistanceMultiplier = 1f;
    #endregion

    #region Stacking
    [Header("Stacking")]
    public bool canStack = false;
    public int maxStacks = 1;
    #endregion
}
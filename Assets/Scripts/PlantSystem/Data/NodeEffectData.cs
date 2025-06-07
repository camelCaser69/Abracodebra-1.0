// FILE: Assets/Scripts/Nodes/Core/NodeEffectData.cs
using System;
using UnityEngine;

[Serializable]
public class NodeEffectData
{
    public NodeEffectType effectType;

    [Tooltip("Primary numeric value for the effect (e.g., Amount, Duration, Radius Bonus).")] // Updated tooltip
    public float primaryValue;
    [Tooltip("Secondary numeric value for the effect (e.g., Speed, Intensity, Strength Bonus).")] // Updated tooltip
    public float secondaryValue;

    [Tooltip("If TRUE, effect runs once during growth. If FALSE, effect executes during mature cycles.")]
    public bool isPassive = false;

    // --- Scent Specific ---
    // [Tooltip("Identifier (scentID from ScentDefinition) of the scent to apply. Used only if effectType is ScentModifier.")]
    // public string scentIdentifier; // <<< REMOVED

    [Tooltip("The Scent Definition to apply/modify. Used only if effectType is ScentModifier.")]
    public ScentDefinition scentDefinitionReference; // <<< ADDED: Direct reference
}
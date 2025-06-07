using UnityEngine;

[CreateAssetMenu(fileName = "Scent_", menuName = "Ecosystem/Scent Definition")]
public class ScentDefinition : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Unique identifier used internally and potentially for node effects.")]
    public string scentID = "default_scent"; // Still useful for debugging/lookup
    [Tooltip("Display name for UI or debugging.")]
    public string displayName = "Default Scent";

    // [Header("Gameplay Properties")] - Removed scentType enum field

    [Tooltip("Base radius for this scent type if not modified by nodes.")]
    public float baseRadius = 1f;
    [Tooltip("Base strength for this scent type if not modified by nodes.")]
    public float baseStrength = 1f;

    [Header("Visuals (Optional)")]
    [Tooltip("Particle effect prefab to instantiate when this scent is active.")]
    public GameObject particleEffectPrefab;
}
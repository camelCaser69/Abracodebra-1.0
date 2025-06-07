using UnityEngine;

// No longer requires RuntimeCircleDrawer directly
// REMOVED: [RequireComponent(typeof(RuntimeCircleDrawer))]
public class ScentSource : MonoBehaviour
{
    [Header("Scent Definition")]
    [Tooltip("The base Scent Definition applied to this object.")]
    public ScentDefinition definition; // Assigned by PlantGrowth/Node effects

    [Header("Applied Modifiers")]
    [Tooltip("Bonus radius added by node effects.")]
    public float radiusModifier = 0f;
    [Tooltip("Bonus strength added by node effects.")]
    public float strengthModifier = 0f;

    // REMOVED: Debugging Reference private RuntimeCircleDrawer circleDrawer;

    // --- Calculated Effective Properties ---
    /// <summary> Gets the effective scent radius (Base Radius + Modifier), clamped >= 0. </summary>
    public float EffectiveRadius => Mathf.Max(0f, (definition != null ? definition.baseRadius : 0f) + radiusModifier);
    /// <summary> Gets the effective scent strength (Base Strength + Modifier), clamped >= 0. </summary>
    public float EffectiveStrength => Mathf.Max(0f, (definition != null ? definition.baseStrength : 0f) + strengthModifier);

    // Keep Gizmo for Editor visualization (runs independently of Update and runtime drawers)
    void OnDrawGizmosSelected()
    {
        float effectiveRadius = EffectiveRadius; // Calculate radius for gizmo

        if (definition != null) // Check if a definition is assigned
        {
            if (effectiveRadius > 0.01f) // Only draw if radius is meaningful
            {
                // Use definition name hash for consistent random color
                // Note: Random.InitState affects the *next* Random call globally,
                // which might be undesirable if other Gizmos rely on it.
                // A more robust way might be a custom color mapping or a simple hash function.
                // For simplicity, we'll keep Random.InitState for now.
                int prevState = Random.state.GetHashCode(); // Store previous state
                Random.InitState(definition.name.GetHashCode());
                Color gizmoColor = Random.ColorHSV(0f, 1f, 0.7f, 0.9f, 0.8f, 1f);
                gizmoColor.a = 0.3f; // Set alpha for gizmo
                Random.InitState(prevState); // Restore previous state

                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, effectiveRadius);
            }
        }
        else // Draw default gray if no definition assigned yet
        {
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 1f); // Default size for editor only
        }
    }
}
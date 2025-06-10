using UnityEngine;

public class ScentSource : MonoBehaviour
{
    public ScentDefinition definition; // Assigned by PlantGrowth/Node effects

    public float radiusModifier = 0f;
    public float strengthModifier = 0f;

    // The radius is now rounded to the nearest integer, effectively snapping it to the grid.
    public float EffectiveRadius => Mathf.RoundToInt(Mathf.Max(0f, (definition != null ? definition.baseRadius : 0f) + radiusModifier));
    public float EffectiveStrength => Mathf.Max(0f, (definition != null ? definition.baseStrength : 0f) + strengthModifier);

    void OnDrawGizmosSelected()
    {
        float effectiveRadius = EffectiveRadius; // Calculate radius for gizmo

        if (definition != null) // Check if a definition is assigned
        {
            if (effectiveRadius > 0.01f) // Only draw if radius is meaningful
            {
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
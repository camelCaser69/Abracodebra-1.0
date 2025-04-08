using UnityEngine;

// Enum to define different scent types later
public enum ScentType { None, FloralSweet, Fruity, Pungent, AttractantA, RepellentB }

/// <summary>
/// Attach this component to GameObjects (like flowers, fruits, projectiles)
/// that should emit a scent detectable by other systems (e.g., Animals).
/// The actual detection logic will be in the detecting entity's script.
/// </summary>
public class ScentSource : MonoBehaviour
{
    [Header("Scent Properties")]
    [Tooltip("Type of scent emitted.")]
    public ScentType type = ScentType.None;

    [Tooltip("Strength or intensity of the scent. Usage depends on detecting system.")]
    [Range(0f, 10f)] public float strength = 1f;

    [Tooltip("Radius within which this scent can typically be detected.")]
    public float radius = 3f;

    // Potential future additions:
    // public float duration = -1f; // -1 for permanent while object exists
    // public AnimationCurve falloffCurve;

    // No complex logic needed here for now. This component just holds data.
    // Other scripts (like AnimalController) will look for this component on nearby objects.

    void OnDrawGizmosSelected()
    {
        // Visualize the scent radius in the editor
        if (radius > 0)
        {
            Color gizmoColor;
            switch (type)
            {
                case ScentType.FloralSweet: gizmoColor = new Color(1f, 0.5f, 1f, 0.3f); break; // Pink
                case ScentType.Fruity:      gizmoColor = new Color(1f, 0.7f, 0f, 0.3f); break; // Orange
                case ScentType.Pungent:     gizmoColor = new Color(0.5f, 1f, 0.5f, 0.3f); break; // Greenish
                case ScentType.AttractantA: gizmoColor = new Color(0.5f, 0.8f, 1f, 0.3f); break; // Light Blue
                case ScentType.RepellentB:  gizmoColor = new Color(1f, 0.4f, 0.4f, 0.3f); break; // Reddish
                case ScentType.None:
                default:                    gizmoColor = new Color(0.8f, 0.8f, 0.8f, 0.2f); break; // Gray
            }
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}
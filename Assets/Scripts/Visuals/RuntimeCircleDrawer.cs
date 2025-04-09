// FILE: Assets/Scripts/Visuals/Debug/RuntimeCircleDrawer.cs
using UnityEngine;

/// <summary>
/// Draws a circle outline using a LineRenderer attached to the same GameObject.
/// Requires a LineRenderer component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RuntimeCircleDrawer : MonoBehaviour
{
    [Range(3, 60)]
    public int segments = 30; // Number of line segments to approximate the circle
    public float radius = 1.0f;
    public float lineWidth = 0.02f;
    public Color color = Color.yellow;
    public Material lineMaterial; // Assign the same material used for firefly lines, or a specific one

    private LineRenderer lineRenderer;
    private bool needsRedraw = true; // Flag to force redraw on first UpdateCircle call or when params change
    private float currentRadius = -1f; // Store current values to detect changes
    private Color currentColor = Color.clear;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) { // Should not happen with RequireComponent
            Debug.LogError($"[{gameObject.name}] RuntimeCircleDrawer: Missing required LineRenderer component!");
            enabled = false; // Disable script if component missing
            return;
        }
        ConfigureLineRendererDefaults();
        lineRenderer.enabled = false; // Start hidden
    }

    // Sets initial parameters that don't change often
    void ConfigureLineRendererDefaults()
    {
        lineRenderer.useWorldSpace = false; // Draw relative to this object's transform
        lineRenderer.loop = true; // Connect the last point to the first
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = lineMaterial;

        // Attempt to match sorting with parent sprite
        SpriteRenderer parentSprite = GetComponentInParent<SpriteRenderer>();
        if (parentSprite != null) {
            lineRenderer.sortingLayerName = parentSprite.sortingLayerName;
            lineRenderer.sortingOrder = parentSprite.sortingOrder + 1; // Draw slightly in front
        } else {
            // Default sorting if no parent sprite found
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 1;
        }
    }

    // Call this method to update the circle's appearance and make it visible
    public void UpdateCircle(float newRadius, Color newColor)
    {
        // Check if parameters have actually changed
        if (!needsRedraw && Mathf.Approximately(currentRadius, newRadius) && currentColor == newColor)
        {
            // Ensure it's enabled if it wasn't already
             if (!lineRenderer.enabled) lineRenderer.enabled = true;
            return; // No change needed
        }

        // Update stored values
        currentRadius = newRadius;
        radius = newRadius; // Update public field for potential inspector viewing
        currentColor = newColor;
        color = newColor; // Update public field

        // Update LineRenderer appearance settings that might change
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        // Update width if you add properties for it too
        // lineRenderer.startWidth = newWidth;
        // lineRenderer.endWidth = newWidth;

        DrawCircle(); // Recalculate points
        lineRenderer.enabled = true; // Ensure it's visible
        needsRedraw = false; // Mark as drawn
    }

    // Call this to hide the circle
    public void HideCircle()
    {
        if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
            needsRedraw = true; // Needs redraw next time it's shown
        }
    }

    void DrawCircle()
    {
        if (lineRenderer == null || segments <= 2 || radius <= 0f) {
            lineRenderer.positionCount = 0; // Clear points if invalid params
            return;
        };

        // Only resize array if segment count changes (optimization)
        if (lineRenderer.positionCount != segments + 1) {
            lineRenderer.positionCount = segments + 1;
        }

        float angleStep = 360f / segments;
        Vector3[] points = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Cos(currentAngle) * radius;
            float y = Mathf.Sin(currentAngle) * radius;
            points[i] = new Vector3(x, y, 0); // Z is 0 for local space relative to transform
        }

        lineRenderer.SetPositions(points);
    }
}
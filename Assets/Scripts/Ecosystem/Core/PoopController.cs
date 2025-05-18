// FILE: Assets/Scripts/Ecosystem/Core/PoopController.cs (Ensure Collider is Added)

using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))] // Add RequireComponent for Collider2D
public class PoopController : MonoBehaviour
{
    [Tooltip("Total lifetime of this poop (in seconds) before disappearing.")]
    public float lifetime = 10f;
    [Tooltip("Duration at the end of the lifetime during which the poop fades out.")]
    public float fadeDuration = 2f;
    [Tooltip("Fadeout curve to control the alpha during fade-out. X axis goes from 0 (start of fade) to 1 (end of fade).")]
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private SpriteRenderer sr;
    private float timer = 0f;
    private Color initialColor;
    private Collider2D poopCollider; // Reference to the collider

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            initialColor = sr.color;
        }
        
        // Ensure there's a collider and it's properly configured
        poopCollider = GetComponent<Collider2D>();
        if (poopCollider == null)
        {
            // If no collider exists, add a CircleCollider2D
            poopCollider = gameObject.AddComponent<CircleCollider2D>();
            Debug.Log($"Added CircleCollider2D to {gameObject.name} for poop detection", gameObject);
        }
        
        // Make sure it's a trigger so it doesn't block movement
        poopCollider.isTrigger = true;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // If within fadeDuration at the end, apply fade-out using the fadeCurve.
        float remaining = lifetime - timer;
        if (remaining < fadeDuration && sr != null)
        {
            // Normalize fade time (0 = start, 1 = end)
            float t = 1f - (remaining / fadeDuration);
            float alphaMultiplier = fadeCurve.Evaluate(t);
            Color newColor = initialColor;
            newColor.a = alphaMultiplier;
            sr.color = newColor;
        }
    }

    // Initialize with default parameters if none are provided.
    public void Initialize(float lifetimeValue = -1f, float fadeDurationValue = -1f)
    {
        if (lifetimeValue > 0f)
            lifetime = lifetimeValue;
        if (fadeDurationValue > 0f)
            fadeDuration = fadeDurationValue;
        timer = 0f;
        if (sr != null)
            initialColor = sr.color;
            
        // Ensure collider is configured properly
        if (poopCollider == null)
        {
            poopCollider = GetComponent<Collider2D>();
            if (poopCollider == null)
            {
                poopCollider = gameObject.AddComponent<CircleCollider2D>();
            }
            poopCollider.isTrigger = true;
        }
    }
}
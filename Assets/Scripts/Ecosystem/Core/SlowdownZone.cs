// FILE: Assets\Scripts\Ecosystem\Core\SlowdownZone.cs
using UnityEngine;
using System.Collections.Generic;

public class SlowdownZone : MonoBehaviour
{
    [Header("Slowdown Settings")]
    [Tooltip("How much to multiply movement speed by (0.5 = half speed)")]
    [Range(0.1f, 1.0f)]
    public float speedMultiplier = 0.5f;

    [Header("Collider Adjustment")]
    [Tooltip("How much to shrink the collider from its edges (in units). Only works with BoxCollider2D.")]
    [Range(0f, 1f)]
    public float colliderShrinkAmount = 0.2f; // Defaulted to 0.2f in original, if 0 no warning will show for non-BoxColliders

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;

    private Dictionary<int, AnimalController> affectedAnimals = new Dictionary<int, AnimalController>();
    private Dictionary<int, GardenerController> affectedPlayers = new Dictionary<int, GardenerController>();

    private Vector2 originalSize;
    private Vector2 originalOffset;
    private BoxCollider2D boxCollider; // Cached BoxCollider2D, null if not a BoxCollider2D

    private const float SHRINK_EPSILON = 0.001f; // For comparing colliderShrinkAmount against zero

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"SlowdownZone on '{gameObject.name}' requires a Collider2D component!", gameObject);
            enabled = false;
            return;
        }

        if (!col.isTrigger)
        {
            col.isTrigger = true;
            // Optional: Log that trigger was auto-enabled
            // if (showDebugMessages) Debug.Log($"SlowdownZone on '{gameObject.name}' automatically enabled isTrigger on its collider.");
        }

        boxCollider = col as BoxCollider2D; // Attempt to cast to BoxCollider2D

        if (boxCollider != null)
        {
            // It's a BoxCollider2D
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;

            if (colliderShrinkAmount > SHRINK_EPSILON)
            {
                ShrinkCollider();
            }
            // No warning needed if it's a BoxCollider2D, regardless of shrinkAmount
        }
        else
        {
            // It's NOT a BoxCollider2D (e.g., TilemapCollider2D, PolygonCollider2D)
            if (colliderShrinkAmount > SHRINK_EPSILON) // Only warn if shrinking was actually intended
            {
               // Debug.LogWarning($"SlowdownZone on '{gameObject.name}' has a 'Collider Shrink Amount' of {colliderShrinkAmount} but is using a '{col.GetType().Name}'. This collider type does not support automatic shrinking. Shrinking will be ignored.", gameObject);
            }
            // If colliderShrinkAmount is 0 (or very close), no warning is issued, which is correct.
        }
    }

    private void ShrinkCollider()
    {
        if (boxCollider == null) return; // Should not happen if called correctly, but safety first

        Vector2 newSize = new Vector2(
            Mathf.Max(0.1f, originalSize.x - (colliderShrinkAmount * 2f)),
            Mathf.Max(0.1f, originalSize.y - (colliderShrinkAmount * 2f))
        );

        boxCollider.size = newSize;
        // Note: originalOffset is not changed by this shrinking method.

        if (showDebugMessages)
        {
            Debug.Log($"SlowdownZone on '{gameObject.name}': Shrunk BoxCollider2D from {originalSize} to {newSize}");
        }
    }

    private void RestoreCollider()
    {
        if (boxCollider == null) return;

        boxCollider.size = originalSize;
        boxCollider.offset = originalOffset; // Ensure offset is also restored

        if (showDebugMessages)
        {
            Debug.Log($"SlowdownZone on '{gameObject.name}': Restored BoxCollider2D to original size {originalSize} and offset {originalOffset}");
        }
    }

    private void OnValidate()
    {
        // This method is called in the editor when a script's properties are changed.
        // It's also called when the script is first loaded or a value is changed in the Inspector.

        // If we have a cached BoxCollider2D (meaning it was a BoxCollider2D at Awake)
        if (boxCollider != null)
        {
            if (colliderShrinkAmount > SHRINK_EPSILON)
            {
                // If playing, originalSize should be set. If not playing, Awake might not have run.
                // To be safe, only shrink if originalSize seems valid (not zero).
                if (originalSize != Vector2.zero || Application.isPlaying) // Application.isPlaying ensures Awake has run
                {
                     ShrinkCollider();
                }
                else if (showDebugMessages && !Application.isPlaying)
                {
                    // Debug.Log($"SlowdownZone OnValidate (Editor): '{gameObject.name}' BoxCollider2D detected, but originalSize not cached (Awake likely not run yet for this specific validation). Shrinking will apply on Play.");
                }
            }
            else // colliderShrinkAmount is effectively zero
            {
                // If originalSize is known, restore it.
                if (originalSize != Vector2.zero || Application.isPlaying)
                {
                    RestoreCollider();
                }
                else if (showDebugMessages && !Application.isPlaying)
                {
                    // Debug.Log($"SlowdownZone OnValidate (Editor): '{gameObject.name}' BoxCollider2D detected, shrink amount is zero, but originalSize not cached. Restoration will apply on Play if needed.");
                }
            }
        }
        // If boxCollider is null, it means it wasn't a BoxCollider2D at Awake (or Awake hasn't run).
        // In this case, OnValidate won't attempt to shrink or restore, aligning with Awake's logic.
        // The warning about non-BoxCollider shrinking (if applicable) is handled by Awake at runtime.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        AnimalController animal = other.GetComponent<AnimalController>();
        if (animal != null)
        {
            int id = animal.GetInstanceID();
            if (!affectedAnimals.ContainsKey(id)) // Ensure not already added
            {
                affectedAnimals.Add(id, animal);
                animal.ApplySpeedMultiplier(speedMultiplier);
                if (showDebugMessages)
                    Debug.Log($"SlowdownZone: '{animal.name}' entered zone, applied multiplier {speedMultiplier}");
            }
            return;
        }

        GardenerController player = other.GetComponent<GardenerController>();
        if (player != null)
        {
            int id = player.GetInstanceID();
            if (!affectedPlayers.ContainsKey(id)) // Ensure not already added
            {
                affectedPlayers.Add(id, player);
                player.ApplySpeedMultiplier(speedMultiplier);
                if (showDebugMessages)
                    Debug.Log($"SlowdownZone: Player '{player.name}' entered zone, applied multiplier {speedMultiplier}");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        AnimalController animal = other.GetComponent<AnimalController>();
        if (animal != null)
        {
            int id = animal.GetInstanceID();
            if (affectedAnimals.ContainsKey(id))
            {
                animal.RemoveSpeedMultiplier(speedMultiplier); // Call RemoveSpeedMultiplier on the animal
                affectedAnimals.Remove(id);
                if (showDebugMessages)
                    Debug.Log($"SlowdownZone: '{animal.name}' exited zone, removed multiplier");
            }
            return;
        }

        GardenerController player = other.GetComponent<GardenerController>();
        if (player != null)
        {
            int id = player.GetInstanceID();
            if (affectedPlayers.ContainsKey(id))
            {
                player.RemoveSpeedMultiplier(speedMultiplier); // Call RemoveSpeedMultiplier on the player
                affectedPlayers.Remove(id);
                if (showDebugMessages)
                    Debug.Log($"SlowdownZone: Player '{player.name}' exited zone, removed multiplier");
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var animalEntry in affectedAnimals)
        {
            if (animalEntry.Value != null) // Check if animal still exists
            {
                animalEntry.Value.RemoveSpeedMultiplier(speedMultiplier);
            }
        }
        affectedAnimals.Clear();

        foreach (var playerEntry in affectedPlayers)
        {
            if (playerEntry.Value != null) // Check if player still exists
            {
                playerEntry.Value.RemoveSpeedMultiplier(speedMultiplier);
            }
        }
        affectedPlayers.Clear();

        // Restore collider on destroy if it was shrunk
        if (boxCollider != null && colliderShrinkAmount > SHRINK_EPSILON)
        {
             // Check if originalSize is valid before restoring.
             // This is mostly relevant if OnDestroy is called before Awake fully completes,
             // or if the object is destroyed from the editor without playing.
            if (originalSize != Vector2.zero)
            {
                RestoreCollider();
            }
        }
    }
}
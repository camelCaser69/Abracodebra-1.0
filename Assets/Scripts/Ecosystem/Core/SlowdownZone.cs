using UnityEngine;
using System.Collections.Generic;

public class SlowdownZone : MonoBehaviour
{
    [Header("Slowdown Settings")]
    [Tooltip("How much to multiply movement speed by (0.5 = half speed)")]
    [Range(0.1f, 1.0f)]
    public float speedMultiplier = 0.5f;
    
    [Header("Collider Adjustment")]
    [Tooltip("How much to shrink the collider from its edges (in units)")]
    [Range(0f, 1f)]
    public float colliderShrinkAmount = 0.2f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;
    
    // Keep track of entities currently affected
    private Dictionary<int, AnimalController> affectedAnimals = new Dictionary<int, AnimalController>();
    private Dictionary<int, GardenerController> affectedPlayers = new Dictionary<int, GardenerController>();
    
    // Original collider properties (for shrinking)
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private BoxCollider2D boxCollider;
    
    private void Awake()
    {
        // Get the collider and ensure it's a trigger
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
            Debug.Log($"SlowdownZone on '{gameObject.name}' automatically enabled isTrigger on its collider.");
        }
        
        // Store original size for BoxCollider2D
        boxCollider = col as BoxCollider2D;
        if (boxCollider != null)
        {
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;
            
            // Apply shrinking
            if (colliderShrinkAmount > 0)
            {
                ShrinkCollider();
            }
        }
        else
        {
            // If it's not a BoxCollider2D, log a warning
            Debug.LogWarning($"SlowdownZone on '{gameObject.name}' is using a collider type other than BoxCollider2D. " +
                             "Collider shrinking will not work.", gameObject);
        }
    }
    
    private void ShrinkCollider()
    {
        if (boxCollider == null) return;
        
        // Calculate new size by subtracting shrink amount from both dimensions
        Vector2 newSize = new Vector2(
            Mathf.Max(0.1f, originalSize.x - (colliderShrinkAmount * 2f)),
            Mathf.Max(0.1f, originalSize.y - (colliderShrinkAmount * 2f))
        );
        
        // Apply the new size
        boxCollider.size = newSize;
        
        if (showDebugMessages)
        {
            Debug.Log($"SlowdownZone: Shrunk collider from {originalSize} to {newSize}");
        }
    }
    
    private void OnValidate()
    {
        // Update collider size when values change in inspector
        if (Application.isPlaying && boxCollider != null)
        {
            ShrinkCollider();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's an animal
        AnimalController animal = other.GetComponent<AnimalController>();
        if (animal != null)
        {
            // Add to affected list
            int id = animal.GetInstanceID();
            affectedAnimals[id] = animal;
            
            // Apply slowdown effect
            animal.ApplySpeedMultiplier(speedMultiplier);
            
            if (showDebugMessages)
                Debug.Log($"SlowdownZone: '{animal.name}' entered zone, applied multiplier {speedMultiplier}");
            
            return; // Skip further checks if it's an animal
        }
        
        // Check if it's the player (gardener)
        GardenerController player = other.GetComponent<GardenerController>();
        if (player != null)
        {
            // Add to affected list
            int id = player.GetInstanceID();
            affectedPlayers[id] = player;
            
            // Apply slowdown effect
            player.ApplySpeedMultiplier(speedMultiplier);
            
            if (showDebugMessages)
                Debug.Log($"SlowdownZone: Player '{player.name}' entered zone, applied multiplier {speedMultiplier}");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if it's an animal
        AnimalController animal = other.GetComponent<AnimalController>();
        if (animal != null)
        {
            // Remove from affected list
            int id = animal.GetInstanceID();
            if (affectedAnimals.ContainsKey(id))
            {
                affectedAnimals.Remove(id);
                
                // Remove slowdown effect
                animal.RemoveSpeedMultiplier(speedMultiplier);
                
                if (showDebugMessages)
                    Debug.Log($"SlowdownZone: '{animal.name}' exited zone, removed multiplier");
            }
            
            return; // Skip further checks if it's an animal
        }
        
        // Check if it's the player (gardener)
        GardenerController player = other.GetComponent<GardenerController>();
        if (player != null)
        {
            // Remove from affected list
            int id = player.GetInstanceID();
            if (affectedPlayers.ContainsKey(id))
            {
                affectedPlayers.Remove(id);
                
                // Remove slowdown effect
                player.RemoveSpeedMultiplier(speedMultiplier);
                
                if (showDebugMessages)
                    Debug.Log($"SlowdownZone: Player '{player.name}' exited zone, removed multiplier");
            }
        }
    }
    
    // Clean up when destroyed
    private void OnDestroy()
    {
        // Remove effects from all affected animals
        foreach (var animal in affectedAnimals.Values)
        {
            if (animal != null)
            {
                animal.RemoveSpeedMultiplier(speedMultiplier);
            }
        }
        affectedAnimals.Clear();
        
        // Remove effects from all affected players
        foreach (var player in affectedPlayers.Values)
        {
            if (player != null)
            {
                player.RemoveSpeedMultiplier(speedMultiplier);
            }
        }
        affectedPlayers.Clear();
    }
}
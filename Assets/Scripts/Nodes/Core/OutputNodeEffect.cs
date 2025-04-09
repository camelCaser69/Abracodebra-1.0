// FILE: Assets/Scripts/Nodes/Core/OutputNodeEffect.cs
using UnityEngine;
using System.Collections.Generic; // Required for Dictionary

public class OutputNodeEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab for the projectile to spawn.")]
    public GameObject projectilePrefab;

    [Header("Settings")]
    public Vector2 spawnOffset = Vector2.up;

    // Store reference needed to call ApplyScentDataToObject
    private PlantGrowth parentPlantGrowth;

    void Awake()
    {
        // Get reference to parent PlantGrowth to access ApplyScentDataToObject helper
        parentPlantGrowth = GetComponentInParent<PlantGrowth>();
        if (parentPlantGrowth == null)
        {
            // This is a critical error if scent application is expected
            Debug.LogError($"[{nameof(OutputNodeEffect)}] Could not find parent PlantGrowth component! Scent application will fail.", gameObject);
        }
    }

    /// <summary>
    /// Activated by PlantGrowth during the Mature Execution Cycle.
    /// Spawns a projectile and applies accumulated effects (damage, scent).
    /// </summary>
    /// <param name="damageMultiplier">Contextual damage modifier calculated from the node chain.</param>
    /// <param name="scentRadiusBonuses">Accumulated radius bonuses per ScentDefinition.</param> // <<< UPDATED PARAM TYPE
    /// <param name="scentStrengthBonuses">Accumulated strength bonuses per ScentDefinition.</param> // <<< UPDATED PARAM TYPE
    public void Activate(float damageMultiplier,
                         Dictionary<ScentDefinition, float> scentRadiusBonuses, // <<< UPDATED TYPE
                         Dictionary<ScentDefinition, float> scentStrengthBonuses) // <<< UPDATED TYPE
    {
        // --- Validations ---
        if (projectilePrefab == null) {
            Debug.LogError($"[{nameof(OutputNodeEffect)}] Projectile Prefab not assigned!", gameObject);
            return;
        }
        
         if (parentPlantGrowth == null) { // Check again in case Awake failed silently
              Debug.LogError($"[{nameof(OutputNodeEffect)}] Cannot activate, parent PlantGrowth reference is missing. Scent application will fail.", gameObject);
             // Decide if we should still spawn projectile without scent or just return
             // return; // Option: Abort if scent cannot be applied
         }

        // Debug.Log($"[OutputNodeEffect] Activate called. Damage Multiplier: {damageMultiplier}. Spawning projectile.");

        // --- Spawn Projectile ---
        Vector2 spawnPos = (Vector2)transform.position + spawnOffset;
        GameObject projGO = Instantiate(projectilePrefab, spawnPos, transform.rotation); // Use plant's rotation or aim logic

        // --- Apply Accumulated Scents to Projectile ---
        // Call the public helper method on the parent PlantGrowth instance
        if (parentPlantGrowth != null) // Check if reference exists before calling
        {
            // Debug.Log($"[{gameObject.name} Activate] Calling ApplyScentDataToObject for {projGO.name}. Passing {scentStrengthsBonuses?.Count ?? 0} scent strength entries."); /////////// here
            
             // Call the public helper with the NEW dictionaries
             parentPlantGrowth.ApplyScentDataToObject(projGO, scentRadiusBonuses, scentStrengthBonuses);
        }
        // -----------------------------------------------

        // --- Initialize Projectile Stats ---
        SpellProjectile spellProj = projGO.GetComponent<SpellProjectile>();
        if(spellProj != null)
        {
            // TODO: Get base damage/speed from effects or projectile definition?
            float baseDamage = 10f; // Example base value
            float baseSpeed = 5f; // Example base value

            float finalDamage = baseDamage * damageMultiplier; // Apply accumulated multiplier
            float finalSpeed = baseSpeed; // TODO: Apply speed modifiers if implemented

            spellProj.Initialize(finalDamage, finalSpeed);
            // Set other properties like friendly fire based on plant context?
        }
        // else { Debug.LogWarning($"[{nameof(OutputNodeEffect)}] Spawned projectile '{projGO.name}' is missing SpellProjectile component.", projGO); }
    }
}
using UnityEngine;

// Example - This component might be on the Plant Prefab or dynamically added/found
public class OutputNodeEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab for the projectile to spawn.")]
    public GameObject projectilePrefab; // Assign your projectile prefab

    [Header("Settings")]
    public Vector2 spawnOffset = Vector2.up; // Offset relative to plant transform

    /// <summary>
    /// Activated by PlantGrowth during the Mature Execution Cycle.
    /// </summary>
    /// <param name="damageMultiplier">Contextual damage modifier calculated from the node chain.</param>
    public void Activate(float damageMultiplier) // Example parameter
    {
        if (projectilePrefab == null) {
            Debug.LogError("[OutputNodeEffect] Projectile Prefab not assigned!", gameObject);
            return;
        }

        Debug.Log($"[OutputNodeEffect] Activate called. Damage Multiplier: {damageMultiplier}. Spawning projectile.");

        // --- Calculate Spawn Position ---
        Vector2 spawnPos = (Vector2)transform.position + spawnOffset;

        // --- Instantiate Projectile ---
        GameObject projGO = Instantiate(projectilePrefab, spawnPos, transform.rotation); // Use plant's rotation or aim logic

        // --- Initialize Projectile (if needed) ---
        SpellProjectile spellProj = projGO.GetComponent<SpellProjectile>();
        if(spellProj != null)
        {
            // TODO: Calculate final damage/speed etc. based on node effects and damageMultiplier
            float finalDamage = 10f * damageMultiplier; // Example calculation
            float speed = 5f; // Get speed from effects?
            spellProj.Initialize(finalDamage, speed);
            // Set other properties like friendly fire based on plant context?
        }
    }
}
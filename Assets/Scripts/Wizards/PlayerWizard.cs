using UnityEngine;

public class PlayerWizard : MonoBehaviour
{
    public float hp = 100f;
    public Transform spellSpawnPoint;  // The point from which spells are cast.
    public GameObject spellProjectilePrefab; // Prefab for the projectile.
    public float projectileSpeed = 10f;

    // Call this when a spell is cast (e.g., from NodeExecutor output).
    public void CastSpell(float damage)
    {
        if (spellProjectilePrefab == null || spellSpawnPoint == null)
            return;

        GameObject projObj = Instantiate(spellProjectilePrefab, spellSpawnPoint.position, Quaternion.identity);
        SpellProjectile projectile = projObj.GetComponent<SpellProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(damage, projectileSpeed);
        }
    }

    // Optionally, implement HP loss etc.
}
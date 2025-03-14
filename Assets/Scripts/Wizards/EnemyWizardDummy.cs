using UnityEngine;

public class EnemyWizardDummy : MonoBehaviour
{
    public float hp = 100f;

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Debug.Log("Enemy defeated!");
            // Add death logic here.
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        SpellProjectile projectile = collision.gameObject.GetComponent<SpellProjectile>();
        if (projectile != null)
        {
            TakeDamage(projectile.damage);
            Destroy(projectile.gameObject);
        }
    }
}
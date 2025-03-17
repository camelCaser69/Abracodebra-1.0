using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Projectile Stats")]
    public float damage;
    public float speed;
    [Tooltip("If true, the projectile is destroyed upon hitting a wizard.")]
    public bool destroyOnHit = true;

    [Header("Burning Effect (optional)")]
    public float burningDamage;   // DPS
    public float burningDuration; // seconds

    public void Initialize(float dmg, float spd)
    {
        damage = dmg;
        speed = spd;
    }

    private void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        WizardController wizard = collision.GetComponent<WizardController>();
        if (wizard != null)
        {
            // If the wizard is an enemy, apply damage.
            if (wizard.isEnemy)
            {
                wizard.TakeDamage(damage);

                // Apply burning effect if present.
                if (burningDamage > 0 && burningDuration > 0)
                {
                    wizard.ApplyStatusEffect(new BurningStatusEffect(burningDuration, burningDamage));
                }
            }
            if (destroyOnHit)
                Destroy(gameObject);
        }
    }
}
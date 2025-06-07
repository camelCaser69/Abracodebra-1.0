using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Projectile Stats")]
    public float damage;
    public float speed;
    [Tooltip("If true, the projectile will be destroyed upon hitting a wizard.")]
    public bool destroyOnHit = true;

    [Header("Burning Effect (optional)")]
    public float burningDamage;   // DPS
    public float burningDuration; // Duration in seconds

    [Header("Friendly Fire")]
    public bool friendlyFire = false;
    public bool casterIsEnemy = false;

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
        
    }
}
using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    public float damage;
    public float speed;

    public void Initialize(float dmg, float spd)
    {
        damage = dmg;
        speed = spd;
    }

    private void Update()
    {
        // Move upward.
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }
}
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum FiringDirection
{
    Up,
    Down
}

public class WizardController : MonoBehaviour
{
    [Header("Wizard Configuration")]
    public bool isEnemy = false;

    [Header("Wizard Stats")]
    public float maxHP = 100f;
    public float currentHP;
    [Tooltip("Base aim spread in degrees; higher values mean greater spread.")]
    public float baseAimSpread = 5f;

    [Header("Firing Settings")]
    [Tooltip("Determines if the wizard fires upward (positive Y) or downward (negative Y).")]
    public FiringDirection firingDirection = FiringDirection.Up;
    public Transform spellSpawnPoint;
    public GameObject spellProjectilePrefab;
    public float projectileSpeed = 10f;

    [Header("UI")]
    public TMP_Text hpText;

    private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHPUI();
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
        UpdateHPUI();

        if (currentHP <= 0)
        {
            Debug.Log($"{gameObject.name} has been defeated.");
            // Additional death logic.
        }
    }

    private void UpdateHPUI()
    {
        if (hpText != null)
            hpText.text = $"HP: {Mathf.Floor(currentHP)}/{maxHP}";
    }

    /// <summary>
    /// Casts a spell by instantiating a projectile. The projectile's rotation is set based on firing direction
    /// and random deviation within ±finalAimSpread. Also, burning parameters and piercing are passed along.
    /// </summary>
    public void CastSpell(float finalDamage, float finalAimSpread, float burningDamage, float burningDuration, bool piercing)
    {
        if (isEnemy)
            return; // Enemies do not cast spells via node chain.

        if (spellProjectilePrefab == null || spellSpawnPoint == null)
        {
            Debug.LogWarning("Spell projectile prefab or spawn point not set.");
            return;
        }

        // Base rotation: for Up, identity; for Down, flip 180°.
        Quaternion baseRotation = (firingDirection == FiringDirection.Up) ? Quaternion.identity : Quaternion.Euler(0, 0, 180);

        // Apply random deviation within ±finalAimSpread.
        float deviation = Random.Range(-finalAimSpread, finalAimSpread);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(0, 0, deviation);

        GameObject projObj = Instantiate(spellProjectilePrefab, spellSpawnPoint.position, finalRotation);
        SpellProjectile projectile = projObj.GetComponent<SpellProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(finalDamage, projectileSpeed);
            projectile.burningDamage = burningDamage;
            projectile.burningDuration = burningDuration;
            projectile.destroyOnHit = !piercing; // If piercing is true, do not destroy the projectile.
        }
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            activeStatusEffects[i].UpdateEffect(this, delta);
            if (activeStatusEffects[i].IsExpired())
                activeStatusEffects.RemoveAt(i);
        }
    }

    public void ApplyStatusEffect(StatusEffect effect)
    {
        activeStatusEffects.Add(effect);
    }
}

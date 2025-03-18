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
    [Tooltip("Base aim spread in degrees; higher values mean a wider spread.")]
    public float baseAimSpread = 5f;

    [Header("Firing Settings")]
    [Tooltip("Determines if the wizard fires upward (default) or downward.")]
    public FiringDirection firingDirection = FiringDirection.Up;
    [Tooltip("The transform where spells are spawned. Its original Y value is used as reference.")]
    public Transform spellSpawnPoint;
    [Tooltip("Projectile prefab to be cast.")]
    public GameObject spellProjectilePrefab;
    public float projectileSpeed = 10f;
    [Tooltip("Enable friendly fire (projectile can damage friendlies)")]
    public bool friendlyFireEnabled = false;

    [Header("UI")]
    public TMP_Text hpText;

    private float originalSpawnY = 0f;
    public List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHPUI();

#if UNITY_EDITOR
        // Store the original Y value if not already stored.
        if (spellSpawnPoint != null && originalSpawnY == 0f)
            originalSpawnY = spellSpawnPoint.localPosition.y;
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // When the firing direction changes, update spellSpawnPoint's Y coordinate.
        if (spellSpawnPoint != null)
        {
            // Ensure we have stored the original Y value.
            if (originalSpawnY == 0f)
                originalSpawnY = spellSpawnPoint.localPosition.y;

            Vector2 pos = spellSpawnPoint.localPosition;
            if (firingDirection == FiringDirection.Down)
                pos.y = -Mathf.Abs(originalSpawnY);
            else
                pos.y = Mathf.Abs(originalSpawnY);
            spellSpawnPoint.localPosition = pos;
        }
    }
#endif

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP < 0)
            currentHP = 0;
        UpdateHPUI();
        if (currentHP <= 0)
        {
            Debug.Log($"{gameObject.name} has been defeated.");
            // Add additional death logic if needed.
        }
    }

    private void UpdateHPUI()
    {
        if (hpText != null)
            hpText.text = $"HP: {Mathf.Floor(currentHP)}/{maxHP}";
    }

    /// <summary>
    /// Casts a spell. The projectile is spawned at spellSpawnPoint.
    /// The projectile's rotation is based on firingDirection and a random deviation within ±finalAimSpread.
    /// Also passes burning and friendly-fire parameters.
    /// </summary>
    public void CastSpell(float finalDamage, float finalAimSpread, float burningDamage, float burningDuration, bool piercing, bool friendlyFire)
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
        float deviation = Random.Range(-finalAimSpread, finalAimSpread);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(0, 0, deviation);

        GameObject projObj = Instantiate(spellProjectilePrefab, spellSpawnPoint.position, finalRotation);
        SpellProjectile projectile = projObj.GetComponent<SpellProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(finalDamage, projectileSpeed);
            projectile.burningDamage = burningDamage;
            projectile.burningDuration = burningDuration;
            projectile.destroyOnHit = !piercing; // If piercing is true, do not destroy.
            projectile.friendlyFire = friendlyFire;
            projectile.casterIsEnemy = isEnemy;
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

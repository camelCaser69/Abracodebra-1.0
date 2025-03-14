using UnityEngine;

public class WizardHP : MonoBehaviour
{
    public float maxHP = 100f;
    private float currentHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Debug.Log($"{gameObject.name} has been defeated.");
            // Add further death logic.
        }
    }

    public float GetCurrentHP() => currentHP;
}
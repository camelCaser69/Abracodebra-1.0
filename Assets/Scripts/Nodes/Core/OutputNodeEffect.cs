using UnityEngine;

public class OutputNodeEffect : MonoBehaviour
{
    public void Activate(float finalDamage)
    {
        PlayerWizard player = GameObject.FindFirstObjectByType<PlayerWizard>();
        if (player != null)
        {
            player.CastSpell(finalDamage);
            Debug.Log($"[OutputNodeEffect] Spell cast with damage: {finalDamage}");
        }
        else
        {
            Debug.LogWarning("[OutputNodeEffect] PlayerWizard not found!");
        }
    }
}
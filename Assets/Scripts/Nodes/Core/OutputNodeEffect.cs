using UnityEngine;
using System.Linq;

public class OutputNodeEffect : MonoBehaviour
{
    /// <summary>
    /// Activates the output node effect, causing the player wizard to cast a spell.
    /// </summary>
    /// <param name="finalDamage">Final damage from node chain.</param>
    /// <param name="aimSpreadModifier">Accumulated aim spread modifier.</param>
    /// <param name="burningDamage">Accumulated burning DPS.</param>
    /// <param name="burningDuration">Accumulated burning duration in seconds.</param>
    /// <param name="piercing">True if piercing effect is active (projectile will not be destroyed on hit).</param>
    public void Activate(float finalDamage, float aimSpreadModifier, float burningDamage, float burningDuration, bool piercing)
    {
        WizardController wizard = GameObject.FindObjectsOfType<WizardController>()
            .FirstOrDefault(w => !w.isEnemy);

        if (wizard != null)
        {
            // Calculate final aim spread: baseAimSpread + modifier, clamped between 0 and 180.
            float finalAimSpread = Mathf.Clamp(wizard.baseAimSpread + aimSpreadModifier, 0f, 180f);
            wizard.CastSpell(finalDamage, finalAimSpread, burningDamage, burningDuration, piercing);
            Debug.Log($"[OutputNodeEffect] Spell cast with damage: {finalDamage}, aim spread: {finalAimSpread}, burning: {burningDamage} DPS for {burningDuration} s, piercing: {piercing}");
        }
        else
        {
            Debug.LogWarning("[OutputNodeEffect] No player WizardController found!");
        }
    }
}
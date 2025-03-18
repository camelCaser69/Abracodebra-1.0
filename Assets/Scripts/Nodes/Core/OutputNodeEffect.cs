using UnityEngine;
using System.Linq;

public class OutputNodeEffect : MonoBehaviour
{
    public void Activate(float finalDamage, float aimSpreadModifier, float burningDamage, float burningDuration, bool piercing, bool friendlyFire)
    {
        // Use FindObjectsByType
        WizardController[] allWizards = Object.FindObjectsByType<WizardController>(FindObjectsSortMode.None);
        WizardController wizard = allWizards.FirstOrDefault(w => !w.isEnemy);

        if (wizard != null)
        {
            float finalAimSpread = Mathf.Clamp(wizard.baseAimSpread + aimSpreadModifier, 0f, 180f);
            wizard.CastSpell(finalDamage, finalAimSpread, burningDamage, burningDuration, piercing, friendlyFire);
            Debug.Log($"[OutputNodeEffect] Spell cast with damage: {finalDamage}, final aim spread: {finalAimSpread}, burning: {burningDamage} DPS for {burningDuration}s, piercing: {piercing}, friendlyFire: {friendlyFire}");
        }
        else
        {
            Debug.LogWarning("[OutputNodeEffect] No player WizardController found!");
        }
    }
}
using UnityEngine;
using System.Linq;

/// <summary>
/// Attach this to the NodeView prefab if the node has Output effect,
/// or have BFS call it directly. 
/// It calls WizardController to spawn a projectile/spell.
/// </summary>
public class OutputNodeEffect : MonoBehaviour
{
    /// <summary>
    /// This method is invoked by NodeExecutor when BFS processes an 'Output' node.
    /// You can pass any parameters (damage, aim, etc.) or just do a test projectile.
    /// </summary>
    public void Activate()
    {
        Debug.Log("[OutputNodeEffect] Activate() called. Spawning projectile or calling wizard cast.");

        // For example, find the local (player) wizard and cast a test projectile:
        WizardController playerWiz = FindObjectsOfType<WizardController>()
            .FirstOrDefault(w => !w.isEnemy);

        if (playerWiz)
        {
            // Just an example. The real logic might pass finalDamage, aimSpread, etc.
            playerWiz.CastSpell(finalDamage: 10f, finalAimSpread: 5f,
                burningDamage: 0f, burningDuration: 0f,
                piercing: false, friendlyFire: false);
            Debug.Log("[OutputNodeEffect] Called player's CastSpell with sample values.");
        }
        else
        {
            Debug.LogWarning("[OutputNodeEffect] No friendly wizard found in scene.");
        }
    }
}
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

        

        
        
    }
}
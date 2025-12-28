using UnityEngine;
using System.Collections;
using WegoSystem;

namespace WegoSystem
{
    public class InitializationManager : SingletonMonoBehaviour<InitializationManager>
    {
        [SerializeField] private GameEvent onCoreSystemsInitialized;
        [SerializeField] private GameEvent onGameManagersInitialized;
        [SerializeField] private GameEvent onGameplaySystemsInitialized;

        private IEnumerator Start()
        {
            Debug.Log("[InitializationManager] Starting initialization sequence...");

            Debug.Log("[InitializationManager] Phase 1: Initializing Core Systems...");
            onCoreSystemsInitialized.Raise();
            yield return null;

            Debug.Log("[InitializationManager] Phase 2: Initializing Game Managers...");
            onGameManagersInitialized.Raise();
            yield return null;

            // --- THIS BLOCK HAS BEEN REMOVED ---
            // The new GameUIManager initializes itself via its own Awake/Start.
            // We no longer need to manually initialize it from here.
            // ------------------------------------
            
            if (EnvironmentalStatusEffectSystem.Instance != null)
            {
                Debug.Log("[InitializationManager] Initializing EnvironmentalStatusEffectSystem...");
                EnvironmentalStatusEffectSystem.Instance.Initialize();
            }
            else
            {
                Debug.LogWarning("[InitializationManager] EnvironmentalStatusEffectSystem instance not found. Tile-based status effects will not function.");
            }

            Debug.Log("[InitializationManager] Phase 3: Initializing Gameplay Systems & UI...");
            onGameplaySystemsInitialized.Raise();
            yield return null;

            Debug.Log("[InitializationManager] All systems initialized successfully.");
        }
    }
}
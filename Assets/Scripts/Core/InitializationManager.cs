using System.Collections;
using UnityEngine;
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

            if (UIManager.Instance != null)
            {
                Debug.Log("[InitializationManager] Initializing UIManager...");
                UIManager.Instance.Initialize();
            }
            else
            {
                Debug.LogError("[InitializationManager] UIManager instance not found! UI will not be initialized.");
            }

            // --- FIX: Initialize the EnvironmentalStatusEffectSystem here ---
            if (EnvironmentalStatusEffectSystem.Instance != null)
            {
                Debug.Log("[InitializationManager] Initializing EnvironmentalStatusEffectSystem...");
                EnvironmentalStatusEffectSystem.Instance.Initialize();
            }
            else
            {
                Debug.LogWarning("[InitializationManager] EnvironmentalStatusEffectSystem instance not found. Tile-based status effects will not function.");
            }
            // --- END OF FIX ---

            Debug.Log("[InitializationManager] Phase 3: Initializing Gameplay Systems & UI...");
            onGameplaySystemsInitialized.Raise();
            yield return null;

            Debug.Log("[InitializationManager] All systems initialized successfully.");
        }
    }
}
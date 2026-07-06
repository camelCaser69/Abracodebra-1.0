// Assets/Scripts/Core/InitializationManager.cs
using System.Collections;
using UnityEngine;

namespace WegoSystem {
    public class InitializationManager : SingletonMonoBehaviour<InitializationManager> {
        [SerializeField] GameEvent onCoreSystemsInitialized;
        [SerializeField] GameEvent onGameManagersInitialized;
        [SerializeField] GameEvent onGameplaySystemsInitialized;

        // A5: deterministic "everything is up" signal for late-binding systems.
        public static bool IsReady { get; private set; }
        public static event System.Action OnReady;

        IEnumerator Start() {
            IsReady = false;
            Debug.Log("[InitializationManager] Starting initialization sequence...");

            Debug.Log("[InitializationManager] Phase 1: Initializing Core Systems...");
            onCoreSystemsInitialized.Raise();
            yield return null;

            Debug.Log("[InitializationManager] Phase 2: Initializing Game Managers...");
            onGameManagersInitialized.Raise();
            yield return null;

            if (EnvironmentalStatusEffectSystem.Instance != null) {
                Debug.Log("[InitializationManager] Initializing EnvironmentalStatusEffectSystem...");
                EnvironmentalStatusEffectSystem.Instance.Initialize();
            }
            else {
                Debug.LogWarning("[InitializationManager] EnvironmentalStatusEffectSystem instance not found. Tile-based status effects will not function.");
            }

            Debug.Log("[InitializationManager] Phase 3: Initializing Gameplay Systems & UI...");
            onGameplaySystemsInitialized.Raise();
            yield return null;

            IsReady = true;
            OnReady?.Invoke();
            Debug.Log("[InitializationManager] All systems initialized successfully.");
        }
    }
}

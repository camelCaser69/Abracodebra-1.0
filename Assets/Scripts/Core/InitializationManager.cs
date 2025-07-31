// Assets/Scripts/Core/InitializationManager.cs
using System.Collections;
using UnityEngine;
using WegoSystem;

public class InitializationManager : SingletonMonoBehaviour<InitializationManager>
{
    [Header("Initialization Events")]
    [SerializeField] private GameEvent onCoreSystemsInitialized;
    [SerializeField] private GameEvent onGameManagersInitialized;
    [SerializeField] private GameEvent onGameplaySystemsInitialized;

    private IEnumerator Start()
    {
        Debug.Log("[InitializationManager] Starting initialization sequence...");

        // Phase 1: Core Systems (Lowest level, no dependencies on other managers)
        // e.g., TickManager, TileInteractionManager
        Debug.Log("[InitializationManager] Phase 1: Initializing Core Systems...");
        onCoreSystemsInitialized.Raise();
        yield return null; // Wait one frame

        // Phase 2: Game Managers (Depend on Core Systems)
        // e.g., GridPositionManager, RunManager, WaveManager, EcosystemManager
        Debug.Log("[InitializationManager] Phase 2: Initializing Game Managers...");
        onGameManagersInitialized.Raise();
        yield return null; // Wait one frame

        // Phase 3: Gameplay & UI (Depend on Game Managers)
        // e.g., FaunaManager, UIManager, Player controllers, etc.
        Debug.Log("[InitializationManager] Phase 3: Initializing Gameplay Systems & UI...");
        onGameplaySystemsInitialized.Raise();
        yield return null; // Wait one frame

        Debug.Log("[InitializationManager] All systems initialized successfully.");
    }
}
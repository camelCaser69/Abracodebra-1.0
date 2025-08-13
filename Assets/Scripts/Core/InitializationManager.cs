using UnityEngine;
using System.Collections;
using WegoSystem;

public class InitializationManager : SingletonMonoBehaviour<InitializationManager>
{
    [SerializeField] private GameEvent onCoreSystemsInitialized;
    [SerializeField] private GameEvent onGameManagersInitialized;
    [SerializeField] private GameEvent onGameplaySystemsInitialized;

    IEnumerator Start()
    {
        Debug.Log("[InitializationManager] Starting initialization sequence...");

        Debug.Log("[InitializationManager] Phase 1: Initializing Core Systems...");
        onCoreSystemsInitialized.Raise();
        yield return null;

        Debug.Log("[InitializationManager] Phase 2: Initializing Game Managers...");
        onGameManagersInitialized.Raise();
        yield return null;

        // FIX: Explicitly initialize the UIManager here to ensure it subscribes to events.
        if (UIManager.Instance != null)
        {
            Debug.Log("[InitializationManager] Initializing UIManager...");
            UIManager.Instance.Initialize();
        }
        else
        {
            Debug.LogError("[InitializationManager] UIManager instance not found! UI will not be initialized.");
        }

        Debug.Log("[InitializationManager] Phase 3: Initializing Gameplay Systems & UI...");
        onGameplaySystemsInitialized.Raise();
        yield return null;

        Debug.Log("[InitializationManager] All systems initialized successfully.");
    }
}
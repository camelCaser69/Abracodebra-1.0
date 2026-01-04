// File: Assets/Scripts/Core/GameBootstrap.cs
using UnityEngine;
using Abracodabra.Genes.Services;

/// <summary>
/// Central bootstrap for all game services.
/// Runs before other scripts via DefaultExecutionOrder.
/// 
/// Initialization order:
/// 1. GameBootstrap (-100): Core services (EventBus, Random, EffectPool)
/// 2. GeneLibraryLoader (-90): GeneLibrary registration
/// 3. Other systems (0+): Normal MonoBehaviour order
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameBootstrap : MonoBehaviour
{
    private static bool hasInitialized = false;

    void Awake()
    {
        if (hasInitialized)
        {
            Debug.Log("[GameBootstrap] Already initialized, skipping.");
            return;
        }

        Debug.Log("[GameBootstrap] === INITIALIZATION START ===");

        // Phase 1: Core gene services (EventBus, Random)
        InitializeCoreServices();

        // Phase 2: Effect pool (requires singleton to exist)
        RegisterEffectPool();

        hasInitialized = true;
        Debug.Log("[GameBootstrap] === INITIALIZATION COMPLETE ===");
    }

    private void InitializeCoreServices()
    {
        if (!GeneServices.IsInitialized)
        {
            GeneServices.Initialize();
            Debug.Log("[GameBootstrap] Phase 1: GeneServices initialized (EventBus, Random)");
        }
        else
        {
            Debug.Log("[GameBootstrap] Phase 1: GeneServices already initialized");
        }
    }

    private void RegisterEffectPool()
    {
        if (GeneEffectPool.Instance != null)
        {
            GeneServices.Register<IGeneEffectPool>(GeneEffectPool.Instance);
            Debug.Log("[GameBootstrap] Phase 2: GeneEffectPool registered");
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] Phase 2: GeneEffectPool not found - effects may not work");
        }
    }

    /// <summary>
    /// Called when the application quits or domain reloads (editor).
    /// Resets static state for clean restarts.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasInitialized = false;
        GeneServices.Reset();
    }
}
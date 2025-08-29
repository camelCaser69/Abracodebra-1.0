using UnityEngine;
using Abracodabra.Genes.Services;
using WegoSystem;

public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Initialize core non-MonoBehaviour services
        InitializeServices();

        // Ensure the MonoBehaviour-based GeneEffectPool exists and is registered
        if (GeneEffectPool.Instance != null)
        {
            // This is the crucial line that was missing:
            // We tell GeneServices about the active GeneEffectPool instance.
            GeneServices.Register<IGeneEffectPool>(GeneEffectPool.Instance);
        }
        else
        {
            Debug.LogError("[GameInitializer] GeneEffectPool instance could not be created or found.");
        }
    }

    private static void InitializeServices()
    {
        // This initializes the EventBus and DeterministicRandom
        GeneServices.Initialize();
    }
}
using UnityEngine;
using Abracodabra.Genes.Services;
using WegoSystem;

// Note: WegoSystem namespace is removed as this script now resides in the global namespace.
// If you intend for it to be in WegoSystem, add the namespace back.
public class GameInitializer : MonoBehaviour
{
    // The MapConfiguration reference is no longer needed here.
    // [Header("Core Configuration")]
    // [Tooltip("Assign the project's MapConfiguration asset here. It's required for multiple systems.")]
    // [SerializeField] private MapConfiguration mapConfig;

    private void Awake()
    {
        // REMOVED: The PixelGridSnapper no longer needs to be initialized here.
        // It now pulls its data directly from the ResolutionManager singleton when needed.
        // PixelGridSnapper.Initialize(mapConfig);

        // Initialize core services
        InitializeServices();
        
        // Ensure the singleton effect pool is created
        if (GeneEffectPool.Instance == null)
        {
            Debug.LogError("[GameInitializer] GeneEffectPool instance could not be created.");
        }
    }

    private static void InitializeServices()
    {
        // This remains from your original script
        GeneServices.Initialize();
    }
}
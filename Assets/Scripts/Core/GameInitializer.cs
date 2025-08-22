using UnityEngine;
using Abracodabra.Genes.Services;
using WegoSystem;

namespace WegoSystem
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Core Configuration")]
        [Tooltip("Assign the project's MapConfiguration asset here. It's required for multiple systems.")]
        [SerializeField] private MapConfiguration mapConfig;

        private void Awake()
        {
            if (mapConfig == null)
            {
                Debug.LogError("[GameInitializer] CRITICAL: MapConfiguration is not assigned! Many systems will fail to initialize.", this);
                return;
            }

            // Initialize static helpers that depend on configuration
            PixelGridSnapper.Initialize(mapConfig);

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
}
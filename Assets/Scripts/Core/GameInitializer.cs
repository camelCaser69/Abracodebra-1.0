// REWORKED FILE: Assets/Scripts/Core/GameInitializer.cs
using UnityEngine;
using Abracodabra.Genes.Services;

public class GameInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeServices()
    {
        // This now ONLY initializes the non-scene-dependent services.
        GeneServices.Initialize();
    }

    void Awake()
    {
        // This ensures the MonoBehaviour singletons are created and ready.
        if (GeneEffectPool.Instance == null)
        {
            Debug.LogError("GeneEffectPool instance could not be created.");
        }
    }
}
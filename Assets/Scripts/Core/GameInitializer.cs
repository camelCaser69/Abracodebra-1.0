// REWORKED FILE: Assets/Scripts/Core/GameInitializer.cs
using UnityEngine;
using Abracodabra.Genes.Services;

public class GameInitializer : MonoBehaviour
{
    // FIX: This method is now static and will be called by the Unity runtime
    // automatically before any scene loads, guaranteeing services are ready.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeServices()
    {
        GeneServices.Initialize();

        if (Abracodabra.Genes.GeneLibrary.Instance == null)
        {
            Debug.LogError("FATAL: Gene Library could not be loaded! Ensure a 'GeneLibrary.asset' exists in a 'Resources' folder.");
        }
    }

    void Awake()
    {
        // The GameObject in the scene can still be used to host singleton MonoBehaviours.
        // Ensure the GeneEffectPool singleton instance is created.
        if (GeneEffectPool.Instance == null)
        {
            Debug.LogError("GeneEffectPool instance could not be created.");
        }
    }
}
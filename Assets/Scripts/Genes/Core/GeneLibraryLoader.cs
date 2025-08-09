// REWORKED FILE: Assets/Scripts/Core/GeneLibraryLoader.cs
using UnityEngine;
using Abracodabra.Genes;
using Abracodabra.Genes.Services; // FIX: Added using statement for services

/// <summary>
/// A robust component to load, initialize, and register the global GeneLibrary instance.
/// Place this on a persistent manager GameObject in your main scene.
/// </summary>
public class GeneLibraryLoader : MonoBehaviour
{
    [Header("Asset Reference")]
    [SerializeField]
    private GeneLibrary geneLibraryAsset;

    void Awake()
    {
        if (geneLibraryAsset == null)
        {
            Debug.LogError("CRITICAL: The GeneLibrary Asset is not assigned in the GeneLibraryLoader component! The gene system will not work.", this);
            return;
        }

        // 1. Set the static instance for direct access (e.g., GeneLibrary.Instance)
        geneLibraryAsset.SetActiveInstance();

        // 2. Register the instance with the service locator for interface-based access.
        // This is crucial for decoupling and testability.
        GeneServices.Register<IGeneLibrary>(geneLibraryAsset);

        Debug.Log("GeneLibrary instance was successfully set, initialized, and registered by GeneLibraryLoader.");
    }
}
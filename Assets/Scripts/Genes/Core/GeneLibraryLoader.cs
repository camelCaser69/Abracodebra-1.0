// REWORKED FILE: Assets/Scripts/Core/GeneLibraryLoader.cs
using UnityEngine;
using Abracodabra.Genes;
using Abracodabra.Genes.Services;

/// <summary>
/// IMPORTANT: To prevent race conditions, this script's execution order should be set to a high priority
/// (a low negative number, e.g., -100) in Project Settings -> Script Execution Order.
/// This ensures the GeneLibrary is initialized and registered before any other script tries to access it.
/// </summary>
public class GeneLibraryLoader : MonoBehaviour
{
    [SerializeField]
    private GeneLibrary geneLibraryAsset;

    private void Awake()
    {
        // This MUST run before any other service getter.
        GeneServices.Initialize();

        if (geneLibraryAsset == null)
        {
            Debug.LogError("CRITICAL: The GeneLibrary Asset is not assigned in the GeneLibraryLoader component! The gene system will not work.", this);
            return;
        }

        // Sets the static instance and builds the internal dictionaries
        geneLibraryAsset.SetActiveInstance();

        // Registers the initialized instance with the service locator
        GeneServices.Register<IGeneLibrary>(geneLibraryAsset);

        Debug.Log("GeneLibrary instance was successfully set, initialized, and registered by GeneLibraryLoader.");
    }
}
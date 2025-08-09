// REWORKED FILE: Assets/Scripts/Core/GeneLibraryLoader.cs
using UnityEngine;
using Abracodabra.Genes;
using Abracodabra.Genes.Services;

public class GeneLibraryLoader : MonoBehaviour
{
    [SerializeField] GeneLibrary geneLibraryAsset;

    void Awake()
    {
        // Initialize services first
        GeneServices.Initialize();
        
        if (geneLibraryAsset == null)
        {
            Debug.LogError("CRITICAL: The GeneLibrary Asset is not assigned in the GeneLibraryLoader component! The gene system will not work.", this);
            return;
        }

        geneLibraryAsset.SetActiveInstance();

        // Register the library with the service system
        GeneServices.Register<IGeneLibrary>(geneLibraryAsset);

        Debug.Log("GeneLibrary instance was successfully set, initialized, and registered by GeneLibraryLoader.");
    }
}
// File: Assets/Scripts/Genes/Core/GeneLibraryLoader.cs
using UnityEngine;
using Abracodabra.Genes;
using Abracodabra.Genes.Services;

/// <summary>
/// Loads and registers the GeneLibrary asset.
/// Runs after GameBootstrap (-100) but before normal scripts.
/// 
/// Requires: GeneLibrary asset assigned in Inspector.
/// </summary>
[DefaultExecutionOrder(-90)]
public class GeneLibraryLoader : MonoBehaviour
{
    [SerializeField]
    private GeneLibrary geneLibraryAsset;

    void Awake()
    {
        if (geneLibraryAsset == null)
        {
            Debug.LogError("[GeneLibraryLoader] CRITICAL: GeneLibrary asset not assigned! Gene system will not work.", this);
            return;
        }

        // Ensure core services are ready (defensive, should already be done by GameBootstrap)
        if (!GeneServices.IsInitialized)
        {
            Debug.LogWarning("[GeneLibraryLoader] GeneServices not initialized - initializing now. Check script execution order.");
            GeneServices.Initialize();
        }

        // Set up the library singleton and build lookups
        geneLibraryAsset.SetActiveInstance();

        // Register with service locator
        GeneServices.Register<IGeneLibrary>(geneLibraryAsset);

        Debug.Log($"[GeneLibraryLoader] GeneLibrary '{geneLibraryAsset.name}' registered successfully.");
    }
}
using UnityEngine;
using Abracodabra.Genes;
using Abracodabra.Genes.Services;

public class GeneLibraryLoader : MonoBehaviour
{
    [SerializeField]
    private GeneLibrary geneLibraryAsset;

    void Awake()
    {
        // The GeneServices.Initialize() call is no longer needed here,
        // as it's handled by the new GeneSystemInitializer script.

        if (geneLibraryAsset == null)
        {
            Debug.LogError("CRITICAL: The GeneLibrary Asset is not assigned in the GeneLibraryLoader component! The gene system will not work.", this);
            return;
        }

        geneLibraryAsset.SetActiveInstance();

        GeneServices.Register<IGeneLibrary>(geneLibraryAsset);

        Debug.Log("GeneLibrary instance was successfully set, initialized, and registered by GeneLibraryLoader.");
    }
}
using UnityEngine;
using Abracodabra.Genes.Services;

/// <summary>
/// This script ensures that the core gene services are initialized before any other script needs them.
/// It uses a very low execution order number to run first.
/// Place this on a persistent GameObject in your startup scene.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GeneSystemInitializer : MonoBehaviour
{
    void Awake()
    {
        // This will now be the one and only place where GeneServices are explicitly initialized.
        if (!GeneServices.IsInitialized)
        {
            GeneServices.Initialize();
        }
    }
}
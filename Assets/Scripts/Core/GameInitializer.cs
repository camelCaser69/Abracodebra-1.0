// File: Assets/Scripts/Core/GameInitializer.cs
using UnityEngine;
using Abracodabra.Genes.Services;

/// <summary>
/// A simple script that runs at the very start of the game to initialize
/// critical systems, like the Gene Services.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    void Awake()
    {
        // Initialize gene services before anything else can use them
        GeneServices.Initialize();

        // Verify the GeneLibrary asset is available in Resources
        if (Abracodabra.Genes.GeneLibrary.Instance == null)
        {
            Debug.LogError("FATAL: Gene Library could not be loaded! Ensure a 'GeneLibrary.asset' exists in a 'Resources' folder.", this);
        }
    }
}
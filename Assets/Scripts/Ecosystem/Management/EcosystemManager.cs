// FILE: Assets/Scripts/Ecosystem/Core/EcosystemManager.cs
using UnityEngine;

public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager Instance { get; private set; }

    [Header("Parent Transforms")]
    public Transform animalParent;
    public Transform plantParent;

    [Header("Libraries")]
    [Tooltip("Reference to the Scent Library asset.")]
    public ScentLibrary scentLibrary; // <<< ADDED

    [Header("Sorting Options")]
    public bool sortAnimalsBySpecies = true;
    public bool sortPlantsBySpecies = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Validate Library Reference
        if (scentLibrary == null)
        {
            Debug.LogWarning($"[{nameof(EcosystemManager)}] Scent Library not assigned! Scent effects will not work.", this);
        }
    }
}
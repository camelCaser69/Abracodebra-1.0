// Assets/Scripts/Ecosystem/Management/EcosystemManager.cs
using UnityEngine;
using WegoSystem;

public class EcosystemManager : SingletonMonoBehaviour<EcosystemManager>
{
    public Transform animalParent;
    public Transform plantParent;

    public ScentLibrary scentLibrary;

    public bool sortAnimalsBySpecies = true;
    public bool sortPlantsBySpecies = true;
    
    protected override void OnAwake()
    {
        if (scentLibrary == null)
        {
            Debug.LogWarning($"[{nameof(EcosystemManager)}] Scent Library not assigned! Scent effects will not work.", this);
        }
    }
}
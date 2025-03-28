using UnityEngine;

public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager Instance { get; private set; }

    [Header("Parent Transforms")]
    public Transform animalParent;   // e.g., an empty GameObject "SpawnedEcosystem/Animals"
    public Transform plantParent;    // e.g., an empty GameObject "SpawnedEcosystem/Plants"

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
    }
}
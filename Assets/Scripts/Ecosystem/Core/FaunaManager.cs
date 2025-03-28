using UnityEngine;

public class FaunaManager : MonoBehaviour
{
    [Header("Animal Definitions (for testing)")]
    public AnimalDefinition bunnyDefinition; 

    [Header("Spawn Settings")]
    public Vector2 bunnySpawnPosition = new Vector2(2, 2);

    private void Start()
    {
        SpawnAnimal(bunnyDefinition, bunnySpawnPosition);
    }

    public GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogWarning("[FaunaManager] Invalid definition or prefab!");
            return null;
        }

        GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity);

        // Use EcosystemManager to assign parent
        if (EcosystemManager.Instance != null && EcosystemManager.Instance.animalParent != null)
        {
            if (EcosystemManager.Instance.sortAnimalsBySpecies)
            {
                Transform speciesParent = EcosystemManager.Instance.animalParent.Find(definition.animalName);
                if (speciesParent == null)
                {
                    GameObject subParent = new GameObject(definition.animalName);
                    subParent.transform.SetParent(EcosystemManager.Instance.animalParent);
                    speciesParent = subParent.transform;
                }
                animalObj.transform.SetParent(speciesParent);
            }
            else
            {
                animalObj.transform.SetParent(EcosystemManager.Instance.animalParent);
            }
        }

        AnimalController controller = animalObj.GetComponent<AnimalController>();
        if (!controller)
        {
            Debug.LogWarning("[FaunaManager] The prefab is missing an AnimalController. Adding one dynamically.");
            controller = animalObj.AddComponent<AnimalController>();
        }
        controller.Initialize(definition);
        return animalObj;
    }
}
using UnityEngine;
using System.Collections.Generic;

public class FaunaManager : MonoBehaviour
{
    [Header("Animal Spawn Settings")]
    public List<AnimalSpawnData> animalsToSpawn;
    public float globalSpawnCooldown = 5f;
    public float spawnRadius = 3f;
    public bool continuousSpawn = true;
    public Vector2 spawnCenter = Vector2.zero;

    [Header("Ecosystem Parent Settings")]
    public Transform ecosystemParent; // e.g., "SpawnedEcosystem/Animals"

    [Header("Global Movement Bounds for Animals")]
    public Vector2 animalMinBounds = new Vector2(-10f, -5f);
    public Vector2 animalMaxBounds = new Vector2(10f, 5f);

    private float spawnTimer = 0f;

    private void Start()
    {
        foreach (var spawnData in animalsToSpawn)
        {
            spawnData.spawnTimer = 0f;
        }
        SpawnInitialAnimals();
    }

    private void Update()
    {
        if (continuousSpawn)
        {
            foreach (var spawnData in animalsToSpawn)
            {
                if (spawnData.spawnRateMultiplier <= 0f)
                    continue;

                spawnData.spawnTimer -= Time.deltaTime;
                if (spawnData.spawnTimer <= 0f)
                {
                    float effectiveCooldown = globalSpawnCooldown / spawnData.spawnRateMultiplier;
                    Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                    SpawnAnimal(spawnData.animalDefinition, spawnCenter + randomOffset);
                    spawnData.spawnTimer = effectiveCooldown;
                }
            }
        }
    }

    private void SpawnInitialAnimals()
    {
        foreach (var spawnData in animalsToSpawn)
        {
            if (spawnData.animalDefinition != null && spawnData.spawnRateMultiplier > 0f)
            {
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                SpawnAnimal(spawnData.animalDefinition, spawnCenter + randomOffset);
            }
        }
    }

    public GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogWarning("[FaunaManager] Invalid animal definition or missing prefab!");
            return null;
        }

        GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity);

        if (ecosystemParent != null)
        {
            Transform speciesParent = ecosystemParent;
            if (!string.IsNullOrEmpty(definition.animalName))
            {
                speciesParent = ecosystemParent.Find(definition.animalName);
                if (speciesParent == null)
                {
                    GameObject subParent = new GameObject(definition.animalName);
                    subParent.transform.SetParent(ecosystemParent);
                    speciesParent = subParent.transform;
                }
            }
            animalObj.transform.SetParent(speciesParent);
        }

        AnimalController controller = animalObj.GetComponent<AnimalController>();
        if (!controller)
        {
            Debug.LogWarning("[FaunaManager] Prefab missing AnimalController. Adding one dynamically.");
            controller = animalObj.AddComponent<AnimalController>();
        }
        controller.Initialize(definition);

        // Set the global movement bounds from FaunaManager
        controller.SetMovementBounds(animalMinBounds, animalMaxBounds);
        return animalObj;
    }
}

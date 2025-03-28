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

    private void Start()
    {
        // Initialize each spawn entry's timer to its effective cooldown.
        foreach (var spawnData in animalsToSpawn)
        {
            if (spawnData.spawnRateMultiplier > 0f)
                spawnData.spawnTimer = globalSpawnCooldown / spawnData.spawnRateMultiplier;
            else
                spawnData.spawnTimer = Mathf.Infinity; // won't spawn if 0
        }
    }

    private void Update()
    {
        if (continuousSpawn)
        {
            foreach (var spawnData in animalsToSpawn)
            {
                if (spawnData.spawnRateMultiplier <= 0f)
                    continue;

                // Check current count for this species.
                int currentCount = 0;
                if (ecosystemParent != null && spawnData.animalDefinition != null && !string.IsNullOrEmpty(spawnData.animalDefinition.animalName))
                {
                    Transform speciesParent = ecosystemParent.Find(spawnData.animalDefinition.animalName);
                    if (speciesParent != null)
                        currentCount = speciesParent.childCount;
                }
                else
                {
                    AnimalController[] allAnimals = FindObjectsOfType<AnimalController>();
                    currentCount = 0;
                    foreach (var a in allAnimals)
                    {
                        if (a != null && a.SpeciesNameEquals(spawnData.animalDefinition.animalName))
                            currentCount++;
                    }
                }
                // If maximum is set (>0) and current count is reached, skip spawn.
                if (spawnData.maximumSpawned > 0 && currentCount >= spawnData.maximumSpawned)
                    continue;

                // Decrement spawn timer and spawn if ready.
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

    public GameObject SpawnAnimal(AnimalDefinition definition, Vector2 position)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogWarning("[FaunaManager] Invalid animal definition or missing prefab!");
            return null;
        }

        GameObject animalObj = Instantiate(definition.prefab, position, Quaternion.identity);

        // Parent the animal under ecosystemParent with species grouping.
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

        // Get the existing AnimalController on the prefab.
        AnimalController controller = animalObj.GetComponent<AnimalController>();
        if (!controller)
        {
            Debug.LogWarning("[FaunaManager] Prefab missing AnimalController. Adding one dynamically.");
            controller = animalObj.AddComponent<AnimalController>();
        }
        controller.Initialize(definition);
        controller.SetMovementBounds(animalMinBounds, animalMaxBounds);
        return animalObj;
    }
}

using UnityEngine;

[System.Serializable]
public class AnimalSpawnData {
    public AnimalDefinition animalDefinition;
    [Range(0f, 1f)]
    [Tooltip("Spawn rate multiplier (0 = no spawn; lower values = less frequent spawns).")]
    public float spawnRateMultiplier = 1f;
    [Tooltip("Maximum number of this animal type allowed to be spawned (0 = no limit).")]
    public int maximumSpawned = 0;
    
    [HideInInspector]
    public float spawnTimer = 0f;
}